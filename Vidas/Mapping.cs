using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vidas;

namespace Vidas
{
    class Mapping
    {
        private string sampleCode;

        private string testCodeMap;
        Database database;
        public Mapping()
        {
            database = new Database();


        }
        public string startMapping(MessageInput input)
        {
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
