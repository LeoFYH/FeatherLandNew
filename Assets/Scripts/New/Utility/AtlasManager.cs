using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityEngine.UI;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// 图集管理器，使用AssetReference方式加载图集
    /// </summary>
    public class UtilityAtlasManager : ViewControllerBase
    {
        // 通过Inspector设置图集和精灵引用
        [SerializeField] private AssetReferenceSpriteAtlas atlasReference = null; // AssetReference方式引用图集
        [SerializeField] private string spriteName = "";  // 精灵名称
        
        private IAssetSystem assetSystem;
        private Image imageComponent; // UI Image组件，用于显示加载的精灵
        private UnityEngine.Sprite loadedSprite; // 保存加载的精灵引用

        private void Awake()
        {
            // 获取AssetSystem实例
            assetSystem = this.GetSystem<IAssetSystem>();
            imageComponent = GetComponent<Image>();
            
            // 如果未设置AssetReference，尝试使用默认路径
            if (atlasReference == null)
            {
                Debug.LogWarning("Atlas Reference未设置，请在Inspector中设置图集引用");
            }
        }

        /// <summary>
        /// 加载精灵（会自动加载其所在的图集）
        /// </summary>
        public async void LoadSprite()
        {
            if (atlasReference == null || string.IsNullOrEmpty(spriteName))
            {
                Debug.LogError("图集引用或精灵名称不能为空");
                return;
            }

            // 使用AssetReference加载图集
            AsyncOperationHandle<SpriteAtlas> atlasHandle = atlasReference.LoadAssetAsync<SpriteAtlas>();
            
            while (!atlasHandle.IsDone)
            {
                Debug.Log($"图集加载进度: {(atlasHandle.PercentComplete * 100):F1}%");
                await System.Threading.Tasks.Task.Yield();
            }

            if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
            {
                SpriteAtlas atlas = atlasHandle.Result;
                
                // 从图集中获取精灵
                UnityEngine.Sprite sprite = atlas.GetSprite(spriteName);
                
                if (sprite != null)
                {
                    loadedSprite = sprite;
                    
                    // 将加载的精灵赋给Image组件
                    if (imageComponent != null)
                    {
                        imageComponent.sprite = sprite;
                    }
                    
                    Debug.Log($"成功加载精灵: {sprite.name} 来自图集: {atlasReference.AssetGUID}");
                }
                else
                {
                    Debug.LogError($"在图集 {atlasReference.AssetGUID} 中找不到精灵: {spriteName}");
                    
                    // 如果无法按名称获取精灵，尝试获取第一个可用精灵作为备用方案
                    var sprites = new UnityEngine.Sprite[atlas.spriteCount];
                    atlas.GetSprites(sprites);
                    
                    if (sprites.Length > 0 && sprites[0] != null)
                    {
                        sprite = sprites[0];
                        loadedSprite = sprite;
                        
                        if (imageComponent != null)
                        {
                            imageComponent.sprite = sprite;
                        }
                        
                        Debug.LogWarning($"使用备用方案: 加载图集中的第一个精灵: {sprite.name}");
                    }
                }
            }
            else
            {
                Debug.LogError($"图集加载失败: {atlasReference.AssetGUID}");
            }
        }

        /// <summary>
        /// 精灵加载完成回调
        /// </summary>
        private void OnSpriteLoaded(UnityEngine.Sprite sprite)
        {
            if (sprite != null)
            {
                loadedSprite = sprite;
                
                // 将加载的精灵赋给Image组件
                if (imageComponent != null)
                {
                    imageComponent.sprite = sprite;
                }
                
                Debug.Log($"成功加载精灵: {sprite.name} 来自图集: {atlasReference.AssetGUID}");
            }
            else
            {
                Debug.LogError($"加载精灵失败");
            }
        }

        /// <summary>
        /// 加载进度回调
        /// </summary>
        private void OnLoadingProgress(float progress)
        {
            Debug.Log($"加载进度: {(progress * 100):F1}%");
            // 这里可以更新UI进度条
        }

        /// <summary>
        /// 释放精灵（当图集不再被任何精灵引用时，会自动释放图集）
        /// </summary>
        public void ReleaseSprite()
        {
            if (loadedSprite != null)
            {
                // 从Image组件移除精灵
                if (imageComponent != null)
                {
                    imageComponent.sprite = null;
                }
                
                loadedSprite = null;
            }

            if (atlasReference != null)
            {
                // 释放图集资源
                Addressables.Release(atlasReference);
                Debug.Log($"已释放图集: {atlasReference.AssetGUID}");
            }
        }

        /// <summary>
        /// 作为示例，在OnDestroy时自动释放精灵资源
        /// </summary>
        private void OnDestroy()
        {
            ReleaseSprite();
        }

        #if UNITY_EDITOR
        [ContextMenu("Load Sprite Example")]
        private void LoadSpriteExample()
        {
            LoadSprite();
        }

        [ContextMenu("Release Sprite Example")]
        private void ReleaseSpriteExample()
        {
            ReleaseSprite();
        }
        #endif
    }
}