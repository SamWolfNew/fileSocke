using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using Microsoft.VisualBasic;
using SocketUtil;
using Base;
using System.Diagnostics;
using ThreadState = System.Threading.ThreadState;
using System.Configuration;

namespace CopyFileServer
{
    public partial class frmMain : Form
    {
        delegate void AppendTextCallback(string text);

        private TcpService server;

        private string serverip;
        private string servername;
        private int port;
        private string savelog = "0";
        private int MAX_CLIENT;
        private int HttpTimeOut;
        private int _LogDay;
        private int _FileDay;
        private bool isDeleteFile;
        private bool isFtpUp;
        private int _FrequencyDelete;
        private int _FrequencyCheck;
        private int _FrequencyUpFtp;
        private string DebugPassword;
        private string inputPassword;
        private string FTPIp;
        private string FTPUser;
        private string FTPPsd;
        private int count_;
        /// <summary> 
        /// 保存的所有的客户端列表
        /// </summary> 
        private Dictionary<string, string> ClientFlag = new Dictionary<string, string>();
        /// <summary> 
        /// 清空数据库保存的所有的客户端列表（长连接）
        /// </summary> 
        private Dictionary<string, DateTime> ConnList = new Dictionary<string, DateTime>();
        /// <summary> 
        /// 清空数据库保存的所有的客户端列表（长连接名称）
        /// </summary> 
        private Dictionary<string, string> ConnName = new Dictionary<string, string>();
        /// <summary> 
        /// 标识连接传输类型
        /// </summary> 
        private Dictionary<string, int> ConnType = new Dictionary<string, int>();
        /// <summary> 
        /// 保存文件属性
        /// </summary> 
        private Dictionary<string, string> FileAttr = new Dictionary<string, string>();
        /// <summary> 
        /// 保存文件内容
        /// </summary> 
        private Dictionary<string, Dictionary<string, Byte[]>> FileContent = new Dictionary<string, Dictionary<string, Byte[]>>();
        private Thread tSend;
        private Thread tcheck;
        private Thread thDeleteFile;
        private Thread thFtpUp;
        public frmMain()
        {
            InitializeComponent();
            serverip = System.Configuration.ConfigurationManager.AppSettings["ServerIP"];
            port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Port"]);
            servername = System.Configuration.ConfigurationManager.AppSettings["ServerName"];
            _LogDay = int.Parse(ConfigurationManager.AppSettings["LogDay"]);
            _FileDay = int.Parse(ConfigurationManager.AppSettings["FileDay"]);
            _FrequencyDelete = int.Parse(ConfigurationManager.AppSettings["FrequencyDelete"]);
            _FrequencyCheck = int.Parse(ConfigurationManager.AppSettings["FrequencyCheck"]);
            _FrequencyUpFtp = int.Parse(ConfigurationManager.AppSettings["FrequencyUpFtp"]);
            MAX_CLIENT = int.Parse(System.Configuration.ConfigurationManager.AppSettings["ClientCount"]);
            DebugPassword = ConfigurationManager.AppSettings["DebugPassword"];
            txtPort.Text = port.ToString();
            txtServerIP.Text = serverip.ToString();
            savelog = System.Configuration.ConfigurationManager.AppSettings["SaveLog"];
            HttpTimeOut = int.Parse(System.Configuration.ConfigurationManager.AppSettings["HttpTimeOut"]);
            FTPIp = ConfigurationManager.AppSettings["FTPIP"];
            FTPUser = ConfigurationManager.AppSettings["FTPUser"];
            FTPPsd = ConfigurationManager.AppSettings["FTPPsd"];
            count_ = 0;
        }
        /// <summary> 
        /// 清空数据库保存的所有的客户端列表
        /// </summary> 
        private void clearA321()
        {
            string SERVER_IP = serverip + ":" + port.ToString();
            string sql = "delete from A321 where SERVER_IP='" + SERVER_IP + "'";
            Oracle db = new Oracle();
            db.BeginTransaction();
            db.ExecuteNonQuery(sql, CommandType.Text);
            db.Commit();
            db.GetDBConnection().Close();
            db.GetDBConnection().Dispose();
            this.ClientFlag.Clear();//删除队列中的数据
            this.ConnType.Clear();//删除队列中的数据
            this.FileAttr.Clear();//删除队列中的数据
            this.ConnList.Clear();
            this.ConnName.Clear();//删除队列中的数据
            this.clientList.Items.Clear();
            LogHelper.WriteLog("清空数据库客户端列表，OK");
        }
        /// <summary> 
        /// 控制下拉框
        /// </summary> 
        private delegate void testDelegate(string str, int type_);
        private void clientListContr(string str, int type_)
        {
            if (this.clientList.InvokeRequired)
            {
                testDelegate myDelegate = new testDelegate(clientListContr);
                this.clientList.Invoke(myDelegate, str, type_);
            }
            else
            {
                if (type_ == 0)
                {
                    string itemValue = "";
                    for (int _i = 0; _i < this.clientList.Items.Count; _i++)
                    {
                        itemValue = clientList.Items[_i].ToString();
                        if (itemValue.Contains(str))
                        {
                            this.clientList.Items.Remove(itemValue);
                            break;
                        }
                    }
                    this.clientList.Items.Remove(str);
                }
                else
                {
                    this.clientList.Items.Add(str);
                }
            }
        }
        /// <summary> 
        /// 向文本框写入数据
        /// </summary> 
        private void AppendText(string text)
        {
            if (this.txtInfo.InvokeRequired)
            {
                //用于Socket线程向主线程记日志
                AppendTextCallback d = new AppendTextCallback(AppendText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                //主线程直接记日志
                this.txtInfo.Text += text + "\r\n";
                if (count_ > 300)
                {
                    count_ = 0;
                    this.txtInfo.Text = "";
                }
            }
            count_++;

        }
        private void start_() {
            try
            {
                //在指定端口上建立监听线程
                AppendText(DateTime.Now.ToLongTimeString() + "----------开始监听------------");
                LogHelper.WriteLog("----------开始监听------------");
                server = new TcpService(port, MAX_CLIENT, serverip);
                server.Connected += new NetEventHandler(server_Connected);
                server.DisConnect += new NetEventHandler(server_DisConnect);
                server.Start(MAX_CLIENT);
                this.btnStart.Enabled = false;
                this.btnStop.Enabled = true;
                this.txtServerIP.Enabled = false;
                this.txtPort.Enabled = false;
                clearA321();

                //监视长连接是否存在
                tcheck = new Thread(Check_Exists);
                tcheck.IsBackground = true;
                tcheck.Start();
                //int time = 1000 * 60 * _FrequencyCheck;
                //System.Timers.Timer timer = new System.Timers.Timer();
                //timer.Elapsed += new System.Timers.ElapsedEventHandler(Check_Exists);
                //timer.Interval = time;////设置时间（定时器多少秒执行一次--注意是毫秒)
                //timer.AutoReset = true;//执行多次--false执行一次
                //timer.Enabled = true;//执行事件为true,定时器启动

                startDeleteFileTh();
                startFtpUpTh();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                AppendText(DateTime.Now.ToLongTimeString() + "----------监听开启失败------------");
                this.WindowState = FormWindowState.Normal;
                this.Activate();
                this.Show();
            }
        }
        /// <summary> 
        /// 开始启动监听
        /// </summary> 
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                start_();
            }
        }
        /// <summary> 
        /// 起线程监视长连接是否存在并在连接中
        /// </summary> 
        private void Check_Exists(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Dictionary<string, DateTime>.KeyCollection keys = ConnList.Keys;
                EndPoint[] sKeys = new EndPoint[server.Session.Keys.Count];
                server.Session.Keys.CopyTo(sKeys, 0);

                foreach (string clientip in keys)
                {
                    Boolean lb_exists = false;
                    for (int j = 0; j < sKeys.Length; j++)
                    {
                        if (!server.Session.ContainsKey(sKeys[j])) continue;
                        IDataTransmit dt = server.Session[sKeys[j]];
                        //检测长连接是否还存在
                        if (dt.RemoteEndPoint.ToString() == clientip && dt.Connected)
                        {
                            lb_exists = true;
                            break;
                        }
                    }
                    if (lb_exists == false)
                    {
                        ClientFlag.Remove(clientip);//删除队列中的数据
                        ConnType.Remove(clientip);//删除队列中的数据
                        FileAttr.Remove(clientip);//删除队列中的数据
                        ConnList.Remove(clientip);
                        ConnName.Remove(clientip.Split(':')[0]);//删除队列中的数据
                        clientListContr(clientip, 0);
                        AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " 已断开连接！");
                        LogHelper.WriteLog("longlink： " + clientip + " 已断开连接！");
                        //通知后台
                        string SERVER_IP = serverip + ":" + port.ToString();
                        string sql = "Bl_Socket_Tool_Api.client_disconnect('" + clientip + "','" + SERVER_IP + "')";
                        //LogHelper.WriteLog("longlink：断开sql_:" + sql);
                        Oracle db = new Oracle();
                        db.BeginTransaction();
                        db.ExecuteNonQuery(sql, CommandType.Text);
                        db.Commit();
                        db.GetDBConnection().Close();
                        db.GetDBConnection().Dispose();
                    }
                    else
                    {
                        //通知后台
                        string SERVER_IP = serverip + ":" + port.ToString();
                        string clientName = "";
                        ConnName.TryGetValue(clientip.Split(':')[0], out clientName);
                        string sql = "Bl_Socket_Tool_Api.Client_Connect('" + clientip + "','" + SERVER_IP + "','" + clientName + "')";
                        //LogHelper.WriteLog("检测连接存在:" + clientip + sql);
                        AppendText(DateTime.Now.ToLongTimeString() + "检测连接存在： " + clientip + " 连接中！");

                        Oracle db = new Oracle();
                        db.BeginTransaction();
                        db.ExecuteNonQuery(sql, CommandType.Text);
                        db.Commit();
                        db.GetDBConnection().Close();
                        db.GetDBConnection().Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
            }
        }
        /// <summary> 
        /// 起线程监视长连接是否存在并在连接中
        /// </summary> 
        private void Check_Exists()
        {
            while (true)
            {
                try
                {
                    Dictionary<string, DateTime>.KeyCollection keys = ConnList.Keys;
                    EndPoint[] sKeys = new EndPoint[server.Session.Keys.Count];
                    server.Session.Keys.CopyTo(sKeys, 0);

                    foreach (string clientip in keys)
                    {
                        Boolean lb_exists = false;
                        for (int j = 0; j < sKeys.Length; j++)
                        {
                            if (!server.Session.ContainsKey(sKeys[j])) continue;
                            IDataTransmit dt = server.Session[sKeys[j]];
                            //检测长连接是否还存在
                            if (dt.RemoteEndPoint.ToString() == clientip && dt.Connected)
                            {
                                lb_exists = true;
                                break;
                            }
                        }
                        if (lb_exists == false)
                        {
                            ClientFlag.Remove(clientip);//删除队列中的数据
                            ConnType.Remove(clientip);//删除队列中的数据
                            FileAttr.Remove(clientip);//删除队列中的数据
                            ConnList.Remove(clientip);
                            ConnName.Remove(clientip.Split(':')[0]);//删除队列中的数据
                            clientListContr(clientip, 0);
                            AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " 已断开连接！");
                            LogHelper.WriteLog("longlink： " + clientip + " 已断开连接！");
                            //通知后台
                            string SERVER_IP = serverip + ":" + port.ToString();
                            string sql = "Bl_Socket_Tool_Api.client_disconnect('" + clientip + "','" + SERVER_IP + "')";
                            //LogHelper.WriteLog("longlink：断开sql_:" + sql);
                            Oracle db = new Oracle();
                            db.BeginTransaction();
                            db.ExecuteNonQuery(sql, CommandType.Text);
                            db.Commit();
                            db.GetDBConnection().Close();
                            db.GetDBConnection().Dispose();
                        }
                        else
                        {
                            //通知后台
                            string SERVER_IP = serverip + ":" + port.ToString();
                            string clientName = "";
                            ConnName.TryGetValue(clientip.Split(':')[0], out clientName);
                            string sql = "Bl_Socket_Tool_Api.Client_Connect('" + clientip + "','" + SERVER_IP + "','" + clientName + "')";
                            //LogHelper.WriteLog("检测连接存在:" + clientip + sql);
                            AppendText(DateTime.Now.ToLongTimeString() + "检测连接存在： " + clientip + " 连接中！");

                            Oracle db = new Oracle();
                            db.BeginTransaction();
                            db.ExecuteNonQuery(sql, CommandType.Text);
                            db.Commit();
                            db.GetDBConnection().Close();
                            db.GetDBConnection().Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {

                    LogHelper.WriteLog(ex.Message.ToString(), ex);
                }
                Thread.Sleep(1000 * 60 * _FrequencyCheck);  //每秒执行一次
            }
        }
        /// <summary> 
        /// 当连接断开时的操作
        /// </summary> 
        void server_DisConnect(IDataTransmit sender, NetEventArgs e)
        {
            try
            {
                string SERVER_IP = serverip + ":" + port.ToString();
                string sql = "Bl_Socket_Tool_Api.client_disconnect('" + sender.RemoteEndPoint.ToString() + "','" + SERVER_IP + "')";
                //LogHelper.WriteLog("link：断开sql_:" + sql);
                Oracle db = new Oracle();
                db.BeginTransaction();
                db.ExecuteNonQuery(sql, CommandType.Text);
                db.Commit();
                db.GetDBConnection().Close();
                db.GetDBConnection().Dispose();
                if (sender.Connected)
                {
                    //LogHelper.WriteLog("link：" + sender.RemoteEndPoint.ToString() + " 连接断开");
                    //LogHelper.WriteLog("当前的连接数量为：" + server.ConnectCount);
                    this.AppendText(DateTime.Now.ToLongTimeString() + " " + sender.RemoteEndPoint.ToString() + " 连接断开");
                    sender.Stop();
                }
                ConnList.Remove(sender.RemoteEndPoint.ToString());//删除队列中的数据
                ClientFlag.Remove(sender.RemoteEndPoint.ToString());//删除队列中的数据
                ConnType.Remove(sender.RemoteEndPoint.ToString());//删除队列中的数据
                FileAttr.Remove(sender.RemoteEndPoint.ToString());//删除队列中的数据
                ConnName.Remove(sender.RemoteEndPoint.ToString().Split(':')[0]);//删除队列中的数据
                clientListContr(sender.RemoteEndPoint.ToString(), 0);
            }
            catch (Exception ex)
            {
                this.AppendText("数据库断开失败：" + sender.RemoteEndPoint.ToString() + ex.Message);
                LogHelper.WriteLog(ex.Message.ToString() + sender.RemoteEndPoint.ToString(), ex);
            }
        }
        /// <summary> 
        /// 当连接开始时的操作
        /// </summary> 
        void server_Connected(IDataTransmit sender, NetEventArgs e)
        {
            string SERVER_IP = serverip + ":" + port.ToString();
            //清空以前的连接
            EndPoint[] sKeys = new EndPoint[server.Session.Keys.Count];
            server.Session.Keys.CopyTo(sKeys, 0);
            for (int j = 0; j < sKeys.Length; j++)
            {
                if (!server.Session.ContainsKey(sKeys[j])) continue;
                IDataTransmit dt = server.Session[sKeys[j]];
                if (dt.RemoteEndPoint.ToString() == serverip && dt.Connected)
                {
                    dt.Stop();
                }
            }
            //xuyinghuai 20190311 短连接不在保存，只保存长连接
            //string sql = "pkg_socket.client_connect('" + sender.RemoteEndPoint.ToString() + "','" + SERVER_IP + "')";
            //Oracle db = new Oracle();
            //db.BeginTransaction();
            //db.ExecuteNonQuery(sql, CommandType.Text);
            //db.Commit();

            //LogHelper.WriteLog("link： " + sender.RemoteEndPoint.ToString() + " 连接成功");
            //LogHelper.WriteLog("当前的连接数量为：" + server.ConnectCount);
            this.AppendText(DateTime.Now.ToLongTimeString() + " " + sender.RemoteEndPoint.ToString() + " 连接成功");
            sender.ReceiveData += new NetEventHandler(sender_ReceiveData);
            //接收数据
            sender.Start();
            this.AppendText(DateTime.Now.ToLongTimeString() + " " + sender.RemoteEndPoint.ToString() + " 接收数据");
        }
        /// <summary> 
        /// 连接开始接受数据
        /// </summary> 
        void sender_ReceiveData(IDataTransmit sender, NetEventArgs e)
        {
            string clientip = sender.RemoteEndPoint.ToString();
            try
            {
                byte[] data = (byte[])e.EventArg;
                int connType_;
                string strDataRec;
                if (data.Length > 0)
                {
                    ConnType.TryGetValue(clientip, out connType_);
                    //心跳包数据-----------------------
                    if ((data[0] == 48 && connType_ == 0) || connType_ == 1)
                    {
                        strDataRec = Encoding.Default.GetString(data);
                        string SERVER_IP = serverip + ":" + port.ToString();
                        if (data.Length > 3)
                        {
                            strDataRec = Encoding.Default.GetString(data, 1, data.Length - 1);
                            LogHelper.WriteLog("接收客户端名称： " + strDataRec + "");
                            this.AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " :接收客户端名称" + strDataRec + "");
                            if (ConnList.ContainsKey(clientip))
                            {
                                ConnList.Remove(clientip);
                                ConnType.Remove(clientip);
                                ConnName.Remove(clientip.Split(':')[0]);
                                this.clientListContr(clientip, 0);
                            }
                            ConnList.Add(clientip, DateTime.Now);
                            ConnType.Add(clientip, 0);
                            ConnName.Add(clientip.Split(':')[0], strDataRec);
                            this.clientListContr(strDataRec + "[" + clientip + "]", 1);
                            //xuyinghuai 20190311 只保存长连接
                            string sql = "Bl_Socket_Tool_Api.Client_Connect('" + sender.RemoteEndPoint.ToString() + "','" + SERVER_IP + "','" + strDataRec + "')";
                            Oracle db = new Oracle();
                            db.BeginTransaction();
                            db.ExecuteNonQuery(sql, CommandType.Text);
                            db.Commit();
                            db.GetDBConnection().Close();
                            db.GetDBConnection().Dispose();
                        }
                        else
                        {
                            //LogHelper.WriteLog("heartbeat： " + sender.RemoteEndPoint.ToString() + "");
                            //this.AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " :heartbeat");
                            if (ConnList.ContainsKey(clientip))
                            {
                                ConnList.Remove(clientip);
                                ConnType.Remove(clientip);
                                //this.clientListContr(clientip, 0);
                            }
                            ConnList.Add(clientip, DateTime.Now);
                            ConnType.Add(clientip, 0);
                        }
                        return;
                    }
                    if ((data[0] == 50 && connType_ == 0) || connType_ == 2)//文本消息
                    {
                        if (connType_ == 2)
                        {
                            strDataRec = Encoding.Default.GetString(data);
                        }
                        else
                        {
                            ConnType.Add(clientip, 2);
                            strDataRec = Encoding.Default.GetString(data, 1, data.Length - 1);
                        }

                        //LogHelper.WriteLog("received： " + sender.RemoteEndPoint.ToString() + "");
                        LogHelper.WriteLog(sender.RemoteEndPoint.ToString() + strDataRec);
                        do_receive(clientip, strDataRec);
                        if (data.Length != 8192)
                        {
                            string xml = "";
                            Boolean lb_get = ClientFlag.TryGetValue(clientip, out xml);
                            this.AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " :" + xml);
                            LogHelper.WriteLog("received： " + sender.RemoteEndPoint.ToString() + "");
                            LogHelper.WriteLog(DateTime.Now.ToLongTimeString() + xml);
                            end_receive(clientip);
                        }
                    }
                    if ((data[0] == 49 && connType_ == 0) || connType_ == 3)//文件消息
                    {
                        if (connType_ == 3)
                        {
                            if (data.Length > 0)
                            {
                                //开始不断的接收文件-------------------------------
                                bool falg_ = do_receive(clientip, data);
                                //this.AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " :" + data.Length + "");
                                if (falg_)
                                {
                                    string _fileArr = "";
                                    string returnMsg = "";
                                    string clientName = "";
                                    Dictionary<string, byte[]> _fileContent = new Dictionary<string, byte[]>();
                                    FileAttr.TryGetValue(clientip, out _fileArr);
                                    FileContent.TryGetValue(clientip, out _fileContent);
                                    Boolean _flag = CreateFile_(_fileArr, _fileContent, clientip, ref returnMsg);
                                    if (_flag)
                                    {
                                        this.AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " :" + returnMsg + "接收完成");
                                        LogHelper.WriteLog(clientip + " :" + returnMsg + "接收完成");
                                        saveFileInfo(returnMsg, clientip);
                                        byte[] buffer = Encoding.Default.GetBytes("OK");
                                        sender.Send(buffer);
                                    }
                                    else
                                    {
                                        this.AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " :" + returnMsg + "");
                                        byte[] buffer = Encoding.Default.GetBytes(returnMsg);
                                        sender.Send(buffer);
                                    }
                                    FileContent.Remove(clientip);
                                    FileAttr.Remove(clientip);
                                }
                            }
                            else
                            {
                                FileContent.Remove(clientip);
                                FileAttr.Remove(clientip);
                            }
                        }
                        else
                        {
                            //获取文件属性
                            ConnType.Add(clientip, 3);
                            strDataRec = Encoding.Default.GetString(data, 1, data.Length - 1);
                            this.AppendText(DateTime.Now.ToLongTimeString() + " " + clientip + " :" + strDataRec + "开始接受");
                            LogHelper.WriteLog(clientip + " :" + strDataRec);
                            FileAttr.Add(clientip, strDataRec);
                            byte[] buffer = Encoding.Default.GetBytes("OK");
                            sender.Send(buffer);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                FileContent.Remove(clientip);
                FileAttr.Remove(clientip);
                sender.Stop();
                this.AppendText(DateTime.Now.ToLongTimeString() + "处理数据出错：" + clientip + ex.Message);
                LogHelper.WriteLog(ex.Message.ToString() + clientip, ex);
            }


        }
        /// <summary> 
        /// 处理接收的数据
        /// </summary> 
        private void do_receive(string clientip, string cxml)
        {
            string xml = "";
            foreach (string cip in ClientFlag.Keys)
            {
                if (cip == clientip)
                {
                    ClientFlag.TryGetValue(clientip, out xml);
                    ClientFlag.Remove(clientip);
                    break;
                }
            }
            ClientFlag.Add(clientip, xml + cxml);
        }
        /// <summary> 
        /// 处理接收的文件数据
        /// </summary> 
        private bool do_receive(string clientip, byte[] cxml)
        {
            try
            {
                Dictionary<string, byte[]> _fileContent = new Dictionary<string, byte[]>();
                string _fileAttr = "";
                FileAttr.TryGetValue(clientip, out _fileAttr);
                if (!string.IsNullOrEmpty(_fileAttr))
                {
                    //标识接收的是文件的结尾
                    if (cxml.Length == 11)
                    {
                        if (Encoding.Default.GetString(cxml) == "SendFileEnd")
                        {
                            return true;
                        }
                    }
                    foreach (string cip in FileContent.Keys)
                    {
                        if (cip == clientip)
                        {
                            FileContent.TryGetValue(clientip, out _fileContent);
                            FileContent.Remove(clientip);
                            break;
                        }
                    }
                    _fileContent.Add(clientip + (_fileContent.Count + 1).ToString(), cxml);
                    //LogHelper.WriteLog("FileContent大小：" + FileContent.Count.ToString());
                    FileContent.Add(clientip, _fileContent);
                    //if (_fileContent.Count == pageCount)
                    //{
                    //    return true;
                    //}
                    //if (cxml.Length != 8192)
                    //{
                    //    return true;
                    //}
                    //}
                }
            }
            catch (Exception ex)
            {
                this.AppendText(DateTime.Now.ToLongTimeString() + "处理数据出错：" + clientip + ex.Message);
                LogHelper.WriteLog(ex.Message.ToString() + clientip, ex);
                string str_ex = ex.Message.ToString();
                if (str_ex.IndexOf("索引超出了数组界限") > 0)
                {
                    AutoRestart();
                }
            }
            return false;
        }

        /// <summary> 
        /// 处理完全接收后的数据
        /// </summary> 
        private void end_receive(string clientip)
        {

            string a320_id = "";
            string xml = "";
            try
            {
                Boolean lb_get = ClientFlag.TryGetValue(clientip, out xml);
                if (lb_get == false)
                {
                    LogHelper.WriteLog("error:" + clientip + "处理失败clientip！");
                    throw new Exception("处理失败clientip！");
                }
                this.AppendText("received：" + xml);
                //获取id---------------------
                Oracle db = new Oracle();
                string sql_ = "select s_a320.nextval from dual ";
                string ip_ = clientip;
                DataTable dt_key = new DataTable();
                db.ExcuteDataTable(dt_key, sql_, CommandType.Text);
                a320_id = dt_key.Rows[0][0].ToString();
                dt_key.Dispose();
                //保存接收的数据-----------------------------
                OracleParameter[] parmeters =
                {
                    new OracleParameter("A320_Id_", OracleType.Number),
                    new OracleParameter("Clientip_", OracleType.NVarChar,50),
                    new OracleParameter("Receivexml_", OracleType.Clob),

               };
                parmeters[0].Direction = ParameterDirection.Input;
                parmeters[1].Direction = ParameterDirection.Input;
                parmeters[2].Direction = ParameterDirection.Input;
                parmeters[0].Value = a320_id;
                parmeters[1].Value = clientip;
                parmeters[2].Value = xml;
                //插入数据-------------------------
                db.BeginTransaction();
                db.ExecuteNonQuery("Bl_Socket_Tool_Api.Insert_A320", parmeters);
                db.Commit();
                db.GetDBConnection().Close();
                db.GetDBConnection().Dispose();
                Do_XML_(a320_id, "0");
            }
            catch (Exception ex)
            {
                this.AppendText(DateTime.Now.ToLongTimeString() + "处理数据出错：" + clientip + ex.Message);
                LogHelper.WriteLog(ex.Message.ToString() + clientip, ex);
            }
        }
        /// <summary>
        /// 保存接收的文件信息
        /// </summary>
        /// <param name="fileAttr"></param>
        /// <param name="clientip"></param>
        private void saveFileInfo(string fileAttr, string clientip)
        {
            //保存接收的数据-----------------------------
            try
            {
                Oracle db = new Oracle();
                OracleParameter[] parmeters =
                {
                    new OracleParameter("Filearr_", OracleType.VarChar),
                    new OracleParameter("Clientip_", OracleType.NVarChar,50)

               };
                parmeters[0].Direction = ParameterDirection.Input;
                parmeters[1].Direction = ParameterDirection.Input;
                parmeters[0].Value = fileAttr;
                parmeters[1].Value = clientip;
                //插入数据-------------------------
                db.BeginTransaction();
                db.ExecuteNonQuery("Bl_File_Upload_Do_Api.Into_", parmeters);
                db.Commit();
                db.GetDBConnection().Close();
                db.GetDBConnection().Dispose();
            }
            catch (Exception ex)
            {
                this.AppendText(DateTime.Now.ToLongTimeString() + "处理数据出错：" + clientip + ex.Message);
                LogHelper.WriteLog(ex.Message.ToString() + clientip, ex);
                string str_ex = ex.Message.ToString();
                //数据库连接不够时重启
                //if (str_ex.IndexOf("ORA-00060") > 0)
                //{
                //    AutoRestart();
                //}
            }
        }
        /// <summary> 
        /// 处理接收的报文信息
        /// </summary> 
        private void Do_XML_(string a320_id, string base_line_no)
        {
            try
            {
                //获取包的处理结果
                DataTable dt_a32001 = new DataTable();
                string sql_ = "Select t.* from A32001 t where a320_id=" + a320_id + " and base_line_no= " + base_line_no + " and state = '0'";
                //LogHelper.WriteLog("sql_：" + sql_);
                Oracle db = new Oracle();
                db.ExcuteDataTable(dt_a32001, sql_, CommandType.Text);
                for (int i = 0; i < dt_a32001.Rows.Count; i++)
                {
                    string next_ip = dt_a32001.Rows[i]["NEXT_IP"].ToString();
                    string state = dt_a32001.Rows[i]["state"].ToString();
                    string line_no_ = dt_a32001.Rows[i]["line_no"].ToString();
                    string next_type = dt_a32001.Rows[i]["Next_Type"].ToString();
                    string xml_ = dt_a32001.Rows[i]["Next_xml"].ToString();
                    string next_port = dt_a32001.Rows[i]["next_port"].ToString();
                    string Next_Characterset = dt_a32001.Rows[i]["Next_Characterset"].ToString();
                    send_next(a320_id, next_type, xml_, next_ip, next_port, line_no_, Next_Characterset);
                }
            }
            catch (Exception ex)
            {
                this.AppendText(DateTime.Now.ToLongTimeString() + "处理数据出错：" + ex.Message);
                LogHelper.WriteLog(ex.Message.ToString(), ex);
            }
        }
        /// <summary> 
        /// 清空显示文本框
        /// </summary> 
        private void btnClear_Click(object sender, EventArgs e)
        {
            this.txtInfo.Text = "";
        }
        /// <summary> 
        /// 转发或者回复客户端结果
        /// </summary> 
        private void send_next(string a320_id_, string next_type_, string xml_, string next_ip_, string next_port_, string a32001_key_, string CharacterSet_)
        {
            string msg_ = "";
            if (next_type_ == "SOCKET")
            {
                Boolean lb_exists = false;
                try
                {
                    //获取对应的ip地址是否已经连接
                    EndPoint[] sKeys = new EndPoint[server.Session.Keys.Count];
                    server.Session.Keys.CopyTo(sKeys, 0);
                    for (int j = 0; j < sKeys.Length; j++)
                    {
                        if (!server.Session.ContainsKey(sKeys[j])) continue;
                        IDataTransmit dt = server.Session[sKeys[j]];
                        if (dt.RemoteEndPoint.ToString() == next_ip_ && dt.Connected)
                        {
                            lb_exists = true;
                            string next_xml = xml_;
                            byte[] buffer = Encoding.Default.GetBytes(next_xml);
                            dt.Send(buffer);
                            this.AppendText(DateTime.Now.ToLongTimeString() + "send to " + next_ip_ + "：" + xml_);
                            //this.AppendText("=========================================================" + Environment.NewLine);
                            LogHelper.WriteLog("send:" + next_ip_ + "：" + xml_);
                            break;
                        }
                    }
                    msg_ = "";
                }
                catch (Exception ex)
                {
                    msg_ = ex.Message.ToString();
                    this.AppendText(DateTime.Now.ToLongTimeString() + "处理数据出错：" + ex.Message);
                    LogHelper.WriteLog(ex.Message.ToString(), ex);
                    lb_exists = false;

                }
                string sql_ = "";
                //表示数据未发送给下级 
                if (lb_exists == false)
                {
                    sql_ = "update a32001  set state = '-1',msg ='" + msg_.Replace("'", "''") + "',modi_date=Systimestamp  where  line_no =  " + a32001_key_;
                }
                else
                {
                    sql_ = "update a32001  set state = '1' ,modi_date=Systimestamp  where  line_no =  " + a32001_key_;
                }
                //LogHelper.WriteLog("sql_:" + next_ip_ + sql_);
                Oracle db = new Oracle();
                //判断是否要去下级                        
                db.ExecuteNonQuery(sql_, CommandType.Text);
                db.Commit();
                db.GetDBConnection().Close();
                db.GetDBConnection().Dispose();
            }
            //一般该情况是用于转发的情况来处理的
            if (next_type_ == "WEB_SERVICE")
            {
                HttpRequest http = new HttpRequest();
                http.TimeOut = HttpTimeOut;
                string CharacterSet = CharacterSet_;
                if (CharacterSet != null && CharacterSet.Trim() != "")
                {
                    http.CharacterSet = CharacterSet_;
                }
                bool if_success = true;
                string receive_xml = "";
                msg_ = "";
                try
                {
                    this.AppendText(DateTime.Now.ToLongTimeString() + " Send:" + next_ip_ + xml_);
                    //this.AppendText("=========================================================" + Environment.NewLine);
                    LogHelper.WriteLog("httpsend:" + next_ip_ + xml_);
                    if (xml_ != null && xml_ != "" && xml_.Length > 4)
                    {
                        if_success = http.OpenRequest(next_ip_, next_ip_, xml_);
                    }
                    else
                    {
                        if_success = http.OpenRequest(next_ip_, next_ip_);
                    }
                    if (if_success == true)
                    {
                        receive_xml = http.HtmlDocument;
                        LogHelper.WriteLog("httpReceive:" + next_ip_ + receive_xml);
                        this.AppendText(DateTime.Now.ToLongTimeString() + "httpReceive:" + next_ip_ + receive_xml);

                    }
                    else
                    {
                        msg_ = "发送请求[" + next_ip_ + "]失败";
                        LogHelper.WriteLog(" httpReceive:" + msg_);
                        this.AppendText(DateTime.Now.ToLongTimeString() + " httpReceive:" + msg_);

                    }

                }
                catch (Exception ex)
                {
                    msg_ = ex.Message.ToString();
                    this.AppendText(DateTime.Now.ToLongTimeString() + "处理数据出错：" + ex.Message);
                    LogHelper.WriteLog(ex.Message.ToString(), ex);
                    if_success = false;
                }

                Oracle db = new Oracle();
                string state_ = "0";
                if (if_success == false)
                {
                    state_ = "-1";
                }
                else
                {
                    state_ = "1";

                }
                OracleParameter[] parmeters =
                {
                    new OracleParameter("A32001_line_", OracleType.NVarChar,50),
                    new OracleParameter("Receivexml_", OracleType.Clob),
                    new OracleParameter("State_", OracleType.NVarChar,50),
                    new OracleParameter("msg_", OracleType.NVarChar,4000),

                };
                parmeters[0].Direction = ParameterDirection.Input;
                parmeters[1].Direction = ParameterDirection.Input;
                parmeters[2].Direction = ParameterDirection.Input;
                parmeters[3].Direction = ParameterDirection.Input;
                parmeters[0].Value = a32001_key_;
                parmeters[1].Value = receive_xml;
                parmeters[2].Value = state_;
                parmeters[3].Value = msg_;
                db.BeginTransaction();
                db.ExecuteNonQuery("Pkg_Socket.Http_Resp_", parmeters);
                db.Commit();
                db.GetDBConnection().Close();
                db.GetDBConnection().Dispose();
                //转发结束后的再次处理
                Do_XML_(a320_id_, a32001_key_);
            }
        }

        private void stop_() {
            server.Close();
            server = null;
            this.btnStop.Enabled = false;
            this.btnStart.Enabled = true;
            if (tSend != null && tSend.ThreadState != ThreadState.Aborted)
            {
                tSend.Abort();
            }
            if (tcheck != null && tcheck.ThreadState != ThreadState.Aborted)
            {
                tcheck.Abort();
            }
            stopFtpUpTh();
            stopDeleteFileTh();
            clearA321();

        }
        /// <summary> 
        /// 停止监听
        /// </summary>
        private void btnStop_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                stop_();
            }
        }
        /// <summary>
        /// 开始删除文件线程
        /// </summary>
        private void startDeleteFileTh()
        {
            isDeleteFile = true;
            thDeleteFile = new Thread(startDeleteFile);
            thDeleteFile.Name = "socketDeleteFile";
            thDeleteFile.IsBackground = true;
            thDeleteFile.Start();
            //int time = 1000 * 60 * 60 * _FrequencyDelete;
            //System.Timers.Timer timer = new System.Timers.Timer();
            //timer.Elapsed += new System.Timers.ElapsedEventHandler(startDeleteFile);
            //timer.Interval = time;////设置时间（定时器多少秒执行一次--注意是毫秒)
            //timer.AutoReset = true;//执行多次--false执行一次
            //timer.Enabled = true;//执行事件为true,定时器启动
        }
        /// <summary>
        /// 循环FTP上传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startFtpUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                sendFtpFile();

            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("上传出错！");
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                //isFtpUp = false;
                //break;
            }
        }

        /// <summary>
        /// 循环上传文件FTP
        /// </summary>
        private void startFtpUp()
        {
            while (isFtpUp)
            {
                try
                {
                    sendFtpFile();
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog("上传出错！");
                    LogHelper.WriteLog(ex.Message.ToString(), ex);
                    //isFtpUp = false;
                    //break;
                }
                Thread.Sleep(1000 * _FrequencyUpFtp);
            }
        }
        /// <summary>
        /// 开始FTP上传
        /// </summary>
        private void startFtpUpTh()
        {
            isFtpUp = true;
            thFtpUp = new Thread(startFtpUp);
            thFtpUp.Name = "socketFtpUpFile";
            thFtpUp.IsBackground = true;
            thFtpUp.Start();
            //int time = 1000 * 60 * _FrequencyUpFtp;
            //System.Timers.Timer timer = new System.Timers.Timer();
            //timer.Elapsed += new System.Timers.ElapsedEventHandler(startFtpUp);
            //timer.Interval = time;////设置时间（定时器多少秒执行一次--注意是毫秒)
            //timer.AutoReset = true;//执行多次--false执行一次
            //timer.Enabled = true;//执行事件为true,定时器启动
        }
        /// <summary>
        /// 停止ftp上传
        /// </summary>
        private void stopFtpUpTh()
        {
            isFtpUp = false;
            if (thFtpUp != null && thFtpUp.ThreadState != ThreadState.Aborted)
            {
                thFtpUp.Abort();
            }
        }
        /// <summary>
        /// 停止删除文件线程
        /// </summary>
        private void stopDeleteFileTh()
        {
            isDeleteFile = false;
            if (thDeleteFile != null && thDeleteFile.ThreadState != ThreadState.Aborted)
            {
                thDeleteFile.Abort();
            }
        }

        /// <summary>
        /// 开始删除日志
        /// </summary>
        private void startDeleteFile(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                deleteLog();
                deleteFileBuck();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                //isDeleteFile = false;
                //break;
            }
        }

        /// <summary>
        /// 开始删除日志
        /// </summary>
        private void startDeleteFile()
        {
            while (isDeleteFile)
            {
                try
                {
                    deleteLog();
                    deleteFileBuck();
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog(ex.Message.ToString(), ex);
                    //isDeleteFile = false;
                    //break;
                }
                Thread.Sleep(1000 * 60 * 60 * _FrequencyDelete);
            }
        }
        /// <summary>
        /// 删除日志文件
        /// </summary>
        private void deleteLog()
        {

            string logFolder = getAppPath() + "\\Logs";//System.Environment.CurrentDirectory + "\\Logs";
            string fileName = "";
            int logDay = _LogDay * -1;
            //循环删除错误日志信息
            DirectoryInfo folder = new DirectoryInfo(logFolder);
            foreach (FileInfo file in folder.GetFiles())
            {
                if (file.CreationTime < DateTime.Now.AddDays(logDay))
                {
                    fileName = file.Name;
                    file.Delete();//删除文件
                    LogHelper.WriteLog("文件【" + fileName + "】删除成功SUCCESS");
                }
            }
        }
        public bool sendFtpFile()
        {
            //获取包的处理结果
            string upfiles = getAppPath() + "\\upfiles";
            string clientName = "";
            string fileName = "";
            string filepath = "";
            string Relative_Path = "";
            int id = 0;
            if (false == System.IO.Directory.Exists(upfiles))
            {
                //创建pic文件夹
                System.IO.Directory.CreateDirectory(upfiles);
            }
            FtpWeb fw = null;
            DataTable dt_upload = new DataTable();
            string sql_ = "Select t.* from BL_FILE_UPLOAD_TAB t where UP_STATE<>'2' and filelength>0 and bl_file_upload_api.Get_Ip_(t.id)in (SELECT a.ip FROM a321 a where a.server_ip = '" + this.serverip + ":" + this.port + "')";
            //LogHelper.WriteLog("开始上传sql_：" + sql_);
            Oracle db = new Oracle();
            db.ExcuteDataTable(dt_upload, sql_, CommandType.Text);
            for (int i = 0; i < dt_upload.Rows.Count; i++)
            {
                clientName = dt_upload.Rows[i]["clientname"].ToString();
                fileName = dt_upload.Rows[i]["filename"].ToString();
                Relative_Path = dt_upload.Rows[i]["RELATIVE_PATH"].ToString();
                filepath = dt_upload.Rows[i]["filepath"].ToString();
                id = int.Parse(dt_upload.Rows[i]["id"].ToString());
                LogHelper.WriteLog("FTP上传ID:" + id + "|" + fileName);
                fw = new FtpWeb(FTPIp, "", FTPUser, FTPPsd);
                updateUpFtpState("1", "", id);
                if (fw.Upload(filepath + "\\" + fileName, clientName, upfiles, ref Relative_Path))
                {
                    updateUpFtpState("2", Relative_Path, id);
                }
                else
                {
                    updateUpFtpState("-1", Relative_Path, id);
                }
                if (File.Exists(upfiles + "\\" + fileName))
                {
                    FileInfo fileInf = new FileInfo(upfiles + "\\" + fileName);
                    fileInf.Delete();
                }
            }
            dt_upload.Dispose();
            db.GetDBConnection().Close();
            db.GetDBConnection().Dispose();
            //LogHelper.WriteLog("上传结束！");
            return true;

        }
        /// <summary>
        /// 更新上传状态
        /// </summary>
        /// <param name="state"></param>
        /// <param name="id_"></param>
        public void updateUpFtpState(string state, string Relative_Path, int id_)
        {
            try
            {
                string attr_ = state + "|" + Relative_Path;
                //保存接收的数据-----------------------------
                Oracle db = new Oracle();
                OracleParameter[] parmeters =
                {
                    new OracleParameter("Attr_", OracleType.VarChar),
                    new OracleParameter("Id_", OracleType.Number)

               };
                parmeters[0].Direction = ParameterDirection.Input;
                parmeters[1].Direction = ParameterDirection.Input;
                parmeters[0].Value = attr_;
                parmeters[1].Value = id_;
                //插入数据-------------------------
                db.BeginTransaction();
                db.ExecuteNonQuery("Bl_File_Upload_Do_Api.UP_STATE_", parmeters);
                db.Commit();
                db.GetDBConnection().Close();
                db.GetDBConnection().Dispose();
            }
            catch (Exception ex)
            {
                this.AppendText(DateTime.Now.ToLongTimeString() + "更新上传状态错误：" + ex.Message);
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                string str_ex = ex.Message.ToString();
                //数据库连接不够时重启
                //if (str_ex.IndexOf("ORA-00060") > 0)
                //{
                //    AutoRestart();
                //}
            }
        }
        public string getAppPath()
        {
            string startup = Application.ExecutablePath;       //取得程序路径   
            int pp = startup.LastIndexOf("\\");
            startup = startup.Substring(0, pp);
            return startup;
        }
        /// <summary>
        /// 定期删除备份的文件
        /// </summary>
        private void deleteFileBuck()
        {
            string logFolder = getAppPath() + "\\Folders";
            string fileName = "";
            DirectoryInfo folder;
            int FileDay = _FileDay * -1;
            if (false == System.IO.Directory.Exists(logFolder))
            {
                //创建pic文件夹
                System.IO.Directory.CreateDirectory(logFolder);
            }
            List<string> folderList = new List<string>();
            getFolderList(logFolder, ref folderList, 1);
            for (int _i = 0; _i < folderList.Count; _i++)
            {
                //循环删除错误日志信息
                folder = new DirectoryInfo(folderList[_i]);
                foreach (FileInfo file in folder.GetFiles())
                {
                    if (file.CreationTime < DateTime.Now.AddDays(FileDay))
                    {
                        fileName = file.Name;
                        file.Delete();//删除文件
                        LogHelper.WriteLog("文件【" + fileName + "】删除成功SUCCESS");
                    }
                }
            }
        }
        /// <summary>
        /// 获取文件夹下所有的文件夹列表
        /// </summary>
        /// <param name="_original_path">遍历的文件夹</param>
        /// <param name="folderList">文件夹列表</param>
        /// <param name="haveFile">是否有文件</param>
        public  void getFolderList(string _original_path, ref List<string> folderList, int haveFile)
        {
            if (!string.IsNullOrEmpty(_original_path) && System.IO.Directory.Exists(_original_path))
            {
                //folderList.Add(_original_path);
                DirectoryInfo folder = new DirectoryInfo(_original_path);
                //判断是否有文件
                if (haveFile == 1)
                {
                    if (folder.GetFiles().Length > 0)
                    {
                        folderList.Add(_original_path);
                    }
                }
                else
                {
                    folderList.Add(_original_path);
                }
                //开始遍历子文件夹
                //遍历子文件夹
                DirectoryInfo[] dirInfo = folder.GetDirectories();
                string folderPath = "";
                foreach (DirectoryInfo NextFolder in dirInfo)
                {
                    folderPath = _original_path + "\\" + NextFolder.ToString();
                    getFolderList(folderPath, ref folderList, haveFile);
                }
            }
        }
        /// <summary> 
        /// 向所有客户端一次发送消息
        /// </summary>
        private void SendAll()
        {
            try
            {
                //遍历所有的Socket，调用Send(string msg,Socket socket)方法
                while (true)
                {
                    if (server == null) return;
                    string sql_ = "Select t.* from A32001 t where a320_id=-1 and state='0'";
                    Oracle db = new Oracle();
                    DataTable dt_a32001 = new DataTable();
                    db.ExcuteDataTable(dt_a32001, sql_, CommandType.Text);
                    for (int i = 0; i < dt_a32001.Rows.Count; i++)
                    {
                        string state = dt_a32001.Rows[i]["state"].ToString();
                        string line_no_ = dt_a32001.Rows[i]["line_no"].ToString();
                        string next_xml = dt_a32001.Rows[i]["NEXT_XML"].ToString();
                        foreach (IDataTransmit dt in server.Session.Values)
                        {
                            if (dt.Connected)
                            {
                                string next_ip = dt.RemoteEndPoint.ToString();
                                this.AppendText(DateTime.Now.ToLongTimeString() + "sendall： " + next_ip + "：" + next_xml);
                                LogHelper.WriteLog("sendall：" + next_ip + "：" + next_xml);
                                byte[] buffer = Encoding.Default.GetBytes(next_xml);
                                dt.Send(buffer);
                            }
                        }
                        sql_ = "update a32001  set state = '1' ,modi_date=Systimestamp  where a320_id=-1 and state='0'  ";
                        LogHelper.WriteLog("sql_:" + sql_);
                        db = new Oracle();
                        db.ExecuteNonQuery(sql_, CommandType.Text);
                        db.Commit();
                        dt_a32001.Dispose();
                        db.GetDBConnection().Close();
                        db.GetDBConnection().Dispose();
                    }
                    Thread.Sleep(5000);
                }
            }
            catch (Exception ex)
            {
                this.AppendText(DateTime.Now.ToLongTimeString() + "处理数据出错：" + ex.Message);
                LogHelper.WriteLog(ex.Message.ToString(), ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                tSend = new Thread(SendAll);
                tSend.IsBackground = true;
                tSend.Start();
            }
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            clearA321();
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            this.port = int.Parse(txtPort.Text);
        }

        private void txtServerIP_TextChanged(object sender, EventArgs e)
        {
            this.serverip = txtServerIP.Text.ToString();
        }
        /// <summary> 
        /// 创建文件
        /// </summary>
        private bool CreateFile_(string _fileArr, Dictionary<string, byte[]> _fileContent, string clientip, ref string _returnMsg)
        {
            try
            {
                string[] str;
                if (_fileArr.Length > 10)
                {
                    str = _fileArr.Split('|');
                    byte[] data;
                    string _fileName = str[0]; //文件名
                    int sumCount = int.Parse(str[1]); //文件大小
                    //int _PacketCount = int.Parse(str[2]); //共多少数据包
                    //int _LastDataPacket = int.Parse(str[3]); //最后一个书包
                    string _CreationTime = str[2];  //文件创建时间
                    string _LastWriteTime = str[3]; //文件最后的修改时间
                    string filePath = "";
                    //float sumCount = _PacketSize * _PacketCount + _LastDataPacket;
                    int recCount = 0;
                    string clientName = str[4]; //客户端名称

                    //ConnName.TryGetValue(clientip.Split(':')[0], out clientName);
                    //string fullPath = "C:\\Users\\xuyinghuai\\Desktop\\MES\\AEmes\\CopyFileServer\\CopyFileServer\\bin\\Debug\\DEBUG01\\"+ _fileName; //Path.Combine(Environment.CurrentDirectory, _fileName);
                    string fullPath = System.Environment.CurrentDirectory + "\\Folders" + "\\" + clientName + str[5];
                    filePath = fullPath;
                    if (false == System.IO.Directory.Exists(fullPath))
                    {
                        //创建pic文件夹
                        System.IO.Directory.CreateDirectory(fullPath);
                    }
                    fullPath += "\\" + _fileName;
                    FileStream MyFileStream = new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite);
                    if (sumCount > 0)
                    {
                        for (int _i = 0; _i < _fileContent.Count; _i++)
                        {
                            _fileContent.TryGetValue(clientip + (_i + 1).ToString(), out data);
                            recCount += data.Length;
                            //if (data.Length == _PacketSize)
                            //{
                            //    //将接收到的数据包写入到文件流对象   
                            MyFileStream.Write(data, 0, data.Length);
                            //}
                            //else
                            //{
                            //    if (data.Length == _LastDataPacket && _i == _fileContent.Count - 1 && _PacketCount == _fileContent.Count - 1)
                            //    {
                            //        //将接收到的数据包写入到文件流对象   
                            //        MyFileStream.Write(data, 0, data.Length);
                            //    }
                            //    else
                            //    {
                            //        _returnMsg += "文件接收不完整";
                            //        break;
                            //    }
                            //}

                        }
                    }
                    //关闭文件流   
                    MyFileStream.Dispose();
                    MyFileStream.Close();
                    FileInfo EzoneFile = new FileInfo(fullPath);
                    EzoneFile.CreationTime = DateTime.Parse(_CreationTime);
                    EzoneFile.LastWriteTime = DateTime.Parse(_LastWriteTime);
                    _returnMsg = _fileName + "|" + sumCount.ToString() + "|" + _CreationTime + "|" + _LastWriteTime + "|" + clientName + "|" + filePath + "|" + str[5] + "|";
                    _returnMsg += _fileName + "大小为：【" + sumCount.ToString() + "】[" + recCount.ToString() + "]";
                    if (recCount != sumCount)
                    {
                        _returnMsg += "文件接收不完整";
                        return false;
                    }
                }
                else
                {
                    _returnMsg = "不明文件，无法添加【" + _fileArr + "】";
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString() + "|" + clientip + "|" + _fileArr, ex);
                _returnMsg = "创建文件出错";
                return false;
            }
            return true;
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            //if (InputPassword())
            //{
            if (MessageBox.Show("你确定要退出程序吗？", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
            {
                clearA321();
                notifyIcon1.Visible = false;
                this.Close();
                this.Dispose();
                Application.Exit();
            }
            //}
        }
        //窗体关闭前发生事件  

        private void frmMain_Load(object sender, EventArgs e)
        {
            start_();
            //this.Hide();
        }

        private void notifyIcon1_MouseDoubleClick_1(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }
            else if (this.WindowState == FormWindowState.Minimized)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            }
        }

        private void hideMenuItem_Click_1(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void showMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }
        /// <summary>
        /// 判断是否可以界面操作
        /// </summary>
        /// <returns></returns>
        private bool InputPassword()
        {
            if (string.IsNullOrEmpty(inputPassword))
            {
                string PM = Interaction.InputBox("请输入DEBUG密码", "DEBUG密码", "", 50, 30);
                if (PM == DebugPassword)
                {
                    inputPassword = PM;
                    AppendText(DateTime.Now.ToString("yyyymmddhh24missff") + ":" + "密码正确，可以操作界面");
                    //LogHelper.WriteLog("密码正确，可以操作界面");
                    return true;
                }
                else
                {
                    inputPassword = "";
                    AppendText(DateTime.Now.ToString("yyyymmddhh24missff") + ":" + "密码错误，不可以操作界面");
                    return false;
                }
            }
            return true;
        }
        private void AutoRestart()
        {
            LogHelper.WriteLog("-------------------开始重启---------------------------");
            AppendText(DateTime.Now.ToString("yyyymmddhh24miss") + ":" + "-------------------开始重启---------------------------");
            stop_();
            start_();
        }


        private void Restart(object sender, System.Timers.ElapsedEventArgs e)
        {
            AppendText(DateTime.Now.ToString("yyyymmddhh24missff") + "开始重启");
            LogHelper.WriteLog("开始重启");
            Restart();
        }
        private void Restart()
        {
            LogHelper.WriteLog("开始重启");
            Application.ExitThread();
            Thread thtmp = new Thread(new ParameterizedThreadStart(run));

            object appName = Application.ExecutablePath;

            Thread.Sleep(2000);

            thtmp.Start(appName);
            //Application.Exit();
        }

        private void run(System.Object obj)
        {
            Process ps = new Process();

            ps.StartInfo.FileName = obj.ToString();

            ps.Start();

        }

        private void btnCreateSchema_Click(object sender, EventArgs e)
        {
            //LogHelper.WriteLog("开始重启");
            //Application.ExitThread();
            Restart();
        }

        private void txtInfo_TextChanged(object sender, EventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }

    internal interface ServiceBase
    {
    }
}

// private void begin_receive(string clientip)
//{




//if (string.IsNullOrEmpty(v[0]) || v[0] == "0")
//{

//    Oracle db = new Oracle();
//    string sql_ = "select s_a320.nextval from dual ";
//    string ip_ = clientip;
//    DataTable dt_key = new DataTable();
//    db.ExcuteDataTable(dt_key, sql_, CommandType.Text);
//    v[0] = dt_key.Rows[0][0].ToString();
//    dt_key.Dispose();
//    sql_ = "Insert into a320(a320_id,CLIENT_IP,ENTER_DATE,STATE)";
//    sql_ += "values (" + v[0] + ",'" + ip_ + "',Systimestamp ,'0')";
//    db.BeginTransaction();
//    db.ExecuteNonQuery(sql_, CommandType.Text);
//    db.Commit();
//    v[1] = "";
//    ClientFlag.Remove(clientip);
//    ClientFlag.Add(clientip, v);

//}
// }
//private void Do_XML(string a320_id)
//{

//    //获取包的处理结果
//    DataTable dt_a32001 = new DataTable();
//    string sql_ = "Select t.* from A32001 t where a320_id=" + a320_id + " and base_line_no= 0";
//    _log.InfoFormat("sql_：" + sql_);
//    Oracle db = new Oracle();
//    db.ExcuteDataTable(dt_a32001, sql_, CommandType.Text);

//    for (int i = 0; i < dt_a32001.Rows.Count; i++)
//    {
//        string next_ip = dt_a32001.Rows[i]["NEXT_IP"].ToString();
//        string state = dt_a32001.Rows[i]["state"].ToString();
//        string line_no_ = dt_a32001.Rows[i]["line_no"].ToString();
//        string next_type = dt_a32001.Rows[i]["Next_Type"].ToString();
//        if (next_ip != null && next_ip.Length > 4 && state == "0")
//        {
//            EndPoint[] sKeys = new EndPoint[server.Session.Keys.Count];
//            server.Session.Keys.CopyTo(sKeys, 0);
//            Boolean lb_exists = false;
//            for (int j = 0; j < sKeys.Length; j++)
//            {
//                if (!server.Session.ContainsKey(sKeys[j])) continue;
//                IDataTransmit dt = server.Session[sKeys[j]];
//                if (dt.RemoteEndPoint.ToString() == next_ip && dt.Connected)
//                {
//                    lb_exists = true;
//                    string next_xml = dt_a32001.Rows[i]["NEXT_XML"].ToString();
//                    this.AppendText("send to " + next_ip + "：" + next_xml);
//                    byte[] buffer = Encoding.Default.GetBytes(next_xml);
//                    dt.Send(buffer);
//                    break;
//                }
//            }
//            //表示数据未发送给下级 
//            if (lb_exists == false)
//            {
//                sql_ = "update a32001  set state = '-1' where  line_no =  " + line_no_;
//            }
//            else
//            {
//                sql_ = "update a32001  set state = '1' where  line_no =  " + line_no_;
//            }
//            //WriteLog.WriteError("sql_：" + sql_);
//            _log.InfoFormat("sql_：" + sql_);
//            //判断是否要去下级 
//            db.BeginTransaction();
//            try
//            {
//                int li_db = db.ExecuteNonQuery(sql_, CommandType.Text);
//                if (li_db < 0)
//                {
//                    db.Rollback();
//                }
//                else
//                {
//                    db.Commit();
//                }
//            }
//            catch (Exception ex)
//            {
//                db.Rollback();
//                //WriteLog.WriteError("ex：" + ex);
//                _log.Error("ex：" + ex);
//            }

//        }

//    }
//}