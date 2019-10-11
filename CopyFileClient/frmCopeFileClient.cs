using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SocketUtil;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualBasic;
using System.Threading;
using DBUtil;
using System.Text.RegularExpressions;
using System.Configuration;
using System.IO;

namespace CopyFileClient
{
    public partial class CopyFileClient : Form
    {
        private string serverip;
        private int port;
        private int count_;
        private int _LogDay;
        private int _FileDay;
        private int copyDay;
        private int upDay;
        private int _FrequencyCopy;
        private int _FrequencyUp;
        private int _FrequencyDelete;
        private int _FrequencyHeart;
        private string _copyFilePath;
        private string _copy_path;
        private string _ClientName;
        private string UpFilePath1;
        private string UpFilePath2;
        private string DebugPassword;
        private string inputPassword;
        private string Fileisrelease;
        private bool isCopyFile;
        private bool isUpFile;
        private bool isDeleteFile;
        private SocketClient sclong;
        delegate void AppendTextCallback(string text);
        private Thread thCopyFile = null;
        private Thread thUpFile = null;
        private Thread thDeleteFile = null;
        private Thread thLisenSclong = null;

        public CopyFileClient()
        {

            init();
        }
        /// <summary>
        /// 通过配置名获取app的配置内容
        /// </summary>
        /// <param name="confName"></param>
        /// <returns></returns>
        private string getConfig(string confName) {
            string confValue = "";
            try
            {
                confValue = ConfigurationManager.AppSettings[confName].ToString();
                AppendText(DateTime.Now.ToString("yyyyMMddhh24missff")+"获取配置"+confName+":"+confValue);
            }
            catch (Exception ex) {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return null;
            }
            return confValue;
        } 

        private void init()
        {
            InitializeComponent();
            serverip = getConfig("ServerIP");//ConfigurationManager.AppSettings["ServerIP"].ToString();
            //System.Configuration.ConfigurationSettings.AppSettings["ServerIP"];
            port = int.Parse(getConfig("Port"));//ConfigurationManager.AppSettings["Port"].ToString());
            _LogDay = int.Parse(getConfig("LogDay"));//ConfigurationManager.AppSettings["LogDay"].ToString());
            _FileDay = int.Parse(getConfig("FileDay"));//ConfigurationManager.AppSettings["FileDay"].ToString());
            _FrequencyCopy = int.Parse(getConfig("FrequencyCopy"));//ConfigurationManager.AppSettings["FrequencyCopy"].ToString());
            _FrequencyUp = int.Parse(getConfig("FrequencyUp"));//ConfigurationManager.AppSettings["FrequencyUp"].ToString());
            _FrequencyDelete = int.Parse(getConfig("FrequencyDelete"));//ConfigurationManager.AppSettings["FrequencyDelete"].ToString());
            _FrequencyHeart = int.Parse(getConfig("FrequencyHeart"));//ConfigurationManager.AppSettings["FrequencyHeart"].ToString());
            copyDay = int.Parse(getConfig("CopyDay"))*-1;//ConfigurationManager.AppSettings["CopyDay"].ToString()) * -1;
            upDay = int.Parse(getConfig("UpDay"))*-1;//ConfigurationManager.AppSettings["UpDay"].ToString()) * -1;
            _copyFilePath = getConfig("CopyFilePath");//ConfigurationManager.AppSettings["CopyFilePath"].ToString();
            _ClientName = getConfig("ClientName");//ConfigurationManager.AppSettings["ClientName"].ToString();
            UpFilePath1 = getConfig("UpFilePath1");//ConfigurationManager.AppSettings["UpFilePath1"].ToString();
            UpFilePath2 = getConfig("UpFilePath2");//ConfigurationManager.AppSettings["UpFilePath2"].ToString();
            DebugPassword = getConfig("DebugPassword");//ConfigurationManager.AppSettings["DebugPassword"].ToString();
            Fileisrelease = getConfig("Fileisrelease");
            this.txtServerIP.Text = serverip;
            this.txtPort.Text = port.ToString();
            this.txtCopyFilePath.Text = _copyFilePath;
            this._copy_path = getAppPath() + "\\" + UpFilePath1;//System.Environment.CurrentDirectory + "\\" + UpFilePath1;
            this.txtCopyFilePathTo.Text = _copy_path;
            this.txtUpFilePath1.Text = _copy_path;
            this.txtName.Text = _ClientName;
            if (!string.IsNullOrEmpty(UpFilePath2))
            {
                txtUpFilePath2.Text = UpFilePath2;
            }


            this.txtCopyFilePath.Enabled = false;
            this.txtCopyFilePathTo.Enabled = false;
        }
        /// <summary> 
        /// 向文本框写入数据
        /// </summary>
        public void AppendText(string text)
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
                if (count_ > 100)
                {
                    count_ = 0;
                    this.txtInfo.Text = "";
                }
            }
            count_++;

        }
        private bool InputPassword()
        {
            if (string.IsNullOrEmpty(inputPassword))
            {
                string PM = Interaction.InputBox("请输入DEBUG密码", "DEBUG密码", "", 50, 30);
                if (PM == DebugPassword)
                {
                    inputPassword = PM;
                    AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "密码正确，可以操作界面");
                    //LogHelper.WriteLog("密码正确，可以操作界面");
                    return true;
                }
                else
                {
                    inputPassword = "";
                    AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "密码错误，不可以操作界面");
                    //LogHelper.WriteLog("密码错误，不可以操作界面");
                    return false;
                }
            }
            return true;
        }
        /// <summary> 
        /// 获取标签的内容
        /// </summary>
        public MatchCollection getAllHyperLinks(String text, String s, string e)
        {
            try
            {
                Regex rg = new Regex("(?<=(" + s + "))[.\\s\\S]*?(?=(" + e + "))", RegexOptions.Multiline | RegexOptions.Singleline);
                MatchCollection matches = rg.Matches(text);
                return matches;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        /// <summary> 
        /// 创建一个长连接
        /// </summary>
        private void btn_ReqFromServer_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                createLongLink();
                this.txtName.Enabled = false;
                this.txtServerIP.Enabled = false;
                this.txtPort.Enabled = false;
            }
        }
        /// <summary>
        /// 建立长连接
        /// </summary>
        private bool createLongLink()
        {
            //长连接------------------------
            sclong = new SocketClient(serverip, port, true);
            sclong.Heart_Time = _FrequencyHeart * 1000;
            if (sclong.Connected)
            {
                AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":连接成功 " + sclong.ip_port);
                LogHelper.WriteLog(DateTime.Now.ToString("yyyyMMddhh24missff") + ":long连接成功 " + sclong.ip_port);
                sclong.sendMessage("0" + this.txtName.Text);
                return true;
            }
            else
            {
                //while (!sclong.Connected)
                //{
                AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":连接失败 ");
                LogHelper.WriteLog(DateTime.Now.ToString("yyyyMMddhh24missff") + ":long连接失败 " + serverip + ":" + port);
                //Thread.Sleep(1000 * 60 * 5);//5分钟重新连接一次。
                //sclong = new SocketClient(serverip, port, true);
                //sclong.Heart_Time = _FrequencyHeart * 1000;
                //}
                //return true;
                return false;
            }
        }
        /// <summary> 
        /// 断开长连接
        /// </summary>
        private void closeLongLink()
        {
            sclong.disconn();
            stopUpFileTh();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                closeLongLink();
                stopListenSclongTH();
                this.txtPort.Enabled = true;
                this.txtServerIP.Enabled = true;
                this.txtName.Enabled = true;

                this.txtUpFilePath1.Enabled = true;
                this.txtUpFilePath1.Enabled = true;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int _i = 1;
                while (_i < 1000)
                {
                    //短连接--------------------------
                    SocketClient sc = new SocketClient(serverip, port);
                    //超时设置 30秒  默认为1分钟-------------------
                    sc.timeOut = 1000 * 30;
                    string msg = "<transactionid>" + DateTime.Now.ToString("yyyyMMddhh24missff") + "</transactionid>";
                    string TransactionID = getAllHyperLinks(msg, "<transactionid>", "</transactionid>")[0].Value;
                    //string data = "<?xml version='1.0' encoding='utf-8'?><miap><miap-header><transactionid>" + TransactionID + "</transactionid><version>1.0</version><messagename>uploadreq</messagename></miap-header><miap-body><uploadreq><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|3Production_Line|3Dispatch_No|3Status_Value|3Record_Contents|3Enter_User|杨宇Enter_Date|2017/4/1 17:37:48</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|4Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 9:41:22</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|5Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:41:23</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|6Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|通信异常Enter_User|adminEnter_Date|2017/7/7 9:49:58</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|7Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|通信异常Enter_User|adminEnter_Date|2017/7/7 9:51:38</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|8Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 9:52:13</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|9Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|通信异常Enter_User|adminEnter_Date|2017/7/7 9:52:24</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|10Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 9:52:32</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|11Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:52:32</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|12Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:52:38</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|13Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:53:32</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|14Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:53:34</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|15Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:53:38</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|16Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:53:42</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|17Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:53:43</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|18Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:53:44</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|19Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:53:44</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|20Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:53:45</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|21Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:54:32</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|22Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:54:33</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|23Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:54:34</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|24Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:55:24</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|25Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:56:25</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|26Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:56:26</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|27Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:57:11</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|28Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:58:08</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|29Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 9:59:15</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|30Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|通信异常Enter_User|adminEnter_Date|2017/7/7 10:00:05</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|31Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|通信异常Enter_User|adminEnter_Date|2017/7/7 10:01:51</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|32Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 10:01:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|33Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:01:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|34Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|通信异常Enter_User|adminEnter_Date|2017/7/7 10:02:07</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|35Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|通信异常Enter_User|adminEnter_Date|2017/7/7 10:02:26</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|36Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 10:02:29</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|37Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:02:29</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|38Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:03:05</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|39Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:03:06</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|40Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:03:08</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|41Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:03:12</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|42Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:03:15</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|43Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:03:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|44Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:04:08</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|45Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:05:10</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|46Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|通信异常Enter_User|adminEnter_Date|2017/7/7 10:06:40</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|47Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 10:09:30</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|48Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:09:30</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|49Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:09:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|50Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:11:11</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|51Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:12:22</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|52Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:13:23</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|53Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:13:24</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|54Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:14:21</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|55Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:15:17</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|56Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:16:19</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|57Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:17:00</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|58Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|报警异常Enter_User|adminEnter_Date|2017/7/7 10:17:01</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|59Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 10:18:37</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|60Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|人工停线Enter_User|adminEnter_Date|2017/7/7 10:35:05</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|61Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 10:35:40</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|62Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|人工停线Enter_User|adminEnter_Date|2017/7/7 10:37:56</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|63Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 10:37:59</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|64Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|人工停线Enter_User|adminEnter_Date|2017/7/7 10:38:24</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|65Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 10:38:26</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|66Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 10:56:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|67Production_Line|D2生产线Dispatch_No|421704180001Status_Value|0Record_Contents|人工停线Enter_User|adminEnter_Date|2017/7/7 11:02:45</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|68Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 11:03:52</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|69Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 11:20:56</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|70Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 11:31:50</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|71Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 11:36:19</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|72Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 11:45:22</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|73Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 11:45:52</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|74Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 11:50:18</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|75Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 1:36:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|76Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 1:50:15</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|77Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 1:50:34</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|78Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 2:10:51</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|79Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 2:18:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|80Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 2:20:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|81Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 2:25:01</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|82Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 2:38:01</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|83Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 2:41:48</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|84Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:00:34</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|85Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:00:49</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|86Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:01:59</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|87Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:02:05</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|88Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:11:42</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|89Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:12:17</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|90Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:29:35</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|91Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:30:51</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|92Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:34:17</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|93Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 3:56:10</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|94Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:01:51</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|95Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:08:53</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|96Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:12:38</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|97Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:17:22</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|98Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:21:22</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|99Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:27:42</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|100Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:30:26</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|101Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:34:26</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|102Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:35:48</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|103Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:37:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|104Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:41:14</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|105Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:42:35</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|106Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:47:38</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|107Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:49:18</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|108Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:52:15</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|109Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:59:37</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|110Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 4:59:47</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|111Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 5:02:27</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|112Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 5:09:11</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|113Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 5:09:24</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|114Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 5:11:45</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|115Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:20:36</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|116Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:20:49</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|117Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:21:06</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|118Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:28:39</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|119Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:28:47</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|120Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:41:41</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|121Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:42:21</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|122Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:47:10</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|123Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:47:36</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|124Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:47:51</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|125Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:48:19</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|126Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:49:13</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|127Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:49:47</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|128Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:50:08</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|129Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:50:40</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|130Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:57:13</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|131Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:57:25</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|132Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:57:37</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|133Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 17:57:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|134Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:00:14</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|135Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:00:30</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|136Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:00:36</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|137Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:02:59</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|138Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:03:06</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|139Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:03:10</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|140Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:07:12</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|141Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:07:39</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|142Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:07:53</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|143Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:13:32</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|144Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:14:24</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|145Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:17:17</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|146Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:17:53</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|147Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:18:04</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|148Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:20:39</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|149Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:22:44</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|150Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:30:33</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|151Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:34:50</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|152Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 18:39:41</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|153Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:02:13</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|154Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:13:38</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|155Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:16:37</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|156Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:16:45</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|157Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:18:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|158Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:26:10</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|159Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:26:30</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|160Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:27:42</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|161Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:27:45</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|162Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:28:31</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|163Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/7 19:37:59</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|164Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/11 11:05:54</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|165Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/11 11:24:47</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|166Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/11 11:24:58</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|167Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/11 11:32:56</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|168Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/11 11:36:08</R><R>TABLE_NAME|PRODUCTION_LINE_STATUS_INFO_TEMPRecord_Id|169Production_Line|D2生产线Dispatch_No|421704180001Status_Value|1Record_Contents|人工开线Enter_User|adminEnter_Date|2017/7/11 13:53:38</R><R>TABLE_NAME|MACHINE_STATUS_INFO_TEMPRecord_Id|1MCH_Code|E0000004Parameter_No|1Status_Value|0Enter_User|yyEnter_Date|2017/7/12 10:19:34</R><R>TABLE_NAME|MACHINE_STATUS_INFO_TEMPRecord_Id|2MCH_Code|E0000013Parameter_No|1Status_Value|0Enter_User|yyEnter_Date|2017/7/12 10:19:49</R><R>TABLE_NAME|MACHINE_STATUS_INFO_TEMPRecord_Id|3MCH_Code|E0000039Parameter_No|2Status_Value|0Enter_User|yyEnter_Date|2017/7/12 10:20:11</R><R>TABLE_NAME|PART_MATIERL_INFO_TEMPRecord_Id|1Dispatch_No|321707110001Part_No|96216708Part_No_Id|1104A55FPart_Seq_No|AD47617005A4Matierl_Code|Lot_Batch_No|16110901Use_QTY|Matierl_Seq_No|4W795H77Matierl_No_Id|1W345H78Enter_User|yyEnter_Date|2017/7/12 10:30:04</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|1Dispatch_No|321704170002Part_No|96216708Part_No_Id|Part_Seq_No|AD47061700BB7Operation_No|50MCH_Code|E0000019Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 10:23:21</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|2Dispatch_No|321704170002Part_No|96216708Part_No_Id|Part_Seq_No|AD47061700BB7Operation_No|50MCH_Code|E0000019Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 10:26:39</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|3Dispatch_No|421704180001Part_No|96216708Part_No_Id|Part_Seq_No|BD17062801EE3Operation_No|15MCH_Code|E0000019Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 11:11:38</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|4Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A55FPart_Seq_No|AD47617005A4Operation_No|5MCH_Code|E0000007Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:14</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|5Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A562Part_Seq_No|AD47617005A3Operation_No|5MCH_Code|E0000006Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:14</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|6Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A560Part_Seq_No|AD47617005A1Operation_No|5MCH_Code|E0000004Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:14</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|7Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A561Part_Seq_No|AD47617005A2Operation_No|5MCH_Code|E0000005Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:14</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|8Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A564Part_Seq_No|AD47617005A5Operation_No|5MCH_Code|E0000004Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:32</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|9Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A563Part_Seq_No|AD47617005A8Operation_No|5MCH_Code|E0000007Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:32</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|10Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A566Part_Seq_No|AD47617005A7Operation_No|5MCH_Code|E0000006Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:32</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|11Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A565Part_Seq_No|AD47617005A6Operation_No|5MCH_Code|E0000005Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:32</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|12Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A567Part_Seq_No|AD47617005ACOperation_No|5MCH_Code|E0000007Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:46</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|13Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A569Part_Seq_No|AD47617005ABOperation_No|5MCH_Code|E0000006Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:46</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|14Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A56APart_Seq_No|AD47617005A9Operation_No|5MCH_Code|E0000004Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:46</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|15Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A568Part_Seq_No|AD47617005AAOperation_No|5MCH_Code|E0000005Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:46:46</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|16Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A561Part_Seq_No|AD47617005A2Operation_No|10MCH_Code|E0000011Test_Item_Code|1Test_Item_Collect_Value|102.06/102.46/101.99/102.53/101.89/103.31/101.86/102.08/68.11/102.42/102.33/101.99/102.51/102.12/101.85/102.56/102.23/101.88/102.54/101.99/102.04/102.48/102.11/101.92/102.56/102.13/102.08/103.79/102.32/102.09/102.54/102.39/102.04/102.00/102.67/102.10/102.04/102.86/102.12/94.95/102.52/102.04/102.14/102.27/102.13/102.08/102.40/102.37/102.63/102.55/101.97/102.02/102.52/102.00/102.50/102.66/101.97/102.40/102.51/101.98/102.08/102.25/101.86/101.86/102.28/101.86/102.32/101.99/101.79/104.22/0.60/0.44Test_Item_Eigen_Value|104.22Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:47:26</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|17Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A562Part_Seq_No|AD47617005A3Operation_No|10MCH_Code|E0000012Test_Item_Code|1Test_Item_Collect_Value|99.65/99.93/99.75/99.70/99.60/100.12/99.58/5.19/99.83/99.78/100.20/99.92/99.86/99.69/99.80/100.37/100.26/100.00/99.51/100.20/99.71/100.28/100.29/99.71/99.85/99.66/99.88/99.83/99.86/100.27/99.93/100.23/99.92/100.09/100.39/100.13/99.82/99.95/99.78/99.79/99.72/99.78/99.77/99.87/100.22/99.63/100.25/100.25/100.11/99.71/99.69/99.71/99.67/99.66/99.91/99.71/100.29/99.81/99.74/100.15/99.74/100.45/99.77/99.90/100.35/99.71/99.97/99.60/99.55/0.57/0.69/0.54/0.64/0.64/5.35/0.60/0.58/0.69/0.62/0.60/0.62/0.58Test_Item_Eigen_Value|5.35Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:47:26</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|18Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A560Part_Seq_No|AD47617005A1Operation_No|10MCH_Code|E0000010Test_Item_Code|1Test_Item_Collect_Value|97.87/98.50/98.55/98.07/98.06/98.07/97.96/97.92/98.52/98.20/98.38/98.18/97.85/98.25/97.75/97.84/97.68/97.89/98.19/98.14/98.35/98.52/98.26/97.76/97.88/97.84/97.91/98.28/98.10/98.10/98.52/98.03/98.42/98.05/98.35/97.82/98.23/97.82/97.89/97.72/98.10/97.68/98.22/98.25/98.12/97.91/97.99/97.86/97.87/98.14/98.04/97.98/98.06/98.46/98.31/98.56/98.12/97.99/97.93/97.94/97.87/97.92/98.24/99.42/98.29/98.11/97.77/97.59/0.68/0.63/0.56/0.63/0.52/0.48/0.56/0.55/0.61/0.67/0.62/0.65/0.66/0.71/0.63/0.51/0.49/0.63Test_Item_Eigen_Value|97.59Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:47:26</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|19Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A55FPart_Seq_No|AD47617005A4Operation_No|10MCH_Code|E0000013Test_Item_Code|1Test_Item_Collect_Value|104.38/104.32/104.54/104.39/104.38/105.32/104.80/104.22/104.13/104.77/104.90/104.85/104.81/104.21/104.86/104.25/104.91/104.69/104.56/104.26/104.84/104.25/104.60/104.41/104.77/104.42/104.77/104.36/104.85/104.60/104.82/104.81/104.55/104.27/104.16/104.20/104.63/104.19/104.37/104.19/104.22/104.19/104.25/104.47/104.97/104.39/104.31/104.92/104.62/104.45/104.35/104.68/104.40/104.23/104.23/104.43/104.64/104.25/104.65/104.11/104.53/104.23/104.81/104.34/104.24/104.31/104.27/104.25/104.37/6.09/0.70/0.43Test_Item_Eigen_Value|6.09Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:47:26</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|20Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A561Part_Seq_No|AD47617005A2Operation_No|10MCH_Code|E0000011Test_Item_Code|2Test_Item_Collect_Value|0.00/0.01/0.01/0.01/0.01/0.01/0.01/0.59/0.59/0.59/6.59/0.59/0.59/0.59/0.59/6.59/0.59/0.01/0.01/0.00/0.01/0.01/0.00/0.01/0.01/0.01/0.01/0.01/0.59/6.59/0.59/2.54/0.59/0.59/6.59/0.59/6.59/0.59/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/6.59/0.59/0.59/0.59/0.60/6.60/0.59/0.59/0.59/0.59/0.00/Test_Item_Eigen_Value|6.60Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:47:32</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|21Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A560Part_Seq_No|AD47617005A1Operation_No|10MCH_Code|E0000010Test_Item_Code|2Test_Item_Collect_Value|0.00/0.01/0.01/0.01/0.56/0.56/0.56/6.48/0.56/0.56/0.57/0.56/0.56/0.56/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.00/0.00/6.48/0.56/0.56/0.56/0.56/3.65/0.56/0.56/6.48/0.56/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/1.65/0.56/6.48/0.56/0.56/1.94/0.56/0.56/0.56/0.56/0.01/0.01/-0.00/-0.00/Test_Item_Eigen_Value|6.48Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:32</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|22Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A562Part_Seq_No|AD47617005A3Operation_No|10MCH_Code|E0000012Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.58/6.36/0.58/0.58/0.58/0.58/6.36/0.58/0.58/0.58/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.58/6.36/0.58/0.58/0.58/0.58/0.58/0.58/0.58/6.36/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.00/0.00/6.36/0.57/0.58/6.36/0.58/0.58/0.58/0.58/6.36/0.57/Test_Item_Eigen_Value|6.36Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:32</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|23Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A55FPart_Seq_No|AD47617005A4Operation_No|10MCH_Code|E0000013Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.01/0.01/0.01/0.57/6.53/0.57/0.58/0.57/0.57/6.53/0.57/0.57/0.57/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.57/0.57/0.57/6.41/0.58/0.57/6.53/0.57/0.57/0.57/0.01/0.01/0.01/0.01/0.01/0.00/0.00/0.00/0.01/0.01/0.57/0.57/0.57/0.57/6.53/0.57/0.58/6.53/0.57/0.57/0.01/0.00/0.00/Test_Item_Eigen_Value|6.53Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:47:33</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|24Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A564Part_Seq_No|AD47617005A5Operation_No|10MCH_Code|E0000010Test_Item_Code|1Test_Item_Collect_Value|0.62/0.59/0.57/0.56/0.52/2.93/0.52/0.41/0.36/0.60/0.44/0.46/0.48/0.77/0.60/0.62/0.57/0.67/0.76/0.66/0.55/0.57/0.59/0.49/0.44/0.60/0.52/0.46/0.57/0.53/0.57/0.55/0.59/0.69/0.62/0.67/0.70/0.53/0.51/0.49/0.51/0.37/0.46/1.36/0.60/0.50/0.54/0.69/0.67/0.60/0.53/0.62/0.65/0.67/0.56/0.68/0.66/0.85/0.58/0.64/0.65/0.61/0.56/0.53/6.15/0.58/0.53/0.52/0.48/0.57/0.44/0.43/0.52/0.49/0.55/0.68/0.51/0.54/0.61/0.73/0.61/0.63/0.74/0.74/0.67/6.32/0.60/0.64/0.73/0.64/0.53/0.65/0.44/0.37/0.51/0.48/7.28/Test_Item_Eigen_Value|0.58Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:47</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|25Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A566Part_Seq_No|AD47617005A7Operation_No|10MCH_Code|E0000012Test_Item_Code|1Test_Item_Collect_Value|0.64/0.62/0.59/0.59/0.68/0.55/0.67/0.52/4.92/0.59/0.69/0.47/0.68/0.75/0.68/0.67/0.71/0.65/0.81/0.60/0.63/0.59/0.59/0.57/0.56/6.11/0.62/0.66/0.49/0.55/0.60/6.07/0.72/0.72/0.56/0.61/0.62/0.72/0.69/0.64/0.61/0.61/0.63/0.58/0.61/0.70/0.54/0.55/0.69/0.63/0.62/0.52/0.52/0.47/4.26/0.64/0.54/0.65/0.67/0.57/3.34/0.59/0.55/0.69/0.63/0.63/0.56/0.67/0.74/0.76/0.63/0.56/0.58/0.69/0.63/0.55/0.70/0.63/0.66/0.76/0.57/0.73/0.65/0.64/0.73/0.55/0.63/0.71/0.59/0.69/1.50/0.58/0.64/0.60/0.68/0.65/7.48/Test_Item_Eigen_Value|0.63Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:47</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|26Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A565Part_Seq_No|AD47617005A6Operation_No|10MCH_Code|E0000011Test_Item_Code|1Test_Item_Collect_Value|0.53/0.43/0.75/0.61/0.48/0.62/0.56/0.62/0.68/0.43/0.37/0.40/0.40/0.51/0.53/0.52/0.42/0.55/0.46/0.48/0.58/0.52/0.61/0.53/0.67/0.77/0.68/0.43/0.53/0.54/0.54/0.58/0.43/0.62/0.45/0.40/0.40/0.50/0.42/0.54/0.59/0.54/0.49/0.52/0.59/0.56/0.71/0.67/0.66/0.59/0.54/0.57/0.54/0.43/0.56/0.41/0.44/0.34/0.36/0.37/0.42/0.51/0.50/0.59/0.53/0.57/0.44/0.48/0.62/0.61/0.69/0.64/0.66/0.62/0.64/0.63/1.52/0.52/0.60/0.54/0.65/0.48/6.01/0.35/0.40/0.40/0.52/0.56/6.58/0.57/0.47/0.47/0.47/0.51/6.73/0.52/7.44/Test_Item_Eigen_Value|0.53Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:47</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|27Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A563Part_Seq_No|AD47617005A8Operation_No|10MCH_Code|E0000013Test_Item_Code|1Test_Item_Collect_Value|0.45/0.69/0.46/0.60/0.57/0.52/0.42/0.47/0.51/0.62/0.68/0.56/0.62/0.60/0.56/0.68/0.46/0.54/0.48/0.55/0.32/0.62/0.51/4.53/0.47/0.47/0.57/0.55/0.60/0.53/5.51/0.43/0.68/0.69/0.55/0.64/0.52/5.71/0.53/0.51/0.53/0.54/0.33/0.43/5.82/0.41/0.47/0.44/0.48/0.52/0.54/0.63/0.54/0.67/0.55/0.47/0.66/0.61/5.44/0.72/0.65/0.57/0.62/0.59/0.61/4.12/0.53/0.54/0.50/0.56/0.57/0.56/2.26/0.46/0.32/0.39/0.43/0.48/0.56/0.55/0.51/0.77/0.62/0.62/0.52/0.73/3.12/0.62/0.54/0.48/0.61/0.51/0.61/5.88/0.36/0.52/0.47/7.28/Test_Item_Eigen_Value|0.54Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:48</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|28Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A565Part_Seq_No|AD47617005A6Operation_No|10MCH_Code|E0000011Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.56/0.56/0.57/0.57/0.57/0.57/0.57/0.57/6.40/0.56/0.01/0.01/0.01/0.01/0.00/0.01/0.01/0.01/0.01/0.01/0.57/0.57/0.57/6.40/0.57/0.57/0.57/0.57/6.40/0.56/0.01/0.00/0.01/0.01/0.01/0.01/0.01/0.01/0.00/0.00/0.56/0.57/0.57/6.41/0.57/0.57/0.57/0.56/6.40/0.56/Test_Item_Eigen_Value|6.41Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:54</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|29Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A564Part_Seq_No|AD47617005A5Operation_No|10MCH_Code|E0000010Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.01/0.00/0.00/0.01/0.01/0.01/0.01/0.01/2.18/0.57/0.57/0.57/0.57/0.57/0.57/6.45/0.57/0.58/0.57/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.57/0.57/0.57/6.45/0.57/0.57/0.57/0.57/6.45/0.58/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.57/0.57/6.45/0.57/0.57/0.57/0.57/6.44/Test_Item_Eigen_Value|6.45Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:54</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|30Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A566Part_Seq_No|AD47617005A7Operation_No|10MCH_Code|E0000012Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.56/6.58/0.57/0.57/6.57/0.56/0.57/0.57/0.56/0.01/0.01/0.01/0.01/0.00/0.01/0.01/0.01/0.01/0.01/0.01/6.58/0.57/0.56/0.57/0.56/0.56/0.57/0.56/6.57/0.56/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.56/0.56/0.56/0.56/6.57/0.56/0.56/Test_Item_Eigen_Value|6.58Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:47:54</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|31Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A563Part_Seq_No|AD47617005A8Operation_No|10MCH_Code|E0000013Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/6.48/0.56/0.56/0.56/0.56/0.56/0.56/0.56/6.48/0.56/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.56/0.56/0.56/0.56/6.48/0.56/0.56/0.56/0.56/0.56/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.56/0.56/0.56/6.48/0.56/0.56/6.47/0.56/-0.00/Test_Item_Eigen_Value|6.48Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:47:54</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|32Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A567Part_Seq_No|AD47617005ACOperation_No|10MCH_Code|E0000013Test_Item_Code|1Test_Item_Collect_Value|0.68/0.74/0.36/0.47/0.57/0.46/0.47/0.54/0.51/0.56/0.61/0.62/0.52/0.57/0.68/0.53/0.47/0.58/0.79/0.64/0.62/0.55/0.64/0.57/6.53/0.57/0.52/0.31/0.46/0.46/0.56/0.49/0.52/0.50/0.51/0.55/0.58/0.65/0.66/0.56/0.63/0.78/0.59/0.64/0.56/0.46/0.53/6.22/0.55/0.60/0.43/0.34/0.41/0.36/0.50/0.50/0.54/0.47/0.52/0.50/0.54/0.61/0.56/0.61/0.68/0.57/0.62/0.51/0.68/0.62/6.48/0.65/0.65/0.73/0.58/0.45/0.55/0.58/0.57/0.44/0.34/0.38/0.36/0.51/0.44/0.54/0.50/0.41/0.56/0.52/0.54/0.49/0.60/0.32/0.39/7.20/Test_Item_Eigen_Value|0.54Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:48:09</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|33Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A568Part_Seq_No|AD47617005AAOperation_No|10MCH_Code|E0000011Test_Item_Code|1Test_Item_Collect_Value|0.46/0.60/0.60/0.51/0.66/0.62/0.58/0.69/0.56/0.59/0.61/0.58/5.13/0.68/0.53/0.45/0.61/0.60/0.43/0.48/0.54/0.38/0.35/0.45/0.44/0.42/0.51/0.38/0.41/0.51/0.52/0.61/0.50/6.20/0.53/0.70/0.66/0.56/0.60/0.66/0.64/0.59/0.66/0.67/0.70/0.58/0.56/0.56/0.62/0.54/5.33/0.52/0.48/0.43/0.43/0.45/0.51/0.52/0.57/0.48/0.50/0.53/0.57/0.58/0.70/0.64/0.62/0.77/0.66/0.62/0.57/6.22/0.59/0.65/0.78/0.67/0.58/0.49/0.56/0.56/0.60/0.51/0.54/0.44/0.41/0.43/0.44/0.55/6.65/0.45/0.47/0.52/0.54/0.61/0.70/0.64/7.37/7.42/Test_Item_Eigen_Value|0.56Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:48:09</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|34Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A569Part_Seq_No|AD47617005ABOperation_No|10MCH_Code|E0000012Test_Item_Code|1Test_Item_Collect_Value|0.65/0.63/0.67/0.61/0.64/0.56/0.47/0.36/0.56/0.47/0.54/0.64/0.56/0.60/0.48/0.48/0.60/0.66/0.57/0.53/0.63/0.61/0.55/0.66/0.68/6.08/0.53/0.51/0.51/0.63/0.54/0.56/0.51/0.44/0.45/0.52/0.52/0.54/0.53/0.52/0.60/0.55/0.48/0.62/0.53/0.51/2.02/0.62/0.51/0.59/0.73/0.56/0.72/0.56/0.59/0.54/0.52/0.50/0.53/0.68/0.56/0.47/0.48/0.34/0.39/0.62/0.55/0.57/0.64/0.61/0.52/6.31/0.46/0.43/0.53/0.70/0.56/0.58/0.55/0.52/0.60/0.57/0.65/0.54/0.72/0.57/0.49/0.56/0.55/0.53/0.55/0.63/0.44/0.45/0.47/0.48/12.78/7.19/Test_Item_Eigen_Value|0.55Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:48:09</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|35Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A56APart_Seq_No|AD47617005A9Operation_No|10MCH_Code|E0000010Test_Item_Code|1Test_Item_Collect_Value|4.88/0.63/0.57/0.45/0.57/0.53/0.57/0.58/0.61/0.66/0.65/0.61/0.54/0.54/0.44/0.48/6.31/0.51/0.44/0.39/0.42/0.44/0.42/0.50/0.58/0.53/0.46/0.51/0.67/0.58/0.57/0.57/0.53/0.52/0.44/0.52/0.48/0.56/0.60/0.59/0.51/0.61/0.61/0.58/0.60/0.66/0.69/0.59/0.54/0.65/0.60/0.65/0.51/0.64/6.30/0.59/0.64/0.70/0.59/0.46/0.41/0.55/0.49/5.72/0.54/0.64/0.43/0.64/0.56/0.60/0.62/0.73/0.54/0.62/0.71/0.73/0.69/0.64/0.71/0.71/0.51/0.67/0.54/0.71/0.74/0.52/0.48/0.43/0.53/0.41/0.55/0.42/0.57/0.54/0.52/0.61/7.35/7.49/Test_Item_Eigen_Value|0.57Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:48:10</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|36Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A56APart_Seq_No|AD47617005A9Operation_No|10MCH_Code|E0000010Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.58/0.58/0.58/0.58/1.40/0.58/0.58/0.58/0.58/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.58/0.58/6.43/0.58/0.58/0.58/0.58/0.58/0.58/0.58/0.01/0.02/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/6.43/0.58/0.58/0.57/Test_Item_Eigen_Value|6.43Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:48:16</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|37Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A567Part_Seq_No|AD47617005ACOperation_No|10MCH_Code|E0000013Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.00/0.00/0.56/0.56/0.57/0.56/0.56/0.56/6.53/0.56/0.57/0.57/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.00/0.00/0.56/0.56/0.56/6.53/0.56/0.56/0.56/0.56/0.56/0.57/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.00/6.53/0.56/0.56/4.83/0.56/0.56/0.56/Test_Item_Eigen_Value|6.53Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:48:16</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|38Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A568Part_Seq_No|AD47617005AAOperation_No|10MCH_Code|E0000011Test_Item_Code|2Test_Item_Collect_Value|0.01/0.01/0.00/0.00/0.00/0.00/0.01/0.00/0.01/0.01/0.01/6.35/0.56/0.56/0.56/0.56/6.35/0.56/0.56/6.35/0.56/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.56/0.56/6.35/0.56/0.56/0.56/0.56/6.35/0.56/0.56/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/6.35/0.56/0.56/0.56/0.55/6.34/Test_Item_Eigen_Value|6.35Test_Item_Judge_Result|1Enter_User|adminEnter_Date|2017/7/11 13:48:16</R><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|39Dispatch_No|421704180001Part_No|96216708Part_No_Id|1104A569Part_Seq_No|AD47617005ABOperation_No|10MCH_Code|E0000012Test_Item_Code|2Test_Item_Collect_Value|0.01/0.00/0.01/0.01/0.01/0.01/0.01/0.01/6.52/0.58/0.58/0.58/0.58/6.53/0.58/0.58/0.58/0.58/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.00/0.00/0.58/0.58/0.58/6.52/0.58/0.58/0.59/0.58/6.52/0.58/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.01/0.69/0.58/6.52/0.58/0.58/0.58/0.58/6.52/0.57/0.57/Test_Item_Eigen_Value|6.53Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/7/11 13:48:16</R><R>TABLE_NAME|ALARM_INFO_TEMPRecord_Id|1Dispatch_No|321704170002Part_No|96216708Production_Line|FSD2Work_Center_No|3106MCH_Code|E0000018Alarm_Level|2Alarm_Category|扫码失败Alarm_Content|1#扫码器扫码失败Alarm_Processing_Method|调整扫码器位置Enter_User|yyEnter_Date|2017/7/12 10:11:57</R><R>TABLE_NAME|ALARM_INFO_TEMPRecord_Id|2Dispatch_No|321704170002Part_No|96216708Production_Line|FSD2Work_Center_No|3103MCH_Code|E0000011Alarm_Level|1Alarm_Category|通信异常Alarm_Content|2#电流表通信异常Alarm_Processing_Method|重启电流表Enter_User|yyEnter_Date|2017/7/12 10:13:38</R><R>TABLE_NAME|ALARM_INFO_TEMPRecord_Id|3Dispatch_No|321704170002Part_No|96216708Production_Line|FSD2Work_Center_No|3113MCH_Code|E0000040Alarm_Level|1Alarm_Category|通信异常Alarm_Content|1#气压表通信异常Alarm_Processing_Method|重启气压表Enter_User|yyEnter_Date|2017/7/12 10:14:50</R></uploadreq></miap-body></miap>";
                    string data = "<?xml version='1.0' encoding='utf-8'?><miap><miap-header><transactionid>" + TransactionID + "</transactionid><version>1.0</version><messagename>uploadreq</messagename></miap-header><miap-body><uploadreq><R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|4786Dispatch_No|321708070001Part_No|96216708Part_No_Id|11053731Part_Seq_No|BD1706280188FOperation_No|55MCH_Code|E0000046Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/8/15 10:31:50</R></uploadreq></miap-body></miap>";
                    string rec_date_ = "";
                    //_flag为1时，表示上报成功----------
                    int _flag = sc.send_msg(data, ref rec_date_);
                    AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + rec_date_);
                    _i = _i + 1;
                }
            }
            catch (Exception ex)
            {
                AppendText(ex.Message);
                LogHelper.WriteLog(ex.Message.ToString(), ex);
            }
        }
        /// <summary> 
        /// 选择文件
        /// </summary>
        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                //打开文件
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "选择要传的文件";
                ofd.InitialDirectory = @"C:\Users\xuyinghuai\Desktop\MES\AEmes\CopyFileServer\CopyFileServer\bin\Debug\Log\LogInfo";
                ofd.Filter = "所有文件|*.*|文本文件|*.txt|图片文件|*.jpg|视频文件|*.avi";
                ofd.ShowDialog();
                //得到选择文件的路径
                txtPath.Text = ofd.FileName;
            }
        }
        /// <summary> 
        /// 发送文件
        /// </summary>
        private void btnSendFile_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                string fullFile = txtPath.Text;
                sendFile(fullFile, "");
            }
        }
        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="fullFile">文件路径</param>
        /// <returns></returns>
        private bool sendFile(string fullFile, string Relative_path)
        {
            try
            {
                if (!string.IsNullOrEmpty(fullFile))
                {
                    SocketClient sc = new SocketClient(serverip, port);
                    string clientName = this.txtName.Text;
                    if (sc.Connected)
                    {
                        //AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "文件发送开始【" + fullFile + "】");
                        bool flag_ = TransferFiles.sendFile(sc, fullFile, clientName, Relative_path);
                        sc.disconn();
                        AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "文件发送SUCCESS【" + fullFile + "】");
                    }
                    else
                    {
                        AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "无法连接服务器");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return false;
            }
        }
        /// <summary>
        /// 启动上传线程
        /// </summary>
        //private void startUpFileTh()
        //{
        //    string _upFilePath01 = txtUpFilePath1.Text;
        //    string _upFilePath02 = txtUpFilePath2.Text;
        //    if ((!string.IsNullOrEmpty(_upFilePath01) && System.IO.Directory.Exists(_upFilePath01)) || (!string.IsNullOrEmpty(_upFilePath02) && System.IO.Directory.Exists(_upFilePath02)))
        //    {
        //        isUpFile = true;
        //        thUpFile = new Thread(startUpFile);
        //        thUpFile.Name = "socketUpFile";
        //        thUpFile.IsBackground = true;
        //        thUpFile.Start();
        //    }
        //    else
        //    {
        //        isUpFile = false;
        //        AppendText("上传路径为空或者不存在！");
        //    }
        //}
        /// <summary>
        /// 停止上传
        /// </summary>
        private void stopUpFileTh()
        {
            isUpFile = false;
            if (thUpFile != null && thUpFile.ThreadState != ThreadState.Aborted)
            {
                thUpFile.Abort();
            }

        }
        /// <summary>
        /// 开始监听线程
        /// </summary>
        private void startListenSclongTH()
        {
            if (sclong.Connected)
            {
                LogHelper.WriteLog("启动监听长连接thLisenSclong");
                thLisenSclong = new Thread(listenSclong);
                thLisenSclong.Name = "thLisenSclong";
                thLisenSclong.IsBackground = true;
                thLisenSclong.Start();
            }
        }
        /// <summary>
        /// 停止监听线程
        /// </summary>
        private void stopListenSclongTH()
        {
            if (thLisenSclong != null && thLisenSclong.ThreadState != ThreadState.Aborted)
            {
                thLisenSclong.Abort();
            }
        }
        /// <summary>
        /// 用于监听长连接
        /// </summary>
        private void listenSclong()
        {
            while (true)
            {
                if (!sclong.Connected)
                {
                    closeLongLink();//断开连接并停止上传
                    if (sclong.TryConnect())
                    {
                        //联通后
                        sclong.disconn();
                        if (createLongLink())//发送长连接
                        {
                            Thread.Sleep(1000 * 5);
                        }
                    }
                    else
                    {
                        AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "尝试连接失败");
                        LogHelper.WriteLog("尝试连接失败");
                    }
                }
                Thread.Sleep(1000 * 2);
            }
        }
        /// <summary>
        /// 启动复制文件的线程
        /// </summary>
        private void startCopyFileTh()
        {
            if (false == System.IO.Directory.Exists(_copy_path))
            {
                //创建pic文件夹
                System.IO.Directory.CreateDirectory(_copy_path);
            }
            string _original_path = txtCopyFilePath.Text.ToString();
            if (!string.IsNullOrEmpty(_original_path) && System.IO.Directory.Exists(_original_path))
            {
                LogHelper.WriteLog(DateTime.Now.ToString("yyyyMMddhh24missff") +"启动复制线程");
                isCopyFile = true;
                thCopyFile = new Thread(startCopyFile1);
                thCopyFile.Name = "socketCopyFile";
                thCopyFile.IsBackground = true;
                thCopyFile.Start();
                //isCopyFile = true;
                //int time = 1000 *_FrequencyCopy;
                //System.Timers.Timer timer = new System.Timers.Timer();
                //timer.Elapsed += new System.Timers.ElapsedEventHandler(startCopyFile);
                //timer.Interval = time;////设置时间（定时器多少秒执行一次--注意是毫秒)
                //timer.AutoReset = true;//执行多次--false执行一次
                //timer.Enabled = true;//执行事件为true,定时器启动
            }
            else
            {
                isCopyFile = false;
                AppendText("复制前路径不存在");
            }
        }
        /// <summary>
        /// 停止复制线程
        /// </summary>
        private void stopCopyFileTh()
        {
            isCopyFile = false;
            if (thCopyFile != null && thCopyFile.ThreadState != ThreadState.Aborted)
            {
                thCopyFile.Abort();
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
        /// 开始上传
        /// </summary>
        //private void startUpFile()
        //{
        //    string _upFilePath01 = txtUpFilePath1.Text;
        //    string _upFilePath02 = txtUpFilePath2.Text;
        //    string returnMsg = "";
        //    while (isUpFile)
        //    {
        //        string logName = DateTime.Now.ToString("yyyyMMdd") + ".log";
        //        string fullPath_ = CopyFileClient.getAppPath() + "\\Logs\\upfile_";
        //        string fullPath = fullPath_ + logName;
        //        if (!System.IO.File.Exists(fullPath))
        //        {
        //            System.IO.File.Create(fullPath);
        //        }
        //        if (!string.IsNullOrEmpty(_upFilePath01) && System.IO.Directory.Exists(_upFilePath01))
        //        {
        //            if (!loopUPFolder(_upFilePath01, ref returnMsg))
        //            {
        //                AppendText(returnMsg);
        //                isUpFile = false;
        //                break;
        //            }
        //            AppendText(returnMsg);
        //        }
        //        else
        //        {
        //            if (!string.IsNullOrEmpty(_upFilePath02) && System.IO.Directory.Exists(_upFilePath02))
        //            {
        //                if (!loopUPFolder(_upFilePath02, ref returnMsg))
        //                {
        //                    AppendText(returnMsg);
        //                    isUpFile = false;
        //                    break;
        //                }
        //                AppendText(returnMsg);
        //            }
        //            else
        //            {
        //                isUpFile = false;
        //                AppendText("上传路径为空或者不存在！");
        //                break;
        //            }
        //        }
        //        Thread.Sleep(1000 *  _FrequencyUp);
        //    }

        //}

        //private void startCopyFile(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    try
        //    {
        //        string _original_path;
        //        string returnMsg = "";
        //        string logName = DateTime.Now.ToString("yyyyMMdd") + ".log";
        //        string fullPath_ = CopyFileClient.getAppPath() + "\\Logs\\copyfile_";
        //        string fullPath = fullPath_ + logName;
        //        if (!System.IO.File.Exists(fullPath))
        //        {
        //            System.IO.File.Create(fullPath);
        //        }
        //        _original_path = txtCopyFilePath.Text.ToString();
                
        //        if (!TransferFiles.loopCopyFolder(_original_path, _copy_path, copyDay, ref returnMsg))
        //        {
        //            //AppendText("遍历复制文件夹【" + _original_path + "】开始\r\n");
        //            AppendText(returnMsg);
        //            //isCopyFile = false;
        //            //break;
        //        }
        //        AppendText(returnMsg);
        //        if (sclong.Connected)
        //        {
        //            //长连接存在则上传
        //            startUpFile1();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogHelper.WriteLog(ex.Message.ToString(), ex);
        //    }

        //}

        private void startCopyFile1()
        {
            string _original_path;
            while (isCopyFile)
            {
                string logName = DateTime.Now.ToString("yyyyMMdd") + ".log";
                string fullPath_ = CopyFileClient.getAppPath() + "\\Logs\\copyfile_";
                string fullPath = fullPath_ + logName;
                if (!System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Create(fullPath);
                }
                _original_path = txtCopyFilePath.Text.ToString();
                string returnMsg = "";
                if (!TransferFiles.loopCopyFolder(_original_path, _copy_path, copyDay,Fileisrelease, ref returnMsg))
                {
                    AppendText(returnMsg);
                    //isCopyFile = false;
                    break;
                }
                AppendText(returnMsg);
                if (sclong.Connected)
                {
                    //AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + "a");
                    //长连接存在则上传
                    startUpFile1();
                }
                Thread.Sleep(1000 *  _FrequencyCopy);
            }
        }
        /// <summary>
        /// 开始复制
        /// </summary>
        //private void startCopyFile()
        //{
        //    string _original_path;
        //    string returnMsg = "";
        //    while (isCopyFile)
        //    {
        //        string logName = DateTime.Now.ToString("yyyyMMdd") + ".log";
        //        string fullPath_ = CopyFileClient.getAppPath() + "\\Logs\\copyfile_";
        //        string fullPath = fullPath_ + logName;
        //        if (!System.IO.File.Exists(fullPath))
        //        {
        //            System.IO.File.Create(fullPath);
        //        }
        //        _original_path = txtCopyFilePath.Text.ToString();
        //        if (!TransferFiles.loopCopyFolder(_original_path, _copy_path, copyDay, ref returnMsg))
        //        {
        //            AppendText(returnMsg);
        //            isCopyFile = false;
        //            break;
        //        }
        //        AppendText(returnMsg);
        //        Thread.Sleep(1000 * _FrequencyCopy);
        //    }
        //}

        /// <summary>
        /// 用于复制完成后直接调用上传
        /// </summary>
        private void startUpFile1()
        {
            string _upFilePath01 = txtUpFilePath1.Text;
            string _upFilePath02 = txtUpFilePath2.Text;
            if ((!string.IsNullOrEmpty(_upFilePath01) && System.IO.Directory.Exists(_upFilePath01)) || (!string.IsNullOrEmpty(_upFilePath02) && System.IO.Directory.Exists(_upFilePath02)))
            {
                string returnMsg = "";
                //if (isUpFile)
                //{
                string logName = DateTime.Now.ToString("yyyyMMdd") + ".log";
                string fullPath_ = CopyFileClient.getAppPath() + "\\Logs\\upfile_";
                string fullPath = fullPath_ + logName;
                if (!System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Create(fullPath);
                }
                if (!string.IsNullOrEmpty(_upFilePath01) && System.IO.Directory.Exists(_upFilePath01))
                {
                    if (!loopUPFolder(_upFilePath01, ref returnMsg))
                    {
                        AppendText(returnMsg);
                        //isUpFile = false;
                    }
                    AppendText(returnMsg);
                }
                else
                {
                    if (!string.IsNullOrEmpty(_upFilePath02) && System.IO.Directory.Exists(_upFilePath02))
                    {
                        if (!loopUPFolder(_upFilePath02, ref returnMsg))
                        {
                            AppendText(returnMsg);
                            //isUpFile = false;
                        }
                        AppendText(returnMsg);
                    }
                    else
                    {
                        //isUpFile = false;
                        AppendText("上传路径为空或者不存在！");
                    }
                }
                //}
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
                    isDeleteFile = false;
                    break;
                }
                Thread.Sleep(1000 * 60 * 60 * _FrequencyDelete);
            }
        }
        /// <summary>
        /// 删除日志文件
        /// </summary>
        private void deleteLog()
        {

            string logFolder = getAppPath() + "\\Logs";
            //string logFolder = System.Environment.CurrentDirectory + "\\Logs";
            string fileName = "";
            int logDay = _LogDay * -1;
            //循环删除错误日志信息
            //循环删除错误日志信息
            List<string> folderList = new List<string>();
            //获取所有的文件夹列表
            TransferFiles.getFolderList(logFolder, ref folderList, 1);
            for (int _i = 0; _i < folderList.Count; _i++)
            {
                DirectoryInfo folder = new DirectoryInfo(folderList[_i]);
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
        }
        /// <summary>
        /// 定期删除备份的文件
        /// </summary>
        private void deleteFileBuck()
        {

            string logFolder = getAppPath() + "\\" + UpFilePath1;
            string fileName = "";
            int FileDay = _FileDay * -1;
            //循环删除错误日志信息
            List<string> folderList = new List<string>();
            //获取所有的文件夹列表
            TransferFiles.getFolderList(logFolder, ref folderList, 1);
            for (int _i = 0; _i < folderList.Count; _i++)
            {
                DirectoryInfo folder = new DirectoryInfo(folderList[_i]);
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
        /// 循环上传文件
        /// </summary>
        /// <param name="_UP_path"></param>
        /// <param name="returnMsg"></param>
        /// <returns></returns>
        public bool loopUPFolder(string _UP_path, ref string returnMsg)
        {
            try
            {
                bool isCopyOk;
                string fileName = "";
                string folderPath = "";
                DirectoryInfo folder;
                AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + "遍历上传文件夹【" + _UP_path + "】开始\r\n");
                //LogHelper.WriteLog("遍历上传文件夹【" + _UP_path + "】开始");
                List<string> folderList = new List<string>();
                //获取所有的文件夹列表
                TransferFiles.getFolderList(_UP_path, ref folderList, 1);
                for (int _i = 0; _i < folderList.Count; _i++)
                {
                    folder = new DirectoryInfo(folderList[_i]);
                    folderPath = folder.FullName.Replace(_UP_path, "");
                    //开始遍历---------------
                    foreach (FileInfo file in folder.GetFiles())
                    {
                        fileName = file.Name;
                        //开始check文件是否需要上传，根据大小、最后的写入时间判断
                        if (TransferFiles.CheckFileIsUp(folderPath + "\\" + fileName, file.CreationTime, file.LastWriteTime, file.Length, upDay))
                        {
                            //开始上传
                            isCopyOk = sendFile(file.FullName, folderPath);
                        }
                    }
                }
                returnMsg = DateTime.Now.ToString("yyyyMMddhh24missff") + "遍历上传文件夹【" + _UP_path + "】SUCCESS\r\n";
                //LogHelper.WriteLog("遍历上传文件夹【" + _UP_path + "】SUCCESS");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                returnMsg = DateTime.Now.ToString("yyyyMMddhh24missff") + "遍历上传文件夹【" + _UP_path + "】失败";
                return false;
            }
        }
        public static string getAppPath()
        {
            string startup = Application.ExecutablePath;       //取得程序路径   
            int pp = startup.LastIndexOf("\\");
            startup = startup.Substring(0, pp);
            return startup;
        }

        private void btnStartUp_Click(object sender, EventArgs e)
        {
            //if (InputPassword())
            //{
            //    startUpFileTh();
            //    this.txtUpFilePath1.Enabled = false;
            //    this.txtUpFilePath2.Enabled = false;
            //}
        }

        private void btnStopUp_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                stopUpFileTh();
                this.txtUpFilePath1.Enabled = true;
                this.txtUpFilePath1.Enabled = true;
            }
        }
        private void btnCopyFile_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                startCopyFileTh();
                this.txtCopyFilePath.Enabled = false;
                this.txtCopyFilePathTo.Enabled = false;
            }
        }
        private void txtCopyFilePathTo_TextChanged(object sender, EventArgs e)
        {
            this._copy_path = txtCopyFilePathTo.Text;
        }
        private void txtCopyFilePath_TextChanged(object sender, EventArgs e)
        {
            this._copyFilePath = txtCopyFilePath.Text;
        }

        private void txtServerIP_TextChanged(object sender, EventArgs e)
        {
            this.serverip = txtServerIP.Text.ToString();
        }
        private void btnStopCopyFile_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                stopCopyFileTh();
                this.txtCopyFilePath.Enabled = true;
                this.txtCopyFilePathTo.Enabled = true;
            }
        }
        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            this.port = int.Parse(txtPort.Text.ToString());
        }
        /// <summary> 
        /// 清空消息显示框
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            this.txtInfo.Text = "";
        }
        /// <summary> 
        /// 清空消息框
        /// </summary>
        private void button1_Click_1(object sender, EventArgs e)
        {
            this.txtMsg.Text = "";
        }
        private void btnCancelDebug_Click(object sender, EventArgs e)
        {
            inputPassword = "";
            AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + "取消DEBUG");
            //LogHelper.WriteLog("取消DEBUG");
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btn_enabled("1");
                this.ControlBox = false;
                if (createLongLink())//发送长连接
                {
                    Thread.Sleep(1000 * 5);
                    startCopyFileTh(); //开始复制
                    AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "启动复制");
                    if (isCopyFile)
                    {
                        //startUpFileTh(); //开始上传
                        //if (isUpFile)
                        //{
                        startDeleteFileTh();
                        if (isDeleteFile)
                        {
                            AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "启动删除");
                            startListenSclongTH();
                            this.txtName.Enabled = false;
                            this.txtServerIP.Enabled = false;
                            this.txtPort.Enabled = false;

                            this.txtCopyFilePath.Enabled = false;
                            this.txtCopyFilePathTo.Enabled = false;

                            this.txtUpFilePath1.Enabled = false;
                            this.txtUpFilePath2.Enabled = false;
                        }
                        else
                        {
                            stopCopyFileTh();
                            closeLongLink();
                            this.WindowState = FormWindowState.Normal;
                            this.Activate();
                            this.Show();
                        }
                        //}
                        //else
                        //{
                        //    stopCopyFileTh();
                        //    closeLongLink();
                        //    this.WindowState = FormWindowState.Normal;
                        //    this.Activate();
                        //    this.Show();
                        //}
                    }
                    else
                    {
                        closeLongLink();
                        this.WindowState = FormWindowState.Normal;
                        this.Activate();
                        this.Show();

                    }
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                    this.Show();
                    //Restart();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                AppendText(DateTime.Now.ToLongTimeString() + "----------监听开启失败------------");
                this.WindowState = FormWindowState.Normal;
                this.Activate();
                this.Show();
            }
            this.Hide();

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                closeLongLink();//断开连接并停止上传
                stopCopyFileTh();//停止复制
                stopDeleteFileTh();//停止删除
                stopListenSclongTH();

                this.txtPort.Enabled = true;
                this.txtServerIP.Enabled = true;
                this.txtName.Enabled = true;

                this.txtCopyFilePath.Enabled = true;
                this.txtCopyFilePathTo.Enabled = true;

                this.txtUpFilePath1.Enabled = true;
                this.txtUpFilePath1.Enabled = true;

            }
        }
        /// <summary> 
        /// 发送消息
        /// </summary>
        private void btn_sendMsg_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                if (this.txtMsg.Text == "")
                {
                    AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + "无消息可以发送");
                }
                else
                {
                    try
                    {
                        string data = "2";
                        string rec_data = "";
                        data += this.txtMsg.Text;
                        SocketClient sc = new SocketClient(serverip, port);
                        if (sc.Connected)
                        {
                            AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "发送消息" + data);
                            sc.send_msg(data, ref rec_data);
                            sc.disconn();
                            AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "接收消息" + rec_data);
                            this.txtMsg.Text = "";
                        }
                        else
                        {
                            AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "未连接到服务器【" + serverip + ":" + port + "】");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "发送消息失败");
                        LogHelper.WriteLog(ex.Message.ToString(), ex);
                    }

                }
            }
        }
        /// <summary> 
        /// 发送消息
        /// </summary>
        private void btn_ReqFromDelegate_Click(object sender, EventArgs e)
        {
            if (InputPassword())
            {
                this.txtMsg.Text = "";
                string msg = "<transactionid>" + DateTime.Now.ToString("yyyyMMddhh24missff") + "</transactionid>";
                string TransactionID = getAllHyperLinks(msg, "<transactionid>", "</transactionid>")[0].Value;
                string data = "<?xml version='1.0' encoding='utf-8'?><miap><miap-header><transactionid>" + TransactionID + "</transactionid><version>1.0</version><messagename>uploadreq</messagename></miap-header><miap-body><uploadreq>";
                data += "<R>TABLE_NAME|PART_TEST_ITEM_INFO_TEMPRecord_Id|4786Dispatch_No|321708070001Part_No|96216708Part_No_Id|11053731Part_Seq_No|BD1706280188FOperation_No|55MCH_Code|E0000046Test_Item_Code|1Test_Item_Collect_Value|Test_Item_Eigen_Value|Test_Item_Judge_Result|0Enter_User|adminEnter_Date|2017/8/15 10:31:50</R>";
                data += "<R>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa</R>";
                data += "<R>中文显示的问题测试；中文显示的问题测试中文显示的问题测试；中文显示的问题测试中文显示的问题测试中文显示的问题测试</R>";
                data += "</uploadreq></miap-body></miap>";
                this.txtMsg.AppendText(data);
                //string rec_date_ = "";
                ////_flag为1时，表示上报成功----------
                //int _flag = sc.send_msg(data, ref rec_date_);
                //AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + rec_date_);
                //sc.disconn();
            }
        }
        private void btn_enabled(string type_)
        {
            if (type_ == "1")
            {
                this.btnCancelDebug.Enabled = false;
                this.btnCopyFile.Enabled = false;
                this.btnELongLink.Enabled = false;
                this.btnSelect.Enabled = false;
                this.btnSendFile.Enabled = false;
                this.btnSLongLink.Enabled = false;
                this.btnStartUp.Enabled = false;
                this.btnStop.Enabled = false;
                this.btnStopUp.Enabled = false;
                this.btnStopCopyFile.Enabled = false;
                this.btn_ReqFromDelegate.Enabled = false;
                this.btn_sendMsg.Enabled = false;
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                //this.button3.Enabled = false;
                this.ControlBox = false;
            }
            else
            {
                this.btnCancelDebug.Enabled = true;
                this.btnCopyFile.Enabled = true;
                this.btnELongLink.Enabled = true;
                this.btnSelect.Enabled = true;
                this.btnSendFile.Enabled = true;
                this.btnSLongLink.Enabled = true;
                this.btnStartUp.Enabled = true;
                this.btnStop.Enabled = true;
                this.btnStopUp.Enabled = true;
                this.btnStopCopyFile.Enabled = true;
                this.btn_ReqFromDelegate.Enabled = true;
                this.btn_sendMsg.Enabled = true;
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                //this.button3.Enabled = true;
                this.ControlBox = true;
            }
        }
        private void CopyFileClient_Load(object sender, EventArgs e)
        {
            btn_enabled("1");
            if (createLongLink())//发送长连接
            {
                Thread.Sleep(1000 * 5);
                startCopyFileTh(); //开始复制
                AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "启动复制");
                if (isCopyFile)
                {
                    //startUpFileTh(); //开始上传
                    //if (isUpFile)
                    //{
                    startDeleteFileTh();
                    if (isDeleteFile)
                    {
                        AppendText(DateTime.Now.ToString("yyyyMMddhh24missff") + ":" + "启动删除");
                        startListenSclongTH();

                        this.txtName.Enabled = false;
                        this.txtServerIP.Enabled = false;
                        this.txtPort.Enabled = false;

                        this.txtCopyFilePath.Enabled = false;
                        this.txtCopyFilePathTo.Enabled = false;
                        this.btnStart.Enabled = false;

                        this.txtUpFilePath1.Enabled = false;
                        this.txtUpFilePath2.Enabled = false;
                    }
                    else
                    {
                        stopCopyFileTh();
                        closeLongLink();
                        this.WindowState = FormWindowState.Normal;
                        this.Activate();
                        this.Show();
                    }
                    //}
                    //else
                    //{
                    //    stopCopyFileTh();
                    //    closeLongLink();
                    //    this.WindowState = FormWindowState.Normal;
                    //    this.Activate();
                    //    this.Show();
                    //}
                }
                else
                {
                    closeLongLink();
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                    this.Show();

                }
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.Activate();
                this.Show();
                //Restart();
            }
        }
        private void Restart()
        {
            Application.ExitThread();
            Thread thtmp = new Thread(new ParameterizedThreadStart(run));

            object appName = Application.ExecutablePath;

            Thread.Sleep(2000);

            thtmp.Start(appName);

        }

        private void run(System.Object obj)

        {

            System.Diagnostics.Process ps = new System.Diagnostics.Process();

            ps.StartInfo.FileName = obj.ToString();

            ps.Start();

        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            //if (InputPassword())
            //{
            if (MessageBox.Show("你确定要退出程序吗？", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
            {
                notifyIcon1.Visible = false;
                this.Close();
                this.Dispose();
                Application.Exit();
            }
            //}
        }

        private void hideMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void showMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
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

        private void button2_Click_1(object sender, EventArgs e)
        {
            Restart();
        }
    }
}