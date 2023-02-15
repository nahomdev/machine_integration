using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using Subscriber.Sockets;
using Maglumi;
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

namespace Maglumi
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
            return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ff") + "       " + status.ToUpper() + " : " + message + " at line " + lineNumber + " (" + caller + ")";
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
        Thread listenerThread;
        Logger logger;
        //Socket receiver;
        public Subscriber(IPEndPoint endPoint)
        {
            logger = new Logger();
            this.endPoint = endPoint;
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(endPoint);
            logger.WriteLog(logger.logMessageWithFormat("Listening to port " + endPoint,"info"));
            listener.Listen(1);
            //listener.ReceiveTimeout = 10000;
            //this.receiver = listener.Accept();
        }
        private void WriteLog(string message, ConsoleColor color = ConsoleColor.Green)
        {
            Helper.WriteLog(message, "OrbitService", color);
        }
        public void startServer()
        {
            listenerThread = new Thread(new ThreadStart(Listen));
            listenerThread.Start();

        }

        public void stopServer()
        {
            listenerThread.Join();
            listenerThread.Abort();
        }
        private void SendOrderTest(Socket sck, string sampleid)
        {

            int bytesRec = 0;
            Database db = new Database();
            DataTable messages = db.SelectAllMessages();
            WriteLog("Message collection checked!!", ConsoleColor.DarkYellow);
            if (messages.Rows.Count > 0)
            {
                for (int i = 0; i < messages.Rows.Count; i++)
                {
                    WriteLog("Started sending message", ConsoleColor.DarkYellow);
                    DataRow dr = messages.Rows[i];
                    WriteLog("raw data is " + dr["message"].ToString(), ConsoleColor.DarkYellow);

                    string dataToSend = dr["message"].ToString().Trim();

                    string[] astmMessageRecords = SplitMessage(dataToSend);
                    string sample = astmMessageRecords[2].Split('|')[2];
                    Console.WriteLine("message order id " + sample);

                    if (sample == sampleid)
                    {
                        WriteLog("Message parsed successfully", ConsoleColor.DarkYellow);

                        int messageId = int.Parse(dr["id"].ToString());

                        sck.Send(Encoding.UTF8.GetBytes(enq));
                        WriteLog("enq sent", ConsoleColor.DarkYellow);
                        Thread.Sleep(10);
                        sck.Send(Encoding.UTF8.GetBytes(stx));
                        WriteLog("stx sent", ConsoleColor.DarkYellow);
                        Thread.Sleep(10);
                        //main message sending started
                        string dataToSendToMaglumi = "";
                        foreach (string am in astmMessageRecords)
                        {
                            sck.Send(Encoding.UTF8.GetBytes(am));

                            dataToSendToMaglumi += am + cr;
                            string data = null;
                            byte[] bytes = null;

                            //handle enquiry
                            //bytes = new byte[1024];
                            //bytesRec = sck.Receive(bytes);
                            //data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                        }
                        sck.Send(Encoding.UTF8.GetBytes(dataToSendToMaglumi));
                        //main message sending finished
                        sck.Send(Encoding.UTF8.GetBytes(etx));
                        WriteLog("etx sent", ConsoleColor.DarkYellow);
                        Thread.Sleep(10);
                        sck.Send(Encoding.UTF8.GetBytes(eot));
                        WriteLog("eot sent", ConsoleColor.DarkYellow);
                        Thread.Sleep(10);

                        //delete message from db
                        bool s = db.DeleteMessage(messageId);
                        WriteLog("Message deleted ->" + s, ConsoleColor.DarkYellow);
                    }

                }

            }
        }

        private void SendOrder(Socket sck)
        {

            int bytesRec = 0;
            Database db = new Database();
            DataTable messages = db.SelectAllMessages();
            WriteLog("Message collection checked!!", ConsoleColor.DarkYellow);
            if (messages.Rows.Count > 0)
            {
                WriteLog("Started sending message", ConsoleColor.DarkYellow);
                DataRow dr = messages.Rows[0];
                WriteLog("raw data is " + dr["message"].ToString(), ConsoleColor.DarkYellow);

                string dataToSend = dr["message"].ToString().Trim();

                string[] astmMessageRecords = SplitMessage(dataToSend);
                WriteLog("Message parsed successfully", ConsoleColor.DarkYellow);

                int messageId = int.Parse(dr["id"].ToString());

                sck.Send(Encoding.UTF8.GetBytes(enq));
                WriteLog("enq sent", ConsoleColor.DarkYellow);
                Thread.Sleep(10);
                sck.Send(Encoding.UTF8.GetBytes(stx));
                WriteLog("stx sent", ConsoleColor.DarkYellow);
                Thread.Sleep(10);
                //main message sending started
                string dataToSendToMaglumi = "";
                foreach (string am in astmMessageRecords)
                {
                    sck.Send(Encoding.UTF8.GetBytes(am));

                    dataToSendToMaglumi += am + cr;
                    string data = null;
                    byte[] bytes = null;

                    ////handle enquiry
                    //bytes = new byte[1024];
                    //bytesRec = sck.Receive(bytes);
                    //data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                }
                sck.Send(Encoding.UTF8.GetBytes(dataToSendToMaglumi));
                //main message sending finished
                sck.Send(Encoding.UTF8.GetBytes(etx));
                WriteLog("etx sent", ConsoleColor.DarkYellow);
                Thread.Sleep(10);
                sck.Send(Encoding.UTF8.GetBytes(eot));
                WriteLog("eot sent", ConsoleColor.DarkYellow);
                Thread.Sleep(10);

                //delete message from db
                bool s = db.DeleteMessage(messageId);
                WriteLog("Message deleted ->" + s, ConsoleColor.DarkYellow);
            }
        }

        public void orderMyTest(Socket sck)
        {
            int bytesRec = 0;
            byte[] bytes = null;
            string data = null;
            string msg = null;

            msg = @"H|^~\&|||||||||||A.2|200508041240" + cr
                 + "P|1|516|||^9953160310||19401028|F||||||||||||||||||||||||20050804" + cr
                 + "OBR|1|S04080622020||WBC~RBC~HGB~HCT|||200508041240||||A|||200508041240||||2001|||||||||R" + cr
                 + "L|1|N" + cr;

            sck.Send(Encoding.UTF8.GetBytes(enq));
            Console.WriteLine("ENQ sent");
            bytes = new byte[1024];
            bytesRec = sck.Receive(bytes);

            data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
            Console.WriteLine("before ack for stx" + data);
            if (data.IndexOf(ack) > -1)
            {
                Console.WriteLine("ACK received");
                sck.Send(Encoding.UTF8.GetBytes(stx));

                bytes = new byte[1024];
                bytesRec = sck.Receive(bytes);
                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                Console.WriteLine("recieving ack ...." + data);

                if (data.IndexOf(ack) > -1)
                {
                    Console.WriteLine("Ack for stx");
                    sck.Send(Encoding.UTF8.GetBytes(msg));

                    bytes = new byte[1024];
                    bytesRec = sck.Receive(bytes);
                    data = Encoding.UTF8.GetString(bytes, 0, bytesRec);

                    sck.Send(Encoding.UTF8.GetBytes(etx));
                    Console.WriteLine("end of text");

                    if (data.IndexOf(ack) > -1)
                    {
                        sck.Send(Encoding.UTF8.GetBytes(eot));
                    }

                }

            }

        }

        public void Listen()
        {

            try
            {
                List<string> mainMessageRecords = new List<string>();

                int bytesRec = 0;
                int iter = 0;
                logger.WriteLog("before accept");
                Socket receiver = listener.Accept();
                //receiver.ReceiveTimeout = 10000;
                logger.WriteLog("subscriber started");

                while (true)
                {

                    //WriteLog("Iteration number " + iter++);
                    try
                    {


                        if (!receiver.IsConnected())
                        {
                            logger.WriteLog(logger.logMessageWithFormat("connection dropout!!!","warring"));
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

                            logger.WriteLog("before data receive");
                            bytes = new byte[1024];
                            bytesRec = receiver.Receive(bytes);
                            data = Encoding.UTF8.GetString(bytes, 0, bytesRec);

                        }
                        catch (Exception e)
                        {
                            continue;
                           // logger.WriteLog("Receive TimeOut!!!");
                        }


                        if (data.IndexOf(enq) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            logger.WriteLog(logger.logMessageWithFormat(">><ENQ>","info"));
                        }


                        if (data.IndexOf(stx) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            logger.WriteLog(logger.logMessageWithFormat(">><STX>","info"));
                        }


                        if (data.IndexOf("H") > -1 ||
                            
                            data.IndexOf("R") > -1 ||
                            data.IndexOf("O") > -1 ||
                            data.IndexOf("L") > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            mainMessageRecords.Add(data);
                        }
                       
                        if (data.IndexOf(etx) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                          

                            Database database = new Database();

                            string[] inputStringArray = mainMessageRecords.Select(i => i.ToString()).ToArray();

                            bool result = Array.Exists(inputStringArray, el => el.StartsWith("R"));

                            if (result)
                            {
                                database.InsertResult(PrepareASTMMessageForSave(inputStringArray));
                            }


                            //********************************************************************************
                            bool query = Array.Exists(inputStringArray, el => el.StartsWith("Q"));
                            string sampleid = "";
                            foreach (string el in inputStringArray)
                            {
                                if (el.StartsWith("Q"))
                                {

                                    sampleid = el.Split('|')[2].Split('^')[1];
                                   
                                }
                            }
                            logger.logMessageWithFormat("----------------------------------------------");
                            Console.WriteLine("query received :" + query);
                            if (query)
                            {
                                Console.WriteLine("we are here. ...");
                                SendOrderTest(receiver, sampleid);
                            }

                            logger.logMessageWithFormat("------------------------------------------------");
                            //********************************************************************************

                            //reset mainMEssageRecord
                            mainMessageRecords.Clear();
                            logger.WriteLog(logger.logMessageWithFormat(">><ETX>", "info"));
                        }
                        
                        if (data.IndexOf(eot) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            logger.WriteLog(logger.logMessageWithFormat(">><EOT>", "info"));
                            SendOrder(receiver);


                            string[] inputStringArray = mainMessageRecords.Select(i => i.ToString()).ToArray();

                        }

                        logger.WriteLog(data + "\n");


                    }
                    catch (Exception ex)
                    {
                        logger.WriteLog(logger.logMessageWithFormat(" tcp connection exceptioin " + ex.ToString(),"error"));
                        WriteLog(ex.ToString());
                        Listen();
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog(logger.logMessageWithFormat(" tcp connection exceptioin " + ex.ToString(),"error"));
                WriteLog(ex.ToString());
                Listen();
            }
        }

        private string HandleMessage(string data)
        {
            string responseMessage = String.Empty;
            try
            {
                WriteLog("Message received.");

                Message msg = new Message();
                msg.DeSerializeMessage(data);


                responseMessage = CreateRespoonseMessage(msg.MessageControlId());
            }
            catch (Exception ex)
            {
                // Exception handling
                Console.WriteLine(ex.Message);
            }
            return responseMessage;
        }

        private string CreateRespoonseMessage(string messageControlID)
        {
            try
            {
                Message response = new Message();

                Segment msh = new Segment("MSH");
                msh.Field(2, "^~\\&");
                msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmsszzz"));
                msh.Field(9, "ACK");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, "P");
                msh.Field(12, "2.5.1");
                response.Add(msh);

                Segment msa = new Segment("MSA");
                msa.Field(1, "AA");
                msa.Field(2, messageControlID);
                response.Add(msa);



                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                return frame.ToString();
            }
            catch (Exception ex)
            {
                 return String.Empty;
            }
        }

        private void PrepareSendingResult(string[] results)
        {
            LabResult labResult = new LabResult();
            ASTMMessage message = new ASTMMessage(results);
            List<ASTMRecord> astmResults = message.GetResultRecords();
            List<ASTMRecord> astmOrders = message.GetOrderRecords();
            foreach (ASTMRecord astmResult in astmResults)
            {
                labResult.Results.Add(new LabResultValues() { Code = astmResult.Fields[2].Value, Result = astmResult.Fields[3].Value });
            }
            foreach (ASTMRecord astmOrder in astmOrders)
            {
                labResult.OrderId = astmOrder.Fields[2].Value;
            }
            // send the result in new thread
            StartWorker(labResult);
        }
        private void StartWorker(LabResult result)
        {

            Thread resultThread = new Thread(
                () =>
                {
                    SendResultToLis(result);
                });
            resultThread.Start();
        }
        private static void SendResultToLis(LabResult result)
        {
            // 
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
