using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sysmex_Lis_Service
{
    public class ResFormat
    {
        public string Message { get; set; }
        public bool OK { get; set; }
    }
    public class TestInput
    {
        public string code { get; set; }
        public string result { get; set; }
    }
    public class MessageInput
    {
        public string order_id { get; set; }

        public string machine_code = "SYSMEX";
        public List<TestInput> result_obs { get; set; }

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
        ResFormat CreateNewOrder(MessageInput messageInput);
    }

    public class LabService : ILabService
    {
        private void WriteLog(string message)
        {
            Helper.WriteLog(message, "LabServiceHTTP");
        }
        public string SayHello()
        {
            return string.Format("Hello world from get");
        }
        public ResFormat CreateNewOrder(MessageInput messageInput)
        {
            Database db = new Database();
            WriteLog("New message from EMR or LIS arrived!");
            try
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
                    string astmMessage = GenerateASTMString(messageInput);
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
        public void StartService()
        {
            WebServiceHost host = new WebServiceHost(typeof(LabService), new Uri("http://localhost:8000/"));
            ServiceEndpoint ep = host.AddServiceEndpoint(typeof(ILabService), new WebHttpBinding(), "");
            ServiceDebugBehavior sdb = host.Description.Behaviors.Find<ServiceDebugBehavior>();
            sdb.HttpHelpPageEnabled = false;

            host.Open();
            WriteLog("Lab Http Service is running at port 8000");
        }
        private string GenerateASTMString(MessageInput input)
        {
            try
            {
                string headerRecord = @"H|\^&||PSWD|Maglumi User|||||Lis||P|E1394-97|20211019";
                string patientRecord = "P|1";
                string orderRecordPrefix = "O|1|" + input.order_id + "^^^||";
                string orderRecordSegment = "";//"^^^" + input.Tests[0] + "^^";
                if (input.result_obs.Count >= 1)
                {
                    foreach (TestInput test in input.result_obs)
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
                string sampleMessage = @"H|\^&||PSWD|Maglumi User|||||Lis||P|E1394-97|20140613##P|1##O|1|TESTORDER1||^^^TSH\^^^FT4\^^^FT3##R|1|^^^TSH|1.22|uIU/mL|0.4 to 4.5|N||||||20131228162937##R|2|^^^FT4|11.06|pg/mL|7.2 to 17.2|N||||||20131228161701##R|3|^^^FT3|1.743|pg/mL|1.21 to 4.18|N||||||20131228162319##L|1|N";
                //return @"H|\^&||PSWD|Maglumi User|||||Lis||P|E1394-97|20211025##P|1##O|1|TESTORDER1^^^||^^^T3^^||20010226090000|||N||1||||||||||||O##L|1|F";
                return sampleMessage;
            }
            catch (Exception exe)
            {
                WriteLog("Error while generating ASTM string _" + exe.Message);
                return null;
            }
        }

    }
}
