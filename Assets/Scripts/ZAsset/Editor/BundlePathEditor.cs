
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZAsset.Editor
{
    public static class BundlePathEditor
    {
        public static readonly string BundleRoot = "Assets/Bundles/";

        //Common组目录 每个文件夹打一个bundle
        private static readonly string[] CommonDirs = {
            $"{BundleRoot}Common"
        };

        //UI组目录 每个文件一个bundle
        private static readonly string[] UIDirs =
        {
            $"{BundleRoot}UI"
        };

        //场景组目录 每个文件一个bundle
        private static readonly string[] SceneDirs =
        {
            $"{BundleRoot}Scenes"
        };

        public static readonly string OutputBundlePath = "AssetBundles";

        //common资源的文件名后缀
        public static readonly string CommonResSuffix = "_cm";
        /// <summary>
        /// 根据路径返回资源类型标签
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static AssetTag GetAssetTag(string assetPath)
        {
            if (EditorUtils.StartsWithAny(assetPath, CommonDirs))
                return AssetTag.Common;
            if (EditorUtils.StartsWithAny(assetPath, UIDirs))
                return AssetTag.UI;
            if (EditorUtils.StartsWithAny(assetPath, SceneDirs))
                return AssetTag.Scene;
            return AssetTag.Default;
        }

     
    }
}

#endif