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

        var handle = await ResManager.Instance.LoadAsync<GameObject>("TestCube");
        var go = GameObject.Instantiate(handle.Asset);
        go.transform.position = new Vector3(0, 0, 0);

        var handle2 = await ResManager.Instance.LoadAsync<GameObject>("TestCube2");
        var go2 = GameObject.Instantiate(handle2.Asset);
        go2.transform.position = new Vector3(3, 0, 0);

        var handle_sync = ResManager.Instance.LoadSync<GameObject>("TestCubeSync");
        var go_sync = GameObject.Instantiate(handle_sync.Asset);
        go_sync.transform.position = new Vector3(6, 0, 0);

        await UniTask.Delay(TimeSpan.FromSeconds(5), ignoreTimeScale: false);


        Debug.Log("释放游戏对象");
        Destroy(go);
        Destroy(go2);
        Destroy(go_sync);
        handle.Dispose();
        handle2.Dispose();
        handle_sync.Dispose();
    }




}
