using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZAsset;
using Cysharp.Threading.Tasks;
using System;
using System.Runtime.Remoting.Lifetime;

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

        await UniTask.Delay(TimeSpan.FromSeconds(2), ignoreTimeScale: false);

        Debug.Log("释放游戏对象");
        Destroy(go);
        handle.Dispose();
    }




}
