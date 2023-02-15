using System;
using System.Collections.Generic;
using System.Text;

namespace Vidas
{
    public class Segment
    {
        private Dictionary<int, String> fields;

        public Segment()
        {
            fields = new Dictionary<int, string>(100);
        }

        public Segment(string name)
        {
            fields = new Dictionary<int, string>(100);
            fields.Add(0, name);
        }

        /// <summary>
        /// First field of a segment is the name of the segment
        /// </summary>
        public string Name
        {
            get
            {
                if (!fields.ContainsKey(0))
                {
                    return String.Empty;
                }
                return fields[0];
            }
        }

        /// <summary>
        /// Only | (vertical bar) is used as field separator character
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Field(int key)
        {
            if (Name == "MSH" && key == 1) return "|";

            if (!fields.ContainsKey(key))
            {
                return String.Empty;
            }
            return fields[key];
        }

        /// <summary>
        /// Only | (vertical bar) is used as field separator character
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public void Field(int key, string value)
        {
            if (Name == "MSH" && key == 1) return;

            if (!String.IsNullOrEmpty(value))
            {
                if (fields.ContainsKey(key))
                {
                    fields.Remove(key);
                }

                fields.Add(key, value);
            }
        }

        /// <summary>
        /// Only | (vertical bar) is used as field separator character
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public void DeSerializeSegment(string segment)
        {
            int count = 0;
            char[] separators = { '|' };

            string temp = segment.Trim('|');
            string[] fields = temp.Split(separators, StringSplitOptions.None);

            foreach (var field in fields)
            {
                Field(count, field);
                if (field == "MSH")
                {
                    // The delimiter after the MSH segment name counts as a field. So consider this as a special case.
                    ++count;
                }
                ++count;
            }
        }

        public string SerializeSegment()
        {
            int max = 0;
            foreach (var field in fields)
            {
                if (max < field.Key)
                {
                    max = field.Key;
                }
            }

            StringBuilder temp = new StringBuilder();

            for (int index = 0; index <= max; index++)
            {
                if (fields.ContainsKey(index))
                {
                    temp.Append(fields[index]);

                    // The delimiter after the MSH segment name counts as a field. So consider this as a special case.
                    if (index == 0 && Name == "MSH")
                    {
                        ++index;
                    }
                }
                if (index != max) temp.Append("|");
            }
            return temp.ToString();
        }
    }
}
