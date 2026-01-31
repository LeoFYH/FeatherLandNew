using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// 图集管理器，用于展示如何使用AssetSystem的图集智能加载功能
    /// </summary>
    public class UtilityAtlasManager : ViewControllerBase
    {
        // 通过Inspector设置图集地址和精灵地址
        [SerializeField] private string atlasAddress = "CharacterAtlas"; // 示例图集地址
        [SerializeField] private string spriteAddress = "PlayerSprite";  // 示例精灵地址
        
        private IAssetSystem assetSystem;
        private Image imageComponent; // UI Image组件，用于显示加载的精灵
        private UnityEngine.Sprite loadedSprite; // 保存加载的精灵引用

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
            // 使用AssetSystem的图集加载功能
            assetSystem.LoadSpriteFromAtlasAsync(
                spriteAddress,           // 精灵地址
                atlasAddress,            // 图集地址
                OnSpriteLoaded,          // 加载完成回调
                OnLoadingProgress        // 加载进度回调
            );
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
                
                Debug.Log($"成功加载精灵: {sprite.name} 来自图集: {atlasAddress}");
            }
            else
            {
                Debug.LogError($"加载精灵失败: {spriteAddress}");
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
                // 释放精灵，如果图集不再被其他精灵引用，图集也会被自动释放
                assetSystem.ReleaseSpriteFromAtlas(spriteAddress, atlasAddress);
                
                // 从Image组件移除精灵
                if (imageComponent != null)
                {
                    imageComponent.sprite = null;
                }
                
                loadedSprite = null;
                Debug.Log($"已释放精灵: {spriteAddress} 来自图集: {atlasAddress}");
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