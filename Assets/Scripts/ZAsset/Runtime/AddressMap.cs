using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 运行时用于从地址（address）解析到 bundle 和资源路径。

namespace ZAsset
{
    [Serializable]
    public class AddressRecord
    {     
        public string bundleName;//属于的AB包名（含后缀）
        public List<string> addressList;//对外使用的地址（调用时候的名称 可读名）每个address需要是唯一的
        public List<string> PathList;//资源在项目中的原始路径（用于AssetBundle.LoadAsset）
    }

    [CreateAssetMenu(fileName ="AddressMap",menuName ="ZAsset/AddressMap",order =0)]
    public class AddressMap : ScriptableObject
    {
        public List<AddressRecord> entries = new List<AddressRecord>();
        private Dictionary<string, AddressRecord> _map; //addressName - >AdressRecord
        public void InitMap()
        {
            if(_map!=null) return;
            _map = new Dictionary<string, AddressRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries)
            {
                foreach(var address in e.addressList)
                {
                    if (!_map.ContainsKey(address))
                        _map.Add(address, e);
                    else
                        Debug.LogWarning($"重复的地址：{address}");
                }          
            }
        }

        //通过地址获取address信息
        public bool TryGet(string address,out AddressRecord record)
        {
            InitMap();
            return _map.TryGetValue(address, out record);
        }
    }

}

