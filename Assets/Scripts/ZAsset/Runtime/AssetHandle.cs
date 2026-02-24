using System;
using UnityEngine;

// 资源句柄，负责生命周期与便捷访问。

namespace ZAsset
{
    public class AssetHandle<T> : IRecycle, IDisposable where T : UnityEngine.Object
    {
        private string _address;
        private string _bundleName;
        private AssetTag _assetTag;
        private T _asset;
        private bool _disposed;

        public T Asset => _asset;

        public AssetHandle()
        {
        }

        public AssetHandle(string address, T asset)
        {
            Init(address, asset, null, AssetTag.Default);
        }

        public void Init(string address, T asset, string bundleName, AssetTag assetTag)
        {
            _address = address;
            _asset = asset;
            _bundleName = bundleName;
            _assetTag = assetTag;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            ResManager.Instance.Release(_address, _bundleName, _assetTag);

            _address = null;
            _bundleName = null;
            _asset = null;

            ObjectPool.Instance.Push<AssetHandle<T>>(this);
        }

        public void Create()
        {
            _disposed = false;
        }
    }
}
