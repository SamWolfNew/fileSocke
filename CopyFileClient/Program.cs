using System;
using System.Windows.Forms;

namespace CopyFileClient
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            string name = "CopyFileClient";
            if (GetPidByProcessName(name) > 1)
            {
                LogHelper.WriteLog("程序关闭");
                Application.Exit();
                return;
            }
            log4net.Config.XmlConfigurator.Configure();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CopyFileClient());
        }
        public static int GetPidByProcessName(string processName)
        {
            int count_ = 0;
            System.Diagnostics.Process[] arrayProcess = System.Diagnostics.Process.GetProcessesByName(processName);

            foreach (System.Diagnostics.Process p in arrayProcess)
            {
                LogHelper.WriteLog("已经存在进程"+p.Id);
                count_ = count_ + 1;
            }
            return count_;
        }
    }
}
