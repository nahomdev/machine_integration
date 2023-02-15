using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Data;
using Maglumi;

namespace Maglumi
{
    class Database
    {
        Logger logger;
        public SQLiteConnection myConnection;
        private void WriteLog(string message, ConsoleColor color = ConsoleColor.White)
        {
            Helper.WriteLog(message, "Database", color);
        }
        public Database()
        {
            logger = new Logger();

            myConnection = new SQLiteConnection("Data Source=maglumi_database.sqlite3");
            if (!File.Exists("./maglumi_database.sqlite3"))
            {
                try
                {
                    SQLiteConnection.CreateFile("maglumi_database.sqlite3");
                    logger.WriteLog(logger.logMessageWithFormat("database file created!","info"));
                    string sql = "create table messages (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL)";
                    string resultSql = "create table results (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL)";
                    string mappingSql = "create table mapping (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL)";

                    myConnection.Open();
                    ExecuteCommand(sql);
                    logger.WriteLog(logger.logMessageWithFormat("Message Table created!","info"));
                    ExecuteCommand(resultSql);
                    logger.WriteLog("Result Table Created!");

                    ExecuteCommand(mappingSql);
                    Console.WriteLine(logger.logMessageWithFormat("result table successfully mapping","info"));
                    logger.WriteLog(logger.logMessageWithFormat("New db instance Connection is ready.","info"));
                }
                catch (Exception exe)
                {
                    logger.WriteLog(logger.logMessageWithFormat(exe.Message,"info"));
                }
            }
            else
            {
                myConnection.Open();
                logger.WriteLog(logger.logMessageWithFormat("New db instance Connection is ready.","info"));
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
                logger.WriteLog(logger.logMessageWithFormat( exe.Message,"error"));
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
                    logger.WriteLog(logger.logMessageWithFormat("Message inserted.","success"));
                    return res;
                }
                else
                {
                    logger.WriteLog(logger.logMessageWithFormat("Failed to insert message.","warning"));
                    return res;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured and Failed to insert message","error"));
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
                    logger.WriteLog(logger.logMessageWithFormat("Result inserted.","success"));
                    return res;
                }
                else
                {
                    logger.WriteLog(logger.logMessageWithFormat("Failed to insert result.","warning"));
                    return res;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured and Failed to insert result","error"));
                return new ResFormat() { Message = "Exception occured while inserting result =>" + exe.Message, OK = false };
            }
        }


        public ResFormat InsertMapping(string result)
        {
            try
            {

                string query = "INSERT INTO mapping (message) VALUES ('" + result + "');";
                ResFormat res = ExecuteCommand(query);
                if (res.OK)
                {
                    logger.WriteLog(logger.logMessageWithFormat("mapping inserted successfully", "success"));
                    return res;
                }
                else
                {
                    logger.WriteLog(logger.logMessageWithFormat("Failed to mapping","warning"));
                    return res;
                }

            }
            catch (Exception ex)
            {
                logger.WriteLog(logger.logMessageWithFormat(ex.Message,"error"));
                Console.WriteLine(ex.Message);
                return new ResFormat() { Message = "Exception occured while inserting =>" + ex.Message, OK = false };
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
                    logger.WriteLog(logger.logMessageWithFormat("message deleted from database","success"));
                    return true;
                }
                else
                {
                    logger.WriteLog(logger.logMessageWithFormat("Failed to delete message with id " + id,"warning"));
                    return false;
                }
            }
            catch (Exception)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured and Failed to delete message with id " + id,"error"));
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
                    logger.WriteLog("result deleted from database");
                    return true;
                }
                else
                {
                    logger.WriteLog("Failed to delete result with id " + id);
                    return false;
                }
            }
            catch (Exception)
            {
                logger.WriteLog("Exception occured and Failed to delete result with id " + id);
                return false;
            }
        }

        public bool DeleteMapping(int id)
        {
            try
            {
                string query = "DELETE FROM mapping WHERE id=" + id + ";";
                ResFormat status = ExecuteCommand(query);
                if (status.OK)
                {
                    logger.WriteLog("mapping deleted from database");
                    return true;
                }
                else
                {
                    logger.WriteLog("Failed to delete mapping with id " + id);
                    return false;
                }
            }
            catch (Exception)
            {
                logger.WriteLog("Exception occured and Failed to delete mapping with id " + id);
                return false;
            }
        }



        public DataTable SelectAllMapping()
        {
            try
            {
                string query = "SELECT id, message FROM mapping";
                SQLiteDataAdapter adp = new SQLiteDataAdapter(query, myConnection);
                DataTable tbl = new DataTable();
                adp.Fill(tbl);
                return tbl;
            }
            catch (Exception)
            {
                logger.WriteLog(logger.logMessageWithFormat("Exception occured while fetching mapping","error"));
                return null;
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
                logger.WriteLog(logger.logMessageWithFormat("Exception occured while fetching messages","error"));
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
            catch (Exception)
            {
                logger.WriteLog("Exception occured while fetching results");
                return null;
            }
        }
        public void CloseConnection()
        {
            myConnection.Close();
            logger.WriteLog("Connection closed");
        }
    }
}
