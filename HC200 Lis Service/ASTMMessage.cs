
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HC200_Lis_Service
{

    public class ResFormat
    {
        public string Message { get; set; }
        public bool OK { get; set; }
    }
    public class TestInput
    {
        public string code { get; set; }
        public string result { get; set; }
    }
    public class MessageInput
    {
        public string patientId { get; set; }

        public string patientName { get; set; }

        public string machine_code = "HS-200";
        public string order_id { get; set; }
        public List<TestInput> result_obs { get; set; }

    }

    public class AllMessage
    {
        public List<MessageInput> all_orders { get; set; }
    }
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

                inp.order_id = rec.Fields[3].Value.ToString().Trim();
                List<TestInput> testInputs = new List<TestInput>();
                foreach (ASTMRecord aSTMRecord in resultRecords)
                {
                    Console.WriteLine("before");
                    testInputs.Add(new TestInput() { code = aSTMRecord.Fields[2].Value, result = aSTMRecord.Fields[8].Value });


                }

                inp.result_obs = testInputs;

                return inp;
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
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
                if (record.Fields[0].Value.Equals("P"))
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
