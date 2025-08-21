#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

// 说明：打包配置 ScriptableObject，指定要打包的资源与地址。
namespace ZAsset.Edidor
{
    [System.Serializable]
    public class AssetConfig
    {
        //[Header("AssetBundle包名")]
        public string bundleName;//指定要写入的ab名称

        //[Header("资源文件夹")]
        public UnityEngine.Object asset;//资源引用

        [Header("对外可读资源名")]
        public List<string> addressList = new List<string>();//对外可读地址 不指定 直接自动读名字

        [Header("资源物理地址")]
        public List<string> pathList = new List<string>();//资源物理地址

        [Header("资源类型")]
        public AssetTag assetTag; // 资源标签

    }

    [CreateAssetMenu(fileName ="BundleBuildConfig",menuName ="ZAsset/BundleBuildConfig",order =0)]
    public class BundleBuildConfig : ScriptableObject
    {
        public BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;
        public string outputFolder = "AssetBundles";//相对于项目根目录
        public List<AssetConfig> assets = new List<AssetConfig>();

        //输出 AddressMap 的保存路径
        public string addressMapAssetPath = "Assets/AddressMap.asset";

        public void OnValidate()
        {
            foreach(var assetConf in assets)
            {
                addAssetInfo(assetConf);
            }
        }

        private void addAssetInfo(AssetConfig assetConf)
        {
            assetConf.addressList.Clear();
            assetConf.pathList.Clear();
            assetConf.assetTag = AssetTag.Common;
            if (assetConf.asset == null) return;
            string path = AssetDatabase.GetAssetPath(assetConf.asset);
            if (AssetDatabase.IsValidFolder(path))
            {
                //文件夹 遍历文件名
                string[] guids = AssetDatabase.FindAssets("", new[] { path });
                foreach (var guid in guids)
                {
                    string filePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!Directory.Exists(filePath))//过滤文件夹 只要文件
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        if (assetConf.addressList.Contains(fileName))
                        {
                            Debug.LogWarning($"存在重名资源{fileName}只保留第一个");
                            continue;
                        }
                        assetConf.addressList.Add(fileName);
                        assetConf.pathList.Add(filePath);
                    }
                }
            }
            else
            {
                //单个文件
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (assetConf.addressList.Contains(fileName))
                {
                    Debug.LogWarning($"存在重名资源{fileName}只保留第一个");
                    return;
                }
                assetConf.addressList.Add(fileName);
                assetConf.pathList.Add(path);
            }

        }

    }

}

#endif