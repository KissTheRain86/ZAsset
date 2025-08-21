using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZAsset
{
    public enum AssetTag
    {
        Default = 0, // 默认资源
        Common = 1,  // common资源(释放资源时，引用计数-1，不真正卸载)
    }
}
