using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZAsset
{
    [System.Serializable]
    public class SerializationDictionary
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();

        public SerializationDictionary(Dictionary<string, string> dict)
        {
            foreach (var kv in dict)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }
    }
}


