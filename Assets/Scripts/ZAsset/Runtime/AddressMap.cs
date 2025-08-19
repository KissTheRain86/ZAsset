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
        public string address;//对外使用的地址（调用时候的名称 可读名）
        public string bundleName;//属于的AB包名（含后缀）
        public string assetPath;//资源在项目中的原始路径（用于AssetBundle.LoadAsset）
    }

    // 可做成 ScriptableObject，打包生成到 StreamingAssets 或 Resources。
    [CreateAssetMenu(fileName ="AddressMap",menuName ="ZAsset/AddressMap",order =0)]
    public class AddressMap : ScriptableObject
    {
        public List<AddressRecord> entries = new List<AddressRecord>();
        private Dictionary<string, AddressRecord> _map;
        public void InitMap()
        {
            if(_map!=null) return;
            _map = new Dictionary<string, AddressRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries)
            {
                if (!_map.ContainsKey(e.address))
                    _map.Add(e.address, e);
                else
                    Debug.LogWarning($"重复的地址：{e.address}");
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

