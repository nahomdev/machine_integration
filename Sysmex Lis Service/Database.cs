using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Data;

namespace Sysmex_Lis_Service
{
    class Database
    {
        public SQLiteConnection myConnection;
        Logger logger;
        private void WriteLog(string message, ConsoleColor color = ConsoleColor.White)
        {
            Helper.WriteLog(message, "Database", color);
        }
        public Database()
        {
            logger = new Logger();
            myConnection = new SQLiteConnection("Data Source=database_s.sqlite3");
            if (!File.Exists("./database_s.sqlite3"))
            {
                try
                {
                    SQLiteConnection.CreateFile("database_s.sqlite3");
                    logger.WriteLog(logger.logMessageWithFormat("database file created!"));
                    string sql = "create table messages (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL)";
                    string resultSql = "create table results (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL)";

                    myConnection.Open();
                    ExecuteCommand(sql);
                    logger.WriteLog(logger.logMessageWithFormat("Message Table created!"));
                    ExecuteCommand(resultSql);
                    logger.WriteLog(logger.logMessageWithFormat("Result Table Created!"));
                    logger.WriteLog(logger.logMessageWithFormat("New db instance Connection is ready."));
                }
                catch (Exception exe)
                {
                    Console.WriteLine(exe.Message);
                }
            }
            else
            {
                myConnection.Open();
                logger.WriteLog(logger.logMessageWithFormat("New db instance Connection is ready."));
             }
        }
        public ResFormat ExecuteCommand(string query)
        {
            try
            {
                SQLiteCommand command = new SQLiteCommand(query, myConnection);
                command.ExecuteNonQuery();
                return new ResFormat() { Message = "Message inserted successfully.", OK = true };
            }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat("error",exe.Message));
                //Subscriber.WriteLogToFile(exe.Message);
                return new ResFormat() { Message = "Exception occured => " + exe.Message, OK = false };
            }

        }
        public ResFormat InsertMessage(string message)
        {
            string log = "";
            try
            {
                string query = "INSERT INTO messages (message) VALUES ('" + message + "');";
                ResFormat res = ExecuteCommand(query);
                if (res.OK)
                {
                    //logger.WriteLog(logger.logMessageWithFormat("Message inserted."));
                    Console.WriteLine("Message inserted");
                    return res;
                }
                else
                {
                    Console.WriteLine("Failed to insert Message.");
                    //logger.WriteLog(logger.logMessageWithFormat("Failed to insert message."));
                    return res;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat("error","Exception occured and Failed to insert message" +exe.ToString()));
               
                return new ResFormat() { Message = "Exception occured while inserting =>" + exe.Message, OK = false };
            }
        }

        public ResFormat InsertResult(string message)
        {
            string log = "";
            try
            {
                string query = "INSERT INTO results (message) VALUES ('" + message + "');";
                ResFormat res = ExecuteCommand(query);
                if (res.OK)
                {
                    //logger.WriteLog(logger.logMessageWithFormat("Result inserted."));
                    Console.WriteLine("Result inserted.");
                    return res;
                }
                else
                {
                    Console.WriteLine("Failed to insert Result");
                    //logger.WriteLog(logger.logMessageWithFormat("Failed to insert result."));
                    return res;
                }
            }
            catch (Exception exe)
            {
                WriteLog("Exception occured and Failed to insert result", ConsoleColor.Cyan);
                logger.WriteLog(logger.logMessageWithFormat("Exception occured and Failed to insert result" + exe.ToString(), "error"));
                return new ResFormat() { Message = "Exception occured while inserting result =>" + exe.Message, OK = false };
            }
        }

        public bool DeleteMessage(int id)
        {
            try
            {
                string query = "DELETE FROM messages WHERE id=" + id + ";";
                ResFormat status = ExecuteCommand(query);
                if (status.OK)
                {
                    WriteLog("message deleted from database");
                    return true;
                }
                else
                {
                    WriteLog("Failed to delete message with id " + id);
                    return false;
                }
            }
            catch (Exception)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured and Failed to delete message with id " + id, "error"));
                //Subscriber.WriteLogToFile("Exception occured and Failed to delete message with id " + id);
                return false;
            }
        }

        public bool DeleteResult(int id)
        {
            try
            {
                string query = "DELETE FROM results WHERE id=" + id + ";";
                ResFormat status = ExecuteCommand(query);
                if (status.OK)
                {
                    //logger.WriteLog("result deleted from database");
                    return true;
                }
                else
                {
                    //logger.WriteLog("Failed to delete result with id " + id);
                    return false;
                }
            }
            catch (Exception)
            {
                WriteLog("Exception occured and Failed to delete result with id " + id);
                logger.WriteLog(logger.logMessageWithFormat("Exception occured and Failed to delete result with id " + id, "error"));
                return false;
            }
        }
        public DataTable SelectAllMessages()
        {
            try
            {
                string query = "SELECT id, message FROM messages";
                SQLiteDataAdapter adp = new SQLiteDataAdapter(query, myConnection);
                DataTable tbl = new DataTable();
                adp.Fill(tbl);
                return tbl;
            }
            catch (Exception)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured while fetching messages", "error"));
                return null;
            }
        }

        public DataTable SelectAllResults()
        {
            try
            {
                string query = "SELECT id, message FROM results";
                SQLiteDataAdapter adp = new SQLiteDataAdapter(query, myConnection);
                DataTable tbl = new DataTable();
                adp.Fill(tbl);
                return tbl;
            }
            catch (Exception ex)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured while fetching results"+ex.ToString()));
                //Subscriber.WriteLogToFile("Exception occured while fetching results"+ex.ToString());
                return null;
            }
        }
        public void CloseConnection()
        {
            myConnection.Close();
            WriteLog("Connection closed");
        }
    }
}
