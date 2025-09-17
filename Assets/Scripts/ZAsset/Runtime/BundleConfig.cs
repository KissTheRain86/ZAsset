using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Bundle包配置类

namespace ZAsset
{
    [Serializable]
    public class BundleConfigItem
    {     
        public string bundleName;//属于的AB包名
        public List<string> addressList;//对外使用的地址（调用时候的名称 可读名）每个address需要是唯一的
        public List<string> pathList;//资源在项目中的原始路径（用于AssetBundle.LoadAsset）
        public AssetTag assetTag; // 资源标签
    }

    public class BundleConfig
    {
        public List<BundleConfigItem> entries = new List<BundleConfigItem>();
        private Dictionary<string, BundleConfigItem> _map; //addressName - >BundleConfigItem
        public BundleConfig(List<BundleConfigItem> addressRecords)
        {
            entries = addressRecords;
        }

        
        public void InitMap()
        {
            if(_map!=null) return;
            _map = new Dictionary<string, BundleConfigItem>(StringComparer.OrdinalIgnoreCase);
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
        public bool TryGet(string address,out BundleConfigItem record)
        {
            InitMap();
            return _map.TryGetValue(address, out record);
        }

        //序列化为json
        public string ToJson()
        {
            return JsonUtility.ToJson(this,true);
        }

        //反序列化
        public static BundleConfig FromJson(string json)
        {
            return JsonUtility.FromJson<BundleConfig>(json);
        }

    }

}

