using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 添加这一行以支持LINQ方法如Take
using System.Threading.Tasks;
using Cysharp.Threading.Tasks; // ✅ 使用UniTask替代Task
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace BirdGame
{
    /// <summary>
    /// 资源管理系统 - 游内所有资源在此管理
    /// </summary>
    public interface IAssetSystem : ISystem
    {
        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="onCompleted"></param>
        /// <param name="onProgress"></param>
        /// <typeparam name="T"></typeparam>
        void LoadAssetAsync<T>(string assetName, Action<T> onCompleted, Action<float> onProgress = null);

        /// <summary>
        /// 通过AssetReference加载预制体（推荐方式，避免GUID解析问题）
        /// </summary>
        void LoadPrefabAsync(AssetReferenceGameObject assetRef, Action<GameObject> onCompleted, Action<float> onProgress = null);
        
        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="assetName"></param>
        void ReleaseAsset(string assetName);

        IEnumerator PreloadEssentialAssets(Action<float> onProgress, Action onComplete);
        
        /// <summary>
        /// 加载图集中的精灵，自动管理图集的加载和释放
        /// </summary>
        /// <param name="spriteAddress">精灵地址</param>
        /// <param name="atlasAddress">图集地址</param>
        /// <param name="onCompleted">加载完成回调</param>
        /// <param name="onProgress">加载进度回调</param>
        void LoadSpriteFromAtlasAsync(string spriteAddress, string atlasAddress, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null);
        
        /// <summary>
        /// 释放图集中的精灵，当图集不再被引用时自动释放图集
        /// </summary>
        /// <param name="spriteAddress">精灵地址</param>
        /// <param name="atlasAddress">图集地址</param>
        void ReleaseSpriteFromAtlas(string spriteAddress, string atlasAddress);
        
        /// <summary>
        /// 使用AssetReference加载图集中的精灵
        /// </summary>
        /// <param name="spriteName">精灵名称</param>
        /// <param name="atlasReference">图集AssetReference</param>
        /// <param name="onCompleted">加载完成回调</param>
        /// <param name="onProgress">加载进度回调</param>
        void LoadSpriteFromAtlasAsync(string spriteName, AssetReferenceSpriteAtlas atlasReference, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null);

        public void AddAtlasReference(string atlasGuid);

        void RemoveAtlasReference(string atlasGuid);
    }

    public class AssetSystem : AbstractSystem, IAssetSystem
    {
        // 存储普通资源的句柄
        private Dictionary<string, AsyncOperationHandle> HandleDic { get; } = new Dictionary<string, AsyncOperationHandle>();
        
        // 存储图集的引用计数
        private Dictionary<string, int> AtlasReferenceCounts { get; } = new Dictionary<string, int>();
        
        // 存储图集的句柄
        private Dictionary<string, AsyncOperationHandle> AtlasHandles { get; } = new Dictionary<string, AsyncOperationHandle>();
        
        // 存储精灵到图集的映射关系
        private Dictionary<string, string> SpriteToAtlasMap { get; } = new Dictionary<string, string>();
        
        // 存储图集精灵的句柄
        private Dictionary<string, AsyncOperationHandle> SpriteHandles { get; } = new Dictionary<string, AsyncOperationHandle>();
        
        // 存储AssetReference图集的引用计数
        private Dictionary<string, int> AssetRefAtlasReferenceCounts { get; } = new Dictionary<string, int>();
        
        // 存储AssetReference图集的句柄
        private Dictionary<string, AsyncOperationHandle> AssetRefAtlasHandles { get; } = new Dictionary<string, AsyncOperationHandle>();

        protected override void OnInit()
        {
            //LoadAssetAsync<GameObject>("OpenEggAnim", null);
        }

        /// <summary>
        /// ✅ 优化：使用UniTask替代Task.Yield()，真正的异步加载，不阻塞主线程
        /// 这是解决"首次加载卡顿"的关键优化
        /// </summary>
        public async void LoadAssetAsync<T>(string assetName, Action<T> onCompleted, Action<float> onProgress = null)
        {
            if (HandleDic.ContainsKey(assetName))
            {
                onCompleted?.Invoke((T)HandleDic[assetName].Result);
                return;
            }

            var handle = Addressables.LoadAssetAsync<T>(assetName);
            HandleDic.Add(assetName, handle);
            
            // ✅ 优化：使用UniTask替代Task.Yield()，不阻塞主线程
            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await UniTask.Yield(); // 使用UniTask，真正的异步
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

        public async void LoadPrefabAsync(AssetReferenceGameObject assetRef, Action<GameObject> onCompleted, Action<float> onProgress = null)
        {
            if (assetRef == null || !assetRef.RuntimeKeyIsValid())
            {
                Debug.LogError("预制体加载失败: AssetReference 为空或 RuntimeKey 无效");
                onCompleted?.Invoke(null);
                return;
            }
            string key = assetRef.AssetGUID;
            if (HandleDic.ContainsKey(key))
            {
                var existingHandle = HandleDic[key];
                while (!existingHandle.IsDone)
                {
                    onProgress?.Invoke(existingHandle.PercentComplete);
                    await UniTask.Yield();
                }
                GameObject result = existingHandle.Status == AsyncOperationStatus.Succeeded && existingHandle.Result != null
                    ? (GameObject)existingHandle.Result
                    : null;
                if (result == null && existingHandle.Status != AsyncOperationStatus.Succeeded)
                    Debug.LogError($"预制体加载失败 key={key}, error={existingHandle.OperationException}");
                onCompleted?.Invoke(result);
                return;
            }
            var handle = assetRef.LoadAssetAsync<GameObject>();
            HandleDic.Add(key, handle);
            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await UniTask.Yield();
            }
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                onCompleted?.Invoke(handle.Result);
            }
            else
            {
                HandleDic.Remove(key);
                if (handle.Status != AsyncOperationStatus.Succeeded)
                    Debug.LogError($"预制体加载失败 key={key}, error={handle.OperationException}");
                else
                    Debug.LogError($"预制体加载失败 key={key}, Result 为空");
                onCompleted?.Invoke(null);
            }
        }

        // 为精灵资源自动处理图集加载
        // ✅ 优化：使用UniTask
        private async UniTask LoadAtlasForSpriteIfNeeded<T>(string assetName, AsyncOperationHandle handle, Action<T> onCompleted, Action<float> onProgress)
        {
            // 先获取精灵资源本身
            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await UniTask.Yield(); // 使用UniTask
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"精灵加载失败: {assetName}");
                onCompleted?.Invoke(default(T));
                return;
            }

            // 检查精灵是否存在映射关系（即是否通过LoadSpriteFromAtlasAsync加载过）
            if (SpriteToAtlasMap.ContainsKey(assetName))
            {
                // 精灵属于图集，确保图集已经加载
                string atlasAddress = SpriteToAtlasMap[assetName];
                
                // 增加图集引用计数
                if (!AtlasReferenceCounts.ContainsKey(atlasAddress))
                {
                    AtlasReferenceCounts[atlasAddress] = 0;
                }
                AtlasReferenceCounts[atlasAddress]++;
                
                // 如果图集尚未加载，说明有问题，因为正常应该先通过LoadSpriteFromAtlasAsync加载图集
                if (!AtlasHandles.ContainsKey(atlasAddress))
                {
                    Debug.LogWarning($"精灵{assetName}标记为属于图集{atlasAddress}，但图集尚未加载。请使用LoadSpriteFromAtlasAsync方法加载图集精灵。");
                    
                    // 不再尝试作为后备方案加载图集
                    // 所有图集精灵都应该通过LoadSpriteFromAtlasAsync方法加载
                }
            }
            // 如果精灵不在已知映射中，我们不再尝试自动查找其所属图集
            // 用户需要通过LoadSpriteFromAtlasAsync方法显式加载图集精灵
            
            onCompleted?.Invoke((T)handle.Result);
        }
        
        public void ReleaseAsset(string assetName)
        {
            if (HandleDic.Remove(assetName, out var handle))
            {
                // 如果是精灵资源，检查是否需要释放关联的图集
                if (handle.Result is Sprite sprite)
                {
                    // 查找此精灵是否与任何图集有关联
                    var atlasAddress = FindAtlasForSprite(assetName);
                    if (!string.IsNullOrEmpty(atlasAddress))
                    {
                        ReleaseAtlasByAddress(atlasAddress);
                    }
                }
                // 如果是GameObject(Prefab)资源，检查其中是否有图集精灵
                else if (handle.Result is GameObject prefab)
                {
                    ProcessPrefabAtlasRelease(prefab, assetName);
                }

                Addressables.Release(handle);
            }
        }

        // 根据精灵地址查找对应的图集地址
        private string FindAtlasForSprite(string spriteAddress)
        {
            // 遍历精灵到图集的映射关系
            foreach (var mapping in SpriteToAtlasMap)
            {
                if (mapping.Key == spriteAddress)
                {
                    return mapping.Value;
                }
            }
            
            // 如果没有找到直接映射，尝试根据命名规则推断图集
            // 这里可以根据项目实际情况调整查找逻辑
            return null;
        }

        // 释放图集
        private void ReleaseAtlasByAddress(string atlasAddress)
        {
            // 检查精灵是否存在映射关系
            if (AtlasReferenceCounts.ContainsKey(atlasAddress))
            {
                // 减少图集引用计数
                AtlasReferenceCounts[atlasAddress]--;
                
                // 如果引用计数为0，释放图集
                if (AtlasReferenceCounts[atlasAddress] <= 0)
                {
                    ReleaseAtlasInternal(atlasAddress);
                }
            }
        }

        /// <summary>
        /// ✅ 优化：使用UniTask异步加载图集和精灵
        /// </summary>
        public async void LoadSpriteFromAtlasAsync(string spriteAddress, string atlasAddress, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null)
        {
            // 建立精灵到图集的映射关系
            if (!SpriteToAtlasMap.ContainsKey(spriteAddress))
            {
                SpriteToAtlasMap[spriteAddress] = atlasAddress;
            }

            // 增加图集引用计数
            if (!AtlasReferenceCounts.ContainsKey(atlasAddress))
            {
                AtlasReferenceCounts[atlasAddress] = 0;
            }
            AtlasReferenceCounts[atlasAddress]++;

            // 如果图集尚未加载，则先加载图集
            if (!AtlasHandles.ContainsKey(atlasAddress))
            {
                Debug.Log($"开始加载图集: {atlasAddress}");
                
                var atlasHandle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasAddress);
                AtlasHandles[atlasAddress] = atlasHandle;
                
                // ✅ 优化：使用UniTask
                while (!atlasHandle.IsDone)
                {
                    onProgress?.Invoke(atlasHandle.PercentComplete * 0.5f); // 图集加载占总进度的50%
                    await UniTask.Yield();
                }

                if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"图集加载成功: {atlasAddress}");
                    // 图集加载完成后，加载精灵
                    LoadSpriteInternal(spriteAddress, onCompleted, onProgress);
                }
                else
                {
                    Debug.LogError($"图集加载失败: {atlasAddress}");
                    AtlasReferenceCounts[atlasAddress]--;
                    if (AtlasReferenceCounts[atlasAddress] <= 0)
                    {
                        AtlasHandles.Remove(atlasAddress);
                    }
                    onCompleted?.Invoke(null);
                }
            }
            else
            {
                // 图集已加载，直接加载精灵
                LoadSpriteInternal(spriteAddress, onCompleted, onProgress);
            }
        }

        /// <summary>
        /// ✅ 优化：使用UniTask加载精灵
        /// </summary>
        private async void LoadSpriteInternal(string spriteAddress, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null)
        {
            // 检查精灵是否已经在加载
            if (SpriteHandles.ContainsKey(spriteAddress))
            {
                var existingHandle = SpriteHandles[spriteAddress];
                if (existingHandle.IsValid())
                {
                    // ✅ 优化：使用UniTask等待
                    while (!existingHandle.IsDone)
                    {
                        await UniTask.Yield();
                    }
                    
                    if (existingHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        onCompleted?.Invoke((UnityEngine.Sprite)existingHandle.Result);
                    }
                    else
                    {
                        Debug.LogError($"精灵加载失败: {spriteAddress}");
                        onCompleted?.Invoke(null);
                    }
                    return;
                }
            }

            // 加载精灵
            var spriteHandle = Addressables.LoadAssetAsync<UnityEngine.Sprite>(spriteAddress);
            SpriteHandles[spriteAddress] = spriteHandle;

            // ✅ 优化：使用UniTask
            while (!spriteHandle.IsDone)
            {
                // 精灵加载占总进度的后50%
                onProgress?.Invoke(0.5f + spriteHandle.PercentComplete * 0.5f);
                await UniTask.Yield();
            }

            if (spriteHandle.Status == AsyncOperationStatus.Succeeded)
            {
                onProgress?.Invoke(1f);
                onCompleted?.Invoke((UnityEngine.Sprite)spriteHandle.Result);
            }
            else
            {
                Debug.LogError($"精灵加载失败: {spriteAddress}");
                onProgress?.Invoke(1f);
                onCompleted?.Invoke(null);
            }
        }

        public void ReleaseSpriteFromAtlas(string spriteAddress, string atlasAddress)
        {
            // 检查精灵是否存在映射关系
            if (SpriteToAtlasMap.ContainsKey(spriteAddress))
            {
                // 减少图集引用计数
                if (AtlasReferenceCounts.ContainsKey(atlasAddress))
                {
                    AtlasReferenceCounts[atlasAddress]--;
                    
                    // 如果引用计数为0，释放图集
                    if (AtlasReferenceCounts[atlasAddress] <= 0)
                    {
                        ReleaseAtlasInternal(atlasAddress);
                    }
                }

                // 释放精灵资源
                if (SpriteHandles.Remove(spriteAddress, out var spriteHandle))
                {
                    Addressables.Release(spriteHandle);
                }
                
                // 从映射中移除
                SpriteToAtlasMap.Remove(spriteAddress);
            }
        }

        private void ReleaseAtlasInternal(string atlasAddress)
        {
            if (AtlasHandles.Remove(atlasAddress, out var atlasHandle))
            {
                Addressables.Release(atlasHandle);
                AtlasReferenceCounts.Remove(atlasAddress);
                Debug.Log($"已释放图集: {atlasAddress}");
            }
        }

        public IEnumerator PreloadEssentialAssets(Action<float> onProgress, Action onComplete)
        {
            onProgress?.Invoke(0);
            var handl = Addressables.InitializeAsync();
            yield return handl;

            // 获取所有带 "preload" 标签的资源
            var locationsHandle = Addressables.LoadResourceLocationsAsync("preload");
            yield return locationsHandle;

            if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("预加载资源位置获取失败");
                yield break;
            }

            var locations = locationsHandle.Result;
            var keys = new List<object>();
            foreach (var location in locations)
                keys.Add(location.PrimaryKey);

            int count = keys.Count;
            int current = 0;

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                string keyStr = key.ToString();
                if (HandleDic.ContainsKey(keyStr))
                {
                    current++;
                    onProgress?.Invoke(current * 1f / count);
                    continue;
                }

                var handle = Addressables.LoadAssetAsync<object>(key);
                while (!handle.IsDone)
                {
                    onProgress?.Invoke((current + handle.PercentComplete) * 1f / count);
                    yield return null;
                }

                current++;
                onProgress?.Invoke(current * 1f / count);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                    HandleDic[keyStr] = handle;
                else
                {
                    Debug.LogError($"预加载资源失败: {key}");
                    Addressables.Release(handle);
                }
            }

            Addressables.Release(locationsHandle);
            onComplete?.Invoke();
        }

        // 处理Prefab中引用的图集资源
        // ✅ 优化：使用UniTask
        private async UniTask ProcessPrefabAtlases(GameObject prefab, string assetName, Action<float> onProgress = null)
        {
            if (prefab == null) return;

            // 获取所有Renderer组件，特别是SpriteRenderer
            var spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in spriteRenderers)
            {
                if (renderer.sprite != null)
                {
                    var spriteName = renderer.sprite.name;
                    // 尝试从SpriteToAtlasMap查找精灵所属的图集
                    if (SpriteToAtlasMap.ContainsKey(spriteName))
                    {
                        string atlasAddress = SpriteToAtlasMap[spriteName];
                        
                        // 增加图集引用计数
                        if (!AtlasReferenceCounts.ContainsKey(atlasAddress))
                        {
                            AtlasReferenceCounts[atlasAddress] = 0;
                        }
                        AtlasReferenceCounts[atlasAddress]++;
                        
                        // 如果图集尚未加载，则先加载图集
                        if (!AtlasHandles.ContainsKey(atlasAddress))
                        {
                            Debug.Log($"开始加载图集: {atlasAddress}");
                            
                            var atlasHandle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasAddress);
                            AtlasHandles[atlasAddress] = atlasHandle;
                            
                            // ✅ 优化：使用UniTask
                            while (!atlasHandle.IsDone)
                            {
                                // 图集加载进度占剩余30%的进度
                                onProgress?.Invoke(0.7f + atlasHandle.PercentComplete * 0.3f);
                                await UniTask.Yield();
                            }

                            if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
                            {
                                Debug.Log($"图集加载成功: {atlasAddress}");
                            }
                            else
                            {
                                Debug.LogError($"图集加载失败: {atlasAddress}");
                                AtlasReferenceCounts[atlasAddress]--;
                            }
                        }
                    }
                }
            }
            
            // 处理Image组件（UI精灵）
            var images = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var image in images)
            {
                if (image.sprite != null)
                {
                    var spriteName = image.sprite.name;
                    // 尝试从SpriteToAtlasMap查找精灵所属的图集
                    if (SpriteToAtlasMap.ContainsKey(spriteName))
                    {
                        string atlasAddress = SpriteToAtlasMap[spriteName];
                        
                        // 增加图集引用计数
                        if (!AtlasReferenceCounts.ContainsKey(atlasAddress))
                        {
                            AtlasReferenceCounts[atlasAddress] = 0;
                        }
                        AtlasReferenceCounts[atlasAddress]++;
                        
                        // 如果图集尚未加载，则先加载图集
                        if (!AtlasHandles.ContainsKey(atlasAddress))
                        {
                            Debug.Log($"开始加载图集: {atlasAddress}");
                            
                            var atlasHandle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasAddress);
                            AtlasHandles[atlasAddress] = atlasHandle;
                            
                            // ✅ 优化：使用UniTask
                            while (!atlasHandle.IsDone)
                            {
                                // 图集加载进度占剩余30%的进度
                                onProgress?.Invoke(0.7f + atlasHandle.PercentComplete * 0.3f);
                                await UniTask.Yield();
                            }

                            if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
                            {
                                Debug.Log($"图集加载成功: {atlasAddress}");
                            }
                            else
                            {
                                Debug.LogError($"图集加载失败: {atlasAddress}");
                                AtlasReferenceCounts[atlasAddress]--;
                            }
                        }
                    }
                }
            }
            
            // 处理TextMeshPro组件（如果项目中有使用）
            #if TEXTMESHPRO_PRESENT
            var tmpTexts = prefab.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var tmpText in tmpTexts)
            {
                if (tmpText.sprite != null)
                {
                    var spriteName = tmpText.sprite.name;
                    // 尝试从SpriteToAtlasMap查找精灵所属的图集
                    if (SpriteToAtlasMap.ContainsKey(spriteName))
                    {
                        string atlasAddress = SpriteToAtlasMap[spriteName];
                        
                        // 增加图集引用计数
                        if (!AtlasReferenceCounts.ContainsKey(atlasAddress))
                        {
                            AtlasReferenceCounts[atlasAddress] = 0;
                        }
                        AtlasReferenceCounts[atlasAddress]++;
                        
                        // 如果图集尚未加载，则先加载图集
                        if (!AtlasHandles.ContainsKey(atlasAddress))
                        {
                            Debug.Log($"开始加载图集: {atlasAddress}");
                            
                            var atlasHandle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasAddress);
                            AtlasHandles[atlasAddress] = atlasHandle;
                            
                            // ✅ 优化：使用UniTask
                            while (!atlasHandle.IsDone)
                            {
                                // 图集加载进度占剩余30%的进度
                                onProgress?.Invoke(0.7f + atlasHandle.PercentComplete * 0.3f);
                                await UniTask.Yield();
                            }

                            if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
                            {
                                Debug.Log($"图集加载成功: {atlasAddress}");
                            }
                            else
                            {
                                Debug.LogError($"图集加载失败: {atlasAddress}");
                                AtlasReferenceCounts[atlasAddress]--;
                            }
                        }
                    }
                }
            }
            #endif
        }

        // 处理Prefab释放时的图集资源释放
        private void ProcessPrefabAtlasRelease(GameObject prefab, string assetName)
        {
            if (prefab == null) return;

            // 获取所有SpriteRenderer组件
            var spriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in spriteRenderers)
            {
                if (renderer.sprite != null)
                {
                    var spriteName = renderer.sprite.name;
                    // 尝试从SpriteToAtlasMap查找精灵所属的图集
                    if (SpriteToAtlasMap.ContainsKey(spriteName))
                    {
                        string atlasAddress = SpriteToAtlasMap[spriteName];
                        // 减少图集引用计数
                        if (AtlasReferenceCounts.ContainsKey(atlasAddress))
                        {
                            AtlasReferenceCounts[atlasAddress]--;
                            
                            // 如果引用计数为0，释放图集
                            if (AtlasReferenceCounts[atlasAddress] <= 0)
                            {
                                ReleaseAtlasInternal(atlasAddress);
                            }
                        }
                    }
                }
            }
            
            // 处理Image组件（UI精灵）
            var images = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var image in images)
            {
                if (image.sprite != null)
                {
                    var spriteName = image.sprite.name;
                    // 尝试从SpriteToAtlasMap查找精灵所属的图集
                    if (SpriteToAtlasMap.ContainsKey(spriteName))
                    {
                        string atlasAddress = SpriteToAtlasMap[spriteName];
                        // 减少图集引用计数
                        if (AtlasReferenceCounts.ContainsKey(atlasAddress))
                        {
                            AtlasReferenceCounts[atlasAddress]--;
                            
                            // 如果引用计数为0，释放图集
                            if (AtlasReferenceCounts[atlasAddress] <= 0)
                            {
                                ReleaseAtlasInternal(atlasAddress);
                            }
                        }
                    }
                }
            }
            
            // 处理TextMeshPro组件（如果项目中有使用）
            #if TEXTMESHPRO_PRESENT
            var tmpTexts = prefab.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var tmpText in tmpTexts)
            {
                if (tmpText.sprite != null)
                {
                    var spriteName = tmpText.sprite.name;
                    // 尝试从SpriteToAtlasMap查找精灵所属的图集
                    if (SpriteToAtlasMap.ContainsKey(spriteName))
                    {
                        string atlasAddress = SpriteToAtlasMap[spriteName];
                        // 减少图集引用计数
                        if (AtlasReferenceCounts.ContainsKey(atlasAddress))
                        {
                            AtlasReferenceCounts[atlasAddress]--;
                            
                            // 如果引用计数为0，释放图集
                            if (AtlasReferenceCounts[atlasAddress] <= 0)
                            {
                                ReleaseAtlasInternal(atlasAddress);
                            }
                        }
                    }
                }
            }
            #endif
        }

        // 实现新的AssetReference图集加载方法
        // ✅ 优化：使用UniTask异步加载
        public async void LoadSpriteFromAtlasAsync(string spriteName, AssetReferenceSpriteAtlas atlasReference, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null)
        {
            if (atlasReference == null || string.IsNullOrEmpty(spriteName))
            {
                Debug.LogError("图集引用或精灵名称不能为空");
                onCompleted?.Invoke(null);
                return;
            }

            string atlasGuid = atlasReference.AssetGUID;
            
            // 增加图集引用计数
            if (!AssetRefAtlasReferenceCounts.ContainsKey(atlasGuid))
            {
                AssetRefAtlasReferenceCounts[atlasGuid] = 0;
            }
            AssetRefAtlasReferenceCounts[atlasGuid]++;

            // 如果图集尚未加载，则先加载图集
            if (!AssetRefAtlasHandles.ContainsKey(atlasGuid))
            {
                Debug.Log($"开始加载图集(AssetReference): {atlasGuid}");
                
                var atlasHandle = atlasReference.LoadAssetAsync<SpriteAtlas>();
                AssetRefAtlasHandles[atlasGuid] = atlasHandle;
                
                // ✅ 优化：使用UniTask
                while (!atlasHandle.IsDone)
                {
                    onProgress?.Invoke(atlasHandle.PercentComplete * 0.5f); // 图集加载占总进度的50%
                    await UniTask.Yield();
                }

                if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"图集(AssetReference)加载成功: {atlasGuid}");
                    // 图集加载完成后，从图集中获取精灵
                    LoadSpriteFromAtlasInternal(spriteName, atlasHandle.Result, onCompleted, onProgress);
                }
                else
                {
                    Debug.LogError($"图集(AssetReference)加载失败: {atlasGuid}");
                    AssetRefAtlasReferenceCounts[atlasGuid]--;
                    if (AssetRefAtlasReferenceCounts[atlasGuid] <= 0)
                    {
                        AssetRefAtlasHandles.Remove(atlasGuid);
                    }
                    onCompleted?.Invoke(null);
                }
            }
            else
            {
                // 图集已加载，直接从已加载的图集中获取精灵
                var existingAtlasHandle = AssetRefAtlasHandles[atlasGuid];
                if (existingAtlasHandle.Result is SpriteAtlas existingAtlas)
                {
                    LoadSpriteFromAtlasInternal(spriteName, existingAtlas, onCompleted, onProgress);
                }
                else
                {
                    onCompleted?.Invoke(null);
                }
            }
        }

        private void LoadSpriteFromAtlasInternal(string spriteName, SpriteAtlas atlas, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null)
        {
            // 从图集中获取精灵
            UnityEngine.Sprite sprite = atlas.GetSprite(spriteName);
            
            if (sprite != null)
            {
                Debug.Log($"从图集中获取精灵成功: {spriteName}");
                onProgress?.Invoke(1f);
                onCompleted?.Invoke(sprite);
            }
            else
            {
                Debug.LogError($"在图集中找不到精灵: {spriteName}");
                
                // 尝试获取第一个可用精灵作为备用方案
                var sprites = new UnityEngine.Sprite[atlas.spriteCount];
                atlas.GetSprites(sprites);
                
                if (sprites.Length > 0 && sprites[0] != null)
                {
                    sprite = sprites[0];
                    Debug.LogWarning($"使用备用方案: 加载图集中的第一个精灵: {sprite.name}");
                    onProgress?.Invoke(1f);
                    onCompleted?.Invoke(sprite);
                }
                else
                {
                    onProgress?.Invoke(1f);
                    onCompleted?.Invoke(null);
                }
            }
        }

        // 释放AssetReference图集
        private void ReleaseAssetRefAtlasInternal(string atlasGuid)
        {
            if (AssetRefAtlasHandles.Remove(atlasGuid, out var atlasHandle))
            {
                Addressables.Release(atlasHandle);
                AssetRefAtlasReferenceCounts.Remove(atlasGuid);
                Debug.Log($"已释放图集(AssetReference): {atlasGuid}");
            }
        }
        
        public void AddAtlasReference(string atlasGuid)
        {
            if (!AssetRefAtlasReferenceCounts.ContainsKey(atlasGuid))
            {
                AssetRefAtlasReferenceCounts[atlasGuid] = 0;
            }
            AssetRefAtlasReferenceCounts[atlasGuid]++;
            
            Debug.Log($"增加图集引用: {atlasGuid}, 当前引用数: {AssetRefAtlasReferenceCounts[atlasGuid]}");
        }
        
        public void RemoveAtlasReference(string atlasGuid)
        {
            if (AssetRefAtlasReferenceCounts.ContainsKey(atlasGuid))
            {
                AssetRefAtlasReferenceCounts[atlasGuid]--;
                
                Debug.Log($"减少图集引用: {atlasGuid}, 当前引用数: {AssetRefAtlasReferenceCounts[atlasGuid]}");
                
                // 如果引用计数为0，释放图集
                if (AssetRefAtlasReferenceCounts[atlasGuid] <= 0)
                {
                    ReleaseAssetRefAtlasInternal(atlasGuid);
                }
            }
        }
    }
}