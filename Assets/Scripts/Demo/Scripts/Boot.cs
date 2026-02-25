using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZAsset;

public class Boot : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        await ResManager.Instance.InitAsync();

        Debug.Log("初始化完成");
        await UniTask.Delay(TimeSpan.FromSeconds(2), ignoreTimeScale: false);

        Debug.Log("开始加载游戏对象");

        // 异步加载-await（可传入超时）
        var handle1 = await ResManager.Instance.LoadAsync<GameObject>(
            "SphereBlue",
            timeout: TimeSpan.FromSeconds(10));
        var go1 = Instantiate(handle1.Asset);
        go1.transform.position = new Vector3(0, 0, 0);

        // 异步加载-await + 取消令牌
        var handle2 = await ResManager.Instance.LoadAsync<GameObject>(
            "SphereGreen",
            cancellationToken: this.GetCancellationTokenOnDestroy(),
            timeout: TimeSpan.FromSeconds(10));
        var go2 = Instantiate(handle2.Asset);
        go2.transform.position = new Vector3(3, 0, 0);

        // 同步加载
        var handleSync = ResManager.Instance.LoadSync<GameObject>("TestCubeSync");
        var goSync = Instantiate(handleSync.Asset);
        goSync.transform.position = new Vector3(6, 0, 0);

        await UniTask.Delay(TimeSpan.FromSeconds(5), ignoreTimeScale: false);

        Debug.Log("释放游戏对象");
        Destroy(go1);
        Destroy(go2);
        Destroy(goSync);

        handle1.Dispose();
        handle2.Dispose();
        handleSync.Dispose();
    }
}
