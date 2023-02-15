using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vidas
{
    public class Helper
    {
        public static void WriteLog(string log, string sender, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.Now + " [" + sender + "] => " + log);
        }
    }
}
