using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZAsset
{
    public interface IRecycle
    {
        public void Create();
        public void Dispose();
    }

    /// <summary>
    /// ObjectPool.cs
    /// Object对象池
    /// </summary>
    public class ObjectPool
    {
        private const int MAX_OBJECT_NUM = 100;
        /// <summary>
        /// 单例管理池
        /// </summary>
        public readonly static ObjectPool Instance = new ObjectPool();

        /// <summary>
        /// 对象管理池
        /// </summary>
        private Dictionary<int, Stack<IRecycle>> ObjectPoolMap;

        private ObjectPool()
        {
            ObjectPoolMap = new Dictionary<int, Stack<IRecycle>>();
        }

        /// <summary>
        /// 初始化指定数量的指定对象
        /// </summary>
        public void Initialize<T>(int number) where T : IRecycle,new()
        {
            for (int i = 0; i < number; i++)
            {
                var obj = new T();
                Push<T>(obj);
            }
            var hashcode = typeof(T).GetHashCode();
            //Debug.Log(string.Format("初始化类型:{0}的剩余数量:{1}", typeof(T).Name, ObjectPoolMap[hashcode].Count));
        }

        /// <summary>
        /// 指定对象进池
        /// </summary>
        public void Push<T>(T obj) where T : IRecycle, new()
        {
            var hashcode = obj.GetType().GetHashCode();
            if (!ObjectPoolMap.ContainsKey(hashcode))
            {
                ObjectPoolMap.Add(hashcode, new Stack<IRecycle>());
            }
            if (ObjectPoolMap[hashcode].Count <= MAX_OBJECT_NUM)
                ObjectPoolMap[hashcode].Push(obj);
            //Debug.Log(string.Format("类型:{0}进对象池!", typeof(T).Name));
            //Debug.Log(string.Format("池里类型:{0}的剩余数量:{1}", typeof(T).Name, ObjectPoolMap[hashcode].Count));
        }

        /// <summary>
        /// 弹出可用指定对象
        /// </summary>
        public T Pop<T>() where T : IRecycle, new()
        {
            var hashcode = typeof(T).GetHashCode();
            if (ObjectPoolMap.ContainsKey(hashcode)&& ObjectPoolMap[hashcode].Count>0)
            {
                var instance = ObjectPoolMap[hashcode].Pop();
                instance.Create();
                //Debug.Log(string.Format("类型:{0}出对象池!", typeof(T).Name));
                //Debug.Log(string.Format("池里类型:{0}的剩余数量:{1}", typeof(T).Name, ObjectPoolMap[hashcode].Count));
                return (T)instance;
            }
            else
            {
                T instance = new T();
                instance.Create();
                return instance;
            }
        }

        /// <summary>
        /// 清除指定类型的对象缓存
        /// </summary>
        public bool Clear<T>() where T : IRecycle, new()
        {
            var hashcode = typeof(T).GetHashCode();
            if (ObjectPoolMap.ContainsKey(hashcode))
            {
                //Debug.Log(string.Format("清除对象池里的类型:{0}", typeof(T).Name));
                ObjectPoolMap.Remove(hashcode);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 清除所有对象缓存
        /// </summary>
        public void ClearAll()
        {
            ObjectPoolMap.Clear();
        }
    }
}
