using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ab包信息维护
namespace ZAsset
{
    public class BundleInfo : IRecycle,IComparable<BundleInfo>
    {
        public  AssetBundle Bundle {  get; private set; }
        public string BundleName {  get; private set; }
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

        public void Create()
        {

        }
        public void Dispose()
        {
            ObjectPool.Instance.Push<BundleInfo>(this);
        }

        

        public BundleInfo()
        {

        }

        public BundleInfo(AssetBundle bundle, string _abName, int _refCount = 1)
        {
            Init(bundle, _abName, _refCount);
        }

        public void Init(AssetBundle bundle, string _abName, int _refCount=1)
        {
            BundleName = _abName;
            refCount = _refCount;
            Bundle = bundle;
        }

        public int CompareTo(BundleInfo other)
        {
            if (this.recentTime < other.recentTime) return -1;
            return 1;
        }

        public void SetBundle(AssetBundle ab)
        {
            this.Bundle = ab;
        }
    }
}
