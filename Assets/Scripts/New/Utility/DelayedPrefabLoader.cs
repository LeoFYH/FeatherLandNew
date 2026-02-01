using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// 延迟预制体加载器
    /// 在图集加载完成后再加载和实例化预制体，避免逐个绑定精灵
    /// </summary>
    public class DelayedPrefabLoader : ViewControllerBase
    {
        [Header("预制体配置")]
        [SerializeField] private AssetReferenceSpriteAtlas atlasReference = null; // 需要的图集
        [SerializeField] private AssetReferenceGameObject prefabReference = null; // 要加载的预制体
        [SerializeField] private Transform parentTransform = null; // 父级变换组件
        [SerializeField] private bool autoLoadOnStart = true; // 是否在Start时自动加载

        private IAssetSystem assetSystem;
        private GameObject loadedPrefabInstance;
        private bool isAtlasLoaded = false;

        private void Awake()
        {
            assetSystem = this.GetSystem<IAssetSystem>();
        }

        private void Start()
        {
            if (autoLoadOnStart)
            {
                LoadAtlasAndPrefab();
            }
        }

        /// <summary>
        /// 加载图集和预制体
        /// </summary>
        public void LoadAtlasAndPrefab()
        {
            if (atlasReference == null || prefabReference == null)
            {
                Debug.LogError("图集引用或预制体引用不能为空");
                return;
            }

            // 开始加载图集
            StartCoroutine(LoadAtlasThenPrefab());
        }

        /// <summary>
        /// 协程：先加载图集，再加载预制体
        /// </summary>
        private IEnumerator LoadAtlasThenPrefab()
        {
            // 增加图集引用计数
            if (!string.IsNullOrEmpty(atlasReference.AssetGUID))
            {
                assetSystem.AddAtlasReference(atlasReference.AssetGUID);
            }

            // 加载图集
            Debug.Log("开始加载图集...");
            AsyncOperationHandle<SpriteAtlas> atlasHandle = atlasReference.LoadAssetAsync<SpriteAtlas>();
            
            while (!atlasHandle.IsDone)
            {
                yield return null;
            }

            if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("图集加载成功，开始加载预制体...");
                isAtlasLoaded = true;
                
                // 图集加载完成后，加载预制体
                LoadPrefab();
            }
            else
            {
                Debug.LogError("图集加载失败");
                // 减少图集引用计数
                if (!string.IsNullOrEmpty(atlasReference.AssetGUID))
                {
                    assetSystem.RemoveAtlasReference(atlasReference.AssetGUID);
                }
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

            // 加载预制体
            AsyncOperationHandle<GameObject> prefabHandle = prefabReference.LoadAssetAsync<GameObject>();
            
            StartCoroutine(WaitForPrefabLoad(prefabHandle));
        }

        /// <summary>
        /// 等待预制体加载完成并实例化
        /// </summary>
        private IEnumerator WaitForPrefabLoad(AsyncOperationHandle<GameObject> handle)
        {
            while (!handle.IsDone)
            {
                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 实例化预制体
                Transform parent = parentTransform != null ? parentTransform : transform;
                loadedPrefabInstance = Instantiate(handle.Result, parent);
                
                Debug.Log($"预制体加载并实例化成功: {loadedPrefabInstance.name}");
                
                // 发送图集和预制体加载完成事件
                OnAtlasAndPrefabLoaded();
            }
            else
            {
                Debug.LogError("预制体加载失败");
            }
        }

        /// <summary>
        /// 图集和预制体加载完成后的回调
        /// </summary>
        protected virtual void OnAtlasAndPrefabLoaded()
        {
            // 子类可以重写此方法来执行特定的初始化逻辑
            Debug.Log("图集和预制体加载完成");
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

            // 减少图集引用计数
            if (isAtlasLoaded && !string.IsNullOrEmpty(atlasReference.AssetGUID))
            {
                assetSystem.RemoveAtlasReference(atlasReference.AssetGUID);
                isAtlasLoaded = false;
                Debug.Log("资源已释放");
            }
        }

        private void OnDestroy()
        {
            ReleaseResources();
        }

        #if UNITY_EDITOR
        [ContextMenu("Load Atlas & Prefab")]
        private void ContextMenuLoadAtlasAndPrefab()
        {
            LoadAtlasAndPrefab();
        }

        [ContextMenu("Release Resources")]
        private void ContextMenuReleaseResources()
        {
            ReleaseResources();
        }
        #endif
    }
}