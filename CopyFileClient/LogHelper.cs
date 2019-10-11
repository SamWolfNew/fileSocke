using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyFileClient
{
    public class LogHelper
    {
        public static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("loginfo");
        public static readonly log4net.ILog logerror = log4net.LogManager.GetLogger("logerror");
        public static readonly log4net.ILog logfile = log4net.LogManager.GetLogger("copyfileloginfo");
        public static readonly log4net.ILog logupfile = log4net.LogManager.GetLogger("upfileloginfo");
        public static void WriteLog(string info)
        {
            if (loginfo.IsInfoEnabled)
            {
                loginfo.Info(info);
            }
        }
        public static void WriteFileLog(string info)
        {
            if (logfile.IsInfoEnabled)
            {
                logfile.Info(info);
            }
        }
        public static void WriteUpFileLog(string info)
        {
            if (logupfile.IsInfoEnabled)
            {
                logupfile.Info(info);
            }
        }

        public static void WriteLog(string info, Exception ex)
        {
            if (logerror.IsErrorEnabled)
            {
                logerror.Error(info, ex);
            }
        }
    }
}
