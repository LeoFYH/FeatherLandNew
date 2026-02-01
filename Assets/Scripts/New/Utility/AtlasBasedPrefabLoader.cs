using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// 基于图集的预制体加载器
    /// 确保所需图集全部加载完成后再加载预制体，无需逐个绑定精灵
    /// </summary>
    public class AtlasBasedPrefabLoader : ViewControllerBase
    {
        [Header("加载配置")]
        [SerializeField] private List<AssetReferenceSpriteAtlas> requiredAtlases = new List<AssetReferenceSpriteAtlas>(); // 所需图集
        [SerializeField] private AssetReferenceGameObject prefabReference = null; // 要加载的预制体
        [SerializeField] private Transform parentTransform = null; // 父级变换组件
        [SerializeField] private bool autoLoadOnStart = true; // 是否在Start时自动加载
        [SerializeField] private bool destroyAfterLoad = true; // 加载完成后是否销毁自身

        private IAssetSystem assetSystem;
        private GameObject loadedPrefabInstance;
        private List<string> loadedAtlasGuids = new List<string>();

        private void Awake()
        {
            assetSystem = this.GetSystem<IAssetSystem>();
        }

        private void Start()
        {
            if (autoLoadOnStart)
            {
                LoadAtlasesAndPrefab();
            }
        }

        /// <summary>
        /// 加载所有必需的图集和预制体
        /// </summary>
        public void LoadAtlasesAndPrefab()
        {
            if (requiredAtlases.Count == 0 || prefabReference == null)
            {
                Debug.LogError("必需的图集或预制体引用不能为空");
                return;
            }

            StartCoroutine(LoadAtlasesThenPrefab());
        }

        /// <summary>
        /// 协程：先加载所有必需的图集，再加载预制体
        /// </summary>
        private IEnumerator LoadAtlasesThenPrefab()
        {
            // 增加所有图集的引用计数
            foreach (var atlasRef in requiredAtlases)
            {
                if (atlasRef != null && !string.IsNullOrEmpty(atlasRef.AssetGUID))
                {
                    assetSystem.AddAtlasReference(atlasRef.AssetGUID);
                    loadedAtlasGuids.Add(atlasRef.AssetGUID);
                }
            }

            Debug.Log($"开始加载 {requiredAtlases.Count} 个图集...");

            // 并行加载所有图集
            var atlasHandles = new List<AsyncOperationHandle<SpriteAtlas>>();
            
            foreach (var atlasRef in requiredAtlases)
            {
                if (atlasRef != null)
                {
                    var handle = atlasRef.LoadAssetAsync<SpriteAtlas>();
                    atlasHandles.Add(handle);
                }
            }

            // 等待所有图集加载完成
            foreach (var handle in atlasHandles)
            {
                while (!handle.IsDone)
                {
                    yield return null;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"图集加载失败: {handle.Result?.name}");
                }
            }

            Debug.Log("所有图集加载完成，开始加载预制体...");
            
            // 所有图集加载完成后，加载预制体
            LoadPrefab();
            
            // 如果设置了加载后销毁自身，则延迟销毁
            if (destroyAfterLoad)
            {
                yield return null; // 等待一帧确保加载完成
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 加载预制体
        /// </summary>
        private void LoadPrefab()
        {
            if (prefabReference == null)
            {
                Debug.LogError("预制体引用不能为空");
                return;
            }

            StartCoroutine(WaitForPrefabLoad());
        }

        /// <summary>
        /// 等待预制体加载完成并实例化
        /// </summary>
        private IEnumerator WaitForPrefabLoad()
        {
            var prefabHandle = prefabReference.LoadAssetAsync<GameObject>();
            
            while (!prefabHandle.IsDone)
            {
                yield return null;
            }

            if (prefabHandle.Status == AsyncOperationStatus.Succeeded)
            {
                // 实例化预制体
                Transform parent = parentTransform != null ? parentTransform : transform;
                loadedPrefabInstance = Instantiate(prefabHandle.Result, parent);
                
                Debug.Log($"预制体加载并实例化成功: {loadedPrefabInstance.name}");
                
                // 发送加载完成事件
                OnAllResourcesLoaded();
            }
            else
            {
                Debug.LogError("预制体加载失败");
            }
        }

        /// <summary>
        /// 所有资源加载完成后的回调
        /// </summary>
        protected virtual void OnAllResourcesLoaded()
        {
            // 子类可以重写此方法来执行特定的初始化逻辑
            Debug.Log("所有资源加载完成，预制体已实例化");
        }

        /// <summary>
        /// 释放加载的资源
        /// </summary>
        public void ReleaseResources()
        {
            // 销毁实例化的预制体
            if (loadedPrefabInstance != null)
            {
                Destroy(loadedPrefabInstance);
                loadedPrefabInstance = null;
            }

            // 减少所有图集的引用计数
            foreach (var atlasGuid in loadedAtlasGuids)
            {
                assetSystem.RemoveAtlasReference(atlasGuid);
            }
            loadedAtlasGuids.Clear();
            
            Debug.Log("所有资源已释放");
        }

        private void OnDestroy()
        {
            // 只有在不是因加载完成而销毁的情况下才释放资源
            if (!destroyAfterLoad || this != null)
            {
                ReleaseResources();
            }
        }

        #if UNITY_EDITOR
        [ContextMenu("Load Atlases & Prefab")]
        private void ContextMenuLoadAtlasesAndPrefab()
        {
            LoadAtlasesAndPrefab();
        }

        [ContextMenu("Release Resources")]
        private void ContextMenuReleaseResources()
        {
            ReleaseResources();
        }
        #endif
    }
}