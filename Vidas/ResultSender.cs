 
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
 
using System.Threading;
using Vidas;

namespace Vidas
{
    public class ResultSender
    {
        Database db = null;
        Thread resultSenderThread;
        ConfigFileHundler configHundler;
        Logger logger;
        public ResultSender()
        {
            this.db = new Database();
            configHundler = new ConfigFileHundler();
            logger = new Logger();
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
            while (true)
            {
                MessageInput inp = GetMessage();

                if (inp != null)
                {


                    DataTable tbl = db.SelectAllMapping();

                    if (tbl.Rows.Count > 0)
                    {
                        DataRow row;
                        for (int i = 0; i < tbl.Rows.Count; i++)
                        {
                            row = tbl.Rows[i];

                            string savedMessage = row["message"].ToString().Trim();


                            string[] splitedMessage = Regex.Split(savedMessage, "##");

                            Console.WriteLine("test code: {0} and mapping code {1}", inp.sampleid, splitedMessage[0]);
                            Console.WriteLine("bool {0}", inp.sampleid == splitedMessage[0]);

                            if (splitedMessage[0] == inp.sampleid)
                            {
                                Console.WriteLine("after condition");
                                for (int j = 1; j < splitedMessage.Length; j++)
                                {

                                    string[] spliteTest = splitedMessage[j].Split('*');
                                    //input.Tests.Where(ts => ts.code == spliteTest[0] ? ts.result = spliteTest[1] : ts.result =null)
                                    foreach (var test in inp.tests)
                                    {
                                        Console.WriteLine("test code {0} ", test.code);
                                        if (test.code == spliteTest[0])
                                        {


                                            //test.result = spliteTest[1];
                                            Console.WriteLine("orginal {0} == mapping {1}", test.code, spliteTest[0]);
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



                    logger.WriteLog("New result from machine arrived!");
                    SendMessage(inp).GetAwaiter();
                }
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

                logger.WriteLog("json : " + json);
                var url = configHundler.GetCmsApi();
                var client = new HttpClient();

                var response = await client.PostAsync(url, data);

                string result = response.Content.ReadAsStringAsync().Result;
                logger.WriteLog("Response from server ->" + result);
            }
            catch (Exception exe)
            {
                logger.WriteLog(exe.Message);
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
                    Console.WriteLine("--> {0}", savedMessage);
                    //string[] splitedMessage = SplitMessage(savedMessage);

                    //ASTMMessage aSTMMessage = new ASTMMessage(splitedMessage);
                    db.DeleteResult(int.Parse(dr["id"].ToString().Trim()));

                    return result_parser(savedMessage);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog("Exception occured while reading and preparing result message " + exe.Message);
                return null;
            }
        }

        public MessageInput result_parser(string msg)
        {

            MessageInput input = new MessageInput();
            TestInput testInput = new TestInput();
            string[] fileds = msg.Split('|');
            string original_id;
            foreach (string fd in fileds)
            {
                if (fd.StartsWith("ci"))
                {
                    //patientId = fd.Substring;
                    original_id = fd.Substring(2);
                    //string[] split_id = original_id.Split('-');
                    //original_id = split_id[0] + DateTime.Now.ToString("MM") + DateTime.Now.ToString("yyyy") + split_id[1];
                    input.sampleid = original_id;
                }
                if (fd.StartsWith("rn"))
                {

                    input.tests = new List<TestInput>();

                    testInput.code = fd.Substring(2);

                    input.tests.Add(testInput);
                }
                else if (fd.StartsWith("qn"))
                {
                    input.tests = new List<TestInput>();

                    testInput.result = fd.Substring(2).Split(' ')[0];
                    input.tests.Add(testInput);
                }
            }

            return input;
        }

        private string[] SplitMessage(string dataToSend)
        {
            return Regex.Split(dataToSend, "##");
        }
    }
}
