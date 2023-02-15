using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using Maglumi;

namespace Maglumi
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
            logger.WriteLog(message + " LabServiceHTTP");
        }
        public string SayHello()
        {
            return string.Format("Hello world from get");
        }
        public ResFormat insertOrders(MessageInput el)
        {
            Database db = new Database();
            try
            {
                if (el.sampleid == null || el.sampleid.Trim() == string.Empty)
                {
                    return new ResFormat() { Message = "Order id cannot be null or empty!", OK = false };
                }
                if (el.tests == null || el.tests.Count == 0)
                {
                    return new ResFormat() { Message = "Test cannot be null or empty!", OK = false };
                }
                foreach (var item in el.tests)
                {
                    if (item.code == null || item.code.Trim() == string.Empty)
                    {
                        return new ResFormat() { Message = "In valid value in Tests (Tests cannot be null or empty)!", OK = false };
                    }
                }
                if (el != null)
                {
                    Console.WriteLine("message get mapping start");
                    Mapping mapping = new Mapping();
                    mapping.startMapping(el);
                    string astmMessage = GenerateASTMString(el);
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
            Console.WriteLine("data received : " + messageInput.Count);
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
        public void StartService()
        {
            WebServiceHost host = new WebServiceHost(typeof(LabService), new Uri("http://localhost:8001/"));
            ServiceEndpoint ep = host.AddServiceEndpoint(typeof(ILabService), new WebHttpBinding(), "");
            ServiceDebugBehavior sdb = host.Description.Behaviors.Find<ServiceDebugBehavior>();
            sdb.HttpHelpPageEnabled = false;
            foreach (ServiceEndpoint EP in host.Description.Endpoints)
                EP.Behaviors.Add(new BehaviorAttribute());
            host.Open();
            logger.WriteLog("Lab Http Service is running at port 8000");
        }
        private string GenerateASTMString(MessageInput input)
        {

            try
            {

                string headerRecord = @"H|\^&||PSWD|Maglumi User|||||Lis||P|E1394-97|20211019";
                string patientRecord = "P|1|";
                string orderRecordPrefix = "O|1|" + input.sampleid + "||";
                string orderRecordSegment = "^^^" + input.tests[0].code;
                int i = 0;
                if (input.tests.Count > 1)
                {
                    foreach (TestInput test in input.tests)
                    {
                        if (i > 0)
                        {
                            orderRecordSegment += @"\" + "^^^" + test.code;
                        }
                        i++;
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

                return orderMessage.ToString();
                //return @"H|\^&|||Host|||||||P|1|20010226080000##P|1|PID001|RID001##O|1|SID001^N^01^5||^^^f1^sIgE^1\^^^f2^sIgE^1||20010226090000|||N||1||||||||||||O##L|1|F";
            }
            catch (Exception exe)
            {
                logger.WriteLog("Error while generating ASTM string _" + exe.Message);
                return null;
            }
        }

    }
}
