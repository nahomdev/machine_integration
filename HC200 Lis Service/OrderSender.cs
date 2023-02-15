using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Security.AccessControl;

namespace HC200_Lis_Service
{
    public class OrderSender
    {
        Database db;
        Thread orderSender;
        ConfigFileHundler configFileHundler;
        Logger logger;
        public OrderSender()
        {
            logger = new Logger();
            db = new Database();
            configFileHundler = new ConfigFileHundler();    
        }

        public void startOrderSender()
        {
            orderSender = new Thread(new ThreadStart(startSending));
            orderSender.Start();
        }

        public void stopOrderSender()
        {
            orderSender.Join();
            orderSender.Abort();
        }
        public void startSending()
        {
            int error = 0;
            while (true)
            {
                try
                {
                    DataTable messages = db.SelectAllMessages();

                    if (messages.Rows.Count > 0)
                    {
                        logger.WriteLog("Sending Order Started!");

                        DataRow dr = messages.Rows[0];
                        Console.WriteLine("raw data is " + dr["message"].ToString(), ConsoleColor.DarkYellow);

                        string dataToSend = dr["message"].ToString().Trim();

                        string[] astmMessageRecords = SplitMessage(dataToSend);
                        Console.WriteLine("Message parsed successfully...", ConsoleColor.DarkYellow);

                        int messageId = int.Parse(dr["id"].ToString());

                        //DirectoryInfo myDirectoryInfo = new DirectoryInfo(configFileHundler.GetFolderPathToSendOrders());
                        //DirectorySecurity myDirectorySecurity = myDirectoryInfo.GetAccessControl();

                        //myDirectorySecurity.AddAccessRule(new FileSystemAccessRule(System.Security.Principal.WindowsIdentity.GetCurrent().Name, FileSystemRights.Read, AccessControlType.Allow));

                        //myDirectoryInfo.SetAccessControl(myDirectorySecurity);

                        var filename = configFileHundler.GetFolderPathToSendOrders() +"test12345" + ".txt";

                        //using (FileStream fs = File.Create(filename))
                        //{
                        //    foreach (string am in astmMessageRecords)
                        //    {

                        //        byte[] p = new UTF8Encoding(true).GetBytes(am + "\n");

                        //        fs.Write(p, 0, p.Length);
                        //    
                        //}
                        string fname = DateTime.Now.ToString("yyyyMMddhhmmss") + ".astm";
                        
                        File.WriteAllLines(configFileHundler.GetLocalFolder() + fname, astmMessageRecords, Encoding.ASCII);
                        logger.WriteLog(fname + " file created inside " + configFileHundler.GetLocalFolder());
                        Thread.Sleep(10);
                        File.Copy(configFileHundler.GetLocalFolder() + fname,
                            configFileHundler.GetFolderPathToSendOrders() +fname);
                        logger.WriteLog(fname + " moved to "+ configFileHundler.GetFolderPathToSendOrders());
                        Thread.Sleep(10);
                        File.Delete(configFileHundler.GetLocalFolder() + fname);
                        logger.WriteLog(fname+" deleted from "+configFileHundler.GetLocalFolder());

                        bool s = db.DeleteMessage(messageId);
                        Console.WriteLine("Message deleted ->" + s, ConsoleColor.DarkYellow);
                        error = 0;
                    }
                }
                catch(Exception ex)
                {
                     
                    Console.WriteLine(ex.Message);
                     
                    logger.WriteLog(logger.logMessageWithFormat(ex.ToString(), "error"));
                     
                   
                }
                Thread.Sleep(10);
            }
        }

        private string[] SplitMessage(string dataToSend)
        {
            return Regex.Split(dataToSend, "##");
        }
    }
}
