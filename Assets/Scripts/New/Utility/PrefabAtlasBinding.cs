using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityEngine.UI;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// Prefab图集绑定组件
    /// 自动管理Prefab与其关联图集的生命周期
    /// </summary>
    [RequireComponent(typeof(PrefabAtlasManager))]
    public class PrefabAtlasBinding : ViewControllerBase
    {
        [Header("图集绑定配置")]
        [SerializeField] private AssetReferenceSpriteAtlas atlasReference = null; // 关联的图集AssetReference
        [SerializeField] private List<SpriteBindingInfo> spriteBindings = new List<SpriteBindingInfo>(); // 精灵绑定信息

        private IAssetSystem assetSystem;
        private bool isInitialized = false;
        private Dictionary<string, int> spriteReferenceCounts = new Dictionary<string, int>();

        private void Awake()
        {
            assetSystem = this.GetSystem<IAssetSystem>();
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                InitializeAtlasBinding();
                isInitialized = true;
            }
            else
            {
                // 重新启用时加载图集
                LoadAtlas();
            }
        }

        /// <summary>
        /// 初始化图集绑定
        /// </summary>
        private void InitializeAtlasBinding()
        {
            if (atlasReference == null)
            {
                Debug.LogWarning($"PrefabAtlasBinding on {gameObject.name} has no atlas reference set.");
                return;
            }

            // 为每个精灵初始化引用计数
            foreach (var binding in spriteBindings)
            {
                if (!string.IsNullOrEmpty(binding.spriteName))
                {
                    spriteReferenceCounts[binding.spriteName] = 0;
                }
            }

            // 加载图集
            LoadAtlas();
        }

        /// <summary>
        /// 加载图集
        /// </summary>
        private void LoadAtlas()
        {
            if (atlasReference == null || spriteBindings.Count == 0)
            {
                return;
            }

            // 增加图集引用计数
            if (!string.IsNullOrEmpty(atlasReference.AssetGUID))
            {
                assetSystem.AddAtlasReference(atlasReference.AssetGUID);
            }

            // 对于每个精灵名称，增加引用计数并加载精灵
            foreach (var binding in spriteBindings)
            {
                if (!string.IsNullOrEmpty(binding.spriteName))
                {
                    // 增加引用计数
                    if (spriteReferenceCounts.ContainsKey(binding.spriteName))
                    {
                        spriteReferenceCounts[binding.spriteName]++;
                    }
                    else
                    {
                        spriteReferenceCounts[binding.spriteName] = 1;
                    }

                    // 使用AssetSystem加载精灵，这会自动加载图集
                    assetSystem.LoadSpriteFromAtlasAsync(binding.spriteName, atlasReference, 
                        (sprite) => {
                            if (sprite != null)
                            {
                                Debug.Log($"精灵 {binding.spriteName} 加载成功，来自图集: {atlasReference.AssetGUID}");
                                
                                // 将精灵应用到目标组件
                                ApplySpriteToTarget(binding, sprite);
                                
                                // 在这里可以执行精灵加载后的操作
                                OnSpriteLoaded(binding.spriteName, sprite);
                            }
                            else
                            {
                                Debug.LogError($"精灵 {binding.spriteName} 加载失败，来自图集: {atlasReference.AssetGUID}");
                            }
                        });
                }
            }
        }

        /// <summary>
        /// 将精灵应用到目标组件
        /// </summary>
        private void ApplySpriteToTarget(SpriteBindingInfo binding, UnityEngine.Sprite sprite)
        {
            if (binding.targetImage != null)
            {
                binding.targetImage.sprite = sprite;
            }
            else if (binding.targetSpriteRenderer != null)
            {
                binding.targetSpriteRenderer.sprite = sprite;
            }
            else if (!string.IsNullOrEmpty(binding.targetComponentPath))
            {
                // 通过路径查找组件
                Transform targetTransform = transform.Find(binding.targetComponentPath);
                if (targetTransform != null)
                {
                    var image = targetTransform.GetComponent<Image>();
                    if (image != null)
                    {
                        image.sprite = sprite;
                    }
                    else
                    {
                        var spriteRenderer = targetTransform.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null)
                        {
                            spriteRenderer.sprite = sprite;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 精灵加载完成后的回调
        /// </summary>
        protected virtual void OnSpriteLoaded(string spriteName, UnityEngine.Sprite sprite)
        {
            // 子类可以重写此方法来处理精灵加载完成后的逻辑
        }

        /// <summary>
        /// 获取精灵引用计数
        /// </summary>
        public int GetSpriteReferenceCount(string spriteName)
        {
            if (spriteReferenceCounts.ContainsKey(spriteName))
            {
                return spriteReferenceCounts[spriteName];
            }
            return 0;
        }

        /// <summary>
        /// 手动增加精灵引用计数
        /// </summary>
        public void AddSpriteReference(string spriteName)
        {
            if (spriteReferenceCounts.ContainsKey(spriteName))
            {
                spriteReferenceCounts[spriteName]++;
            }
            else
            {
                spriteReferenceCounts[spriteName] = 1;
            }

            // 如果这是第一次引用，加载精灵
            if (spriteReferenceCounts[spriteName] == 1)
            {
                assetSystem.LoadSpriteFromAtlasAsync(spriteName, atlasReference, 
                    (sprite) => {
                        if (sprite != null)
                        {
                            OnSpriteLoaded(spriteName, sprite);
                        }
                    });
            }
        }

        /// <summary>
        /// 手动减少精灵引用计数
        /// </summary>
        public void RemoveSpriteReference(string spriteName)
        {
            if (spriteReferenceCounts.ContainsKey(spriteName))
            {
                spriteReferenceCounts[spriteName]--;
                
                // 如果引用计数降到0，释放精灵
                if (spriteReferenceCounts[spriteName] <= 0)
                {
                    spriteReferenceCounts[spriteName] = 0;
                }
            }
        }

        /// <summary>
        /// 完全释放图集（通常在对象销毁时调用）
        /// </summary>
        private void ReleaseAtlas()
        {
            if (atlasReference == null)
            {
                return;
            }

            // 释放图集引用
            if (!string.IsNullOrEmpty(atlasReference.AssetGUID))
            {
                assetSystem.RemoveAtlasReference(atlasReference.AssetGUID);
            }

            Debug.Log($"图集已完全释放: {atlasReference.AssetGUID}");
        }

        private void OnDestroy()
        {
            // 在对象销毁时释放图集
            ReleaseAtlas();
        }

        #if UNITY_EDITOR
        [ContextMenu("Force Reload Atlas")]
        private void ContextMenuReloadAtlas()
        {
            LoadAtlas();
        }

        [ContextMenu("Log Sprite References")]
        private void ContextMenuLogSpriteReferences()
        {
            foreach (var kvp in spriteReferenceCounts)
            {
                Debug.Log($"Sprite: {kvp.Key}, References: {kvp.Value}");
            }
        }
        #endif
    }

    /// <summary>
    /// 精灵绑定信息
    /// </summary>
    [Serializable]
    public class SpriteBindingInfo
    {
        public string spriteName = "";                 // 精灵名称
        public Image targetImage = null;               // 目标UI Image组件
        public SpriteRenderer targetSpriteRenderer = null; // 目标Sprite Renderer组件
        public string targetComponentPath = "";        // 目标组件路径（相对于当前GameObject）
    }
}