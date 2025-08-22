using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZAsset
{
    public class ZAssetBundleInfo : IComparable<ZAssetBundleInfo>
    {
        public string abName;
        private int refCount;
        private DateTime recentTime;
        public int RefCount
        {
            get
            {
                return refCount;
            }
            set
            {
                refCount = value;
                recentTime = DateTime.Now;
            }
        }

        public ZAssetBundleInfo(string _abName, int _refCount = 1)
        {
            this.abName = _abName;
            refCount = _refCount;
        }

        public int CompareTo(ZAssetBundleInfo other)
        {
            if (this.recentTime < other.recentTime) return -1;
            return 1;
        }
    }
}
