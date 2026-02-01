using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using UnityEngine.UI;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// 精灵图集应用器，用于将加载的精灵应用到UI或渲染器组件
    /// </summary>
    public class SpriteAtlasApplier : ViewControllerBase
    {
        [Header("精灵配置")]
        [SerializeField] private string spriteName = "";
        [SerializeField] private AssetReferenceSpriteAtlas atlasReference = null;
        
        [Header("目标组件")]
        [SerializeField] private Image targetImage = null;
        [SerializeField] private SpriteRenderer targetSpriteRenderer = null;
        
        private IAssetSystem assetSystem;

        private void Awake()
        {
            assetSystem = this.GetSystem<IAssetSystem>();
        }

        private void Start()
        {
            LoadAndApplySprite();
        }

        /// <summary>
        /// 加载精灵并应用到目标组件
        /// </summary>
        public void LoadAndApplySprite()
        {
            if (string.IsNullOrEmpty(spriteName) || atlasReference == null)
            {
                Debug.LogError("精灵名称或图集引用不能为空", gameObject);
                return;
            }

            // 增加图集引用计数
            if (!string.IsNullOrEmpty(atlasReference.AssetGUID))
            {
                assetSystem.AddAtlasReference(atlasReference.AssetGUID);
            }

            // 加载精灵
            assetSystem.LoadSpriteFromAtlasAsync(spriteName, atlasReference, (sprite) =>
            {
                if (sprite != null)
                {
                    ApplySprite(sprite);
                    Debug.Log($"精灵 {spriteName} 已成功应用到组件");
                }
                else
                {
                    Debug.LogError($"精灵 {spriteName} 加载失败");
                }
            });
        }

        /// <summary>
        /// 将精灵应用到目标组件
        /// </summary>
        private void ApplySprite(UnityEngine.Sprite sprite)
        {
            if (targetImage != null)
            {
                targetImage.sprite = sprite;
            }
            
            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.sprite = sprite;
            }
        }

        /// <summary>
        /// 释放精灵和图集引用
        /// </summary>
        public void ReleaseSprite()
        {
            if (targetImage != null)
            {
                targetImage.sprite = null;
            }
            
            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.sprite = null;
            }

            // 减少图集引用计数
            if (!string.IsNullOrEmpty(atlasReference.AssetGUID))
            {
                assetSystem.RemoveAtlasReference(atlasReference.AssetGUID);
            }
        }

        private void OnDestroy()
        {
            ReleaseSprite();
        }
    }
}