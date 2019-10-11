namespace CopyFileClient
{
    partial class CopyFileClient
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CopyFileClient));
            this.btnSLongLink = new System.Windows.Forms.Button();
            this.btn_ReqFromDelegate = new System.Windows.Forms.Button();
            this.txtInfo = new System.Windows.Forms.TextBox();
            this.btnELongLink = new System.Windows.Forms.Button();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btnSelect = new System.Windows.Forms.Button();
            this.btnSendFile = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.txtServerIP = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.btn_sendMsg = new System.Windows.Forms.Button();
            this.txtMsg = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtCopyFilePath = new System.Windows.Forms.TextBox();
            this.btnCopyFile = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtCopyFilePathTo = new System.Windows.Forms.TextBox();
            this.btnStopCopyFile = new System.Windows.Forms.Button();
            this.txtUpFilePath1 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtUpFilePath2 = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnStartUp = new System.Windows.Forms.Button();
            this.btnStopUp = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnCancelDebug = new System.Windows.Forms.Button();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hideMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.button2 = new System.Windows.Forms.Button();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSLongLink
            // 
            this.btnSLongLink.Location = new System.Drawing.Point(501, 4);
            this.btnSLongLink.Name = "btnSLongLink";
            this.btnSLongLink.Size = new System.Drawing.Size(75, 23);
            this.btnSLongLink.TabIndex = 0;
            this.btnSLongLink.Text = "开始连接";
            this.btnSLongLink.UseVisualStyleBackColor = true;
            this.btnSLongLink.Click += new System.EventHandler(this.btn_ReqFromServer_Click);
            // 
            // btn_ReqFromDelegate
            // 
            this.btn_ReqFromDelegate.Location = new System.Drawing.Point(513, 293);
            this.btn_ReqFromDelegate.Name = "btn_ReqFromDelegate";
            this.btn_ReqFromDelegate.Size = new System.Drawing.Size(75, 23);
            this.btn_ReqFromDelegate.TabIndex = 2;
            this.btn_ReqFromDelegate.Text = "生成消息";
            this.btn_ReqFromDelegate.UseVisualStyleBackColor = true;
            this.btn_ReqFromDelegate.Click += new System.EventHandler(this.btn_ReqFromDelegate_Click);
            // 
            // txtInfo
            // 
            this.txtInfo.Location = new System.Drawing.Point(12, 29);
            this.txtInfo.Multiline = true;
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtInfo.Size = new System.Drawing.Size(818, 152);
            this.txtInfo.TabIndex = 5;
            this.txtInfo.WordWrap = false;
            // 
            // btnELongLink
            // 
            this.btnELongLink.Location = new System.Drawing.Point(582, 4);
            this.btnELongLink.Name = "btnELongLink";
            this.btnELongLink.Size = new System.Drawing.Size(75, 23);
            this.btnELongLink.TabIndex = 5;
            this.btnELongLink.Text = "断开连接";
            this.btnELongLink.UseVisualStyleBackColor = true;
            this.btnELongLink.Click += new System.EventHandler(this.button2_Click);
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(12, 188);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(658, 21);
            this.txtPath.TabIndex = 6;
            // 
            // btnSelect
            // 
            this.btnSelect.Location = new System.Drawing.Point(676, 188);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(75, 23);
            this.btnSelect.TabIndex = 7;
            this.btnSelect.Text = "选择文件";
            this.btnSelect.UseVisualStyleBackColor = true;
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // btnSendFile
            // 
            this.btnSendFile.Location = new System.Drawing.Point(755, 187);
            this.btnSendFile.Name = "btnSendFile";
            this.btnSendFile.Size = new System.Drawing.Size(75, 23);
            this.btnSendFile.TabIndex = 7;
            this.btnSendFile.Text = "发送文件";
            this.btnSendFile.UseVisualStyleBackColor = true;
            this.btnSendFile.Click += new System.EventHandler(this.btnSendFile_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 12);
            this.label1.TabIndex = 8;
            this.label1.Text = "ServerIp:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(218, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 12);
            this.label2.TabIndex = 9;
            this.label2.Text = "PORT:";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(259, 6);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(94, 21);
            this.txtPort.TabIndex = 10;
            this.txtPort.TextChanged += new System.EventHandler(this.txtPort_TextChanged);
            // 
            // txtServerIP
            // 
            this.txtServerIP.Location = new System.Drawing.Point(77, 6);
            this.txtServerIP.Name = "txtServerIP";
            this.txtServerIP.Size = new System.Drawing.Size(135, 21);
            this.txtServerIP.TabIndex = 10;
            this.txtServerIP.TextChanged += new System.EventHandler(this.txtServerIP_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(365, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 12);
            this.label3.TabIndex = 9;
            this.label3.Text = "NAME:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(401, 6);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(94, 21);
            this.txtName.TabIndex = 10;
            this.txtName.TextChanged += new System.EventHandler(this.txtPort_TextChanged);
            // 
            // btn_sendMsg
            // 
            this.btn_sendMsg.Location = new System.Drawing.Point(675, 293);
            this.btn_sendMsg.Name = "btn_sendMsg";
            this.btn_sendMsg.Size = new System.Drawing.Size(75, 23);
            this.btn_sendMsg.TabIndex = 2;
            this.btn_sendMsg.Text = "发送";
            this.btn_sendMsg.UseVisualStyleBackColor = true;
            this.btn_sendMsg.Click += new System.EventHandler(this.btn_sendMsg_Click);
            // 
            // txtMsg
            // 
            this.txtMsg.Location = new System.Drawing.Point(14, 215);
            this.txtMsg.Multiline = true;
            this.txtMsg.Name = "txtMsg";
            this.txtMsg.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtMsg.Size = new System.Drawing.Size(816, 70);
            this.txtMsg.TabIndex = 5;
            this.txtMsg.WordWrap = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(594, 293);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "清空消息";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(432, 293);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "清空显示";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 302);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(107, 12);
            this.label4.TabIndex = 8;
            this.label4.Text = "文件复制路径From:";
            // 
            // txtCopyFilePath
            // 
            this.txtCopyFilePath.Location = new System.Drawing.Point(14, 320);
            this.txtCopyFilePath.Name = "txtCopyFilePath";
            this.txtCopyFilePath.Size = new System.Drawing.Size(736, 21);
            this.txtCopyFilePath.TabIndex = 11;
            this.txtCopyFilePath.TextChanged += new System.EventHandler(this.txtCopyFilePath_TextChanged);
            // 
            // btnCopyFile
            // 
            this.btnCopyFile.Location = new System.Drawing.Point(756, 320);
            this.btnCopyFile.Name = "btnCopyFile";
            this.btnCopyFile.Size = new System.Drawing.Size(75, 23);
            this.btnCopyFile.TabIndex = 2;
            this.btnCopyFile.Text = "开始复制";
            this.btnCopyFile.UseVisualStyleBackColor = true;
            this.btnCopyFile.Click += new System.EventHandler(this.btnCopyFile_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 349);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(95, 12);
            this.label5.TabIndex = 8;
            this.label5.Text = "文件复制路径to:";
            // 
            // txtCopyFilePathTo
            // 
            this.txtCopyFilePathTo.Location = new System.Drawing.Point(14, 367);
            this.txtCopyFilePathTo.Name = "txtCopyFilePathTo";
            this.txtCopyFilePathTo.Size = new System.Drawing.Size(736, 21);
            this.txtCopyFilePathTo.TabIndex = 11;
            this.txtCopyFilePathTo.TextChanged += new System.EventHandler(this.txtCopyFilePathTo_TextChanged);
            // 
            // btnStopCopyFile
            // 
            this.btnStopCopyFile.Location = new System.Drawing.Point(756, 365);
            this.btnStopCopyFile.Name = "btnStopCopyFile";
            this.btnStopCopyFile.Size = new System.Drawing.Size(75, 23);
            this.btnStopCopyFile.TabIndex = 2;
            this.btnStopCopyFile.Text = "停止复制";
            this.btnStopCopyFile.UseVisualStyleBackColor = true;
            this.btnStopCopyFile.Click += new System.EventHandler(this.btnStopCopyFile_Click);
            // 
            // txtUpFilePath1
            // 
            this.txtUpFilePath1.Location = new System.Drawing.Point(16, 412);
            this.txtUpFilePath1.Name = "txtUpFilePath1";
            this.txtUpFilePath1.Size = new System.Drawing.Size(734, 21);
            this.txtUpFilePath1.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 394);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(89, 12);
            this.label6.TabIndex = 12;
            this.label6.Text = "文件上传路径1:";
            // 
            // txtUpFilePath2
            // 
            this.txtUpFilePath2.Location = new System.Drawing.Point(16, 450);
            this.txtUpFilePath2.Name = "txtUpFilePath2";
            this.txtUpFilePath2.Size = new System.Drawing.Size(734, 21);
            this.txtUpFilePath2.TabIndex = 15;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 435);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(89, 12);
            this.label7.TabIndex = 14;
            this.label7.Text = "文件上传路径2:";
            // 
            // btnStartUp
            // 
            this.btnStartUp.Location = new System.Drawing.Point(756, 410);
            this.btnStartUp.Name = "btnStartUp";
            this.btnStartUp.Size = new System.Drawing.Size(75, 23);
            this.btnStartUp.TabIndex = 16;
            this.btnStartUp.Text = "开始上传";
            this.btnStartUp.UseVisualStyleBackColor = true;
            this.btnStartUp.Click += new System.EventHandler(this.btnStartUp_Click);
            // 
            // btnStopUp
            // 
            this.btnStopUp.Location = new System.Drawing.Point(756, 448);
            this.btnStopUp.Name = "btnStopUp";
            this.btnStopUp.Size = new System.Drawing.Size(75, 23);
            this.btnStopUp.TabIndex = 17;
            this.btnStopUp.Text = "停止上传";
            this.btnStopUp.UseVisualStyleBackColor = true;
            this.btnStopUp.Click += new System.EventHandler(this.btnStopUp_Click);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(755, 4);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 18;
            this.btnStart.Text = "启动";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(672, 4);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 19;
            this.btnStop.Text = "断开";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnCancelDebug
            // 
            this.btnCancelDebug.Location = new System.Drawing.Point(756, 293);
            this.btnCancelDebug.Name = "btnCancelDebug";
            this.btnCancelDebug.Size = new System.Drawing.Size(75, 23);
            this.btnCancelDebug.TabIndex = 20;
            this.btnCancelDebug.Text = "取消DEBUG";
            this.btnCancelDebug.UseVisualStyleBackColor = true;
            this.btnCancelDebug.Click += new System.EventHandler(this.btnCancelDebug_Click);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "COPY FILE CLIENT";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitMenuItem,
            this.hideMenuItem,
            this.showMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(101, 70);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(100, 22);
            this.exitMenuItem.Text = "退出";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // hideMenuItem
            // 
            this.hideMenuItem.Name = "hideMenuItem";
            this.hideMenuItem.Size = new System.Drawing.Size(100, 22);
            this.hideMenuItem.Text = "隐藏";
            this.hideMenuItem.Click += new System.EventHandler(this.hideMenuItem_Click);
            // 
            // showMenuItem
            // 
            this.showMenuItem.Name = "showMenuItem";
            this.showMenuItem.Size = new System.Drawing.Size(100, 22);
            this.showMenuItem.Text = "显示";
            this.showMenuItem.Click += new System.EventHandler(this.showMenuItem_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(355, 293);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(73, 22);
            this.button2.TabIndex = 21;
            this.button2.Text = "重启";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Visible = false;
            this.button2.Click += new System.EventHandler(this.button2_Click_1);
            // 
            // CopyFileClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(837, 474);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.btnCancelDebug);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnStopUp);
            this.Controls.Add(this.btnStartUp);
            this.Controls.Add(this.txtUpFilePath2);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtUpFilePath1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtCopyFilePathTo);
            this.Controls.Add(this.txtCopyFilePath);
            this.Controls.Add(this.txtMsg);
            this.Controls.Add(this.txtServerIP);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSendFile);
            this.Controls.Add(this.btnSelect);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.btnELongLink);
            this.Controls.Add(this.txtInfo);
            this.Controls.Add(this.btnStopCopyFile);
            this.Controls.Add(this.btnCopyFile);
            this.Controls.Add(this.btn_sendMsg);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.btn_ReqFromDelegate);
            this.Controls.Add(this.btnSLongLink);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CopyFileClient";
            this.ShowInTaskbar = false;
            this.Text = "CopyFileClient";
            this.Load += new System.EventHandler(this.CopyFileClient_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSLongLink;
        private System.Windows.Forms.Button btn_ReqFromDelegate;
        private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.Button btnELongLink;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Button btnSendFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.TextBox txtServerIP;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Button btn_sendMsg;
        private System.Windows.Forms.TextBox txtMsg;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtCopyFilePath;
        private System.Windows.Forms.Button btnCopyFile;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtCopyFilePathTo;
        private System.Windows.Forms.Button btnStopCopyFile;
        private System.Windows.Forms.TextBox txtUpFilePath1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtUpFilePath2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnStartUp;
        private System.Windows.Forms.Button btnStopUp;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnCancelDebug;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hideMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showMenuItem;
        private System.Windows.Forms.Button button2;
    }
}

