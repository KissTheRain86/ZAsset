#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 说明：打包配置 ScriptableObject，指定要打包的资源与地址。
// 在同一个group中，每个文件夹打为一个bundle，文件夹外的单独的prefab每个prefab单独打一个bundle
namespace ZAsset.Editor
{
    [System.Serializable]
    public class AssetGroupConfig
    {
        public string GroupName { get
            {
                if (string.IsNullOrEmpty(groupName)) return "default";
                return groupName;
            } 
        }

        [SerializeField]
        private string groupName;//指定要写入的group名

        public UnityEngine.Object asset;//资源引用

        [Header("Bundle列表")]
        public List<string> bundleList = new();//bundle列表，单个prefab独立bundle 一个文件夹一个bundle

        [Header("资源对外可读资源名")]
        public List<string> addressList = new();//对外可读地址 自动读名字

        [Header("资源物理地址")]
        public List<string> pathList = new();//资源物理地址

        [Header("资源标签")]
        public AssetTag assetTag = AssetTag.Default; // 资源标签

        //bundleName->addressList
        public Dictionary<string, List<string>> BundleAddressMap = new();
        //bundleName->pathList
        public Dictionary<string, List<string>> BundlePathMap = new();

    }

    [CreateAssetMenu(fileName ="BundleBuildConfig",menuName ="ZAsset/BundleBuildConfig",order =0)]
    public class BundleBuildConfig : ScriptableObject
    {
        public BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;
        public string outputFolder = "AssetBundles";//相对于项目根目录
        public List<AssetGroupConfig> assets = new List<AssetGroupConfig>();

        private HashSet<string> assetNameSet = new HashSet<string>();//不允许重名bundlename
        public void OnValidate()
        {
            assetNameSet.Clear();
            foreach (var assetConf in assets)
            {
                if (!assetNameSet.Contains(assetConf.GroupName))
                {
                    assetNameSet.Add(assetConf.GroupName);             
                }
                else
                {
                    Debug.LogWarning($"BundleBuildConfig 存在重复的groupName:{assetConf.GroupName}");
                }
                addAssetInfo(assetConf);
            }
        }

        public AssetGroupConfig GetGroupByBundleName(string bundleName)
        {
            foreach(var assetConf in assets)
            {
                if (assetConf.bundleList.Contains(bundleName)) return assetConf;
            }
            return null;
        }

        public string GetBundleNameByAssetPath(string assetPath)
        {
            foreach (var assetConf in assets)
            {
                foreach (var key in assetConf.BundlePathMap.Keys)
                {
                    if (assetConf.BundlePathMap[key].Contains(assetPath)) return key;
                }
            }
            return null;
        }
    

        private void addAssetInfo(AssetGroupConfig assetConf)
        {
            assetConf.addressList.Clear();
            assetConf.pathList.Clear();
            assetConf.bundleList.Clear();
            assetConf.BundleAddressMap.Clear();
            assetConf.BundlePathMap.Clear();

            if (assetConf.asset == null) return;
            string path = AssetDatabase.GetAssetPath(assetConf.asset);
            if (AssetDatabase.IsValidFolder(path))
            {
                //获取所有文件 单独作为bundle包
                var bundlePaths = EditorUtils.GetDirectFiles(path);
                foreach(var bundlePath in bundlePaths)
                {
                    var bundleName = Path.GetFileNameWithoutExtension(bundlePath); 
                    if (assetConf.addressList.Contains(bundleName))
                    {
                        Debug.LogWarning($"存在重名资源名{bundleName}将只取第一个");
                        continue;
                    }
                    assetConf.addressList.Add(bundleName);
                    assetConf.pathList.Add(bundlePath);
                    assetConf.bundleList.Add(bundleName);
                    assetConf.BundleAddressMap[bundleName] = new List<string> { bundleName };
                    assetConf.BundlePathMap[bundleName] = new List<string> { bundlePath };
                }

                //获取所有文件夹
                var multiBundleDir = EditorUtils.GetDirectFolders(path);
                foreach(var bundleDir in multiBundleDir)
                {
                    var pathNames = EditorUtils.GetDirAllPathName(bundleDir);
                    assetConf.addressList.AddRange(EditorUtils.GetFileNamesByPaths(pathNames));
                    assetConf.pathList.AddRange(pathNames);
                    assetConf.bundleList.Add(Path.GetFileName(bundleDir));
                    assetConf.BundleAddressMap[Path.GetFileName(bundleDir)] = EditorUtils.GetFileNamesByPaths(pathNames);
                    assetConf.BundlePathMap[Path.GetFileName(bundleDir)] = pathNames;
                }

            }
            else
            {
                //单个文件 不允许 必须要文件夹
                Debug.LogError("打包组必须是文件夹，重新选择");               
            }
        }
    }
}

#endif