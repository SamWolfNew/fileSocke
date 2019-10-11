using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using CopyFileClient;

class SocketClient
{
    /// <summary>
    /// socket 连接
    /// </summary>
    private Socket _newclient;
    /// <summary>
    /// 服务器地址
    /// </summary>
    private string _serverip;
    /// <summary>
    /// 端口
    /// </summary>
    private int _port;
    /// <summary>
    /// 是否长连接
    /// </summary>
    private bool _longflag = false;
    /// <summary>
    /// 接收报文的线程
    /// </summary>
    private Thread _th = null;
    /// <summary>
    /// 心跳包线程
    /// </summary>
    private Thread _th_heart = null;
    /// <summary>
    /// 短连接 超时事件 毫秒
    /// </summary>
    private int _timeout = 1000 * 60*2; // 2分钟
    /// <summary>
    /// 连接是否 有效
    /// </summary>
    private bool _connected;
    /// <summary>
    /// 开始发送时间
    /// </summary>
    private DateTime _start_time;
    /// <summary>
    /// 编码
    /// </summary>
    private string _encoding_code = Encoding.Default.HeaderName;
    private int _conCount;
    /// <summary>
    /// 接收报文只存在 100 个有效报文 
    /// </summary>
    Array _reclist;
    /// <summary>
    /// 心跳发送包
    /// </summary>
    /// 
    private string _heart_req = "0";
    /// <summary>
    /// 心跳接收
    /// </summary>
    private string _heart_resp = "0";

    /// <summary>
    /// 心跳包频率
    /// </summary>
    private int _heart_time = 1000 * 30;//30 秒一个心跳
    /// <summary>
    ///  短连接 是否存在接收包
    /// </summary>
    private bool _have_receive = true;
    /// <summary>
    /// 尝试连接次数
    /// </summary>
    private int _tryTimes = 3;
    /// <summary>
    /// 本次连接的ip和端口
    /// </summary>
    private string _ip_port;
    private byte[] getBytes(string msg_)
    {

        return Encoding.GetEncoding(Encoding_Code).GetBytes(msg_);
    }
    private string getString(byte[] bytes_list)
    {

        return Encoding.GetEncoding(Encoding_Code).GetString(bytes_list);
    }
    /// <summary>
    /// 构造函数长连接
    /// </summary>
    public SocketClient(string serverip, int port, bool longflag)
    {
        _longflag = longflag;
        _serverip = serverip;
        _port = port;
        conn();
    }
    /// <summary>
    /// 默认短连接
    /// </summary>
    public SocketClient(string serverip, int port)
    {
        _serverip = serverip;
        _port = port;
        _longflag = false;
        conn();
    }
    
    /// <summary>
    /// 初始化 数值
    /// </summary>
    private void array_config()
    {
        if (_longflag == false)
        {
            _reclist = new Array[100];
        }
        else
        {
            _reclist = new Array[1];
        }

        for (int i = 0; i < _reclist.Length; i++)
        {
            string[] row = new string[3];
            row[0] = i.ToString();
            row[1] = "0";
            _reclist.SetValue(row, i);
        }
    }
    
    Exception lastException;
    /// <summary>
    /// 尝试连接
    /// </summary>
    public bool TryConnect()
    {
        for (int i = 0; i < _tryTimes; i++)
        {
            try
            {
                conn();
                return true;
            }
            catch (Exception ex)
            {
                //LogHelper.WriteLog(ex.Message.ToString(), ex);
                this.lastException = ex;
            }
        }
        return false;
    }

    /// <summary>
    /// 给数组赋值
    /// </summary>
    /// <param name="value_"></param>
    private int set_value(string value_)
    {
        //空和心跳包 不做任何处理
        if (value_ == "" || value_ == _heart_resp)
        {
            return -1;
        }
        for (int i = 0; i < _reclist.Length; i++)
        {
            string[] row = (string[])(_reclist.GetValue(i));
            if (row[1] == "0")
            {
                _have_receive = true;
                row[1] = "1";
                row[2] = value_;
                _reclist.SetValue(row, i);
                return i;
            }
        }
        return -1;
    }
    /// <summary>
    /// 获取报文内容
    /// </summary>
    /// <param name="index_"></param>
    /// <returns></returns>
    public string get_value(int index_)
    {
        string[] row = (string[])(_reclist.GetValue(index_));
        return row[2];
    }
    private int _read_index = 0;
    private string getReceive()
    {
        string rec_str = "";
        bool lb_read = false;
        for (int i = _read_index; i < _reclist.Length; i++)
        {
            string[] row = (string[])(_reclist.GetValue(i));
            if (row[1] == "1")
            {
                rec_str = row[2];
                row[1] = "0";
                row[2] = "";
                _reclist.SetValue(row, i);
                _read_index = i + 1;
                lb_read = true;
                if (_read_index >= _reclist.Length)
                {
                    _read_index = 0;
                }
                break;
            }
        }
        if (lb_read == false)
        {
            _read_index = 0;
        }
        return rec_str;
    }
    /// <summary>
    /// 连接服务器,当长连接断开时，重新连接
    /// </summary>
    private void reconn()
    {
        try
        {
            IPEndPoint ie = new IPEndPoint(IPAddress.Parse(_serverip), _port);//服务器的IP和端口
            _newclient.Connect(ie);
        }
        catch (Exception ex)
        {
            LogHelper.WriteLog(ex.Message.ToString(), ex);
            this.lastException = ex;
        }

    }


    /// <summary>
    /// 连接服务器
    /// </summary>
    public void conn()
    {
        _newclient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint ie;
        try
        {
            ie = new IPEndPoint(IPAddress.Parse(_serverip), _port);//服务器的IP和端口
            _newclient.Connect(ie);
            this._ip_port = ie.Address.ToString()+":" + ie.Port.ToString(); 
            this._connected = true;
            array_config();
            if (_connected)
            {
                _th = new Thread(Rec);
                _th.Start();
                _th.IsBackground = true;
                _th.Name = "socketCon"+conCount.ToString();
                if (_longflag)
                {
                    //xuyinghuai 20170912 标示该链接为长连接-------------------------------
                    _th_heart = new Thread(SendHeart);
                    _th_heart.Start();
                    _th_heart.IsBackground = true;
                    _th_heart.Name = "socktHeart";
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.WriteLog(ex.Message.ToString(), ex);
            this._connected = false;
        }
    }
    /// <summary>
    /// 断开连接 释放接收线程
    /// </summary>
    public void disconn()
    {
        try
        {
            if (_newclient.Connected)
            {
                _newclient.Shutdown(SocketShutdown.Both);
                _newclient.Disconnect(true);
            }
            this._connected = false;
            _newclient.Close();
            //释放线程
            //释放线程
            if (_th != null && _th.ThreadState != ThreadState.Aborted)
            {
                _th.Abort();
            }
            if (_longflag)
            {
                //释放线程
                if (_th_heart != null && _th_heart.ThreadState != ThreadState.Aborted)
                {
                    _th_heart.Abort();
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.WriteLog(ex.Message.ToString(), ex);
        }


    }
    /// <summary>
    /// 长连接发送报文，主要用于发送心跳包，其他报文用短连接发送
    /// </summary>
    public int sendMessage(string msg)
    {
        try
        {
            if (Connected)
            {
                _newclient.Send(getBytes(msg));
            }
        }
        catch (Exception ex)
        {
            LogHelper.WriteLog(ex.Message.ToString(), ex);
            return -1;//出错 返回 -1
        }
        return 1; //发送成功
    }


    /// <summary>
    /// 短连接发送报文
    /// </summary>
    /// <param name="send_msg_"> 发送报文内容 </param>
    /// <param name="receive_msg_"> 接收报文内容 </param>
    /// <returns></returns>
    public int send_msg(string send_msg_, ref string receive_msg_)
    {
        //如果连接不存在
        if (!Connected)
        {
            receive_msg_ = null;
            return -1;
        }
        //记录开始时间
        _start_time = DateTime.Now;
        try
        {
            //发送报文
            _newclient.Send(getBytes(send_msg_));

        }
        catch (Exception ex)
        {
            LogHelper.WriteLog(ex.Message.ToString(), ex);
            disconn();
            return -1;
        }
        if (_longflag == false)
        {
            DateTime end_time = _start_time.AddMilliseconds(_timeout);
            while (true)
            {
                if (DateTime.Now > end_time)
                {   //超时 断开线程
                    disconn();
                    //throw new Exception("处理连接超时！");
                    LogHelper.WriteLog("处理连接超时！");
                    return -2;
                }
                //已经接收到报文
                if (_have_receive)
                {
                    if (!string.IsNullOrEmpty(get_value(0))) { 
                        receive_msg_ = get_value(0);
                        //断开连接 和释放线程
                        //disconn();
                        return 1;
                    }
                }        //等待100ms
                Thread.Sleep(100);
            }
        }
        return 1;
    }

    /// <summary>
    /// 短连接发送报文
    /// </summary>
    /// <param name="send_msg_"> 发送报文内容 </param>
    /// <param name="receive_msg_"> 接收报文内容 </param>
    /// <returns></returns>
    public int send_msg(byte[] send_msg_, ref string receive_msg_)
    {
        //如果连接不存在
        if (!Connected)
        {
            receive_msg_ = null;
            return -1;
        }
        //记录开始时间
        _start_time = DateTime.Now;
        try
        {
            //发送报文
            _newclient.Send(send_msg_);

        }
        catch (Exception ex)
        {
            LogHelper.WriteLog(ex.Message.ToString(), ex);
            disconn();
            return -1;
        }
        if (_longflag == false)
        {
            DateTime end_time = _start_time.AddMilliseconds(_timeout);
            while (true)
            {
                if (DateTime.Now > end_time)
                {   //超时 断开线程
                    disconn();
                    //throw new Exception("处理连接超时！");
                    LogHelper.WriteLog("处理连接超时！");
                    return -2;
                }
                //已经接收到报文
                if (_have_receive)
                {
                    receive_msg_ = get_value(0);
                    Thread.Sleep(1000);
                    //断开连接 和释放线程
                    //disconn();
                    return 1;
                }        //等待100ms
                Thread.Sleep(100);
            }
        }
        return 1;
    }

    /// <summary>
    /// 处理未处理的报文
    /// </summary>
    public void do_receive(string msg_)
    {
        if (msg_ == _heart_resp)
        {
            return;
        }
        //  string result_ = "<?xml version=\"1.0\" encoding=\"utf-8\"?><miap><miap-header><transactionid>20170911081617797940500</transactionid><version>1.0</version><messagename>uploadreq</messagename><req_ip>192.168.16.121:35093</req_ip></miap-header><miap-body><uploadreq><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|124574Dispatch_No|361709080018Part_No|96216708Part_No_Id|0A04D2C5Part_Seq_No|AD1709080184BOperation_No|25MCH_Code|E0000019Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/9/11 8:15:58</R></uploadreq></miap-body></miap>";

        //******************************************************************************************************
        //xuyinghuai 20170912
        //可以在这里调用对下发数据的处理调用方式为：-----------------------------
        //首先注释这行代码。
        //set_value(msg_);
        //XXX：为方法名，msg_ 接收数据 result_ 为返回的参数，用于发送返回消息。
        //string result_ = "";
        //SocketResp sr_ = new SocketResp();
        //sr_.RespXML(msg_, ref result_);
        //if (result_ != "" || result_ != null)
        //{
        //    sendMessage(result_);
        //}
        //******************************************************************************************************


    }



    /// <summary>
    /// 接收报文
    /// </summary>
    /// <param name="text"></param>
    delegate void Receive_Callback(string text);

    private System.Windows.Forms.Label lab1 = new Label();


    /// <summary>
    /// 接收报文内容
    /// </summary>
    /// <param name="text"></param>
    private void Receive_(string text)
    {
        if (lab1.InvokeRequired)
        {
            //用于Socket线程向主线程记日志
            Receive_Callback d = new Receive_Callback(Receive_);
            lab1.Invoke(d, new object[] { text });
        }
        else
        {
            //主线程直接记日志
            int i = set_value(text);
            //长连接
            if (_longflag)
            {
                do_receive(text);
            }
        }
    }


    /// <summary>
    /// 接收报文
    /// </summary>
    private void Rec()
    {
        while (Connected)
        {
            try
            {
                byte[] buffer = new byte[1024 * 1024];
                int n = _newclient.Receive(buffer);//socket是连接服务器的
                                                   //读取服务端发来的消息
                string msg = Encoding.GetEncoding(_encoding_code).GetString(buffer, 0, n);
                if (!string.IsNullOrEmpty(msg))
                {
                    Receive_(msg);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                this.lastException = ex;
                disconn();

            }
        }
    }

    /// <summary>
    /// 发送心跳包
    /// </summary>
    private void SendHeart()
    {
        while (true)
        {
            Thread.Sleep(_heart_time);
            if (Connected)
            {
                try
                {
                    //发送心跳包
                    _newclient.Send(getBytes(_heart_req));
                }
                catch
                {
                    disconn();
                }
            }
            else
            {
                //重新连接
                //reconn();
                disconn();
            }
            Thread.Sleep(_heart_time);
        }
    }
    public int timeOut
    {
        get
        {
            return _timeout;
        }
        set
        {
            _timeout = value;
        }
    }
    public bool Connected
    {
        get
        {
            return _newclient.Connected;
        }
        set
        {
            _connected = false;
        }
    }
    public string Encoding_Code
    {
        get
        {
            return _encoding_code;
        }
        set
        {
            _encoding_code = value;
        }

    }
    public string ip_port
    {
        get
        {
            return _ip_port;
        }
        set
        {
            _ip_port = value;
        }

    }
    public int Heart_Time
    {
        get
        {
            return _heart_time;
        }
        set
        {
            _heart_time = value;
        }
    }

    public int tryTimes
    {
        get
        {
            return _tryTimes;
        }
        set
        {
            _tryTimes = value;
        }

    }
    public int conCount
    {
        get
        {
            return _conCount;
        }
        set
        {
            _conCount = value;
        }

    }
}

