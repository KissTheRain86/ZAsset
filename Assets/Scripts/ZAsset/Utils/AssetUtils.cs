using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using System.IO;

namespace ZAsset
{
    public class AssetUtils
    {
        /// <summary>
        /// 计算指定文件的md5值
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string CaculateFileMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }
    }
}

