using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZAsset
{
    public static class BundlePathRuntime
    {
        //Common组名
        public static readonly string[] CommonGroupNames = {
            "Common"
        };

        //UI组目录
        public static readonly string[] UIGroupNames =
        {
            "UI"
        };

        //场景组目录
        public static readonly string[] SceneGroupNames =
        {
            "Scenes"
        };

        //common资源的文件名后缀
        public static readonly string CommonResSuffix = "_cm";

        /// <summary>
        /// 根据路径返回资源类型标签 暂时只区分common和非common
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static AssetTag GetAssetTag(string adressName)
        {
            if (adressName.EndsWith(CommonResSuffix))
                return AssetTag.Common;
            return AssetTag.Default;
        }

   
    }
}