using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZAsset.Edidor
{
    public static class Utils
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
    }
}

