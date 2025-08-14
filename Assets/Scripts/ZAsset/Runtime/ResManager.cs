using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace ZAsset
{
    public enum BundleLocateMode
    {
        StreamingAssets,   // Application.streamingAssetsPath
        PersistentDataPath,// Application.persistentDataPath（适合远程热更后放这里）
        CustomAbsolutePath // 绝对路径（自行设置 RootPath）
    }
    public class ResManager : MonoBehaviour
    {
        public static ResManager Instance {  get; private set; }

        [Header("加载根目录模式")]
        public BundleLocateMode locateMode = BundleLocateMode.StreamingAssets;

        [Header("自定义根目录（当模式=CustomAbsolutePath）")]
        public string customRootPath;

        [Header("AddressMap 引用（建议放到 Resources 或手动赋值）")]
        public AddressMap addressMap;

        private AssetBundleManifest _manifest;//由主包 main manifest bundle提供
        private string _abRoot;//运行时解析出的AB根目录
        private AssetBundle _manifestBundle;//用于加载manifest的bundle

        //bundleName -> AssetBundle + refCount 已经加载的ab
        private readonly Dictionary<string, (AssetBundle ab, int refCount)> _bundles = new Dictionary<string, (AssetBundle ab, int refCount)>();

        //address -> asset refCount 用于按地址引用计数卸载
        private readonly Dictionary<string, int> _addressRef = new Dictionary<string, int>();

        //bundleName -> task 异步等待
        private readonly Dictionary<string, Task<AssetBundle>> _loadingTasks = new Dictionary<string, Task<AssetBundle>>();


        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            resolveRoot();
        }

        private void resolveRoot()
        {
            switch (locateMode)
            {
                case BundleLocateMode.StreamingAssets:
                    _abRoot = Application.streamingAssetsPath;
                    break;
                case BundleLocateMode.PersistentDataPath:
                    _abRoot = Application.persistentDataPath;
                    break;
                case BundleLocateMode.CustomAbsolutePath:
                    _abRoot = customRootPath;
                    break;
            }
        }

        #region 初始化
        public async Task InitAsync(string manifestBundleName = "StandaloneWindows64")
        {
            // 注意：manifestBundleName 应与打包平台一致。Editor Builder 会把主 bundle 命名为目标平台名。
            var bundlePath = Path.Combine(_abRoot, manifestBundleName);
            _manifestBundle = await LoadBundleInternalAsync(manifestBundleName, bundlePath);
            _manifest = _manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (_manifest == null)
                Debug.LogError("AssetBundleManifest not found. Check your build output.");

            // 构建AddressMap 索引
            if (addressMap == null)
                addressMap = Resources.Load<AddressMap>("AddressMap");
            if (addressMap == null)
                Debug.LogWarning("AddressMap is null. You won't be able to load by address.");
            else
                addressMap.InitMap();
        }
        #endregion

        #region 公用 API

        //这个是真正加载资源 解包的 不只是建立链接
        public async Task<AssetHandle<T>> LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address)) throw new ArgumentNullException(nameof(address));
            if (!addressMap || !addressMap.TryGet(address, out var rec))
                throw new Exception($"没有找到地址：{address}");

            //地址引用计数 +1 （按需在Dispose时-1）
            if (_addressRef.ContainsKey(address)) _addressRef[address]++;
            else _addressRef[address] = 1;

            //先保证bundle及其依赖
            await LoadBundleWithDependenciesAsync(rec.bundleName);

            //加载资源解包
            var ab = _bundles[rec.bundleName].ab;
            var req = ab.LoadAssetAsync<T>(rec.assetPath);
            await AwaitAsyncOperation(req);
            var asset = req.asset as T;
            if (asset == null) throw new Exception($"资源加载失败：{address}->{rec.assetPath}");
            return new AssetHandle<T>(address, asset);
        }

        //释放某个地址的资源 引用计数维护
        public void Release(string address)
        {
            if (!_addressRef.TryGetValue(address, out var cnt)) return;
            cnt--;
            if (cnt <= 0) _addressRef.Remove(address);
            else _addressRef[address] = cnt;

            //当对某个地址的引用为0时 尝试卸载bundle
            if(addressMap && addressMap.TryGet(address,out var rec))
            {
                TryUnloadBundle(rec.bundleName);
            }
        }

        //卸载所有无引用的bundle
        public void UnloadUnusedBundles(bool unloadAllLoadedObj = false)
        {
            var toUnload  = new List<string>(); 
            foreach(var kv in _bundles)
            {
                string name = kv.Key;
                if(BundleRefCount(name)<=0)
                    toUnload.Add(name);
            }
            foreach (var name in toUnload)
            {
                UnloadBundle(name, unloadAllLoadedObj);
            }
        }

        //不管引用计数 强制卸载
        public void UnloadAll(bool unloadAllLoadedObj = false)
        {
            foreach (var kv in _bundles)
                kv.Value.ab.Unload(unloadAllLoadedObj);
            _bundles.Clear();
            _loadingTasks.Clear();
            _addressRef.Clear();
            if (_manifestBundle)
            {
                _manifestBundle.Unload(true);
                _manifestBundle= null;
            }

        }

        #endregion 

        #region bundle 加载实现
        private async Task LoadBundleWithDependenciesAsync(string bundleName)
        {
            //递归加载依赖项
            var deps = _manifest?.GetAllDependencies(bundleName) ?? Array.Empty<string>();
            foreach(var dep in deps)
            {
                await LoadBundleAsync(dep);
            }
            await LoadBundleAsync(bundleName);
        }

        private async Task LoadBundleAsync(string bundleName)
        {
            //已经加载过了
            if(_bundles.TryGetValue(bundleName,out var entry))
            {
                _bundles[bundleName] = (entry.ab, entry.refCount + 1);
                return;
            }

            //如果正在加载 等待
            if(_loadingTasks.TryGetValue(bundleName,out var task))
            {
                var abWait = await task;
                if(_bundles.TryGetValue(bundleName,out entry))
                {
                    _bundles[bundleName] = (entry.ab,entry.refCount + 1);
                }
                else
                {
                    _bundles[bundleName] = (abWait, 1);
                }
                return;
            }
            //没有加载过 也没有正在加载
            var path = Path.Combine(_abRoot, bundleName);
            var loadTask = LoadBundleInternalAsync(bundleName, path);
            _loadingTasks[bundleName] = loadTask;
            var ab = await loadTask;
            //加载完成后 移除正在加载的task
            _loadingTasks.Remove(bundleName);
            //加载完成后 记录加载完成的dic
            _bundles[bundleName] = (ab, 1); 
        }

        private async Task<AssetBundle> LoadBundleInternalAsync(string bundleName,string fullPath)
        {
            if(!File.Exists(fullPath))
                throw new FileNotFoundException($"Bundle not found: {fullPath}");
            var req = AssetBundle.LoadFromFileAsync(fullPath);
            await AwaitAsyncOperation(req);
            if(! req.assetBundle)
                throw new Exception($"Load bundle failed: {bundleName}");
            return req.assetBundle;

        }

        #endregion

        #region bundle 卸载实现       
        private void TryUnloadBundle(string bundleName)
        {
            //若引用计数<=0 则卸载 同时检查其依赖是否也可以卸载
            if (BundleRefCount(bundleName) > 0) return;

            UnloadBundle(bundleName, false);

            var deps = _manifest?.GetAllDependencies(bundleName)?? Array.Empty<string>();
            foreach(var d in deps)
            {
                if (BundleRefCount(d) <= 0)
                    UnloadBundle(d, false);
            }

        }

        private void UnloadBundle(string bundleName,bool unloadAllLoadedObj)
        {
            if (!_bundles.TryGetValue(bundleName, out var entry)) return;
            if (BundleRefCount(bundleName) > 0) return;

            entry.ab.Unload(unloadAllLoadedObj);
            _bundles.Remove(bundleName);
        }

        private int BundleRefCount(string bundleName)
        {
            // 以“地址引用”推导 bundle 引用数（保守估算）
            int count = 0;
            if (addressMap)
            {
                foreach (var r in addressMap.entries)
                {
                    if (!string.Equals(r.bundleName, bundleName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (_addressRef.TryGetValue(r.address, out var c))
                        count += c;
                }
            }

            //加上bundle自身的递归引用
            if (_bundles.TryGetValue(bundleName, out var entry))
                count += entry.refCount;
            return count;
        }

        #endregion

        //-------------- 工具 ---------------
        //将unity异步操作转为Task类型 （uniTask的临时代替）
        private static async Task AwaitAsyncOperation(AsyncOperation op)
        {
            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;
        }

    }
}

