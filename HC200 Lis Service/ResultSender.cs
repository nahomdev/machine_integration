using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading;

namespace HC200_Lis_Service
{
    class ResultSender
    {
        Database db;
        Thread resultSender;
        ConfigFileHundler configFileHundler;
        Logger logger;
        public ResultSender()
        {
            db = new Database();
            configFileHundler = new ConfigFileHundler();
            logger = new Logger();
        }

        public void startResultSender()
        {
            resultSender = new Thread(new ThreadStart(StartSending));
            resultSender.Start();
        }

        public void stopResultSender()
        {
            resultSender.Join();
            resultSender.Abort();
        }
        public void StartSending()
        {
            logger.WriteLog(logger.logMessageWithFormat("result sender started"));
            while (true)
            {

                
                MessageInput inp = GetMessage();

                
                if (inp != null)
                {
                    Console.WriteLine("3");
                    WriteLog("New result from machine arrived!");
                    logger.WriteLog(logger.logMessageWithFormat("New result from machine arrived!"));
                    SendMessage(inp).GetAwaiter();
                }
                Thread.Sleep(10);
                }
        }
        private async Task SendMessage(MessageInput message)
        {
            try
            {
                logger.WriteLog(logger.logMessageWithFormat("sending result to orbit started"));
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                var data = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

                var url = configFileHundler.GetCmsApi();
                //var url = "http://10.10.2.39:8051/orders/send-to-nordic";
                var client = new HttpClient();
                Console.WriteLine("json : {0}", json);
                var response = await client.PostAsync(url, data);
                logger.WriteLog(logger.logMessageWithFormat(@"sending data to "+configFileHundler.GetCmsApi()) );
                string result = response.Content.ReadAsStringAsync().Result;
                WriteLog("Response from server ->" + result);
                logger.WriteLog(logger.logMessageWithFormat("Server Response " + result));
            }
            catch (Exception exe)
            {
               
                logger.WriteLog(logger.logMessageWithFormat(exe.Message, "error"));
               
            }
        }

        private void WriteLog(string message, ConsoleColor color = ConsoleColor.DarkBlue)
        {
            Helper.WriteLog(message, "ResultSender", color);
        }


        private MessageInput GetMessage()
        {
            try
            {
                
                DataTable tbl = db.SelectAllResults();
                //Subscriber.WriteLogToFile("tbl =>"+tbl);

                 if (tbl.Rows.Count > 0) 
                {
                    DataRow dr = tbl.Rows[0];
                    string savedMessage = dr["message"].ToString().Trim();
                    string[] splitedMessage = SplitMessage(savedMessage);
                    ASTMMessage aSTMMessage = new ASTMMessage(splitedMessage);


                    db.DeleteResult(int.Parse(dr["id"].ToString().Trim()));
                    return aSTMMessage.GetResultMessageObject();
                }
                else
                {
                    //logger.WriteLog(logger.logMessageWithFormat("returnning null because no results found in db count must be 0"));
                    return null;
                }
            }
            catch (Exception exe)
            {
                WriteLog("Exception occured while reading and preparing result message " + exe.Message);
                logger.WriteLog(logger.logMessageWithFormat("exception occured while reading and preparing reuslts"+ exe.Message, "error"));
                return null;
            }
        }
        private string[] SplitMessage(string dataToSend)
        {
            return Regex.Split(dataToSend, "##");
        }
    }
}
