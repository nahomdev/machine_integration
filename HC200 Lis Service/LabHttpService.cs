using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;


namespace HC200_Lis_Service
{

    [ServiceContract]
    public interface ILabService
    {
        [OperationContract]
        [WebGet]
        string SayHello();


        [OperationContract]
        [WebInvoke(
            ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json)]
        ResFormat CreateNewOrder(AllMessage allMessage);

    }

    public class LabService : ILabService
    {

        public int patientS = 1;
        public int orderS = 1;
        Thread startServiceThread;
        Logger logger;

        public LabService()
        {
            logger = new Logger();
        }
        private void WriteLog(string message)
        {
            Helper.WriteLog(message, "LabServiceHTTP");
        }
        public string SayHello()
        {
            return string.Format("Hello world from get");
        }
        public ResFormat CreateNewOrder(AllMessage allMessage)
        {
            Database db = new Database();
            WriteLog("New message from EMR or LIS arrived!");
            string headerRecord = @"H|\^&|||HSX00^V1.0|||||Host||P|1|20110117";
            string endRecord = "L||N";
            string astmMessage = "" + headerRecord + "##";

            try
            {
                
                foreach (var messageInput in allMessage.all_orders)
                {
                    if (messageInput != null)
                    {
                        if (messageInput.order_id == null || messageInput.order_id.Trim() == string.Empty)
                        {
                            return new ResFormat() { Message = "Order id cannot be null or empty!", OK = false };
                        }
                        if (messageInput.result_obs == null || messageInput.result_obs.Count == 0)
                        {
                            return new ResFormat() { Message = "Test cannot be null or empty!", OK = false };
                        }

                        foreach (var item in messageInput.result_obs)
                        {

                            if (item.code == null || item.code.Trim() == string.Empty)
                            {
                                return new ResFormat() { Message = "In valid value in Tests (Tests cannot be null or empty)!", OK = false };
                            }
                        }
                        if (messageInput != null)
                        {


                            astmMessage += GenerateASTMString(messageInput);

                            this.patientS++;
                        }
                        else
                        {
                            return new ResFormat() { Message = "Message content cannot be null!", OK = false };
                        }
                    }
                }


                astmMessage += endRecord;

                Console.WriteLine("data to insert : {0}", astmMessage);
                return db.InsertMessage(astmMessage);
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

        public void startLabService()
        {
            startServiceThread = new Thread(new ThreadStart(StartService));
            startServiceThread.Start();
        }

        public void stopLabService()
        {
            startServiceThread?.Join();
            startServiceThread.Abort();
        }
        

        public void StartService()
        {

            ConfigFileHundler fileHundler = new ConfigFileHundler();
           
            WebServiceHost host = new WebServiceHost(typeof(LabService), new Uri("http://"+fileHundler.GetIpAddress()+":"+fileHundler.GetPort()+"/hs200"));
            ServiceEndpoint ep = host.AddServiceEndpoint(typeof(ILabService), new WebHttpBinding(), "");
            ServiceDebugBehavior sdb = host.Description.Behaviors.Find<ServiceDebugBehavior>();
            sdb.HttpHelpPageEnabled = false;

            host.Open();
            Console.WriteLine(new  Uri("http://" + fileHundler.GetIpAddress() + ":" + fileHundler.GetPort() + "/hs200"));
            logger.WriteLog(logger.logMessageWithFormat("Lab Http Service is running at port 8000"));
        }

        private string GenerateASTMString(MessageInput input)
        {


            try
            {
                string[] name = input.patientName.Split(' ');
                string fullname = name[1] + " " + name[2];

                string headerRecord = @"H|\^&|||HSX00^V1.0|||||Host||P|1|20110117";

                string patientRecord = "P|" + this.patientS + "||" + input.order_id + "||test||" + DateTime.Now.ToString("yyyyMMdd") + "|Undefined|||||||||||||||||||||||||";
                string orderRecordeach = "";
                string commentRecord = "C|1|||";
                
                string orderRecordSegment = "^^^" + input.result_obs[0].code;
                List<string> orderRecord = new List<string>();
                int i = 0;
                
                if (input.result_obs.Count >= 1)
                {
                    foreach (TestInput test in input.result_obs)
                    {
                        if (i >= 0)
                        {
                            orderRecordeach = "O|" + this.orderS + "|||" + test.code;
                            string orderRecordSuffix = "|False||||||||||Serum|||||||||||||||";

                            orderRecord.Add(orderRecordeach + orderRecordSuffix);
                        }
                        this.orderS++;
                        i++;
                    }
                }


                string endRecord = "L||F";

                StringBuilder orderMessage = new StringBuilder();

                orderMessage.Append(patientRecord + "##"+commentRecord+"##");
                foreach (string order in orderRecord)
                {
                    orderMessage.Append(order + "##");
                }


                this.orderS = 1;
                return orderMessage.ToString();
              }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat("Error while generating ASTM string _" + exe.Message + exe.StackTrace, "error"));
                return null;
            }
        }

    }
}

