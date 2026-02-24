using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 资源句柄，负责生命周期与便捷访问。

namespace ZAsset
{
    public class AssetHandle<T> : IRecycle, IDisposable where T : UnityEngine.Object
    {
        private string _address;
        private T _asset;
        private bool _disposed;

        public T Asset => _asset;
        public AssetHandle()
        {

        }
        public AssetHandle(string address, T asset)
        {
            Init(address, asset);
        }

        public void Init(string address, T asset)
        {
            _address = address;
            _asset = asset;
        }

        public void Dispose()
        {
            if(_disposed) return;
            _disposed = true;
            _address = null;
            _asset = null;
            ResManager.Instance.Release(_address);
            ObjectPool.Instance.Push<AssetHandle<T>>(this);
        }

        public void Create()
        {
            _disposed = false;
        }
    }

}
