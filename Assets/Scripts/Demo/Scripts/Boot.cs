using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZAsset;
using Cysharp.Threading.Tasks;
using System;

public class Boot : MonoBehaviour
{

    private void Awake()
    {
        
    }

    private async UniTaskVoid Start()
    {
        await ResManager.Instance.InitAsync();

        Debug.Log("初始化完成");

        await UniTask.Delay(TimeSpan.FromSeconds(2), ignoreTimeScale: false);

        Debug.Log("开始加载游戏对象");

        //异步加载-回调
        //AssetHandle<GameObject> handle1 = default;
        //GameObject go1 = default;
        //ResManager.Instance.LoadAsync<GameObject>("SphereBlue", (handle) => {
        //    handle1 = handle;
        //    go1 = GameObject.Instantiate(handle1.Asset);
        //    go1.transform.position = new Vector3(0, 0, 0);
        //});
      
        //异步加载-await
        var handle = await ResManager.Instance.LoadAsync<GameObject>("SphereGreen");
        var go = GameObject.Instantiate(handle.Asset);
        go.transform.position = new Vector3(3, 0, 0);

        //同步加载
        var handle_sync = ResManager.Instance.LoadSync<GameObject>("TestCubeSync");
        var go_sync = GameObject.Instantiate(handle_sync.Asset);
        go_sync.transform.position = new Vector3(6, 0, 0);

        await UniTask.Delay(TimeSpan.FromSeconds(5), ignoreTimeScale: false);


        Debug.Log("释放游戏对象");
 
        Destroy(go);
        Destroy(go_sync);

        handle.Dispose();
        handle_sync.Dispose();
        var handle_sync2 = ResManager.Instance.LoadSync<GameObject>("TestCubeSync");
        var go_sync2 = GameObject.Instantiate(handle_sync2.Asset);
        go_sync2.transform.position = new Vector3(6, 0, 0);
        await UniTask.Delay(TimeSpan.FromSeconds(5), ignoreTimeScale: false);
        Destroy(go_sync2);
        handle_sync2.Dispose();
    }




}
