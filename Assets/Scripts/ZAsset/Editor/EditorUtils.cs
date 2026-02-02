using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZAsset.Editor
{
    public static class EditorUtils
    {
        public static void CleanDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }
                foreach (var dir in Directory.GetDirectories(path))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        /// <summary>
        /// 根据路径获取文件名
        /// </summary>
        public static string GetFileNameByPath(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// 根据路径列表获取文件名列表
        /// </summary>
        public static List<string> GetFileNamesByPaths(List<string> paths)
        {
            List<string> addressList = new List<string>();
            foreach (var path in paths)
            {
                addressList.Add(GetFileNameByPath(path));
            }
            return addressList;
        }

        /// <summary>
        /// 递归获取一个文件夹下所有文件的pathName
        /// </summary>
        /// <returns></returns>
        public static List<string> GetDirAllPathName(string path)
        {
            List<string> paths = new();
            string[] guids = AssetDatabase.FindAssets("", new[] { path });
            foreach (var guid in guids)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!Directory.Exists(filePath))//过滤文件夹 只要文件
                {
                    paths.Add(filePath);
                }
            }
            return paths;
        }

        /// <summary>
        /// 获取指定路径下的所有文件（不递归，不包含 .meta）
        /// </summary>
        public static List<string> GetDirectFiles(string path)
        {
            List<string> results = new List<string>();

            // 转绝对路径
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"目录不存在: {path}");
                return results;
            }

            var files = Directory.GetFiles(fullPath, "*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                if (file.EndsWith(".meta")) continue; // 跳过 meta
                string relPath = file.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, "")
                                     .Replace("\\", "/");
                results.Add(relPath);
            }

            return results;
        }

        /// <summary>
        /// 获取指定路径下的所有子文件夹（不递归）
        /// </summary>
        public static List<string> GetDirectFolders(string path)
        {
            List<string> results = new List<string>();

            // 转绝对路径
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"目录不存在: {path}");
                return results;
            }

            var dirs = Directory.GetDirectories(fullPath, "*", SearchOption.TopDirectoryOnly);
            foreach (var dir in dirs)
            {
                string relPath = dir.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, "")
                                     .Replace("\\", "/");
                results.Add(relPath);
            }

            return results;
        }



    }
}

