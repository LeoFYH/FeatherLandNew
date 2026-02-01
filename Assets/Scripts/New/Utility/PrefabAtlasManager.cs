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
    /// Prefab图集管理器，用于自动管理Prefab关联的图集资源
    /// 在Prefab加载时自动加载关联图集，在Prefab销毁时自动释放图集
    /// </summary>
    public class PrefabAtlasManager : ViewControllerBase
    {
        [Header("图集引用配置")]
        [SerializeField] private AssetReferenceSpriteAtlas atlasReference = null; // 关联的图集AssetReference
        [SerializeField] private string[] spriteNames = new string[0]; // 需要使用的精灵名称数组

        private IAssetSystem assetSystem;
        private bool isAtlasLoaded = false;

        private void Awake()
        {
            // 获取AssetSystem实例
            assetSystem = this.GetSystem<IAssetSystem>();
        }

        private void Start()
        {
            // 在Start中加载图集，确保其他组件已初始化
            LoadAtlas();
        }

        /// <summary>
        /// 加载关联的图集
        /// </summary>
        public void LoadAtlas()
        {
            if (atlasReference == null || isAtlasLoaded || string.IsNullOrEmpty(atlasReference.AssetGUID))
            {
                return;
            }

            // 使用AssetSystem增加图集引用计数
            assetSystem.AddAtlasReference(atlasReference.AssetGUID);
            
            // 如果有精灵名称，加载所有精灵
            if (spriteNames.Length > 0)
            {
                foreach (string spriteName in spriteNames)
                {
                    if (!string.IsNullOrEmpty(spriteName))
                    {
                        // 加载精灵，这会确保图集被加载
                        assetSystem.LoadSpriteFromAtlasAsync(spriteName, atlasReference, 
                            (sprite) => {
                                if (sprite != null)
                                {
                                    Debug.Log($"精灵 {spriteName} 加载成功，来自图集: {atlasReference.AssetGUID}");
                                    
                                    // 尝试将精灵应用到当前对象上的Image或SpriteRenderer组件
                                    ApplySpriteToComponents(sprite);
                                }
                                else
                                {
                                    Debug.LogError($"精灵 {spriteName} 加载失败，来自图集: {atlasReference.AssetGUID}");
                                }
                            });
                    }
                }
            }
            else
            {
                // 如果没有指定精灵名称，只是确保图集引用计数增加
                Debug.Log($"Prefab图集引用已增加: {atlasReference.AssetGUID}");
            }

            isAtlasLoaded = true;
            OnAtlasLoaded();
        }

        /// <summary>
        /// 将加载的精灵应用到组件上
        /// </summary>
        private void ApplySpriteToComponents(UnityEngine.Sprite sprite)
        {
            // 检查当前GameObject是否有Image组件
            Image image = GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                return;
            }

            // 检查当前GameObject是否有SpriteRenderer组件
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                return;
            }

            // 如果当前对象没有图像组件，可以考虑遍历子对象
            // 但这通常意味着用户应该使用更具体的组件来处理精灵分配
        }

        /// <summary>
        /// 图集加载完成后的回调
        /// </summary>
        protected virtual void OnAtlasLoaded()
        {
            // 子类可以重写此方法来执行特定的初始化逻辑
        }

        /// <summary>
        /// 释放关联的图集
        /// </summary>
        public void ReleaseAtlas()
        {
            if (atlasReference == null || !isAtlasLoaded || string.IsNullOrEmpty(atlasReference.AssetGUID))
            {
                return;
            }

            // 使用AssetSystem减少图集引用计数，当引用计数为0时自动释放图集
            assetSystem.RemoveAtlasReference(atlasReference.AssetGUID);

            isAtlasLoaded = false;
            Debug.Log($"Prefab图集引用已减少: {atlasReference.AssetGUID}");
        }

        private void OnDestroy()
        {
            // 在对象销毁时自动释放图集
            ReleaseAtlas();
        }

        #if UNITY_EDITOR
        [ContextMenu("Load Atlas")]
        private void ContextMenuLoadAtlas()
        {
            LoadAtlas();
        }

        [ContextMenu("Release Atlas")]
        private void ContextMenuReleaseAtlas()
        {
            ReleaseAtlas();
        }
        #endif
    }
}