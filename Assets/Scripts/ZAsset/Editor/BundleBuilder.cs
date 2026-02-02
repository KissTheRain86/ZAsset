#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;
using Unity.VisualScripting.FullSerializer;

// 说明：根据配置构建 AssetBundle，并生成 BundleConfig。
namespace ZAsset.Editor
{
    public static class BundleBuilder
    {
        /// <summary>
        /// 各个资源的引用计数
        /// </summary> 
        private static Dictionary<string, HashSet<string>> RefCounts = new();
        /// <summary>
        /// 公共Bundle哈希表，存储已经存储在公共资源包中的资源路径
        /// </summary>
        private static HashSet<string> CommonBundleSet = new();
        /// <summary>
        /// 独立Bundle哈希表，存储已经存储在所有独立Bundle的资源路径
        /// </summary>
        private static HashSet<string> SeparateBundleSet = new();
        /// <summary>
        /// 打包配置
        /// </summary>
        private static BundleBuildConfig BuildConfig = null;
        /// <summary>
        /// 用于生成AddressMap的列表
        /// </summary>
        private static List<BundleConfigItem> AddressRecords = new();

        /// <summary>
        /// 存储已经处理好的Bundle打包数据
        /// </summary>
        private static Dictionary<string, List<AssetBundleBuild>> BundleMap = new();


        [MenuItem("ZAsset/Build Bundles (Current Platform)")]

        public static void BuildBundles()
        {
            //初始化数据
            ClearData();

            BuildConfig = LoadConfig();
            if (BuildConfig == null)
            {
                Debug.LogError("BundleBuildConfig 未创建，通过 Assets/Create/ZAsset/BundleBuildConfig 进行创建！");
                return;
            }
           
            //构建BuildPipeline的输入
            foreach (var group in BuildConfig.assets)
            {
                if (group.asset == null) { Debug.LogWarning($"地址资源为空:{group.GroupName}");continue; }
                SetPrefabRefCount(group);
                if (group.assetTag == AssetTag.Common)
                {
                    BuildCommonBundle(group);
                }
                else 
                {
                    BuildSeparateBundle(group);
                }
            }
            //根据生成的 BundleMap 执行最后的打包操作
            FinalBuildAllBundle();
        }
        /// <summary>
        /// 构建公共Bundle数据 一组一个bundle
        /// </summary>
        /// <param name="path"></param>
        private static void BuildCommonBundle(AssetGroupConfig group)
        {        
            
            foreach(var bundle in group.BundlePathMap.Keys)
            {             
                AddBundleToBuild(bundle, group.BundlePathMap[bundle], group.BundleAddressMap[bundle],AssetTag.Common);
                //添加标记
                foreach (var path in group.BundlePathMap[bundle])
                {
                    CommonBundleSet.Add(path);
                }
            }    
        }

        /// <summary>
        /// 构建独立Bundle包数据
        /// </summary>
        /// <param name="path"></param>
        private static void BuildSeparateBundle(AssetGroupConfig group)
        {
            foreach (var bundle in group.bundleList)
            {
                AddBundleToBuild(bundle, group.BundlePathMap[bundle], group.BundleAddressMap[bundle], AssetTag.Default);
                foreach (var path in group.BundlePathMap[bundle])
                {
                    
                    TryBuildSharedResources(bundle, group);
                }
            }    
        }

        /// <summary>
        /// 最终构建Bundle数据并生成Bundle文件
        /// </summary>
        public static void FinalBuildAllBundle()
        {
            //从字典 生成AssetBundleBuild列表
            var finalList = new List<AssetBundleBuild>();
            foreach (var kv in BundleMap)
            {
                var merged = new AssetBundleBuild
                {
                    assetBundleName = kv.Key,                   
                };
                var names = new List<string>();
                var addrs = new List<string>();
                foreach (var b in kv.Value)
                {
                    names.AddRange(b.assetNames);
                    addrs.AddRange(b.addressableNames);
                }
                merged.assetNames = names.ToArray();
                merged.addressableNames = addrs.ToArray();
                finalList.Add(merged);
            }
            BuildBundle(finalList);
            ClearData();
        }

        /// <summary>
        /// 构建Bundle文件
        /// </summary>
        private static void BuildBundle(List<AssetBundleBuild> finalList)
        {
            if (finalList == null || finalList.Count == 0)
                return;
            //输出路径
            var outputDir = Path.Combine(Environment.CurrentDirectory, BuildConfig.outputFolder);

            if (Directory.Exists(outputDir))
            {
                EditorUtils.CleanDirectory(outputDir);
            }
            else
            {
                Directory.CreateDirectory(outputDir);
            }

            //构建资源
            var manifest = BuildPipeline.BuildAssetBundles(
                outputDir,
                finalList.ToArray(),
                BuildConfig.options,
                EditorUserBuildSettings.activeBuildTarget);
            
            
            if (manifest == null)
            {
                Debug.LogError("BuildAssetBundles 构建失败.");
                return;
            }
            //删除所有manifest文件
            //foreach (var file in Directory.GetFiles(outputDir, "*.manifest"))
            //{
            //    File.Delete(file);
            //}
            //生成一个存储所有Bundle及对应hash值的容器
            Dictionary<string, string> bundleHashMap = new Dictionary<string, string>();

            string[] bundles = manifest.GetAllAssetBundles();
            foreach (var bundle in bundles)
            {           
                bundleHashMap[bundle] = manifest.GetAssetBundleHash(bundle).ToString();
            }
            // 生成MD5字典
            Dictionary<string, string> bundleMD5s = new();
            foreach (var bundle in BundleMap)
            {
                var path = Path.Combine(outputDir, bundle.Key);
                bundleMD5s[bundle.Key] = AssetUtils.CaculateFileMd5(path);
            }
            // 写入 JSON 文件
            string jsonPath = Path.Combine(outputDir, "AllBundleHash.json");
            string jsonText = JsonUtility.ToJson(new SerializationDictionary(bundleHashMap), true);
            File.WriteAllText(jsonPath, jsonText);

            // md5 写入 version 文件
            string md5JsonPath = Path.Combine(outputDir, "version.json");
            string md5JsonText = JsonUtility.ToJson(new SerializationDictionary(bundleMD5s), true);
            File.WriteAllText(md5JsonPath, md5JsonText);

            //生成addressmap资源
            GenerateAddressMap(BuildConfig, AddressRecords);
            Debug.Log($"构建完成，输出路径 : {outputDir}");
            //自动打开
            EditorUtility.RevealInFinder(outputDir+"/");
        }
        #region 处理数据
        /// <summary>
        /// 计算并存储各个资源被引用的计数
        /// </summary>
        public static void SetPrefabRefCount(AssetGroupConfig config)
        {
            for (int i = 0; i < config.pathList.Count; i++)
            {
                // 获取该 Prefab 的所有依赖
                var deps = AssetDatabase.GetDependencies(config.pathList[i], true)
                    .Where(d => !d.EndsWith(".cs") && !d.EndsWith(".unity"))
                    .ToHashSet(); // 去重
                foreach (var dep in deps)
                {
                    if (dep == config.pathList[i])
                        continue;
                    var bundleName = BuildConfig.GetBundleNameByAssetPath(config.pathList[i]);
                   
                    if (!RefCounts.ContainsKey(dep))
                        RefCounts[dep] = new HashSet<string> { bundleName };
                    RefCounts[dep].Add(bundleName); 
                }
            }
        }

        /// <summary>
        /// 检查资源的依赖 如果某依赖的被引用数大于1 则要单独打包
        /// </summary>
        private static void TryBuildSharedResources(string bundle, AssetGroupConfig group)
        {
            var paths = group.BundlePathMap[bundle];//获取所有资源的物理地址
            foreach(var path in paths)
            {
                var depStrs = AssetDatabase.GetDependencies(path, true)//会返回依赖的依赖
              .Where(d => !d.EndsWith(".cs") && !d.EndsWith(".unity"))
              .ToHashSet();
                var deps = depStrs.ToHashSet();
                //var result = new List<string>();
                foreach (var dep in deps)
                {
                    if (CommonBundleSet.Contains(dep) || SeparateBundleSet.Contains(dep))
                        continue;
                    if (RefCounts.ContainsKey(dep) && RefCounts[dep].Count > 1 && dep != path)
                    {
                        SeparateBundleSet.Add(dep);//添加独占资源标签
                        //将这个共享资源单独打包
                        AddBundleToBuild(dep);
                        continue;
                    }
                }
            }        
        }

        //获取在editor配置的bundlebuildconfig内容
        private static BundleBuildConfig LoadConfig()
        {
            var guids = AssetDatabase.FindAssets("t:BundleBuildConfig");
            if (guids == null || guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<BundleBuildConfig>(path);
        }

        //根据BundleBuildConfig 生成 BundleConfig
        private static void GenerateAddressMap(BundleBuildConfig config, List<BundleConfigItem> records)
        {
            var bundleConf = new BundleConfig(records);
            string json = bundleConf.ToJson();
            string jsonPath = Path.Combine(Environment.CurrentDirectory, config.outputFolder, "BundleConfig.json");
            File.WriteAllText(jsonPath, json);
            Debug.Log($"BundleConfig 已经保存: {jsonPath}");
        }

        //将某个path的资源单独打包
        private static void AddBundleToBuild(string path)
        {
            string bundleName = Path.GetFileNameWithoutExtension(path);
            string addressName = Path.GetFileNameWithoutExtension(path);
            AddBundleToBuild(bundleName, new List<string> { path }, new List<string> { addressName }, AssetTag.Default);
        }

        private static void AddBundleToBuild(string bundle, List<string> pathList, List<string> addressList, AssetTag tag)
        {
            if (!BundleMap.TryGetValue(bundle, out var list))
            {
                list = new List<AssetBundleBuild>();
                BundleMap[bundle] = list;
            }

            var build = new AssetBundleBuild
            {
                assetBundleName = bundle,
                assetNames = pathList.ToArray(),
                addressableNames = addressList.ToArray()
            };
            list.Add(build);//获取bundle对应的AssetBundleBuild list

            //构建运行时调用的addressmap信息
            AddressRecords.Add(new BundleConfigItem
            {
                bundleName = bundle,
                addressList = addressList,
                pathList = pathList,
                assetTag = tag
            });
        }


        /// <summary>
        /// 清空缓存数据
        /// </summary>
        private static void ClearData()
        {
            CommonBundleSet.Clear();
            RefCounts.Clear();
            AddressRecords.Clear();
            BundleMap.Clear();
            SeparateBundleSet.Clear();
            BuildConfig = null;
        }
        #endregion


    }
}

#endif
