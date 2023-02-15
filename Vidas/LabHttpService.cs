using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
 

namespace Vidas
{
    public class ResFormat
    {
        public string Message { get; set; }
        public bool OK { get; set; }
    }
    public class TestInput
    {
        public string testid { get; set; }
        public string panel { get; set; }
        public string code { get; set; }
        public string result { get; set; }
    }



    public class MessageInput
    {
        public string sampleid { get; set; }
        public List<TestInput> tests { get; set; }

    }

    public class Test
    {
        public string code { get; set; }
    }
    public class OrderInput
    {

        public string PatientId { get; set; }
        public string OrderId { get; set; }
        public List<Test> Tests { get; set; }

    }

    [ServiceContract]
    public interface ILabService
    {
        [OperationContract]
        [WebGet]
        string SayHello();

        [OperationContract]
        [WebInvoke(
            ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json)]
        ResFormat CreateNewOrder(List<MessageInput> messageInput);
    }

    public class LabService : ILabService
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
        Thread httpThread;
        Logger logger;
        public LabService()
        {
            logger = new Logger();
        }

        public void startServer()
        {
            httpThread = new Thread(new ThreadStart(StartService));
            httpThread.Start();
        }

        public void stopServer()
        {
            httpThread.Join();
            httpThread.Abort();
        }
        private void WriteLog(string message)
        {
            Helper.WriteLog(message, "LabServiceHTTP");
        }
        public string SayHello()
        {
            return string.Format("Hello world from get");
        }

        public ResFormat insertOrders(MessageInput messageInput)
        {
            Database db = new Database();
            try
            {

                if (messageInput.sampleid == null || messageInput.sampleid.Trim() == string.Empty)
                {
                    return new ResFormat() { Message = "Order id cannot be null or empty!", OK = false };
                }
                if (messageInput.tests == null || messageInput.tests.Count == 0)
                {
                    return new ResFormat() { Message = "Test cannot be null or empty!", OK = false };
                }
                foreach (var item in messageInput.tests)
                {
                    if (item.code == null || item.code.Trim() == string.Empty)
                    {
                        return new ResFormat() { Message = "In valid value in Tests (Tests cannot be null or empty)!", OK = false };
                    }
                }
                if (messageInput != null)
                {
                    Console.WriteLine("generating full order");
                    Mapping mapping = new Mapping();
                    mapping.startMapping(messageInput);
                    string astmMessage = GenerateOrderString(messageInput);


                    return db.InsertMessage(astmMessage);
                }
                else
                {
                    return new ResFormat() { Message = "Message content cannot be null!", OK = false };
                }
            }
            catch (Exception exe)
            {
                return new ResFormat() { Message = "Something went wrong, failed to insert message => " + exe.Message, OK = false };
            }
            finally
            {
                db.CloseConnection();
            }

        }


        public ResFormat CreateNewOrder(List<MessageInput> messageInput)
        {



            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");

            WriteLog("New message from EMR or LIS arrived!");
            
            List<ResFormat> returns = new List<ResFormat>();
            foreach (MessageInput el in messageInput)
            {
                ResFormat format = insertOrders(el);
                Console.WriteLine("something : " + format.Message.Split(' ')[2]);

                if (format.Message.Split(' ')[2] != "successfully.")
                {
                    returns.Add(new ResFormat() { Message = "" + format.Message, OK = false });
                }
                else
                {
                    continue;
                }


            }
            if (returns.Count > 0)
            {
                string resMessage = String.Join("-", returns.Select(x => x.Message.ToString()).ToList());
                return new ResFormat() { Message = resMessage, OK = false };
            }
            else
            {
                return new ResFormat() { Message = "Message inserted successfully.", OK = true };

            }

            return new ResFormat() { Message = "all cases not fulfilled :(", OK = false };
        }

        public string GenerateOrderString(MessageInput orderInput)
        {
            try
            {

                string msg = null;
                msg = "mtmpr|pi" + 123456 + "|si|ci" + orderInput.sampleid + "|rt" + orderInput.tests[0].code + "|qd1|";
                msg = stx + 1 + msg + etx + checksumCalculate(msg) + cr + lf;
                Console.WriteLine(checksumCalculate(msg));

                return msg;

            }
            catch (Exception ex)
            {
                logger.WriteLog("LabHttpService :" + ex.Message);
                return null;
            }
        }


        public string checksumCalculate(string msg)
        {
            string sum = "0011";
            byte[] res = null;
            string truncated = null;
            string full_order_msg = "1" + msg;
            var binaryString = ToBinary(ConvertToByteArray(full_order_msg, Encoding.ASCII));

            var arr = binaryString.Split(' ');

            foreach (var item in arr)
            {
                sum = AddBinary(sum, item);
            }


            for (int i = sum.Length - 1; i >= 0; i--)
            {

                truncated = sum.ElementAt(i) + truncated;
                if (truncated.Length == 8)
                {
                    break;
                }


            }

            var hex = BinaryStringToHexString(truncated);

            return hex;
        }




        public static byte[] ConvertToByteArray(string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }
        public static String ToBinary(Byte[] data)
        {
            return string.Join(" ", data.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        }

        public static string AddBinary(string a, string b)
        {
            string result = "";

            int s = 0;

            int i = a.Length - 1, j = b.Length - 1;
            while (i >= 0 || j >= 0 || s == 1)
            {
                s += ((i >= 0) ? a[i] - '0' : 0);
                s += ((j >= 0) ? b[j] - '0' : 0);
                result = (char)(s % 2 + '0') + result;

                s /= 2;
                i--; j--;
            }
            return result;
        }

        public static string BinaryStringToHexString(string binary)
        {
            StringBuilder result = new StringBuilder(binary.Length / 8 + 1);



            int mod4Len = binary.Length % 8;
            if (mod4Len != 0)
            {
                binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
            }

            for (int i = 0; i < binary.Length; i += 8)
            {
                string eightBits = binary.Substring(i, 8);
                result.AppendFormat("{0:X2}", Convert.ToByte(eightBits, 2));
            }

            return result.ToString();
        }


        public void StartService()
        {
            WebServiceHost host = new WebServiceHost(typeof(LabService), new Uri("http://localhost:8002/"));
            ServiceEndpoint ep = host.AddServiceEndpoint(typeof(ILabService), new WebHttpBinding(), "");
            ServiceDebugBehavior sdb = host.Description.Behaviors.Find<ServiceDebugBehavior>();
            sdb.HttpHelpPageEnabled = false;

            host.Open();
        }
        private string GenerateASTMString(MessageInput input)
        {
            try
            {
                string headerRecord = @"H|\^&||PSWD|Maglumi User|||||Lis||P|E1394-97|20211019";
                string patientRecord = "P|1";
                string orderRecordPrefix = "O|1|" + input.sampleid + "^^^||";
                string orderRecordSegment = "^^^" + input.tests[0] + "^^";
                if (input.tests.Count > 1)
                {
                    foreach (TestInput test in input.tests)
                    {
                        orderRecordSegment += @"\" + "^^^" + test.code + "^^";
                    }
                }
                string orderRecordSuffix = "|||" + DateTime.Now.ToString("yyyyMMddHHmmss") + "||||N||1||||||||||||O";
                string orderRecord = orderRecordPrefix + orderRecordSegment + orderRecordSuffix;
                string endRecord = "L|1|F";

                StringBuilder orderMessage = new StringBuilder();
                orderMessage.Append(headerRecord + "##");
                orderMessage.Append(patientRecord + "##");
                orderMessage.Append(orderRecord + "##");
                orderMessage.Append(endRecord);
                //return orderMessage.ToString();
                return @"H|\^&|||Host|||||||P|1|20010226080000##P|1|PID001|RID001##O|1|SID001^N^01^5||^^^f1^sIgE^1\^^^f2^sIgE^1||20010226090000|||N||1||||||||||||O##L|1|F";
            }
            catch (Exception exe)
            {
                logger.WriteLog("Error while generating ASTM string _" + exe.Message);
                return null;
            }
        }

    }
}
