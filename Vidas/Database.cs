using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Data;
using Vidas;

namespace Vidas
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

            myConnection = new SQLiteConnection("Data Source=database_vidas.sqlite3");
            if (!File.Exists("./database_vidas.sqlite3"))
            {
                try
                {
                    SQLiteConnection.CreateFile("./database_vidas.sqlite3");
                    logger.WriteLog(logger.logMessageWithFormat("database file created!", "info"));
                    string sql = "create table messages (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL)";
                    string resultSql = "create table results (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL)";
                    string mappingSql = "create table mapping (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL)";

                    myConnection.Open();
                    ExecuteCommand(sql);
                    logger.WriteLog(logger.logMessageWithFormat("Message Table created!", "info"));
                    ExecuteCommand(resultSql);
                    logger.WriteLog(logger.logMessageWithFormat("Result Table Created!", "info"));

                    ExecuteCommand(mappingSql);
                    Console.WriteLine("result table successfully mapping");
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
                logger.WriteLog(logger.logMessageWithFormat(exe.Message,"error"));
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
                    logger.WriteLog("Message inserted.");
                    return res;
                }
                else
                {
                    logger.WriteLog("Failed to insert message.");
                    return res;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog("Exception occured and Failed to insert message");
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
                    logger.WriteLog("Result inserted.");
                    return res;
                }
                else
                {
                    logger.WriteLog("Failed to insert result.");
                    return res;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog("Exception occured and Failed to insert result");
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
                    logger.WriteLog("mapping inserted successfully");
                    return res;
                }
                else
                {
                    logger.WriteLog("Failed to mapping");
                    return res;
                }

            }
            catch (Exception ex)
            {
                logger.WriteLog("Database.cs (line 143 insert mapping catch ) =>" + ex.Message);
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
                    logger.WriteLog("message deleted from database");
                    return true;
                }
                else
                {
                    logger.WriteLog("Failed to delete message with id " + id);
                    return false;
                }
            }
            catch (Exception)
            {
                logger.WriteLog("Exception occured and Failed to delete message with id " + id);
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
                logger.WriteLog("Exception occured while fetching results");
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
                logger.WriteLog("Exception occured while fetching messages");
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
