 
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using System.IO;
using System.Reflection;

namespace Sysmex_Lis_Service

{

    public class TestSer
    {
        public string hello_text;
    }

    public class ResultSender
    {
        Database db = null;
        Thread startRS;
        ConfigFileHundler confighundler;
        Logger logger;
        bool isStopped = false;
        public ResultSender()
        {
            confighundler = new ConfigFileHundler();
            this.db = new Database();
            logger = new Logger();
        }

        public void startResultSender()
        {
            startRS = new Thread(new ThreadStart(StartSending));
            startRS.Start();
        }

        public void stopResultSender()
        {
            isStopped = true;
            startRS.Join();
            startRS.Abort();

        }

        public async void StartSending()
        {
            Console.WriteLine("result sending started");
            //Subscriber.WriteLogToFile("result sender started...");
            while (!isStopped)
            {
                MessageInput inp = GetMessage();
                
                if (inp != null)
                {

                    try
                    {

                        //logger.WriteLog("New result from machine arrived!");
                        //Subscriber.WriteLogToFile("New Result from machine arrived");
                        SendMessage(inp);

                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(inp);
                        var data = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

                        Console.WriteLine("our data : {0}", json);
                        
                        Console.WriteLine(data);
                        Console.WriteLine(json);
                        var url = confighundler.GetCmsApi();
                        Console.WriteLine(url);
                        HttpClient client = new HttpClient();

                        var response = await client.PostAsync(url, data);
                        //Subscriber.WriteLogToFile("response =>"+response);
                        logger.WriteLog(logger.logMessageWithFormat("sending data to " + url));
                        string result = response.Content.ReadAsStringAsync().Result;

                        logger.WriteLog(logger.logMessageWithFormat("Response from server ->" + result));
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        logger.WriteLog(logger.logMessageWithFormat(ex.Message, "error"));
                    }
                 
                }
                Thread.Sleep(10);
            }
        }
        private void WriteLog(string message, ConsoleColor color = ConsoleColor.DarkBlue)
        {
            Helper.WriteLog(message, "ResultSender", color);
        }

        private async void SendMessage(MessageInput message)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                logger.WriteLog(json);
                var data = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

                var url = "http://localhost:9000";
                var client = new HttpClient();

                var response = await client.PostAsync(url, data);

                string result = response.Content.ReadAsStringAsync().Result;
              
                
            }
            catch (Exception exe)
            {
                WriteLog(exe.Message);
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
                    foreach (string s in splitedMessage)
                    {
                        Console.WriteLine("let's see : {0}", s);
                    }
                    ASTMMessage aSTMMessage = new ASTMMessage(splitedMessage);
                    Console.WriteLine("");
                    db.DeleteResult(int.Parse(dr["id"].ToString().Trim()));
                    //logger.WriteLog("result "+ aSTMMessage.GetResultMessageObject());
                    return aSTMMessage.GetResultMessageObject();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured while reading and preparing result message " + exe.Message, "error"));
                return null;
            }
        }
        private string[] SplitMessage(string dataToSend)
        {
            return Regex.Split(dataToSend, "##");
        }
    }
}
