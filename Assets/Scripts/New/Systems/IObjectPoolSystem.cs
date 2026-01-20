using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 对象池系统 - 用于管理可重用的游戏对象，避免频繁的实例化和销毁
    /// </summary>
    public interface IObjectPoolSystem : ISystem
    {
        /// <summary>
        /// 从对象池中获取对象
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        /// <param name="parent">父物体Transform</param>
        /// <param name="onLoaded">加载完成回调</param>
        void Get(string prefabName, Transform parent, Action<GameObject> onLoaded);

        /// <summary>
        /// 将对象回收到对象池
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        /// <param name="obj">要回收的对象</param>
        void Recycle(string prefabName, GameObject obj);

        /// <summary>
        /// 清空指定对象池
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        void Clear(string prefabName);

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        void ClearAll();
    }

    public class ObjectPoolSystem : AbstractSystem, IObjectPoolSystem
    {
        /// <summary>
        /// 单个对象池
        /// </summary>
        private class Pool
        {
            public GameObject prefab;
            public Stack<GameObject> inactive = new Stack<GameObject>();
            public HashSet<GameObject> active = new HashSet<GameObject>();
            // 保存预制体的原始transform设置
            public Vector3 originalLocalPosition;
            public Quaternion originalLocalRotation;
            public Vector3 originalLocalScale;
        }

        // 所有对象池的字典
        private Dictionary<string, Pool> pools = new Dictionary<string, Pool>();

        // 对象池根节点
        private Transform poolRoot;

        protected override void OnInit()
        {
            // 创建对象池根节点
            GameObject rootObj = new GameObject("ObjectPool");
            GameObject.DontDestroyOnLoad(rootObj);
            poolRoot = rootObj.transform;
        }

        public void Get(string prefabName, Transform parent, Action<GameObject> onLoaded)
        {
            // 如果对象池不存在，创建它
            if (!pools.ContainsKey(prefabName))
            {
                // 异步加载预制体
                this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>(prefabName, (prefab) =>
                {
                    if (prefab == null)
                    {
                        Debug.LogError($"无法加载预制体: {prefabName}");
                        onLoaded?.Invoke(null);
                        return;
                    }

                    Pool pool = new Pool 
                    { 
                        prefab = prefab,
                        // 保存预制体的原始transform
                        originalLocalPosition = prefab.transform.localPosition,
                        originalLocalRotation = prefab.transform.localRotation,
                        originalLocalScale = prefab.transform.localScale
                    };
                    pools[prefabName] = pool;

                    // 创建新对象
                    GameObject obj = CreateNewObject(pool, parent);
                    onLoaded?.Invoke(obj);
                });
                return;
            }

            Pool targetPool = pools[prefabName];

            // 从池中获取对象
            if (targetPool.inactive.Count > 0)
            {
                GameObject obj = targetPool.inactive.Pop();
                if (obj != null)
                {
                    obj.transform.SetParent(parent);
                    // 恢复预制体的原始transform设置
                    obj.transform.localPosition = targetPool.originalLocalPosition;
                    obj.transform.localRotation = targetPool.originalLocalRotation;
                    obj.transform.localScale = targetPool.originalLocalScale;
                    obj.SetActive(true);
                    targetPool.active.Add(obj);
                    onLoaded?.Invoke(obj);
                    return;
                }
            }

            // 池中没有可用对象，创建新对象
            GameObject newObj = CreateNewObject(targetPool, parent);
            onLoaded?.Invoke(newObj);
        }

        private GameObject CreateNewObject(Pool pool, Transform parent)
        {
            GameObject obj = GameObject.Instantiate(pool.prefab, parent);
            // 保持预制体的原始transform设置（Instantiate已经自动复制了这些值）
            // 不需要额外设置，Unity的Instantiate会保留预制体的localPosition/Rotation/Scale
            
            // 添加PooledObject组件，用于标记对象属于哪个池
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = obj.AddComponent<PooledObject>();
            }
            pooledObj.poolName = GetPoolName(pool);
            
            pool.active.Add(obj);
            return obj;
        }

        private string GetPoolName(Pool pool)
        {
            foreach (var kvp in pools)
            {
                if (kvp.Value == pool)
                    return kvp.Key;
            }
            return string.Empty;
        }

        public void Recycle(string prefabName, GameObject obj)
        {
            if (obj == null) return;

            if (!pools.ContainsKey(prefabName))
            {
                Debug.LogWarning($"对象池不存在: {prefabName}，直接销毁对象");
                GameObject.Destroy(obj);
                return;
            }

            Pool pool = pools[prefabName];
            
            // 从活跃列表移除
            pool.active.Remove(obj);

            // 添加到非活跃列表
            if (!pool.inactive.Contains(obj))
            {
                obj.SetActive(false);
                obj.transform.SetParent(poolRoot);
                pool.inactive.Push(obj);
            }
        }

        public void Clear(string prefabName)
        {
            if (!pools.ContainsKey(prefabName)) return;

            Pool pool = pools[prefabName];

            // 销毁所有非活跃对象
            while (pool.inactive.Count > 0)
            {
                GameObject obj = pool.inactive.Pop();
                if (obj != null)
                    GameObject.Destroy(obj);
            }

            // 销毁所有活跃对象
            foreach (GameObject obj in pool.active)
            {
                if (obj != null)
                    GameObject.Destroy(obj);
            }
            pool.active.Clear();

            pools.Remove(prefabName);
        }

        public void ClearAll()
        {
            List<string> poolNames = new List<string>(pools.Keys);
            foreach (string name in poolNames)
            {
                Clear(name);
            }
            pools.Clear();
        }
    }

    /// <summary>
    /// 标记对象属于哪个对象池
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string poolName;
    }
}
