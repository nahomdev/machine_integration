using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading; 
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using Subscriber.Sockets;

using System.Runtime.CompilerServices;
namespace Subscriber.Sockets
{
    public static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}

namespace Sysmex_Lis_Service
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
            return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ff")+"       " + status.ToUpper() +" : "+ message + " at line " + lineNumber + " (" + caller + ")";
        }
        public  void WriteLog(string strLog)
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
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
    public class LabResultValues
    {
        public string Code { get; set; }
        public string Result { get; set; }
    }
    public class LabResult
    {
        public string OrderId { get; set; }
        public List<LabResultValues> Results { get; set; }
    }
    public class Subscriber
    {
        static string soh = char.ConvertFromUtf32(1);
        static string stx = char.ConvertFromUtf32(2);
        static string etx = char.ConvertFromUtf32(3);
        static string eot = char.ConvertFromUtf32(4);
        static string enq = char.ConvertFromUtf32(5);
        static string ack = char.ConvertFromUtf32(6);
        static string nack = char.ConvertFromUtf32(21);
        static string etb = char.ConvertFromUtf32(23);
        static string lf = char.ConvertFromUtf32(10);
        static string cr = char.ConvertFromUtf32(13);
        private System.Net.Sockets.Socket listener;
        private IPEndPoint endPoint;
        Thread resultSender;
        Thread listnerThread;
        static ConfigFileHundler confighundler;
        Logger logger;
        bool isStopped= false;
        //Socket receiver;
        public Subscriber()
        {
             confighundler = new ConfigFileHundler();
           
                logger = new Logger();
             
            System.Net.IPEndPoint endPoint = confighundler.GetEndPoint();
            Console.WriteLine("endpoint :=>"+endPoint);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            listener.Bind(endPoint);
            
            logger.WriteLog(logger.logMessageWithFormat("Listening to port " + endPoint));
            listener.ReceiveTimeout = 10000;
            listener.Listen(1);
            //this.receiver = listener.Accept();

        }

        public void startServer()
        {
            resultSender = new Thread(new ThreadStart(Listen));
            resultSender.Start();
        }
        public void stopServer()
        {
            isStopped = true;
            resultSender.Join();
            resultSender.Abort();
        }
        private void WriteLog(string message, ConsoleColor color = ConsoleColor.Green)
        {
            Helper.WriteLog(message, "OrbitService", color);
        }

        private void SendOrder(Socket sck)
        {
            Database db = new Database();
            DataTable messages = db.SelectAllMessages();
            
            logger.WriteLog("Message collection checked!!");
            if (messages.Rows.Count > 0)
            {

                string[] sampleMessage = new string[] {
                    @"H|\^&||PSWD|Maglumi User|||||Lis||P|E1394-97|20140613" + cr ,
                    @"P|1" + cr + @"O|1|TESTORDER5||^^^TSH\^^^FT4\^^^FT3" + cr ,
                    @"L|1|N" + cr
            };

                logger.WriteLog("Started sending message");
                DataRow dr = messages.Rows[0];
                logger.WriteLog("raw data is " + dr["message"].ToString());

                string dataToSend = dr["message"].ToString().Trim();

                string[] astmMessageRecords = SplitMessage(dataToSend);
                logger.WriteLog("Message parsed successfully");

                int messageId = int.Parse(dr["id"].ToString());

                sck.Send(Encoding.ASCII.GetBytes(enq));
                logger.WriteLog("enq sent");
                Thread.Sleep(500);
                sck.Send(Encoding.ASCII.GetBytes(stx));
                logger.WriteLog("stx sent");
                Thread.Sleep(500);
                //main message sending started
                foreach (string am in sampleMessage)
                {
                    sck.Send(Encoding.ASCII.GetBytes(am));
                    WriteLog(am, ConsoleColor.DarkYellow);
                    Thread.Sleep(500);
                }
                //sck.Send(Encoding.ASCII.GetBytes(sampleMessage));
                //WriteLog("Message set >" + sampleMessage);
                //Thread.Sleep(50);
                //main message sending finished
                sck.Send(Encoding.ASCII.GetBytes(etx));
                logger.WriteLog("etx sent");
                Thread.Sleep(500);
                sck.Send(Encoding.ASCII.GetBytes(eot));
                logger.WriteLog("eot sent");
                Thread.Sleep(500);

                //delete message from db
                bool s = db.DeleteMessage(messageId);
                logger.WriteLog("Message deleted ->" + s);
            }
        }

        
        //public static bool IsConnected(Socket socket)
        //{
        //    try
        //    {
        //        return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        //    }
        //    catch (Exception ex) { return false; }
        //}
        public void Listen()
        {
            try
            {
                List<string> mainMessageRecords = new List<string>();

                int bytesRec = 0;
                int iter = 0;
                
                Socket receiver = listener.Accept();
                receiver.ReceiveTimeout = 10000;
                Console.WriteLine("connected!!!");

                logger.WriteLog(logger.logMessageWithFormat("subscriber started"));
                string msg = null;
                List<string> dataArr = new List<string>();

                logger.WriteLog(logger.logMessageWithFormat("waiting for the result"));
                
                while (!isStopped)
                {   
                    try
                    {
                        if (!receiver.IsConnected())
                        {
                            logger.WriteLog(logger.logMessageWithFormat("connection dropout", "warning"));
                            Listen();
                        }
                        else
                        {
                            //logger.WriteLog("Subscriber.cs => info: connection alive!!!");
                        }

                        
                        string data = null;
                        byte[] bytes = null;

                        try
                        {
                            bytes = new byte[1];
                            bytesRec = receiver.Receive(bytes);
                            data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                        }
                        catch (Exception ex)
                        {
                            
                            continue;

                        }
                       
                        msg += data;
                        if (data == cr)
                        {
                            Console.WriteLine("message : {0}", msg);
                            dataArr.Add(msg);
                            if (msg.StartsWith("L"))
                            {
                                string dataToSave = String.Join("##", dataArr);

                                Database database = new Database();
                                Console.WriteLine("final data to save : {0}", data);
                                database.InsertResult(dataToSave);
                                dataArr.Clear();
                            }
 
                            msg = null;
                        }
 
                        if (data.IndexOf(enq) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            logger.WriteLog(">><ENQ>");
                        }

                        //if (data.IndexOf(stx) > -1)
                        //{
                        //    receiver.Send(Encoding.UTF8.GetBytes(ack));
                        //    WriteLog(">><STX>");
                        //}

                        // handle main body
                        //if (data.IndexOf(stx) > -1)
                        //{
                        //    receiver.Send(Encoding.UTF8.GetBytes(ack));
                        //}
                        //handle etx
                        if (data.IndexOf(stx) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            string cleanData = Encoding.UTF8.GetString(bytes, 2, bytesRec - 8);


                            mainMessageRecords.Add(cleanData);
                            logger.WriteLog(cleanData);

                            //save mainMessageRecord to database
                            //Database database = new Database();
                            //string[] inputStringArray = mainMessageRecords.Select(i => i.ToString()).ToArray();
                            //database.InsertResult(PrepareASTMMessageForSave(inputStringArray));
                            //reset mainMEssageRecord
                            //mainMessageRecords.Clear();
                            //WriteLog(">><ETX>");
                            //string cleanData = Encoding.UTF8.GetString(bytes, 2, bytesRec - 8);

                            //WriteLog(cleanData);
                        }
                        //handle eot
                        if (data.IndexOf(eot) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            Database database = new Database();
                            string[] inputStringArray = mainMessageRecords.Select(i => i.ToString()).ToArray();
                            database.InsertResult(PrepareASTMMessageForSave(inputStringArray));
                            //reset mainMEssageRecord

                             
                            mainMessageRecords.Clear();
                            //WriteLog(">><ETX>");
                            //string cleanData = Encoding.UTF8.GetString(bytes, 2, bytesRec - 8);

                            //WriteLog(cleanData);
                            logger.WriteLog(">><EOT>");
                        }

                        //WriteLog(data);
                        //send order
                        //SendOrder(receiver);
                    }
                    catch (Exception e)
                    {
                        logger.WriteLog(logger.logMessageWithFormat("tcp connection exceptioin " +e.ToString(), "error"));
                        WriteLog(e.ToString());
                        Listen();
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
                logger.WriteLog(logger.logMessageWithFormat("connection exception " + ex.ToString(),"error"));
                Listen();
            }
        }
        //private string HandleMessage(string data)
        //{
        //    string responseMessage = String.Empty;
        //    try
        //    {
        //        WriteLog("Message received.");

        //        Message msg = new Message();
        //        msg.DeSerializeMessage(data);

        //        // You can do what you want with the message here as per your appliation requirements.
        //        // For eg: read patient ID, patient last name, age etc.

        //        // Create a response message
        //        //
        //        responseMessage = CreateRespoonseMessage(msg.MessageControlId());
        //    }
        //    catch (Exception ex)
        //    {
        //        // Exception handling
        //    }
        //    return responseMessage;
        //}

        //private string CreateRespoonseMessage(string messageControlID)
        //{
        //    try
        //    {
        //        Message response = new Message();

        //        Segment msh = new Segment("MSH");
        //        msh.Field(2, "^~\\&");
        //        msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmsszzz"));
        //        msh.Field(9, "ACK");
        //        msh.Field(10, Guid.NewGuid().ToString());
        //        msh.Field(11, "P");
        //        msh.Field(12, "2.5.1");
        //        response.Add(msh);

        //        Segment msa = new Segment("MSA");
        //        msa.Field(1, "AA");
        //        msa.Field(2, messageControlID);
        //        response.Add(msa);


        //        // Create a Minimum Lower Layer Protocol (MLLP) frame.
        //        // For this, just wrap the data lik this: <VT> data <FS><CR>
        //        StringBuilder frame = new StringBuilder();
        //        frame.Append((char)0x0b);
        //        frame.Append(response.SerializeMessage());
        //        frame.Append((char)0x1c);
        //        frame.Append((char)0x0d);

        //        return frame.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        // Exception handling

        //        return String.Empty;
        //    }
        //}

        //private void PrepareSendingResult(string[] results)
        //{
        //    LabResult labResult = new LabResult();
        //    ASTMMessage message = new ASTMMessage(results);
        //    List<ASTMRecord> astmResults = message.GetResultRecords();
        //    List<ASTMRecord> astmOrders = message.GetOrderRecords();
        //    foreach (ASTMRecord astmResult in astmResults)
        //    {
        //        labResult.Results.Add(new LabResultValues() { Code = astmResult.Fields[2].Value, Result = astmResult.Fields[3].Value });
        //    }
        //    foreach (ASTMRecord astmOrder in astmOrders)
        //    {
        //        labResult.OrderId = astmOrder.Fields[2].Value;
        //    }
        //    // send the result in new thread
        //    StartWorker(labResult);
        //}
        //private void StartWorker(LabResult result)
        //{

        //    Thread resultThread = new Thread(
        //        () =>
        //        {
        //            SendResultToLis(result);
        //        });
        //    resultThread.Start();
        //}
        private static void SendResultToLis(LabResult result)
        {
            // consume api
        }

        private string PrepareASTMMessageForSave(string[] records)
        {
            int count = records.Length;
            string msg = "";
            for (int i = 0; i < records.Length; i++)
            {
                if (i == count - 1)
                {
                    msg += (records[i].Trim());
                    continue;
                }
                msg += (records[i].Trim() + "##");

            }
            return msg.ToString();
        }
        private string[] SplitMessage(string dataToSend)
        {
            return Regex.Split(dataToSend, "##");
        }
    }
}
