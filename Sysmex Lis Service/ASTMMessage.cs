
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sysmex_Lis_Service
{
    public class ASTMField
    {
        public int Index { get; set; }
        public string Value { get; set; }
    }
    public class ASTMRecord
    {
        public List<ASTMField> Fields { get; set; }
        public string Header
        {
            get
            {
                if (Fields.Count > 0)
                    return Fields[0].Value;
                else
                    return string.Empty;
            }
        }
        public ASTMRecord(string input)
        {
            Fields = new List<ASTMField>();
            string[] tokens = Regex.Split(input, "\\|");
            for (int i = 0; i < tokens.Length; i++)
            {
                string current = tokens[i].ToString();
                Fields.Add(new ASTMField() { Index = i, Value = current });
            }
        }


    }
    public class ASTMMessage
    {
        public List<ASTMRecord> Records { get; set; }

        public MessageInput GetResultMessageObject()
        {
            try
            {
                MessageInput inp = new MessageInput();
                List<ASTMRecord> orderRecords = GetOrderRecords();
                List<ASTMRecord> resultRecords = GetResultRecords();
                if (orderRecords.Count == 0 || resultRecords.Count == 0)
                {
                    return null;
                }
                ASTMRecord rec = orderRecords[0];
                inp.order_id = rec.Fields[3].Value.Split('^')[2].ToString().Trim();
                List<TestInput> testInputs = new List<TestInput>();
                foreach (ASTMRecord aSTMRecord in resultRecords)
                {
                    testInputs.Add(new TestInput() { code = aSTMRecord.Fields[2].Value.Split('^')[4], result = aSTMRecord.Fields[3].Value });
                }
                inp.result_obs = testInputs;
               
                return inp;
            }
            catch (Exception exe)
            {
                return null;
            }
        }

        public List<ASTMRecord> GetResultRecords()
        {
            List<ASTMRecord> records = new List<ASTMRecord>();
            foreach (ASTMRecord record in Records)
            {
                if (record.Fields[0].Value.Equals("R"))
                    records.Add(record);
            }
            return records;
        }

        public List<ASTMRecord> GetOrderRecords()
        {
            List<ASTMRecord> records = new List<ASTMRecord>();
            foreach (ASTMRecord record in Records)
            {
                if (record.Fields[0].Value.Equals("O"))
                    records.Add(record);
            }
            return records;
        }
        public ASTMMessage(string[] records)
        {
            this.Records = new List<ASTMRecord>();
            foreach (string rec in records)
            {
                this.Records.Add(new ASTMRecord(rec));
            }

        }
    }
}
