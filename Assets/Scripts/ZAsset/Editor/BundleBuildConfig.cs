#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 说明：打包配置 ScriptableObject，指定要打包的资源与地址。
namespace ZAsset.Edidor
{
    [System.Serializable]
    public class AssetConfig
    {
        public string address;//对外可读地址
        public UnityEngine.Object asset;//资源引用
        public string bundleName;//指定要写入的ab名称
    }

    [CreateAssetMenu(fileName ="BundleBuildConfig",menuName ="ZAsset/BundleBuildConfig",order =0)]
    public class BundleBuildConfig : ScriptableObject
    {
        public BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;
        public string outputFolder = "AssetBundles";//相对于项目根目录
        public List<AssetConfig> assets = new List<AssetConfig>();

        //输出 AddressMap 的保存路径
        public string addressMapAssetPath = "Assets/AddressMap.asset";
    }

}



#endif