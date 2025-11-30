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

        IEnumerator PreloadEssentialAssets(Action<float> onProgress, Action onCpmplete);
    }

    public class AssetSystem : AbstractSystem, IAssetSystem
    {
        protected override void OnInit()
        {
            LoadAssetAsync<GameObject>("OpenEggAnim", null);
        }

        private Dictionary<string, AsyncOperationHandle> HandleDic { get; } = new Dictionary<string, AsyncOperationHandle>();

        public async void LoadAssetAsync<T>(string assetName, Action<T> onCompleted, Action<float> onProgress = null)
        {
            if (HandleDic.ContainsKey(assetName))
            {
                onCompleted?.Invoke((T)HandleDic[assetName].Result);
                return;
            }
            Debug.Log(assetName);
            var handle = Addressables.LoadAssetAsync<T>(assetName);
            HandleDic.Add(assetName, handle);
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
            }
        }

        public void ReleaseAsset(string assetName)
        {
            if (HandleDic.Remove(assetName, out var handle))
            {
                Addressables.Release(handle);
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