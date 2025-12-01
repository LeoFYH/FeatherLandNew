using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace BirdGame
{
    /// <summary>
    /// 资源管理系统 - 游戏内所有资源在此管理
    /// </summary>
    public interface IAssetSystem : ISystem
    {
        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="onCompleted"></param>
        /// <typeparam name="T"></typeparam>
        void LoadAssetAsync<T>(string assetName, Action<T> onCompleted, Action<float> onProgress = null);
        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="assetName"></param>
        void ReleaseAsset(string assetName);
        /// <summary>
        /// 释放所有资源（场景切换时使用）
        /// </summary>
        void ReleaseAllAssets();

        IEnumerator PreloadEssentialAssets(Action<float> onProgress, Action onCpmplete);
    }

    public class AssetSystem : AbstractSystem, IAssetSystem
    {
        protected override void OnInit()
        {
        }

        private Dictionary<string, AsyncOperationHandle> HandleDic { get; } = new Dictionary<string, AsyncOperationHandle>();
        private Dictionary<string, float> lastAccessTime = new Dictionary<string, float>();
        private const float ASSET_CLEANUP_INTERVAL = 60f; // Cleanup unused assets every 60 seconds
        private const float ASSET_UNUSED_TIME = 300f; // Release assets unused for 5 minutes
        private float lastCleanupTime = 0f;

        public async void LoadAssetAsync<T>(string assetName, Action<T> onCompleted, Action<float> onProgress = null)
        {
            // Periodic cleanup of unused assets
            if (Time.realtimeSinceStartup - lastCleanupTime > ASSET_CLEANUP_INTERVAL)
            {
                CleanupUnusedAssets();
                lastCleanupTime = Time.realtimeSinceStartup;
            }

            if (HandleDic.ContainsKey(assetName))
            {
                lastAccessTime[assetName] = Time.realtimeSinceStartup;
                onCompleted?.Invoke((T)HandleDic[assetName].Result);
                return;
            }
            
            // Only log in editor to reduce memory allocations
            #if UNITY_EDITOR
            Debug.Log(assetName);
            #endif
            
            var handle = Addressables.LoadAssetAsync<T>(assetName);
            HandleDic.Add(assetName, handle);
            lastAccessTime[assetName] = Time.realtimeSinceStartup;
            
            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await Task.Yield();
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onProgress?.Invoke(1f);
                onCompleted?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"资源加载失败: {assetName}");
                onProgress?.Invoke(1f);
                onCompleted?.Invoke(default(T)); // 传递默认值，让UI层处理
                // Clean up failed handle
                HandleDic.Remove(assetName);
                lastAccessTime.Remove(assetName);
            }
        }

        public void ReleaseAsset(string assetName)
        {
            if (HandleDic.Remove(assetName, out var handle))
            {
                Addressables.Release(handle);
            }
            lastAccessTime.Remove(assetName);
        }

        /// <summary>
        /// Clean up assets that haven't been accessed for a while
        /// </summary>
        private void CleanupUnusedAssets()
        {
            float currentTime = Time.realtimeSinceStartup;
            List<string> assetsToRelease = new List<string>();

            foreach (var kvp in lastAccessTime)
            {
                if (currentTime - kvp.Value > ASSET_UNUSED_TIME)
                {
                    assetsToRelease.Add(kvp.Key);
                }
            }

            foreach (var assetName in assetsToRelease)
            {
                ReleaseAsset(assetName);
            }

            if (assetsToRelease.Count > 0)
            {
                #if UNITY_EDITOR
                Debug.Log($"释放了 {assetsToRelease.Count} 个未使用的资源");
                #endif
            }
        }

        /// <summary>
        /// Force cleanup all assets (use when changing scenes)
        /// </summary>
        public void ReleaseAllAssets()
        {
            var keys = new List<string>(HandleDic.Keys);
            foreach (var key in keys)
            {
                ReleaseAsset(key);
            }
        }

        public IEnumerator PreloadEssentialAssets(Action<float> onProgress, Action onComplete)
        {
            onProgress?.Invoke(0);
            var handles = new List<AsyncOperationHandle>();
            // 获取所有需要预加载的资源
            var locationsHandle = Addressables.LoadResourceLocationsAsync("preload");
            yield return locationsHandle;
        
            if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("预加载资源位置获取失败");
                yield break;
            }

            List<object> keys = new List<object>();
            foreach (var location in locationsHandle.Result)
            {
                keys.Add(location.PrimaryKey);
            }

            int count = keys.Count;
            int current = 0;
            // 分帧加载所有资源
            foreach (var key in keys)
            {
                float startTime = Time.realtimeSinceStartup;
            
                var handle = Addressables.LoadAssetAsync<object>(key);
                handles.Add(handle);
            
                // 等待加载完成或超过帧时间限制
                while (!handle.IsDone)
                {
                    if (Time.realtimeSinceStartup - startTime > 0.01f)
                    {
                        yield return null; // 让出一帧
                        startTime = Time.realtimeSinceStartup;
                    }
                    else
                    {
                        // 小等待避免过度消耗
                        System.Threading.Thread.Sleep(1);
                    }
                }

                current++;
                onProgress?.Invoke(current * 1f / count);
            
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"资源加载失败: {key}");
                }
            }

            foreach (var handle in handles)
            {
                Addressables.Release(handle);
            }
            
            onComplete?.Invoke();
        }
    }
}