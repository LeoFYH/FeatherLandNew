using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // 添加这一行以支持LINQ方法如Take
using System.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

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

        protected override void OnInit()
        {
            LoadAssetAsync<GameObject>("OpenEggAnim", null);
        }

        public async void LoadAssetAsync<T>(string assetName, Action<T> onCompleted, Action<float> onProgress = null)
        {
            if (HandleDic.ContainsKey(assetName))
            {
                onCompleted?.Invoke((T)HandleDic[assetName].Result);
                return;
            }

            var handle = Addressables.LoadAssetAsync<T>(assetName);
            HandleDic.Add(assetName, handle);
            
            // 检查是否是精灵资源，如果是则需要处理图集
            if (typeof(T) == typeof(Sprite))
            {
                // 为精灵资源自动处理图集加载
                await LoadAtlasForSpriteIfNeeded(assetName, handle, onCompleted, onProgress);
            }
            // 检查是否是GameObject类型的资源（如Prefab），需要检查其中是否包含图集精灵
            else if (typeof(T) == typeof(GameObject))
            {
                // 普通资源加载流程
                while (!handle.IsDone)
                {
                    onProgress?.Invoke(handle.PercentComplete * 0.7f); // Prefab加载占70%的进度
                    await Task.Yield();
                }

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var prefab = handle.Result as GameObject;
                    if (prefab != null)
                    {
                        // 加载完成后，检查Prefab中是否有使用图集的精灵渲染器
                        await ProcessPrefabAtlases(prefab, assetName, onProgress);
                        
                        onProgress?.Invoke(1f);
                        onCompleted?.Invoke(handle.Result);
                    }
                    else
                    {
                        onProgress?.Invoke(1f);
                        onCompleted?.Invoke(handle.Result);
                    }
                }
                else
                {
                    Debug.LogError($"资源加载失败: {assetName}");
                    onProgress?.Invoke(1f);
                    onCompleted?.Invoke(default(T)); // 传递默认值，让UI层处理
                }
            }
            else
            {
                // 普通资源加载流程
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
        }

        // 为精灵资源自动处理图集加载
        private async Task LoadAtlasForSpriteIfNeeded<T>(string assetName, AsyncOperationHandle handle, Action<T> onCompleted, Action<float> onProgress)
        {
            // 先获取精灵资源本身
            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await Task.Yield();
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
                        ReleaseAtlasByAddress(atlasAddress, assetName);
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
        private void ReleaseAtlasByAddress(string atlasAddress, string spriteAddress)
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
                
                // 监听图集加载进度
                while (!atlasHandle.IsDone)
                {
                    onProgress?.Invoke(atlasHandle.PercentComplete * 0.5f); // 图集加载占总进度的50%
                    await Task.Yield();
                }

                if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"图集加载成功: {atlasAddress}");
                    // 注册图集到Unity的图集管理系统 - 使用正确的方法替代已废弃或不存在的方法
                    
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

        private async void LoadSpriteInternal(string spriteAddress, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null)
        {
            // 检查精灵是否已经在加载
            if (SpriteHandles.ContainsKey(spriteAddress))
            {
                var existingHandle = SpriteHandles[spriteAddress];
                if (existingHandle.IsValid())
                {
                    // 等待现有加载完成
                    while (!existingHandle.IsDone)
                    {
                        await Task.Yield();
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

            while (!spriteHandle.IsDone)
            {
                // 精灵加载占总进度的后50%
                onProgress?.Invoke(0.5f + spriteHandle.PercentComplete * 0.5f);
                await Task.Yield();
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

        // 处理Prefab中引用的图集资源
        private async Task ProcessPrefabAtlases(GameObject prefab, string assetName, Action<float> onProgress = null)
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
                            
                            // 监听图集加载进度
                            while (!atlasHandle.IsDone)
                            {
                                // 图集加载进度占剩余30%的进度
                                onProgress?.Invoke(0.7f + atlasHandle.PercentComplete * 0.3f);
                                await Task.Yield();
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
                            
                            // 监听图集加载进度
                            while (!atlasHandle.IsDone)
                            {
                                // 图集加载进度占剩余30%的进度
                                onProgress?.Invoke(0.7f + atlasHandle.PercentComplete * 0.3f);
                                await Task.Yield();
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
                            
                            // 监听图集加载进度
                            while (!atlasHandle.IsDone)
                            {
                                // 图集加载进度占剩余30%的进度
                                onProgress?.Invoke(0.7f + atlasHandle.PercentComplete * 0.3f);
                                await Task.Yield();
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
    }
}