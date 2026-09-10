using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Delta.GameOps
{
    public struct ConfigValue
    {
        public bool BooleanValue { get { return bool.Parse(StringValue); } }
        public double DoubleValue { get { return double.Parse(StringValue, System.Globalization.CultureInfo.GetCultureInfo("en-US")); } }
        public long LongValue { get { return long.Parse(StringValue); } }
        public int IntValue { get { return int.Parse(StringValue); } }
        public string StringValue { get; private set; }

        public ConfigValue(string rawValue)
        {
            StringValue = rawValue;
        }
    }

    public class AppConfigs
    {
        Dictionary<string, Dictionary<string, ConfigValue>> _sections = new Dictionary<string, Dictionary<string, ConfigValue>>();

        public void Load(byte[] data)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(new System.IO.MemoryStream(data)))
            {
                Load(reader);
            }
        }

        public void Load(string iniFile)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(iniFile))
            {
                Load(reader);
            }
        }

        public ConfigValue GetValue(string key, string section)
        {
            return _sections[section][key];
        }

        void Load(System.IO.StreamReader reader)
        {
            string section = null;
            Dictionary<string, ConfigValue> attributes = null;

            string line;
            int count = 0;
            while ((line = reader.ReadLine()) != null)
            {
                count++;

                if (line.Length == 0)
                    continue;

                if (line[0] == ' ' || line[0] == '\t' || line[line.Length - 1] == ' ' || line[line.Length - 1] == '\t')
                    line = line.Trim();

                if (line[0] == ';')
                    continue;

                if (line[0] == '[')
                {
                    if (line[line.Length - 1] != ']')
                    {
                        Debug.LogErrorFormat("Parse configs failed at line {0}. Expect \']\'!", count);
                        _sections.Clear();
                        return;
                    }

                    section = line.Substring(1, line.Length - 2);
                    attributes = new Dictionary<string, ConfigValue>();
                    _sections.Add(section, attributes);
                    continue;
                }

                if (section == null)
                {
                    Debug.LogErrorFormat("Parse configs failed at line {0}. Section not found!", count);
                    return;
                }

                // parse key & value
                var splitIdx = line.IndexOf('=');
                if (splitIdx < 0)
                {
                    Debug.LogErrorFormat("Parse configs failed at line {0}. Key and value not found!", count);
                    _sections.Clear();
                    return;
                }

                var keyIdx = splitIdx - 1;
                var valueIdx = splitIdx + 1;

                while (line[keyIdx] == ' ' || line[keyIdx] == '\t')
                {
                    keyIdx--;
                    continue;
                }
                var key = line.Substring(0, keyIdx + 1);

                var value = string.Empty;
                if (valueIdx < line.Length)
                {
                    while (line[valueIdx] == ' ' || line[valueIdx] == '\t')
                    {
                        valueIdx++;
                        continue;
                    }
                    value = line.Substring(valueIdx, line.Length - valueIdx);
                }

                var configValue = new ConfigValue(value);
                attributes.Add(key, configValue);
            }
        }
    }
}
