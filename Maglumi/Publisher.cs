using Maglumi;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Maglumi
{
    public class Publisher
    {
        string soh = char.ConvertFromUtf32(1);
        string stx = char.ConvertFromUtf32(2);
        string etx = char.ConvertFromUtf32(3);
        string eot = char.ConvertFromUtf32(4);
        string enq = char.ConvertFromUtf32(5);
        string ack = char.ConvertFromUtf32(6);
        string nack = char.ConvertFromUtf32(21);
        string etb = char.ConvertFromUtf32(23);
        string lf = char.ConvertFromUtf32(10);
        string cr = char.ConvertFromUtf32(13);
        private System.Net.Sockets.Socket sender;
        byte[] localhost;
        int port;
        public Publisher(byte[] localhost, int port)
        {
            this.localhost = localhost;
            this.port = port;
        }
        private void WriteLog(string message)
        {
            Helper.WriteLog(message, "Machine", ConsoleColor.Blue);
        }

        public void Send()
        {
            IPAddress address = new IPAddress(localhost);
            IPEndPoint endPoint = new IPEndPoint(address, port);
            sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(endPoint);

            WriteLog("Socket connected to " +
                sender.RemoteEndPoint.ToString());
            int iter = 0;
            while (true)
            {
                WriteLog("Iteration number " + iter++);

                try
                {
                    // Connect to Remote EndPoint  
                    string[] resultMessage = new string[] {
                        @"H|\^&||| ImmunoCAP Data Manager^1.00^1.00|||||||P|1|20010226080000",
                        "P|1|PID001|RID001",
                        "O|1|21427^N^01^5||^^^f1^sIgE^1|||20010226090000||||N||1||||||||||||O",
                        "R|1|^^^f1^sIgE^1|17.500^2^Positive^0/1^1.300|ml/g||||F||||20010226100000|I000001",
                        "L|1|F",
                    };

                    string[] magluminResult = new string[] {
                        @"H|\^&||PSWD|Maglumi User|||||Lis||P|E1394-97|20211019",
                        "P|1",
                        "O|1|21427||^^^TSH",
                        "R|1|^^^TSH|8.174|uIU/mL|0.3 to 4.5|H||||||20211015173356",
                        "R|1|^^^FT4|4.74|uIU/mL|0.3 to 4.5|H||||||20211015173356",
                        "L|1|N"
                    };


                    string[] mainMessage = new string[] {
                        @"H|\^&||PSWD|Maglumi 1000|||||Lis||P|E1394-97|20100319",
                        "P|1",
                        "O|1|4||^^^ACTH|R",
                        "O|2|4||^^^AFP|R",
                        "O|3|4||^^^ALD|R",
                        "O|4|4||^^^B-HCG|R",
                        "O|5|4||^^^B2-MG|R",
                        "O|6|4||^^^BGP|R",
                        "O|7|4||^^^BGW|R",
                        "O|8|4||^^^C IV|R",
                        "O|9|4||^^^CA125|R",
                        "O|10|4||^^^CA153|R",
                        "O|11|4||^^^CA199|R",
                        "O|12|4||^^^CA242|R",
                        "O|13|4||^^^CA50|R",
                        "O|14|4||^^^CA724|R",
                        "O|15|4||^^^CAFP|R",
                        "L|1|N"
                };
                    // Encode the data string into a byte array.    


                    //send enq
                    sender.Send(Encoding.UTF8.GetBytes(enq));
                    WriteLog("enqury sent");
                    Thread.Sleep(1000);
                    //GetResponse(sender);
                    //send stx
                    sender.Send(Encoding.UTF8.GetBytes(stx));
                    WriteLog("stx sent");
                    Thread.Sleep(1000);
                    //GetResponse(sender);
                    //send main message

                    foreach (string msg in magluminResult)
                    {
                        byte[] message = Encoding.UTF8.GetBytes(msg);
                        sender.Send(message);
                        Thread.Sleep(100);
                        //GetResponse(sender);

                    }

                    Thread.Sleep(1000);
                    //send stx
                    sender.Send(Encoding.UTF8.GetBytes(etx));
                    WriteLog("etx sent");
                    Thread.Sleep(1000);
                    //GetResponse(sender);


                    //send stx
                    sender.Send(Encoding.UTF8.GetBytes(eot));
                    WriteLog("eot sent");
                    Thread.Sleep(1000);
                    //GetResponse(sender);







                    // Release the socket.    
                    //sender.Shutdown(SocketShutdown.Both);
                    //sender.Close();
                    //Console.ReadKey();

                }
                catch (ArgumentNullException ane)
                {
                    WriteLog("ArgumentNullException " + ane.ToString());
                }
                catch (SocketException se)
                {
                    WriteLog("SocketException : " + se.ToString());
                }
                catch (Exception e)
                {
                    WriteLog("Unexpected exception : " + e.ToString());
                }
                //finally
                //{
                //    sender.Close();
                //}

            }

        }
    }
}

