using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> where T :class,new()
{
    private static readonly object _lock = new object();
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)//为了性能，减少不必要的加锁。
            {
                lock (_lock)//保证一次只有一个线程进入
                {
                    if(_instance == null)
                        _instance = new T();
                }             
            }                
            return _instance;
        }
    }

    public static void Dispose()
    {
        _instance = null;
    }
}
