using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// AssetReference图集加载示例
    /// </summary>
    public class AssetReferenceAtlasExample : MonoBehaviour
    {
        [Header("AssetReference方式加载图集")]
        [SerializeField] private AssetReferenceSpriteAtlas atlasReference = null; // AssetReference方式引用图集
        [SerializeField] private string spriteName = "DefaultSprite"; // 精灵名称
        
        [Header("UI组件")]
        [SerializeField] private Image targetImage = null; // 目标Image组件
        
        private IAssetSystem assetSystem;
        private AtlasManager atlasManager;
        private UnityEngine.Sprite loadedSprite;

        private void Start()
        {
            // 获取系统引用
            assetSystem = GameApp.Interface.GetSystem<IAssetSystem>();
            atlasManager = AtlasManager.Instance;
            
            if (atlasManager == null)
            {
                Debug.LogError("AtlasManager未初始化！");
                return;
            }
        }

        /// <summary>
        /// 使用AssetReference加载精灵
        /// </summary>
        public void LoadSpriteWithAssetReference()
        {
            if (atlasReference == null || string.IsNullOrEmpty(spriteName))
            {
                Debug.LogError("图集引用或精灵名称不能为空");
                return;
            }

            if (atlasManager == null)
            {
                Debug.LogError("AtlasManager未初始化！");
                return;
            }

            // 使用AtlasManager的AssetReference方法加载精灵
            atlasManager.LoadSpriteFromAtlasAsync(spriteName, atlasReference, OnSpriteLoaded, OnLoadingProgress);
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
                if (targetImage != null)
                {
                    targetImage.sprite = sprite;
                }
                
                Debug.Log($"成功加载精灵: {sprite.name} 来自图集AssetReference");
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
            Debug.Log($"AssetReference加载进度: {(progress * 100):F1}%");
            // 这里可以更新UI进度条
        }

        /// <summary>
        /// 释放精灵资源
        /// </summary>
        public void ReleaseSprite()
        {
            if (loadedSprite != null && atlasReference != null)
            {
                // 注意：对于AssetReference方式，我们需要通过AssetSystem来释放
                
                // 从Image组件移除精灵
                if (targetImage != null)
                {
                    targetImage.sprite = null;
                }
                
                loadedSprite = null;
                Debug.Log("已释放精灵资源");
            }
        }

        #if UNITY_EDITOR
        [ContextMenu("Load Sprite With AssetReference")]
        private void ContextMenu_LoadSprite()
        {
            LoadSpriteWithAssetReference();
        }

        [ContextMenu("Release Sprite")]
        private void ContextMenu_ReleaseSprite()
        {
            ReleaseSprite();
        }
        #endif
    }
}