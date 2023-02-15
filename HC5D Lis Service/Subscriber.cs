using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

using System.Runtime.CompilerServices;
using System.IO;

namespace HC5D_Lis_Service
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
    class Subscriber
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
        private IPEndPoint endpoint;
        private Socket listener;
        private static readonly byte[] ArshoIP = { 10, 10, 2, 28 };
        private static readonly byte[] Localhost = { 127, 0, 0, 1 };
        private const int Port = 9999;
        Thread resultSender;
        Thread listnerThread;
        Logger logger;

        public Subscriber()
        {
            logger = new Logger();
            ConfigFileHundler confighundler = new ConfigFileHundler();

            
            System.Net.IPEndPoint endPoint = confighundler.GetEndPoint();
           
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(endPoint);
            Console.WriteLine("Listening on port " + endPoint);
            logger.WriteLog(logger.logMessageWithFormat("Listening on port " + endPoint));
            listener.Listen(1);
        }
        public void startServer()
        {
            listnerThread = new Thread(new ThreadStart(Listen));
            listnerThread.Start();
        }
        public void stopServer()
        {
            listnerThread?.Join();
            listnerThread.Abort();
        }
        
        private string[] SplitMessage(string dataToSend)
        {
            return Regex.Split(dataToSend, "##");
        }






        private static char END_OF_BLOCK = '\u001c';
        private static char START_OF_BLOCK = '\u000b';
        private static char CARRIAGE_RETURN = (char)13;



        public void sendOrder(Socket networkStream, string header)
        {
            string output = new string(header.Where(c => !char.IsControl(c)).ToArray());
            string[] msh9 = output.Split('|');

            msh9[8] = "ACK^R01";

            output = String.Join("|", msh9);




            var testHl7MessageToTransmit = new StringBuilder();
            //testHl7MessageToTransmit.Append(START_OF_BLOCK)
            //   .Append("MSH|^~\\&|AcmeHIS|StJohn|CATH|StJohn|20061019172719||ORM^O01|MSGID12349876|P|2.3")
            //   .Append(CARRIAGE_RETURN)
            //   .Append("PID|||20301||Durden^Tyler^^^Mr.||19700312|M|||88 Punchward Dr.^^Los Angeles^CA^11221^USA|||||||")
            //   .Append(CARRIAGE_RETURN)
            //   .Append("PV1||O|OP^^||||4652^Paulson^Robert|||OP|||||||||9|||||||||||||||||||||||||20061019172717|20061019172718")
            //   .Append(CARRIAGE_RETURN)
            //   .Append("ORC|NW|20061019172719")
            //   .Append(CARRIAGE_RETURN)
            //   .Append("OBR|1|20061019172719||76770^Ultrasound: retroperitoneal^C4|||12349876")
            //   .Append(CARRIAGE_RETURN)
            //   .Append(END_OF_BLOCK)
            //   .Append(CARRIAGE_RETURN);


            testHl7MessageToTransmit.Append(START_OF_BLOCK)
              .Append(output)
              .Append(CARRIAGE_RETURN)
              .Append("MSA|AA|" + msh9[9])
              .Append(CARRIAGE_RETURN)
              .Append(END_OF_BLOCK)
              .Append(CARRIAGE_RETURN);

            var sendMessageByteBuffer = Encoding.UTF8.GetBytes(testHl7MessageToTransmit.ToString());

            //send a message through this connection using the IO stream


            networkStream.Send(sendMessageByteBuffer);
            Console.WriteLine("Data was sent data to server successfully....");
            //var receiveMessageByteBuffer = Encoding.UTF8.GetBytes(testHl7MessageToTransmit.ToString());

            //var bytesReceivedFromServer = networkStream.Read(receiveMessageByteBuffer, 0, receiveMessageByteBuffer.Length);
            //// Our server for this example has been designed to echo back the message
            //// keep reading from this stream until the message is echoed back
            //while (bytesReceivedFromServer > 0)
            //{
            //    if (networkStream.CanRead)
            //    {
            //        bytesReceivedFromServer = networkStream.Read(receiveMessageByteBuffer, 0, receiveMessageByteBuffer.Length);
            //        if (bytesReceivedFromServer == 0)
            //        {
            //            break;
            //        }
            //    }
            //}
            //var receivedMessage = Encoding.UTF8.GetString(receiveMessageByteBuffer);
            //Console.WriteLine("Received message from server: {0}", receivedMessage);

            return;
        }
        public void Listen()
        {
            try { 

            logger.WriteLog(logger.logMessageWithFormat("Subscriber Started."));
            int byteRec = 0;



            byte[] bytes = new byte[1];

            string responsedata = null;

            List<string> msg = new List<string>();
            string res = null;

            string full_message = null;
            bool flag = false;

            Socket receiver = listener.Accept();
                logger.WriteLog("after client accepted ");
            while (true)
            { 
                try
                {
                    
                    bytes = new byte[1];
                    byteRec = receiver.Receive(bytes);
                    //responsedata = Encoding.UTF8.GetString(bytes, 0, byteRec);

                        responsedata = Encoding.UTF8.GetString(bytes);
                       
                    if (responsedata == char.ConvertFromUtf32(13))
                    {
                       
                        //s = new string(msg.Where(c => !char.IsControl(c)).ToArray());

                        msg.Add(res);
                            logger.WriteLog(res);
                        res = null;


                        //var message = parser.Parse(s); 
                        if (flag == true)
                        {

                            sendOrder(receiver, msg[0]);
                            msg.Clear();
                            flag = false;
                        }


                        continue;
                    }


                    if (responsedata == char.ConvertFromUtf32(28))
                    {

                        full_message = String.Join("##", msg);

                        Database database = new Database();
                            logger.WriteLog("before inserting results");
                        database.InsertResult(full_message);
                            logger.WriteLog("after inserting result, Successful");

                        flag = true;

                    }

                    //msg.Add(responsedata);
                    res += responsedata;
 

                }
                catch (Exception ex)
                {
                    logger.WriteLog(logger.logMessageWithFormat("Exception occured "+ex.ToString(),"error"));
                    Console.WriteLine(ex.Message);
                    Listen();
                }
                Thread.Sleep(10);
            }
            } catch(Exception ex)
            {
                logger.WriteLog("Exception occured " + ex.ToString());
            }
        }
    }
}
