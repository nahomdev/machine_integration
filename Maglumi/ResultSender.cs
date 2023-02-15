using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using Maglumi;

namespace Maglumi
{
    public class ResultSender
    {
        Database db = null;
        Thread resultSenderThread;
        Logger logger;
        ConfigFileHundler configHundler;
        public ResultSender()
        {
            db = new Database();
            logger = new Logger();
            configHundler = new ConfigFileHundler();
        }

        public void startServer()
        {
            resultSenderThread = new Thread(new ThreadStart(StartSending));
            resultSenderThread.Start();

        }

        public void stopServer()
        {
            resultSenderThread.Join();
            resultSenderThread?.Abort();

        }


        public void StartSending()
        {
            logger.WriteLog(logger.logMessageWithFormat("Result Sender Started", "info"));

            while (true)
            {
                MessageInput inp = GetMessage();
                if (inp != null)
                {



                    DataTable tbl = db.SelectAllMapping();
                    logger.WriteLog(""+tbl.Rows.Count);
                    if (tbl.Rows.Count > 0)
                    {
                        DataRow row;
                        for (int i = 0; i < tbl.Rows.Count; i++)
                        {
                            row = tbl.Rows[i];

                            string savedMessage = row["message"].ToString().Trim();

                            string[] splitedMessage = Regex.Split(savedMessage, "##");
                            logger.WriteLog("flag 1");
                            
                            if (splitedMessage[0] == inp.sampleid)
                            {
                                logger.WriteLog("flag 2");
                                Console.WriteLine("inside maching codes");

                                for (int j = 1; j < splitedMessage.Length; j++)
                                {
                                    logger.WriteLog("flag 3");
                                    string[] spliteTest = splitedMessage[j].Split('*');
                                    //input.Tests.Where(ts => ts.code == spliteTest[0] ? ts.result = spliteTest[1] : ts.result =null)
                                    foreach (var test in inp.tests)
                                    {
                                        logger.WriteLog("flag 4");
                                         if (test.code == spliteTest[0])
                                        {
                                            logger.WriteLog("flag 5");
                                            //test.result = spliteTest[1];
                                            Console.WriteLine("test code real {0} and test code comming", test.code, spliteTest[0]);
                                            test.testid = spliteTest[1];
                                            test.panel = spliteTest[2];

                                        }
                                    }
                                }

                            }
                            //db.DeleteMapping(int.Parse(row["id"].ToString().Trim()));
                            //db.DeleteMessage(int.Parse(row["id"].ToString().Trim()));
                        }
                    }

                    logger.WriteLog(logger.logMessageWithFormat("New result from machine arrived!","info"));
                    SendMessage(inp).GetAwaiter();
                }
                Thread.Sleep(10);
            }
        }
        private void WriteLog(string message, ConsoleColor color = ConsoleColor.DarkBlue)
        {
            Helper.WriteLog(message, "ResultSender", color);
        }

        private async Task SendMessage(MessageInput message)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                var data = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

                var url = configHundler.GetCmsApi();
                var client = new HttpClient();

                Console.WriteLine(json);
                logger.WriteLog(logger.logMessageWithFormat("sending json data : \n"+ json +" \n to url :"+url,"info"));
                var response = await client.PostAsync(url, data);

                string result = response.Content.ReadAsStringAsync().Result;
                logger.WriteLog(logger.logMessageWithFormat("Response from server ->" + result,"info"));
            }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat(exe.Message,"error"));
            }
        }
        private MessageInput GetMessage()
        {
            try
            {
                DataTable tbl = db.SelectAllResults();
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
                    return null;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured while reading and preparing result message " + exe.Message,"error"));
                return null;
            }
        }
        private string[] SplitMessage(string dataToSend)
        {
            return Regex.Split(dataToSend, "##");
        }
    }
}
