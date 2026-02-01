using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityEngine.UI;
using QFramework;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace BirdGame
{
    /// <summary>
    /// 使用AssetReference的图集管理器
    /// </summary>
    public class AssetReferenceAtlasManager : ViewControllerBase
    {
        [SerializeField] private AssetReferenceSpriteAtlas atlasReference = null; // AssetReference方式引用图集
        [SerializeField] private string spriteName = ""; // 精灵名称
        
        private IAssetSystem assetSystem;
        private Image imageComponent; // UI Image组件，用于显示加载的精灵
        private UnityEngine.Sprite loadedSprite; // 保存加载的精灵引用
        private SpriteAtlas loadedAtlas; // 保存加载的图集引用

        private void Awake()
        {
            // 获取AssetSystem实例
            assetSystem = this.GetSystem<IAssetSystem>();
            imageComponent = GetComponent<Image>();
        }

        /// <summary>
        /// 加载精灵（会自动加载其所在的图集）
        /// </summary>
        public void LoadSprite()
        {
            if (atlasReference == null || string.IsNullOrEmpty(spriteName))
            {
                Debug.LogError("图集引用或精灵名称不能为空");
                return;
            }

            // 先加载图集
            LoadAtlasThenSprite();
        }

        private async void LoadAtlasThenSprite()
        {
            // 使用AssetReference加载图集
            AsyncOperationHandle<SpriteAtlas> atlasHandle = atlasReference.LoadAssetAsync<SpriteAtlas>();
            
            while (!atlasHandle.IsDone)
            {
                Debug.Log($"图集加载进度: {(atlasHandle.PercentComplete * 100):F1}%");
                await System.Threading.Tasks.Task.Yield();
            }

            if (atlasHandle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedAtlas = atlasHandle.Result;
                
                // 从图集中获取精灵
                UnityEngine.Sprite sprite = loadedAtlas.GetSprite(spriteName);
                
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
                    var sprites = new UnityEngine.Sprite[loadedAtlas.spriteCount];
                    loadedAtlas.GetSprites(sprites);
                    
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
        /// 释放精灵和图集
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

            if (loadedAtlas != null && atlasReference != null)
            {
                // 释放图集资源
                Addressables.Release(atlasReference);
                loadedAtlas = null;
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

    /// <summary>
    /// AssetReference扩展类，支持SpriteAtlas类型
    /// </summary>
    [System.Serializable]
    public class AssetReferenceSpriteAtlas : AssetReferenceT<SpriteAtlas>
    {
        public AssetReferenceSpriteAtlas(string guid) : base(guid) { }
    }
}