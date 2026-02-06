using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// ✅ 优化：资源预加载系统
    /// 预加载常用资源，避免运行时首次加载卡顿
    /// </summary>
    public interface IAssetPreloadSystem : ISystem
    {
        /// <summary>
        /// 预加载常用资源
        /// </summary>
        UniTask PreloadCommonAssets();
        
        /// <summary>
        /// 检查资源是否已预加载
        /// </summary>
        bool IsAssetPreloaded(string assetName);
    }

    public class AssetPreloadSystem : AbstractSystem, IAssetPreloadSystem
    {
        private HashSet<string> preloadedAssets = new HashSet<string>();
        private IAssetSystem assetSystem;

        protected override void OnInit()
        {
            assetSystem = this.GetSystem<IAssetSystem>();
        }

        /// <summary>
        /// ✅ 预加载常用资源列表
        /// 这些资源会在游戏启动时预加载，避免运行时首次加载卡顿
        /// </summary>
        public async UniTask PreloadCommonAssets()
        {
            Debug.Log("🎮 开始预加载常用资源...");

            // 预加载UI常用资源
            await PreloadAsset<GameObject>("Heart");
            await PreloadAsset<GameObject>("Num");
            
            // 预加载音效（如果需要）
            // await PreloadAsset<AudioClip>("StrokeSound");
            // await PreloadAsset<AudioClip>("GrowUpSound");
            
            // 预加载常用图标
            // await PreloadAsset<Sprite>("FoodIcon");
            
            Debug.Log($"✅ 预加载完成！共加载 {preloadedAssets.Count} 个资源");
        }

        /// <summary>
        /// 预加载单个资源
        /// </summary>
        private async UniTask PreloadAsset<T>(string assetName)
        {
            try
            {
                // 使用UniTask包装回调
                var tcs = new UniTaskCompletionSource<T>();
                
                assetSystem.LoadAssetAsync<T>(assetName, result =>
                {
                    if (result != null)
                    {
                        preloadedAssets.Add(assetName);
                        Debug.Log($"  ✓ 预加载: {assetName}");
                        tcs.TrySetResult(result);
                    }
                    else
                    {
                        Debug.LogWarning($"  ✗ 预加载失败: {assetName}");
                        tcs.TrySetResult(default(T));
                    }
                });

                await tcs.Task;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"预加载资源失败 {assetName}: {e.Message}");
            }
        }

        /// <summary>
        /// 检查资源是否已预加载
        /// </summary>
        public bool IsAssetPreloaded(string assetName)
        {
            return preloadedAssets.Contains(assetName);
        }
    }
}
