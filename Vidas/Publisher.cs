using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Vidas;

namespace Vidas
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


                    string[] magluminResult = new string[] {
                        @"H|\^&||PSWD|Maglumi User|||||Lis||P|E1394-97|20211019",
                        "P|1",
                        "O|1|BCHE_435C||^^^TSH",
                        "R|1|^^^TSH|8.174|uIU/mL|0.3 to 4.5|H||||||20211015173356",
                        "L|1|N"
                    };


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

                    //foreach (string msg in mainMessage)
                    //{
                    //    byte[] message = Encoding.UTF8.GetBytes(msg);
                    //    sender.Send(message);
                    //    Thread.Sleep(100);
                    //    //GetResponse(sender);

                    //}
                    string message = "1mtrsl|pis0913-024|pn|pb|ps-|so|si|cis21427|rtFSH|rnFSH|tt12:25|td13/01/2022|ql|qn30.94 mUI/ml|y3mUI/ml|qd1|ncvalid|idVIDASPC01|sn|m4haimanot|♥12";
                    sender.Send(Encoding.UTF8.GetBytes(message));

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
