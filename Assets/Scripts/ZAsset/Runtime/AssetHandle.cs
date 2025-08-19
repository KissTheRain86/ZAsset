using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 资源句柄，负责生命周期与便捷访问。

namespace ZAsset
{
    public class AssetHandle<T> : IDisposable where T : UnityEngine.Object
    {
        private readonly string _address;
        private readonly T _asset;
        private bool _disposed;

        public T Asset => _asset;

        public AssetHandle(string address, T asset)
        {
            _address = address;
            _asset = asset;
        }

        public void Dispose()
        {
            if(_disposed) return;
            _disposed = true;
            ResManager.Instance.Release(_address);
        }
    }

}
