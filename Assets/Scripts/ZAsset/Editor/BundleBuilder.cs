#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

// 说明：根据配置构建 AssetBundle，并生成 AddressMap。
namespace ZAsset.Edidor
{
    public static class BundleBuilder
    {
        [MenuItem("ZAsset/Build Bundles (Current Platform)")]
        public static void BuildBundles()
        {
            var config = LoadConfig();
            if (config == null)
            {
                Debug.LogError("BundleBuildConfig 未创建，通过 Assets/Create/ZAsset/BundleBuildConfig 进行创建！");
                return;
            }
            //构建BuildPipeline的输入
            var map = new Dictionary<string, List<AssetBundleBuild>>();//bundle->build list
            var addressRecords = new List<AddressRecord>();

            foreach(var e in config.assets)
            {
                if (!e.asset) { Debug.LogWarning($"地址资源为空:{e.address}");continue; }
                var assetPath = AssetDatabase.GetAssetPath(e.asset);
                var bundle = e.bundleName.ToLowerInvariant();

                if(!map.TryGetValue(bundle,out var list))
                {
                    list = new List<AssetBundleBuild>();
                    map[bundle] = list;
                }
                // 使用AssetBundleBuild 每个Build指定asset名称数组
                var build = new AssetBundleBuild
                {
                    assetBundleName = bundle,
                    assetNames = new[] { assetPath },
                    addressableNames = new[] { e.address }
                };
                list.Add(build);//获取bundle对应的AssetBundleBuild list

                addressRecords.Add(new AddressRecord
                {
                    address = e.address,
                    bundleName = bundle,
                    assetPath = assetPath
                });

                //合并每个bundle的assets
                var finalList = new List<AssetBundleBuild>();
                foreach(var kv in map)
                {
                    var merged = new AssetBundleBuild
                    {
                        assetBundleName = kv.Key
                    };
                    var names = new List<string>();
                    var addrs = new List<string>();
                    foreach(var b in kv.Value)
                    {
                        names.AddRange(b.assetNames);
                        if (b.addressableNames != null)
                            addrs.AddRange(b.addressableNames);
                    }
                    merged.assetNames = names.ToArray();
                    merged.addressableNames = addrs.ToArray();
                    finalList.Add(merged);
                }

                //输出路径
                var outputDir = Path.Combine(Environment.CurrentDirectory, config.outputFolder);
                if(!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                //构建资源
                var manifest = BuildPipeline.BuildAssetBundles(
                    outputDir,
                    finalList.ToArray(),
                    config.options,
                    EditorUserBuildSettings.activeBuildTarget);
                if (manifest == null)
                {
                    Debug.LogError("BuildAssetBundles 构建失败.");
                    return;
                }
                //生成addressmap资源
                GenerateAddressMap(config, addressRecords);

                Debug.Log($"构建完成，输出路径 : {outputDir}");
            }


        }

        //获取配置的bundlebuildconfig内容
        private static BundleBuildConfig LoadConfig()
        {
            var guids = AssetDatabase.FindAssets("t:BundleBuildConfig");
            if(guids == null || guids.Length==0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<BundleBuildConfig>(path);
        }

        //根据BundleBuildConfig AddressRecord列表 生成AddressMap
        private static void GenerateAddressMap(BundleBuildConfig config,List<AddressRecord> records)
        {
            var map = AssetDatabase.LoadAssetAtPath<AddressMap>(config.addressMapAssetPath);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<AddressMap>();
                var dir = Path.GetDirectoryName(config.addressMapAssetPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(map, config.addressMapAssetPath);
            }
            map.entries = records;
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            Debug.Log($"AddressMap saved: {config.addressMapAssetPath}");
        }
    }
}

#endif
