using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Security.Principal;
using Tools.Network;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;


namespace HC200_Lis_Service
{
    public class Logger
    {
        ConfigFileHundler confighundler;

        public Logger()
        {
            confighundler = new ConfigFileHundler();
        }
        public string logMessageWithFormat(string message, string status = "info",
          [CallerLineNumber] int lineNumber = 0,
          [CallerMemberName] string caller = null)
        {
            return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ff")
                                    + "       " + status.ToUpper() + " : " + message + " at line " + lineNumber + " (" + caller + ")";
        }
        public void WriteLog(string strLog)
        {
            try
            {

                string logFilePath = confighundler.getPathToLogs() + @"\Log-" + System.DateTime.Today.ToString("MM-dd-yyyy") + "." + "txt";
                FileInfo logFileInfo = new FileInfo(logFilePath);
                DirectoryInfo logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
                if (!logDirInfo.Exists) logDirInfo.Create();
                using (FileStream fileStream = new FileStream(logFilePath, FileMode.Append))
                {
                    using (StreamWriter log = new StreamWriter(fileStream))
                    {
                        log.WriteLine(strLog);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
    internal class Subscriber
    {

        private static List<string> data = new List<string>();
        FileSystemWatcher watcher;
        Thread fileWatcher;
        static ConfigFileHundler configfilehundler;
        Logger logger;

        public Subscriber()
        {
            configfilehundler = new ConfigFileHundler();
            logger = new Logger();
        }

        public void startSubscriber()
        {

            fileWatcher = new Thread(new ThreadStart(watchForFile));
            fileWatcher.Start();
        }


        public void stopSubscriber()
        {
            fileWatcher.Join();
            fileWatcher.Abort();
        }

        public void OnError(object source, ErrorEventArgs e)
        {

            logger.WriteLog(logger.logMessageWithFormat(e.ToString(), "error"));
            logger.WriteLog(logger.logMessageWithFormat("watcher restarted"));
            watcher = new FileSystemWatcher();
            while (!watcher.EnableRaisingEvents)
            {
                try
                {
                    watcher = new FileSystemWatcher();
                    watcher.Path = configfilehundler.GetFolderPathToReadResult();
                    watcher.NotifyFilter = NotifyFilters.Attributes |
                                          NotifyFilters.CreationTime |
                                          NotifyFilters.DirectoryName |
                                          NotifyFilters.FileName |
                                          NotifyFilters.LastAccess |
                                          NotifyFilters.LastWrite |
                                          NotifyFilters.Security |
                                          NotifyFilters.Size;

                    watcher.Filter = "*.*";
                    //watcher.Changed += new FileSystemEventHandler(OnChanged);

                    watcher.Created += new FileSystemEventHandler(OnChanged);
                    //watcher.Deleted += new FileSystemEventHandler(OnChanged);
                    watcher.Error += new ErrorEventHandler(OnError);

                    watcher.EnableRaisingEvents = true;
                    watcher.InternalBufferSize = 65536;

                    DirectoryInfo d1 = new DirectoryInfo(configfilehundler.GetFolderPathToReadResult());
                }
                catch
                {
                    System.Threading.Thread.Sleep(30000); //Wait for retry 30 sec.
                }
                Thread.Sleep(10);
            }
        }
        
         
        public void watchForFile()
        {


            try
            {
                logger.WriteLog(logger.logMessageWithFormat("file watcher started."));

                watcher = new FileSystemWatcher(configfilehundler.GetFolderPathToReadResult(), "*.*");
           

                while (!watcher.EnableRaisingEvents)
                {
                    try
                    {
                        //watcher.Path = configfilehundler.GetFolderPathToReadResult();


                        watcher.NotifyFilter = NotifyFilters.Attributes |
                                                NotifyFilters.CreationTime |
                                                NotifyFilters.DirectoryName |
                                                NotifyFilters.FileName |
                                                NotifyFilters.LastAccess |
                                                NotifyFilters.LastWrite |
                                                NotifyFilters.Security |
                                                NotifyFilters.Size;

                        watcher.Filter = "*.*";
                        //watcher.Changed += new FileSystemEventHandler(OnChanged);
                        Thread.Sleep(10);
                        watcher.Created += new FileSystemEventHandler(OnChanged);
                        //watcher.Deleted += new FileSystemEventHandler(OnChanged);
                        watcher.Error += new ErrorEventHandler(OnError);

                        watcher.EnableRaisingEvents = true;

                        DirectoryInfo d1 = new DirectoryInfo(configfilehundler.GetFolderPathToReadResult());

                    }
                    catch (Exception ex)
                    {
                        System.Threading.Thread.Sleep(30000);
                        Console.WriteLine(ex.ToString());
                        logger.WriteLog(logger.logMessageWithFormat(ex.ToString(), "error"));
                    }
                    Thread.Sleep(10);
                }

            }
            catch (ArgumentException er)
            {
                logger.WriteLog("unable to access shared folder");
                Thread.Sleep(10000);
                watchForFile();
            }
           

        }

        public  void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                logger.WriteLog("on change file read started");
                string filedirectory = configfilehundler.GetFolderPathToReadResult();
                List<string> patientResults = new List<string>();

                Database database = new Database();
                Thread.Sleep(10);

                if (File.Exists(e.FullPath))
                {
                    data = readResults(e.Name, e.FullPath);


                    string header = data[0];
                    string end = data[data.Count - 1];


                    patientResults.Add(header);
                    int j = 0;
                    int i = 0;
                    string dataToStore = "";
                    foreach (string line in data)
                    {
                        string nline = String.Concat(line.Where(c => !Char.IsWhiteSpace(c)));

                        if (line == header || line == end)
                        {
                            continue;
                        }
                       
                         if (nline.StartsWith("R"))
                        {
                            patientResults.Add(nline);
                            patientResults.Add(end);
                            dataToStore = String.Join("##", patientResults.ToArray());
                            database.InsertResult(dataToStore);

                            patientResults.Clear();

                            patientResults.Add(header);
                       
                        }
                        else
                        {
                            patientResults.Add(nline);
                            continue;
                        }

                    }

                    logger.WriteLog("data received trying to delete the file");
                    File.Delete(filedirectory + e.Name);
                    logger.WriteLog("file deleted successfully");
                }
                else
                {
                    logger.WriteLog(logger.logMessageWithFormat("file with the full path not found!"));
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                logger.WriteLog(logger.logMessageWithFormat(ex.ToString(),"error"));
           
            }
        }

        public  List<string> readResults(string fname,string fullName)
        {
           
            string filename = configfilehundler.GetFolderPathToReadResult() + fname;

            List<string> msg = new List<string>();

            var fileNames = Directory.GetFiles(configfilehundler.GetFolderPathToReadResult());
            
            foreach (string fileName in fileNames)
            {
                string[] lines = File.ReadAllLines(fileName);
                
                Console.WriteLine("=> " + filename == fullName);
                if (fileName == fullName)
                {
                    msg = lines.ToList();
                }
                continue;
                
            }
        
            return msg;


            //if (File.Exists(filename))
            //{
            //    using (StreamReader sr = File.OpenText(filename))
            //    {
            //        string s = "";

            //        while ((s = sr.ReadLine()) != null)
            //        {
            //            msg.Add(s);
            //        }
            //    }
            //    logger.WriteLog("successfully Read file");
            //    return msg;
            //}
            //else
            //{
            //    logger.WriteLog(logger.logMessageWithFormat("File Not Found!"));
            //    return null;
            //}
        }

            
    }
     
}

