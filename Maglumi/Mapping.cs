using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Maglumi;

namespace Maglumi
{
    class Mapping
    {
        private string sampleCode;
        Logger logger;
        private string testCodeMap;

        Database database;
        public Mapping()
        {
            database = new Database();
            logger = new Logger();


        }
        public string startMapping(MessageInput input)
        {
            logger.WriteLog(logger.logMessageWithFormat("Mapping Started", "info"));

            this.sampleCode = input.sampleid + "##";
            foreach (var test in input.tests)
            {
                this.testCodeMap += test.code + "*" + test.testid + "*" + test.panel + "##";
            }

            string dataToSave = this.sampleCode + this.testCodeMap;
            database.InsertMapping(dataToSave);
            return "done";
        }
    }
}
