using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

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
        public static string GetFileNameByPath(string path,bool needExtension = false)
        {
            if(!needExtension) return Path.GetFileName(path);
            else return Path.GetFileNameWithoutExtension(path);

        }

        /// <summary>
        /// 根据路径列表获取文件名列表
        /// </summary>
        public static List<string> GetFileNamesByPaths(List<string> paths,bool needExtension = false)
        {
            List<string> addressList = new List<string>();
            foreach (var path in paths)
            {
                addressList.Add(GetFileNameByPath(path,needExtension));
            }
            return addressList;
        }



        /// <summary>
        /// 获取指定路径下的所有文件（不包含 .meta）
        /// </summary>
        public static List<string> GetDirectFiles(string path,bool recursion = false)
        {
            List<string> results = new List<string>();

            // 转绝对路径
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"目录不存在: {path}");
                return results;
            }

            SearchOption searchOption = recursion ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var files = Directory.GetFiles(fullPath, "*", searchOption);
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
        /// 获取指定路径下的所有子文件夹
        /// </summary>
        public static List<string> GetDirectFolders(string path,bool recursion = false)
        {
            List<string> results = new List<string>();

            // 转绝对路径
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"目录不存在: {path}");
                return results;
            }
            SearchOption searchOption = recursion ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var dirs = Directory.GetDirectories(fullPath, "*", searchOption);
            foreach (var dir in dirs)
            {
                string relPath = dir.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, "")
                                     .Replace("\\", "/");
                results.Add(relPath);
            }

            return results;
        }

        public static bool StartsWithAny(string path, string[] prefixes)
        {
            foreach (var p in prefixes)
            {
                if (path.StartsWith(p)) return true;
            }
            return false;
        }

       
        public static bool CheckFilesToEndWithSuffix(string folderPath, string suffix)
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"目录不存在: {folderPath}");
                return false;
            }
            var files = GetDirectFiles(folderPath, true);
            if (files != null)
            {
                foreach (string file in files)
                {
                  
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    if (!fileNameWithoutExt.EndsWith(suffix))
                    {
                        Debug.Log($"文件{fileNameWithoutExt}后缀不为{suffix}");
                        return false;
                    }
                    
                }

            }
            return true;
        }


    }
}

