using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace CopyFileClient
{
    class TransferFiles
    {
        /// <summary> 
        /// 发送文件
        /// </summary>
        public static bool sendFile(SocketClient client, string fullPath, string clientName, string Relative_path)
        {
            //创建一个文件对象
            FileInfo EzoneFile = new FileInfo(fullPath);
            //打开文件流
            FileStream EzoneStream = EzoneFile.OpenRead();
            try
            {
                String rec_data_ = "NO";
                string send_xml = "";
                send_xml = EzoneFile.Name + "|" + EzoneStream.Length + "|" + EzoneFile.CreationTime + "|" + EzoneFile.LastWriteTime + "|" + clientName + "|" + Relative_path;
                //发送[文件名]到客户端
                client.send_msg("1" + send_xml, ref rec_data_);
                //发送[包的大小]到客户端
                if (rec_data_ == "OK")
                {
                    bool isCut = true;
                    byte[] data = new byte[EzoneStream.Length];
                    EzoneStream.Read(data, 0, int.Parse(EzoneStream.Length.ToString()));
                    if (client.send_msg(data, ref rec_data_) < 0)
                    {
                        isCut = false;
                    }
                    if (isCut)
                    {
                        //发送完成，发送结束标识
                        client.send_msg("SendFileEnd", ref rec_data_);
                    }
                    LogHelper.WriteLog(EzoneFile.Name + "发送：" + rec_data_);
                    if (rec_data_ == "OK")
                    {
                        //将结果写入日志
                        string _fileLog = Relative_path + "\\" + EzoneFile.Name + "|" + EzoneFile.CreationTime + "|" + EzoneFile.LastAccessTime + "|" + EzoneFile.LastWriteTime + "|" + EzoneFile.Length + "|" + DateTime.Now.ToString();
                        LogHelper.WriteUpFileLog(_fileLog);
                    }
                    else
                    {
                        //LogHelper.WriteLog(rec_data_);
                    }

                    //关闭文件流
                    EzoneStream.Dispose();
                    EzoneStream.Close();
                    return isCut;
                }
                return true;

            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                if (EzoneStream != null)
                {
                    //关闭文件流
                    EzoneStream.Dispose();
                    EzoneStream.Close();
                }
                return false;
            }
        }
        /// <summary>
        /// 复制文件
        /// </summary>
        public static bool copyFile(string _original_path, string Relative_path, string _copy_path, string _file_name)
        {
            try
            {
                if (false == System.IO.Directory.Exists(_copy_path + Relative_path))
                {
                    //创建pic文件夹
                    System.IO.Directory.CreateDirectory(_copy_path + Relative_path);
                }
                //直接复制文件
                System.IO.File.Copy(_original_path + "\\" + _file_name, _copy_path + Relative_path + "\\" + _file_name, true);
                FileInfo _file1 = new FileInfo(_original_path + "\\" + _file_name);
                FileInfo _file = new FileInfo(_copy_path + Relative_path + "\\" + _file_name);
                _file.CreationTime = _file1.CreationTime;
                //将结果写入日志
                string _fileLog = Relative_path + "\\" + _file_name + "|" + _file.CreationTime + "|" + _file.LastAccessTime + "|" + _file.LastWriteTime + "|" + _file.Length + "|" + DateTime.Now.ToString() + "|" + _file1.CreationTime;
                LogHelper.WriteFileLog(_fileLog);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return false;
            }
        }
        /// <summary>
        /// 遍历并复制文件
        /// </summary>
        /// <param name="_original_path">复制前路径</param>
        /// <param name="_copy_path">复制后路径</param>
        /// <param name="returnMsg">返回消息</param>
        /// <returns></returns>
        public static bool loopCopyFolder(string _original_path, string _copy_path, int copyDay, string Fileisrelease, ref string returnMsg)
        {
            try
            {
                bool isCopyOk;
                string fileName = "";
                string folderPath = "";
                //returnMsg = "遍历复制文件夹【" + _original_path + "】开始\r\n";
                List<string> folderList = new List<string>();
                //获取所有的文件夹列表
                getFolderList(_original_path, ref folderList, 1);
                for (int _i = 0; _i < folderList.Count; _i++)
                {
                    DirectoryInfo folder = new DirectoryInfo(folderList[_i]);
                    folderPath = folder.FullName.Replace(_original_path, "");
                    //开始遍历---------------
                    foreach (FileInfo file in folder.GetFiles())
                    {
                        fileName = file.Name;
                        if (Fileisrelease == "0")
                        {
                            //只判断文件的创建时间
                            if (file.CreationTime > DateTime.Now.AddDays(copyDay))
                            {
                                //开始复制
                                isCopyOk = copyFile(folderList[_i], folderPath, _copy_path, fileName);
                                if (isCopyOk)
                                {
                                    returnMsg += DateTime.Now.ToString("yyyyMMddhh24missff") + "文件【" + fileName + "】复制成功！\r\n";
                                }
                                else
                                {
                                    returnMsg += DateTime.Now.ToString("yyyyMMddhh24missff") + "文件【" + fileName + "】复制失败！\r\n";
                                }
                            }
                        }
                        else
                        {
                            //开始check文件是否需要复制，根据大小、最后的写入时间判断
                            if (CheckFileIsCopy(folderPath + "\\" + fileName, file.CreationTime, file.LastWriteTime, file.Length, copyDay))
                            {
                                //开始复制
                                isCopyOk = copyFile(folderList[_i], folderPath, _copy_path, fileName);
                                if (isCopyOk)
                                {
                                    returnMsg += DateTime.Now.ToString("yyyyMMddhh24missff") + "文件【" + fileName + "】复制成功！\r\n";
                                }
                                else
                                {
                                    returnMsg += DateTime.Now.ToString("yyyyMMddhh24missff") + "文件【" + fileName + "】复制失败！\r\n";
                                }
                            }
                        }
                    }
                }
                returnMsg += DateTime.Now.ToString("yyyyMMddhh24missff") + "遍历复制文件夹【" + _original_path + "】SUCCESS\r\n";
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                returnMsg += DateTime.Now.ToString("yyyyMMddhh24missff") + "遍历复制文件夹【" + _original_path + "】失败";
                return false;
            }
        }
        /// <summary>
        /// 获取文件夹下所有的文件夹列表
        /// </summary>
        /// <param name="_original_path">遍历的文件夹</param>
        /// <param name="folderList">文件夹列表</param>
        /// <param name="haveFile">是否有文件</param>
        public static void getFolderList(string _original_path, ref List<string> folderList, int haveFile)
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
        /// 判断文件是否可以 复制
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="lastWriteTime">最后的写入时间</param>
        /// <param name="size">文件大小</param>
        /// <returns></returns>
        public static bool CheckFileIsCopy(string fileName, DateTime createTime, DateTime lastWriteTime, long size, int copyDay)
        {
            try
            {
                //int copyDay = int.Parse(System.Configuration.ConfigurationSettings.AppSettings["CopyDay"]) * -1;
                string returnMsg = "";
                string[] fileInfo;
                string logName = DateTime.Now.ToString("yyyyMMdd") + ".log";
                string fullPath_ = CopyFileClient.getAppPath() + "\\Logs\\copyfile_";
                string fullPath = fullPath_ + logName;
                //我们只复制配置时间内的文件
                if (createTime > DateTime.Now.AddDays(copyDay) || lastWriteTime > DateTime.Now.AddDays(copyDay))
                {
                    for (int i_ = 0; i_ >= copyDay; i_--)
                    {
                        logName = DateTime.Now.AddDays(i_).ToString("yyyyMMdd") + ".log";
                        fullPath = fullPath_ + logName;
                        if (FindFileContent(fileName, fullPath, ref returnMsg))
                        {
                            if (returnMsg != "NULL")
                            {
                                fileInfo = returnMsg.Split('|');
                                //当大小变化时复制
                                if (int.Parse(fileInfo[4]) != size)
                                {
                                    return true;
                                }
                                //当最后最后的修改时间变化时复制
                                if (fileInfo[3] != lastWriteTime.ToString())
                                {
                                    return true;
                                }
                                //表示没有变化，不用复制
                                return false;
                            }
                        }
                        logName = DateTime.Now.AddDays(i_).ToString("yyyyMMdd") + ".logcopyfile_" + DateTime.Now.AddDays(i_).ToString("yyyyMMdd") + ".log";
                        fullPath = fullPath_ + logName;
                        if (System.IO.File.Exists(fullPath))
                        {
                            if (FindFileContent(fileName, fullPath, ref returnMsg))
                            {
                                if (returnMsg != "NULL")
                                {
                                    fileInfo = returnMsg.Split('|');
                                    //当大小变化时复制
                                    if (int.Parse(fileInfo[4]) != size)
                                    {
                                        return true;
                                    }
                                    //当最后最后的修改时间变化时复制
                                    if (fileInfo[3] != lastWriteTime.ToString())
                                    {
                                        return true;
                                    }
                                    //表示没有变化，不用复制
                                    return false;
                                }
                            }
                        }
                    }
                    if (returnMsg == "NULL")
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return false;
            }
        }
        /// <summary>
        /// 判断文件是否可以上传
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="lastWriteTime">最后的写入时间</param>
        /// <param name="size">文件大小</param>
        /// <returns></returns>
        public static bool CheckFileIsUp(string fileName, DateTime createTime, DateTime lastWriteTime, long size, int upDay)
        {
            try
            {
                //int upDay = int.Parse(System.Configuration.ConfigurationSettings.AppSettings["UpDay"]) * -1;
                string returnMsg = "";
                string[] fileInfo;
                string logName = DateTime.Now.ToString("yyyyMMdd") + ".log";
                string fullPath_ = CopyFileClient.getAppPath() + "\\Logs\\upfile_";
                string fullPath = fullPath_ + logName;
                //我们只复制配置时间内的文件
                if (createTime > DateTime.Now.AddDays(upDay) || lastWriteTime > DateTime.Now.AddDays(upDay))
                {
                    for (int i_ = 0; i_ >= upDay; i_--)
                    {
                        logName = DateTime.Now.AddDays(i_).ToString("yyyyMMdd") + ".log";
                        fullPath = fullPath_ + logName;
                        if (FindFileContent(fileName, fullPath, ref returnMsg))
                        {
                            if (returnMsg != "NULL")
                            {
                                fileInfo = returnMsg.Split('|');
                                //当大小变化时复制
                                if (int.Parse(fileInfo[4]) != size)
                                {
                                    return true;
                                }
                                //当最后最后的修改时间变化时复制
                                if (fileInfo[3] != lastWriteTime.ToString())
                                {
                                    return true;
                                }
                                //表示没有变化，不用复制
                                return false;
                            }
                        }
                        logName = DateTime.Now.AddDays(i_).ToString("yyyyMMdd") + ".logupfile_" + DateTime.Now.AddDays(i_).ToString("yyyyMMdd") + ".log";
                        fullPath = fullPath_ + logName;
                        if (System.IO.File.Exists(fullPath))
                        {
                            if (FindFileContent(fileName, fullPath, ref returnMsg))
                            {
                                if (returnMsg != "NULL")
                                {
                                    fileInfo = returnMsg.Split('|');
                                    //当大小变化时复制
                                    if (int.Parse(fileInfo[4]) != size)
                                    {
                                        return true;
                                    }
                                    //当最后最后的修改时间变化时复制
                                    if (fileInfo[3] != lastWriteTime.ToString())
                                    {
                                        return true;
                                    }
                                    //表示没有变化，不用复制
                                    return false;
                                }
                            }
                        }
                    }
                    if (returnMsg == "NULL")
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return false;
            }
        }
        /// <summary>
        /// 查找字符串在文件中的位置行并返回行
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="returnLine"></param>
        /// <returns></returns>
        public static bool FindFileContent(string fileName, string fullPath, ref string returnLine)
        {
            FileStream fs = null;
            StreamReader sr = null;
            try
            {
                if (System.IO.File.Exists(fullPath))
                {
                    returnLine = "NULL";
                    fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (fs.Length > 0)
                    {
                        sr = new StreamReader(fs, System.Text.Encoding.Default);
                        string content = "";
                        while (!sr.EndOfStream)
                        {
                            content = sr.ReadLine();
                            if (content.Contains(fileName))
                            {
                                returnLine = content;
                            }
                        }
                    }
                }
                else
                {
                    System.IO.File.Create(fullPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                if (sr != null)
                {
                    sr.Dispose();
                    sr.Close();
                }
                if (fs != null)
                {
                    fs.Dispose();
                    fs.Close();
                }
                return false;
            }
            finally
            {
                if (sr != null)
                {
                    sr.Dispose();
                    sr.Close();
                }
                if (fs != null)
                {
                    fs.Dispose();
                    fs.Close();
                }
            }
        }
    }
}
