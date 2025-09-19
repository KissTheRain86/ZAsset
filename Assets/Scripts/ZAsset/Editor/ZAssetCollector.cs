#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

// 说明：一个组，一个组内为同一个类型。组路径在BundlePath脚本中进行设置
// common组：一个文件夹一个包
// 其他组：一个文件一个包，基于引用计数
namespace ZAsset.Editor
{
    //打包时 该管理类自动搜集要打包的资源信息
    public class ZAssetCollector:Singleton<ZAssetCollector>
    {
        public BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;
        public string OutputFolder = "AssetBundles";//相对于项目根目录
        public Dictionary<string,AssetGroup> GroupMap
        {
            get { return _groupMap; }           
        }

        private Dictionary<string,AssetGroup> _groupMap = new();
        public void InitInfo()
        {
            _groupMap.Clear();
            //遍历所有group 构建 _groupMap
            var groupPaths = EditorUtils.GetDirectFolders(BundlePathEditor.BundleRoot);

            foreach (var groupPath in groupPaths)
            {
                string groupName = EditorUtils.GetFileNameByPath(groupPath);
                if (_groupMap.ContainsKey(groupName))
                {
                    Debug.LogError($"BundleBuildConfig 存在重复的groupName:{groupName}");
                }
                else
                {
                    var groupTag = BundlePathEditor.GetAssetTag(groupPath);
                  
                    _groupMap[groupName]=new AssetGroup(groupName, groupPath,groupTag);
                }            
            }          
        }
    }

    public class AssetGroup
    {      
        private string groupName;//指定要写入的group名

        public AssetTag GroupTag = AssetTag.Default; // 资源标签

        public List<string> PathList
        {
            get { return pathList; }
        }
        //bundleName->addressList
        public Dictionary<string, List<string>> BundleAddressMap = new();
        //bundleName->pathList
        public Dictionary<string, List<string>> BundlePathMap = new();

        private List<string> pathList = new();//资源物理地址

        public AssetGroup(string groupName,string groupPath,AssetTag groupTag)
        {
            pathList.Clear();
            BundleAddressMap.Clear();
            BundlePathMap.Clear();
            groupName = groupName ?? "defaultGroup";
            GroupTag = groupTag;
            if (groupPath==null || groupPath==string.Empty)
            {
                Debug.LogError($"资源组路径为空:{groupName}");
                return;
            }
            
            if (AssetDatabase.IsValidFolder(groupPath))
            {

                switch (groupTag)
                {
                    case AssetTag.Common:
                        //获取所有文件夹（此为特殊情况 处理为common）
                        var multiBundleDir = EditorUtils.GetDirectFolders(groupPath, false);
                        foreach (var bundleDir in multiBundleDir)
                        {
                            if( !EditorUtils.CheckFilesToEndWithSuffix(bundleDir, BundlePathEditor.CommonResSuffix))
                            {
                                throw new Exception($"Common组内的文件{bundleDir} 要以 {BundlePathEditor.CommonResSuffix} 作为后缀，请重命名后再执行打包操作");
                            }
                            var pathNames = EditorUtils.GetDirectFiles(bundleDir);
                            pathList.AddRange(pathNames);
                            BundleAddressMap[Path.GetFileName(bundleDir)] = EditorUtils.GetFileNamesByPaths(pathNames, false);
                            BundlePathMap[Path.GetFileName(bundleDir)] = pathNames;
                        }
                        break;               
                    case AssetTag.Scene:
                    case AssetTag.UI:
                    case AssetTag.Default:
                        //递归获取所有文件 单独作为bundle包
                        var bundlePaths = EditorUtils.GetDirectFiles(groupPath,true);
                        foreach (var bundlePath in bundlePaths)
                        {
                            var bundleName = Path.GetFileNameWithoutExtension(bundlePath);
                            if (BundlePathMap.ContainsKey(bundleName))
                            {
                                Debug.LogWarning($"存在重名资源名{bundleName}将只取第一个");
                                continue;
                            }
                            pathList.Add(bundlePath);
                            BundleAddressMap[bundleName] = new List<string> { bundleName };
                            BundlePathMap[bundleName] = new List<string> { bundlePath };
                        }
                        break;
                   
                }              

            }
            else
            {
                //单个文件 不允许 组必须要文件夹
                throw new Exception($"打包组必须是文件夹：{groupPath},请重新打包");
            }
        }



    }

}

#endif