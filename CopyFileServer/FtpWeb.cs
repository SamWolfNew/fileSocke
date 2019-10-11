using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CopyFileServer
{
    class FtpWeb
    {
        string ftpServerIP;
        string ftpRemotePath;
        string ftpUserID;
        string ftpPassword;
        string ftpURI;

        public string FtpURI
        {
            get
            {
                return ftpURI;
            }

            set
            {
                ftpURI = value;
            }
        }

        /// <summary>
        /// 连接FTP
        /// </summary>
        /// <param name="FtpServerIP">FTP连接地址</param>
        /// <param name="FtpRemotePath">指定FTP连接成功后的当前目录, 如果不指定即默认为根目录</param>
        /// <param name="FtpUserID">用户名</param>
        /// <param name="FtpPassword">密码</param>
        public FtpWeb(string FtpServerIP, string FtpRemotePath, string FtpUserID, string FtpPassword)
        {
            ftpServerIP = FtpServerIP;
            ftpRemotePath = FtpRemotePath;
            ftpUserID = FtpUserID;
            ftpPassword = FtpPassword;
            ftpURI = "ftp://" + ftpServerIP + "/" + ftpRemotePath + "/";
        }
        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public string getUpLoadPath(string clientName,DateTime dt,string RELATIVE_PATH)
        {
            string path = "";
            if (!DirectoryExist(clientName))
            {
                MakeDir(clientName);
                LogHelper.WriteLog("createFolder:"+ clientName);
            }
            if (!DirectoryExist(dt.ToString("yyyy"), clientName))
            {
                MakeDir(clientName + "/" + dt.ToString("yyyy"));
                LogHelper.WriteLog("createFolder:" + clientName + "/" + dt.ToString("yyyy"));
            }
            if (!DirectoryExist(dt.ToString("MM"), clientName + "/" + dt.ToString("yyyy") + "/"))
            {
                MakeDir(clientName + "/" + dt.ToString("yyyy") + "/" + dt.ToString("MM"));
                LogHelper.WriteLog("createFolder:" + clientName + "/" + dt.ToString("yyyy") + "/" + dt.ToString("MM"));
            }
            if (!DirectoryExist(dt.ToString("dd"), clientName + "/" + dt.ToString("yyyy") + "/" + dt.ToString("MM") + "/"))
            {
                MakeDir(clientName + "/" + dt.ToString("yyyy") + "/" + dt.ToString("MM") + "/" + dt.ToString("dd"));
                LogHelper.WriteLog("createFolder:" + clientName + "/" + dt.ToString("yyyy") + "/" + dt.ToString("MM") + "/" + dt.ToString("dd"));
            }
            if (!string.IsNullOrEmpty(RELATIVE_PATH))
            {
                RELATIVE_PATH = RELATIVE_PATH.Replace("\\","/");
                RELATIVE_PATH = RELATIVE_PATH.Substring(1);
                string _path = clientName + "/" + dt.ToString("yyyy") + "/" + dt.ToString("MM") + "/" + dt.ToString("dd") + "/";
                string[] pathList = RELATIVE_PATH.Split('/');
                string tmpe = "";
                for(int i = 0;i<pathList.Length;i++){
                    if (!string.IsNullOrEmpty(pathList[i]))
                    {
                        if(!DirectoryExist(pathList[i], _path + tmpe))
                        {
                            MakeDir(_path + tmpe + pathList[i] );
                            LogHelper.WriteLog("createFolder:" + _path + tmpe + pathList[i]);
                        }
                        tmpe += pathList[i] + "/";
                    }
                }
                //if (!DirectoryExist( RELATIVE_PATH, _path))
                //{
                //    MakeDir(clientName + "/" + dt.ToString("yyyy") + "/" + dt.ToString("MM") + "/" + dt.ToString("dd") + "/" + RELATIVE_PATH);
                //}
                path = _path +  RELATIVE_PATH+"/";
            }
            else
            {
                path = clientName + "/" + dt.ToString("yyyy") + "/" + dt.ToString("MM") + "/" + dt.ToString("dd") + "/";
            }
            return path;
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        public  void copyFile(string _original_path, string Relative_path)
        {
            try
            {
                //if (false == System.IO.Directory.Exists(Relative_path))
                //{
                //    //创建pic文件夹
                //    System.IO.Directory.CreateDirectory(Relative_path);
                //}
                //直接复制文件
                System.IO.File.Copy(_original_path, Relative_path, true);
                //将结果写入日志
                string _fileLog ="文件复制from:"+_original_path+"--to:"+ Relative_path;
                LogHelper.WriteLog(_fileLog);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
            }
        }

        /// <summary>
        /// 先复制后上传
        /// </summary>
        /// <param name="filename"></param>
        public bool Upload(string filename, string clientName,  string up_path_, ref string Relative_Path)
        {
            try
            {
                
                FileInfo fileInf1 = new FileInfo(filename);
                string upPath = getUpLoadPath(clientName, fileInf1.CreationTime, Relative_Path);
                Relative_Path = upPath;
                copyFile(filename, up_path_ + "\\" + fileInf1.Name);
                //获取上传路径---------------
                FileInfo fileInf = new FileInfo(up_path_+"\\"+ fileInf1.Name);

                string uri = ftpURI + upPath + fileInf.Name;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
                reqFTP.UseBinary = true;
                reqFTP.ContentLength = fileInf.Length;
                int buffLength = 2048;
                byte[] buff = new byte[buffLength];
                int contentLen;
                FileStream fs = fileInf.OpenRead();
                System.IO.Stream strm = reqFTP.GetRequestStream();
                try
                {
                    contentLen = fs.Read(buff, 0, buffLength);
                    while (contentLen != 0)
                    {
                        strm.Write(buff, 0, contentLen);
                        contentLen = fs.Read(buff, 0, buffLength);
                    }
                    strm.Dispose();
                    fs.Dispose();
                    strm.Close();
                    fs.Close();
                    //删除复制文件-----------------
                    //fileInf.Delete();
                    return true;
                }
                catch (Exception ex)
                {
                    if (strm != null)
                    {
                        strm.Dispose();
                        strm.Close();
                    }
                    if (fs != null)
                    {
                        fs.Dispose();
                        fs.Close();
                    }
                    //throw new Exception(ex.Message);
                    LogHelper.WriteLog(ex.Message.ToString(), ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(filename + "|" + clientName, ex);
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return false;
            }
        }

        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="filename"></param>
        public bool Upload(string filename,string clientName,ref string Relative_Path)
        {
            try
            {
                FileInfo fileInf = new FileInfo(filename);
                string upPath = getUpLoadPath(clientName, fileInf.CreationTime, Relative_Path);
                Relative_Path = upPath;
                string uri = ftpURI + upPath + fileInf.Name;
                FtpWebRequest reqFTP;

                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
                reqFTP.UseBinary = true;
                reqFTP.ContentLength = fileInf.Length;
                int buffLength = 2048;
                byte[] buff = new byte[buffLength];
                int contentLen;
                FileStream fs = fileInf.OpenRead();
                System.IO.Stream strm = reqFTP.GetRequestStream();
                try
                {
                    contentLen = fs.Read(buff, 0, buffLength);
                    while (contentLen != 0)
                    {
                        strm.Write(buff, 0, contentLen);
                        contentLen = fs.Read(buff, 0, buffLength);
                    }
                    strm.Dispose();
                    fs.Dispose();
                    strm.Close();
                    fs.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    if (strm != null)
                    {
                        strm.Dispose();
                        strm.Close();
                    }
                    if (fs != null)
                    {
                        fs.Dispose();
                        fs.Close();
                    }
                    //throw new Exception(ex.Message);
                    LogHelper.WriteLog(ex.Message.ToString(), ex);
                    return false;
                }
            }catch(Exception ex)
            {
                LogHelper.WriteLog(filename+"|"+ clientName, ex);
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return false;
            }
        }

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        public bool Download(string filePath, string fileName)
        {
            FtpWebRequest reqFTP;
            FtpWebResponse response;
            FileStream outputStream;
            Stream ftpStream;
            try
            {
                outputStream = new FileStream(filePath + "\\" + fileName, FileMode.Create);
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + fileName));
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                response = (FtpWebResponse)reqFTP.GetResponse();
                ftpStream = response.GetResponseStream();
                long cl = response.ContentLength;
                int bufferSize = 2048;
                int readCount;
                byte[] buffer = new byte[bufferSize];

                readCount = ftpStream.Read(buffer, 0, bufferSize);
                while (readCount > 0)
                {
                    outputStream.Write(buffer, 0, readCount);
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                }

                ftpStream.Dispose();
                outputStream.Dispose();
                response.Dispose();
                ftpStream.Close();
                outputStream.Close();
                response.Close();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return false;
                //throw new Exception(ex.Message);
            }
        }


        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileName"></param>
        public void Delete(string fileName)
        {
            try
            {
                string uri = ftpURI + fileName;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));

                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;

                string result = String.Empty;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new StreamReader(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
            }
        }

        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="folderName"></param>
        public void RemoveDirectory(string folderName)
        {
            try
            {
                string uri = ftpURI + folderName;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));

                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.RemoveDirectory;

                string result = String.Empty;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new StreamReader(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
            }
        }
        /// <summary>
        /// 获取指定目录下明细(包含文件和文件夹)
        /// </summary>
        /// <returns></returns>
        public string[] GetFilesDetailList(string folderPath)
        {
            string[] downloadFiles;
            try
            {
                StringBuilder result = new StringBuilder();
                FtpWebRequest ftp;
                ftp = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI+ folderPath));
                ftp.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                ftp.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = ftp.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);

                //while (reader.Read() > 0)
                //{

                //}
                string line = reader.ReadLine();
                //line = reader.ReadLine();
                //line = reader.ReadLine();

                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }
                try
                {
                    result.Remove(result.ToString().LastIndexOf("\n"), 1);
                }
                catch { }
                reader.Close();
                response.Close();
                if (result.ToString().Length > 2)
                {
                    return result.ToString().Split('\n');
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                downloadFiles = null;
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return downloadFiles;
            }
        }

        /// <summary>
        /// 获取当前目录下明细(包含文件和文件夹)
        /// </summary>
        /// <returns></returns>
        public string[] GetFilesDetailList()
        {
            string[] downloadFiles;
            try
            {
                StringBuilder result = new StringBuilder();
                FtpWebRequest ftp;
                ftp = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI));
                ftp.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                ftp.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = ftp.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);

                //while (reader.Read() > 0)
                //{

                //}
                string line = reader.ReadLine();
                //line = reader.ReadLine();
                //line = reader.ReadLine();

                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }
                result.Remove(result.ToString().LastIndexOf("\n"), 1);
                reader.Close();
                response.Close();
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                downloadFiles = null;
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return downloadFiles;
            }
        }

        /// <summary>
        /// 获取当前目录下文件列表(仅文件)
        /// </summary>
        /// <returns></returns>
        public string[] GetFileList(string mask)
        {
            string[] downloadFiles;
            StringBuilder result = new StringBuilder();
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);

                string line = reader.ReadLine();
                while (line != null)
                {
                    if (mask.Trim() != string.Empty && mask.Trim() != "*.*")
                    {

                        string mask_ = mask.Substring(0, mask.IndexOf("*"));
                        if (line.Substring(0, mask_.Length) == mask_)
                        {
                            result.Append(line);
                            result.Append("\n");
                        }
                    }
                    else
                    {
                        result.Append(line);
                        result.Append("\n");
                    }
                    line = reader.ReadLine();
                }
                result.Remove(result.ToString().LastIndexOf('\n'), 1);
                reader.Close();
                response.Close();
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                downloadFiles = null;
                return downloadFiles;
            }
        }

        /// <summary>
        /// 获取当前目录下所有的文件夹列表(仅文件夹)
        /// </summary>
        /// <returns></returns>
        public string[] GetDirectoryList()
        {
            string[] drectory = GetFilesDetailList();
            string m = string.Empty;
            foreach (string str in drectory)
            {
                int dirPos = str.IndexOf("<DIR>");
                if (dirPos > 0)
                {
                    /*判断 Windows 风格*/
                    m += str.Substring(dirPos + 5).Trim() + "\n";
                }
                else if (str.Trim().Substring(0, 1).ToUpper() == "D")
                {
                    /*判断 Unix 风格*/
                    string dir = str.Substring(54).Trim();
                    if (dir != "." && dir != "..")
                    {
                        m += dir + "\n";
                    }
                }
            }

            char[] n = new char[] { '\n' };
            return m.Split(n);
        }
        /// <summary>
        /// 获取指定目录下所有的文件夹列表(仅文件夹)
        /// </summary>
        /// <returns></returns>
        public string[] GetDirectoryList(string folderPath)
        {
            string[] drectory = GetFilesDetailList(folderPath);
            if (drectory != null)
            {
                string m = string.Empty;
                foreach (string str in drectory)
                {
                    int dirPos = str.IndexOf("<DIR>");
                    if (dirPos > 0)
                    {
                        /*判断 Windows 风格*/
                        m += str.Substring(dirPos + 5).Trim() + "\n";
                    }
                    else if (str.Trim().Substring(0, 1).ToUpper() == "D")
                    {
                        /*判断 Unix 风格*/
                        string dir = str.Substring(54).Trim();
                        if (dir != "." && dir != "..")
                        {
                            m += dir + "\n";
                        }
                    }
                }

                char[] n = new char[] { '\n' };
                return m.Split(n);
            }
            return null;
        }

        /// <summary>
        /// 判断当前目录下指定的子目录是否存在
        /// </summary>
        /// <param name="RemoteDirectoryName">指定的目录名</param>
        public bool DirectoryExist(string RemoteDirectoryName)
        {
            string[] dirList = GetDirectoryList();
            foreach (string str in dirList)
            {
                if (str.Trim() == RemoteDirectoryName.Trim())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断指定目录目录下指定的子目录是否存在
        /// </summary>
        /// <param name="RemoteDirectoryName">指定的目录名</param>
        public bool DirectoryExist(string RemoteDirectoryName,string folderPath)
        {
            string[] dirList = GetDirectoryList(folderPath);
            if (dirList != null)
            {
                foreach (string str in dirList)
                {
                    if (str.Trim() == RemoteDirectoryName.Trim())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 判断当前目录下指定的文件是否存在
        /// </summary>
        /// <param name="RemoteFileName">远程文件名</param>
        public bool FileExist(string RemoteFileName)
        {
            string[] fileList = GetFileList("*.*");
            foreach (string str in fileList)
            {
                if (str.Trim() == RemoteFileName.Trim())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="dirName"></param>
        public void MakeDir(string dirName)
        {
            FtpWebRequest reqFTP;
            try
            {
                // dirName = name of the directory to create.
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + dirName));
                reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(dirName);
                LogHelper.WriteLog(ex.Message.ToString(), ex);
            }
        }

        /// <summary>
        /// 获取指定文件大小
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public long GetFileSize(string filename)
        {
            FtpWebRequest reqFTP;
            long fileSize = 0;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + filename));
                reqFTP.Method = WebRequestMethods.Ftp.GetFileSize;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                fileSize = response.ContentLength;

                ftpStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                //throw new Exception(ex.Message);
            }
            return fileSize;
        }

        /// <summary>
        /// 改名
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <param name="newFilename"></param>
        public void ReName(string currentFilename, string newFilename)
        {
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + currentFilename));
                reqFTP.Method = WebRequestMethods.Ftp.Rename;
                reqFTP.RenameTo = newFilename;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                //throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <param name="newFilename"></param>
        public void MovieFile(string currentFilename, string newDirectory)
        {
            ReName(currentFilename, newDirectory);
        }

        /// <summary>
        /// 切换当前目录
        /// </summary>
        /// <param name="DirectoryName"></param>
        /// <param name="IsRoot">true 绝对路径   false 相对路径</param>
        public void GotoDirectory(string DirectoryName, bool IsRoot)
        {
            if (IsRoot)
            {
                ftpRemotePath = DirectoryName;
            }
            else
            {
                ftpRemotePath += DirectoryName + "/";
            }
            ftpURI = "ftp://" + ftpServerIP + "/" + ftpRemotePath + "/";
        }

        /// <summary>
        /// 删除订单目录
        /// </summary>
        /// <param name="ftpServerIP">FTP 主机地址</param>
        /// <param name="folderToDelete">FTP 用户名</param>
        /// <param name="ftpUserID">FTP 用户名</param>
        /// <param name="ftpPassword">FTP 密码</param>
        public static void DeleteOrderDirectory(string ftpServerIP, string folderToDelete, string ftpUserID, string ftpPassword)
        {
            try
            {
                if (!string.IsNullOrEmpty(ftpServerIP) && !string.IsNullOrEmpty(folderToDelete) && !string.IsNullOrEmpty(ftpUserID) && !string.IsNullOrEmpty(ftpPassword))
                {
                    FtpWeb fw = new FtpWeb(ftpServerIP, folderToDelete, ftpUserID, ftpPassword);
                    //进入订单目录
                    fw.GotoDirectory(folderToDelete, true);
                    //获取规格目录
                    string[] folders = fw.GetDirectoryList();
                    foreach (string folder in folders)
                    {
                        if (!string.IsNullOrEmpty(folder) || folder != "")
                        {
                            //进入订单目录
                            string subFolder = folderToDelete + "/" + folder;
                            fw.GotoDirectory(subFolder, true);
                            //获取文件列表
                            string[] files = fw.GetFileList("*.*");
                            if (files != null)
                            {
                                //删除文件
                                foreach (string file in files)
                                {
                                    fw.Delete(file);
                                }
                            }
                            //删除冲印规格文件夹
                            fw.GotoDirectory(folderToDelete, true);
                            fw.RemoveDirectory(folder);
                        }
                    }

                    //删除订单文件夹
                    string parentFolder = folderToDelete.Remove(folderToDelete.LastIndexOf('/'));
                    string orderFolder = folderToDelete.Substring(folderToDelete.LastIndexOf('/') + 1);
                    fw.GotoDirectory(parentFolder, true);
                    fw.RemoveDirectory(orderFolder);
                }
                else
                {
                    throw new Exception("FTP 及路径不能为空！");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("删除订单时发生错误，错误信息为：" + ex.Message);
            }
        }
    }
}
