using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ZAsset
{
    public enum BundleLocateMode
    {
        StreamingAssets,   // Application.streamingAssetsPath
        PersistentDataPath,// Application.persistentDataPath
        CustomAbsolutePath // 自定义路径（自行设置 RootPath）
    }
    public class ResManager : MonoBehaviour
    {
        public static ResManager Instance {  get; private set; }

        [Header("加载根目录模式")]
        public BundleLocateMode locateMode = BundleLocateMode.StreamingAssets;

        [Header("自定义根目录（当模式=CustomAbsolutePath）")]
        public string customRootPath;

        [Header("AddressMap 引用")]
        public AddressMap addressMap;

        private AssetBundleManifest _manifest;//由主包 main manifest bundle提供
        private string _abRoot;//运行时解析出的AB根目录
        private AssetBundle _manifestBundle;//用于加载manifest的bundle

        //bundleName -> AssetBundle + refCount 已经加载的ab
        private readonly Dictionary<string, (AssetBundle ab, int refCount, ZAssetBundleInfo abInfo)> _bundles = new Dictionary<string, (AssetBundle ab, int refCount, ZAssetBundleInfo abInfo)>();

        //address -> asset refCount 用于按地址引用计数卸载
        private readonly Dictionary<string, int> _addressRef = new Dictionary<string, int>();

        //bundleName -> task 异步等待
        private readonly Dictionary<string, UniTask<AssetBundle>> _loadingTasks = new Dictionary<string, UniTask<AssetBundle>>();

        //依赖缓存
        private readonly Dictionary<string, string[]> _depsCache = new Dictionary<string, string[]>();

        // 待卸载队列
        private readonly PriorityQueue<ZAssetBundleInfo> _waitUnloadQueue = new PriorityQueue<ZAssetBundleInfo>();

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
        public async UniTask InitAsync(string manifestBundleName = "AssetBundles")
        {
            // AssetBundles 为默认自动生成的manifestBundleName名称
            var bundlePath = Path.Combine(_abRoot, manifestBundleName);
            _manifestBundle = await LoadBundleInternalAsync(manifestBundleName, bundlePath);
            _manifest = _manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (_manifest == null)
                Debug.LogError("AssetBundleManifest没有找到");

            // 构建AddressMap 索引
            if (addressMap == null)
            {
                string addressMapPath = Path.Combine(_abRoot, "AddressMap.json");
                addressMap = LoadFromJson(addressMapPath);
            }
               
            if (addressMap == null)
                Debug.LogWarning("AddressMap没有找到");
            else
                addressMap.InitMap();
        }

        public void InitSync(string manifestBundleName = "AssetBundles")
        {
            // AssetBundles 为默认自动生成的manifestBundleName名称
            var bundlePath = Path.Combine(_abRoot, manifestBundleName);
            _manifestBundle = LoadBundleInternalSync(manifestBundleName, bundlePath);
            _manifest = _manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (_manifest == null)
                Debug.LogError("AssetBundleManifest没有找到");

            // 构建AddressMap索引
            if(addressMap ==null)
            {
                string addressMapPath = Path.Combine(_abRoot, "AddressMap.json");
                addressMap = LoadFromJson(addressMapPath);
            }

            if (addressMap == null)
                Debug.LogWarning("AddressMap没有找到");
            else
                addressMap.InitMap();
        }

        private AddressMap LoadFromJson(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"AddressMap.json 不存在: {path}");
                return null;
            }
            string json = File.ReadAllText(path);
            var map = AddressMap.FromJson(json);
            map.InitMap();
            return map;
        }

        #endregion

        #region 公用 API

        //这个是真正加载资源 解包的 不只是建立链接
        public async UniTask<AssetHandle<T>> LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address)) throw new ArgumentNullException(nameof(address));
            if (addressMap == null || !addressMap.TryGet(address, out var rec))
                throw new Exception($"没有找到地址：{address}");

            //地址引用计数 +1 （按需在Dispose时-1）
            if (_addressRef.ContainsKey(address)) _addressRef[address]++;
            else _addressRef[address] = 1;

            //先保证bundle及其依赖 已经加载，这一步是建立和bundle的链接
            await LoadBundleWithDependenciesAsync(rec.bundleName);

            //加载资源解包
            var ab = _bundles[rec.bundleName].ab;
            var asset = await ab.LoadAssetAsync<T>(address);//注意这里要用逻辑名加载 不能用物理路径 会找不到
           
            var realAsset = asset as T;
            if (realAsset == null) throw new Exception($"资源加载失败: {address}");
            return new AssetHandle<T>(address, realAsset);
        }

        // 同步加载真正资源
        public AssetHandle<T> LoadSync<T>(string address) where T : UnityEngine.Object
        {
            if(string.IsNullOrEmpty(address)) throw new ArgumentNullException(nameof(address));
            if (addressMap == null || !addressMap.TryGet(address, out var rec))
                throw new Exception($"没有找到地址：{address}");

            // 地址引用计数 +1  (按需在Dispose时-1)
            if (_addressRef.ContainsKey(address)) _addressRef[address]++;
            else _addressRef[address] = 1;

            // 加载bundle及其依赖，这一步是建立和bundle的链接
            LoadBundleWithDependenciesSync(rec.bundleName);

            // 加载资源解包
            var ab = _bundles[rec.bundleName].ab;
            var asset = ab.LoadAsset<T>(address);

            var realAsset = asset as T;
            if (realAsset == null) throw new Exception($"资源加载失败：{address}");
            return new AssetHandle<T>(address, realAsset);
        }

        //释放某个地址的资源 引用计数维护
        public void Release(string address)
        {
            if (!_addressRef.TryGetValue(address, out var cnt)) return;
            cnt--;
            if (cnt <= 0) _addressRef.Remove(address);
            else _addressRef[address] = cnt;

            //当对某个地址的引用为0时 尝试卸载bundle
            if (addressMap!=null && addressMap.TryGet(address, out var rec))
            {
                //对bundle作链式计数减一
                DecreaseBundleRefChain(rec.bundleName);
                switch (rec.assetTag)
                {
                    case AssetTag.Common: // 对公共资源，不卸载
                        Debug.LogWarning(rec.bundleName + " 为公共资源， 不卸载");
                        break;
                    default:
                        //根据address引用和bundle引用的双重结果尝试卸载
                        TryUnloadBundle(rec.bundleName);
                        break;
                }
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

        #region bundle 异步加载

        private async UniTask LoadBundleWithDependenciesAsync(string bundleName)
        {
            //递归加载依赖项
            var deps = GetDeps(bundleName);
            foreach(var dep in deps)
            {
                await LoadBundleAsync(dep);
            }
            await LoadBundleAsync(bundleName);
        }

        private async UniTask LoadBundleAsync(string bundleName)
        {
            //已经加载过了
            if(_bundles.TryGetValue(bundleName,out var entry))
            {
                entry.abInfo.RefCount++;
                _bundles[bundleName] = (entry.ab, entry.refCount + 1, entry.abInfo);
                return;
            }

            //如果正在加载 等待
            if(_loadingTasks.TryGetValue(bundleName,out var task))
            {
                var abWait = await task;
                if(_bundles.TryGetValue(bundleName,out entry))
                {
                    entry.abInfo.RefCount++;
                    _bundles[bundleName] = (entry.ab, entry.refCount + 1, entry.abInfo);
                }
                else
                {
                    _bundles[bundleName] = (abWait, 1, new ZAssetBundleInfo(bundleName));
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
            _bundles[bundleName] = (ab, 1, new ZAssetBundleInfo(bundleName)); 
        }

        private async UniTask<AssetBundle> LoadBundleInternalAsync(string bundleName,string fullPath)
        {
            if(!File.Exists(fullPath))
                throw new FileNotFoundException($"Bundle没有找到: {fullPath}");
            var assetBundle = await AssetBundle.LoadFromFileAsync(fullPath);
           
            if(assetBundle==null)
                throw new Exception($"加载bundle失败: {bundleName}");
            return assetBundle;

        }

        #endregion

        #region bundle 同步加载

        private void LoadBundleWithDependenciesSync(string bundleName)
        {
            // 递归加载依赖项
            var deps = GetDeps(bundleName);
            foreach (var dep in deps)
            {
                LoadBundleSync(dep);
            }
            LoadBundleSync(bundleName);
        }

        private void LoadBundleSync(string bundleName)
        {
            // 已经加载过了
            if (_bundles.TryGetValue(bundleName, out var entry))
            {
                entry.abInfo.RefCount++;
                _bundles[bundleName] = (entry.ab, entry.refCount + 1, entry.abInfo);
                if(_waitUnloadQueue.Contains(entry.abInfo))
                    _waitUnloadQueue.Remove(entry.abInfo);
                return;
            }

            // 没有加载过
            var path = Path.Combine(_abRoot, bundleName);
            var ab = LoadBundleInternalSync(bundleName, path);

            // 加载完成后 记录加载完成的dic
            _bundles[bundleName] = (ab, 1, new ZAssetBundleInfo(bundleName));
        }

        private AssetBundle LoadBundleInternalSync(string bundleName, string fullPath)
        {
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Bundle没有找到：{fullPath}");
            var assetBundle = AssetBundle.LoadFromFile(fullPath);

            if (assetBundle == null)
                throw new Exception($"加载bundle失败：{bundleName}");
            return assetBundle;
        }
        #endregion

        #endregion



        #region bundle 卸载实现       
        private void TryUnloadBundle(string bundleName)
        {
            //若引用计数<=0 则卸载 同时检查其依赖是否也可以卸载
            if (BundleRefCount(bundleName) > 0) return;

            UnloadBundle(bundleName, false);

            var deps = GetDeps(bundleName);
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

            // 引用计数为0的资源 加入待卸载队列
            _waitUnloadQueue.Enqueue(_bundles[bundleName].abInfo);

            entry.ab.Unload(unloadAllLoadedObj);
            _bundles.Remove(bundleName);
        }

        private int BundleRefCount(string bundleName)
        {
            // 以“地址引用”推导 bundle 引用数（保守估算）
            int count = 0;
            if (addressMap!=null)
            {
                foreach (var r in addressMap.entries)
                {
                    if (!string.Equals(r.bundleName, bundleName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach(var address in r.addressList)
                    {
                        if (_addressRef.TryGetValue(address, out var c))
                            count += c;
                    }
                   
                }
            }

            //加上bundle自身的递归引用
            if (_bundles.TryGetValue(bundleName, out var entry))
                count += entry.refCount;
            return count;
        }

        //对所有bundle的引用计数减一
        private void DecreaseBundleRefChain(string bundleName)
        {
            // 先对所有依赖减一
            var deps = GetDeps(bundleName);
            foreach (var d in deps)
            {
                if (_bundles.TryGetValue(d, out var depEntry))
                {
                    int next = Mathf.Max(0, depEntry.refCount - 1);
                    depEntry.abInfo.RefCount = next;
                    _bundles[d] = (depEntry.ab, next, depEntry.abInfo);
                }
            }

            // 再对根bundle自身做减一
            if (_bundles.TryGetValue(bundleName, out var entry))
            {
                int next = Mathf.Max(0, entry.refCount - 1);
                entry.abInfo.RefCount = next;
                _bundles[bundleName] = (entry.ab, next, entry.abInfo);
            }
        }

        #endregion

        //-------------- 工具 ---------------
        private string[] GetDeps(string bundleName)
        {
            if(_depsCache.TryGetValue(bundleName, out var depsArr)) return depsArr;
            var deps = _manifest?.GetAllDependencies(bundleName)?? Array.Empty<string>();
            _depsCache[bundleName] = deps;  
            return deps;
        }

    }
}

