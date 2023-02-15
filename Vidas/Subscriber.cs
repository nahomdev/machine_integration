
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
using Vidas;
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

namespace Vidas
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
        static string gs = char.ConvertFromUtf32(29);
        static string rs = char.ConvertFromUtf32(30);
        private System.Net.Sockets.Socket listener;
        private IPEndPoint endPoint;
        Thread listenerThread;
        Logger logger;
        ConfigFileHundler fileHundler;
        //Socket receiver;
        public Subscriber(IPEndPoint endPoint)
        {
            logger = new Logger();
            fileHundler = new ConfigFileHundler();
            this.endPoint = fileHundler.GetEndPoint();
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(endPoint);
            listener.Listen(1);
            //this.receiver = listener.Accept();
            //listener.ReceiveTimeout = 10000;

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
        private void WriteLog(string message, ConsoleColor color = ConsoleColor.Green)
        {
            Helper.WriteLog(message, "OrbitService", color);
        }

        //private void SendOrder(Socket sck)
        //{
        //    int bytesRec = 0;
        //    Database db = new Database();
        //    DataTable messages = db.SelectAllMessages();
        //    WriteLog("Message collection checked!!", ConsoleColor.DarkYellow);
        //    if (messages.Rows.Count > 0)
        //    {
        //        WriteLog("Started sending message", ConsoleColor.DarkYellow);
        //        DataRow dr = messages.Rows[0];
        //        WriteLog("raw data is " + dr["message"].ToString(), ConsoleColor.DarkYellow);

        //        string dataToSend = dr["message"].ToString().Trim();

        //        string[] astmMessageRecords = SplitMessage(dataToSend);
        //        WriteLog("Message parsed successfully", ConsoleColor.DarkYellow);

        //        int messageId = int.Parse(dr["id"].ToString());

        //        sck.Send(Encoding.UTF8.GetBytes(enq));
        //        WriteLog("enq sent", ConsoleColor.DarkYellow);
        //        Thread.Sleep(10);
        //        sck.Send(Encoding.UTF8.GetBytes(stx));
        //        WriteLog("stx sent", ConsoleColor.DarkYellow);
        //        Thread.Sleep(10);
        //        //main message sending started
        //        foreach (string am in astmMessageRecords)
        //        {
        //            Console.WriteLine("inside ......");
        //            sck.Send(Encoding.UTF8.GetBytes(am));
        //            WriteLog(am, ConsoleColor.DarkYellow);
        //            Thread.Sleep(10); 

        //        }

        //        string data = null;
        //            byte[] bytes = null;
        //            bytes = new byte[1024];
        //            bytesRec = sck.Receive(bytes);
        //            data = Encoding.UTF8.GetString(bytes, 0, bytesRec);


        //            Console.WriteLine("received data...." + data);
        //        //main message sending finished
        //        sck.Send(Encoding.UTF8.GetBytes(etx));
        //        WriteLog("etx sent", ConsoleColor.DarkYellow);
        //        Thread.Sleep(10);
        //        sck.Send(Encoding.UTF8.GetBytes(eot));
        //        WriteLog("eot sent", ConsoleColor.DarkYellow);
        //        Thread.Sleep(10);

        //        //delete message from db
        //        bool s = db.DeleteMessage(messageId);
        //        WriteLog("Message deleted ->" + s, ConsoleColor.DarkYellow);
        //    }
        //}
        private void sendorderme(Socket sck)
        {
            int bytesRec = 0;
            string msg = null;

            Database db = new Database();
            DataTable messages = db.SelectAllMessages();
            Console.WriteLine("Message collection checked!!", ConsoleColor.DarkYellow);


            if (messages.Rows.Count > 0)
            {
                for (int i = 0; i < messages.Rows.Count; i++)
                {

                    DataRow drow = messages.Rows[i];
                string dataToSend = drow["message"].ToString().Trim();

                int messageId = int.Parse(drow["id"].ToString());


                Console.WriteLine("sending message started");
                sck.Send(Encoding.UTF8.GetBytes(enq));
                WriteLog("enq sent", ConsoleColor.DarkYellow);
                Thread.Sleep(1000);


                string data = null;
                byte[] bytes = null;


                bytes = new byte[1024];
                bytesRec = sck.Receive(bytes);
                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);


                    if (data.IndexOf(ack) > -1)
                    {

                        Console.WriteLine("data to send {0}", dataToSend);
                        dataToSend += cr;
                        dataToSend += lf;
                        sck.Send(Encoding.UTF8.GetBytes(dataToSend));
                        WriteLog("msg sent", ConsoleColor.DarkYellow);
                        Thread.Sleep(1000);

                        sck.Send(Encoding.UTF8.GetBytes(eot));

                        WriteLog("eot sent", ConsoleColor.DarkYellow);
                        Thread.Sleep(1000);


                        bool s = db.DeleteMessage(messageId);
                        Console.WriteLine("Message deleted ->" + s, ConsoleColor.DarkYellow);


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

                logger.WriteLog(logger.logMessageWithFormat("subscriber started", "info"));
                Socket receiver = listener.Accept();
                //receiver.ReceiveTimeout = 10000;

               // logger.WriteLog(logger.logMessageWithFormat("successfully connected to machine", "info"));

                string data = null;

                byte[] bytes = null;


                //sendorderme(receiver);


                while (true)
                {
                    //WriteLog("Iteration number " + iter++);
                    try
                    {
                        // Incoming data from the client.    

                        if (!receiver.IsConnected())
                        {
                            logger.WriteLog(logger.logMessageWithFormat("Connection dropout", "warning"));
                            Listen();
                        }
                        else
                        {
                            //logger.WriteLog("Subscriber.cs => info: connection alive!!!");
                        }


                        //SendOrder(receiver);
                        sendorderme(receiver);
                        //handle enquiry
                        bytes = new byte[1024];
                        bytesRec = receiver.Receive(bytes);
                        data = Encoding.UTF8.GetString(bytes, 0, bytesRec);

                        Console.WriteLine(data);

                        //receiver.Send(Encoding.UTF8.GetBytes(hl7ack));

                        if (data.IndexOf(enq) > -1)

                        {

                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            logger.WriteLog(logger.logMessageWithFormat(">><ENQ>","info"));
                        }

                        //handle text start
                        if (data.IndexOf(stx) > -1)
                        {

                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            logger.WriteLog( logger.logMessageWithFormat(">><STX>","info"));
                        }
                        //if (data.IndexOf(rs) > -1)
                        //{
                        //    Console.WriteLine("rs received!");
                        //}
                        // handle main body

                        //handle etx
                        if (data.IndexOf(etx) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            //save mainMessageRecord to database
                            Database database = new Database();
                            string[] inputStringArray = mainMessageRecords.Select(i => i.ToString()).ToArray();
                            database.InsertResult(data);
                            //reset mainMEssageRecord
                            mainMessageRecords.Clear();
                            logger.WriteLog(logger.logMessageWithFormat(">><ETX>","info"));
                        }
                        //handle eot
                        if (data.IndexOf(eot) > -1)
                        {
                            receiver.Send(Encoding.UTF8.GetBytes(ack));
                            logger.WriteLog(logger.logMessageWithFormat(">><EOT>", "info"));

                        }


                        //send order
                        //SendOrder(receiver);

                    }
                    catch (Exception e)
                    {
                        logger.WriteLog(logger.logMessageWithFormat(e.ToString(),"error"));
                        //throw e;
                        Listen();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog(logger.logMessageWithFormat(ex.ToString(), "error"));
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
                msh.Field(3, "DH56");
                msh.Field(4, "Human");
                msh.Field(7, DateTime.Now.ToString("yyyyMMddhhmmsszzz"));
                msh.Field(9, "ACK^R01");
                msh.Field(10, Guid.NewGuid().ToString());
                msh.Field(11, "P");
                msh.Field(12, "2.3.1");
                msh.Field(18, "UNICODE");
                response.Add(msh);

                Segment msa = new Segment("MSA");
                msa.Field(1, "AA");
                msa.Field(2, messageControlID);
                response.Add(msa);


                // Create a Minimum Lower Layer Protocol (MLLP) frame.
                // For this, just wrap the data lik this: <VT> data <FS><CR>
                StringBuilder frame = new StringBuilder();
                frame.Append((char)0x0b);
                frame.Append(response.SerializeMessage());
                frame.Append((char)0x1c);
                frame.Append((char)0x0d);

                return frame.ToString();
            }
            catch (Exception ex)
            {
                // Exception handling

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
