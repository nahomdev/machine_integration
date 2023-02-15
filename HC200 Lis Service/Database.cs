using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Data;

namespace HC200_Lis_Service
{
    class Database
    {
        public SQLiteConnection myConnection;
        Logger logger;
        public Database()
        {
            logger = new Logger();
            myConnection = new SQLiteConnection("Data Source=database_HS200.sqlite3");
           
            if (!File.Exists("./database_HS200.sqlite3"))
            {
                try
                {
                    SQLiteConnection.CreateFile("database_HS200.sqlite3");
                    
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
                    logger.WriteLog(logger.logMessageWithFormat("error",exe.Message));
                }
            }
            else
            {
                myConnection.Open();
                Console.WriteLine("New db instance Connection is ready.");
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
                logger.WriteLog(logger.logMessageWithFormat("New db instance Connection is ready."));
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
                logger.WriteLog(logger.logMessageWithFormat("error", "Exception occured => " + exe.Message));
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
                    Console.WriteLine("Message inserted.");
                    return res;
                }
                else
                {
                    Console.WriteLine("Failed to insert message.");
                    return res;
                }
            }
            catch (Exception exe)
            {
                logger.WriteLog(logger.logMessageWithFormat("error", "Exception occured "+ exe.Message));

                return new ResFormat() { Message = "Exception occured while inserting =>" + exe.Message, OK = false };
                 
            }
        }

        public ResFormat InsertResult(string message)
        {
            string log = "";
            try
            {
                logger.WriteLog("---------------------------- Result Insert Error Boundary ----------------------- ");
                logger.WriteLog(logger.logMessageWithFormat("inserting message = {0}", message));
                string query = "INSERT INTO results (message) VALUES ('" + message + "');";
                ResFormat res = ExecuteCommand(query);
                if (res.OK)
                {
                    logger.WriteLog(logger.logMessageWithFormat("response after execute is OK"));
                    Console.WriteLine("Result inserted.", ConsoleColor.Cyan);
                    return res;
                }
                else
                {
                    logger.WriteLog(logger.logMessageWithFormat("Failed to insert res => {0}"+ res));
                    Console.WriteLine("Failed to insert result.", ConsoleColor.Cyan);
                    return res;
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine("Exception occured and Failed to insert result", ConsoleColor.Cyan);
                logger.WriteLog(logger.logMessageWithFormat("error","Exception occured => " + exe.Message));
                return new ResFormat() { Message = "Exception occured while inserting result =>" + exe.Message, OK = false };
                logger.WriteLog("---------------------------- Result Insert Error Boundary ----------------------- ");
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
                    Console.WriteLine("message deleted from database");
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to delete message with id " + id);
                    return false;
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine("Exception occured and Failed to delete message with id " + id);
                logger.WriteLog( logger.logMessageWithFormat("error","Exception occured => " + exe.Message));
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
                    Console.WriteLine("result deleted from database");
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to delete result with id " + id);
                    return false;
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine("Exception occured and Failed to delete result with id " + id);
                logger.WriteLog(logger.logMessageWithFormat("error","Exception occured => " + exe.Message));
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
                Console.WriteLine("Exception occured while fetching messages");
                logger.WriteLog(logger.logMessageWithFormat("error","Exception occured while fetching messages"));
                return null;
            }
        }

        public DataTable SelectAllResults()
        {
            try
            {
                 //SELECT* FROM results ORDER BY id OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY;
                string query = "SELECT id, message FROM results";
          
                 
                SQLiteDataAdapter adp = new SQLiteDataAdapter(query, myConnection);
                //Subscriber.WriteLogToFile("adp " + adp.ToString());
                DataTable tbl = new DataTable();

                //Subscriber.WriteLogToFile("adp have =>"+ );
                adp.Fill(tbl);
                adp.Dispose();
                return tbl;
            }
            catch (Exception exe)
            {
                Console.WriteLine("Exception occured while fetching results");
                
                logger.WriteLog(logger.logMessageWithFormat("error","Exception occured while fetching resulsts"+exe.ToString()));
                return null;
            }
        }
        public void CloseConnection()
        {
            myConnection.Close();
            Console.WriteLine("Connection closed");
        }
    }
}
