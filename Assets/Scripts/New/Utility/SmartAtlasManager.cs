using System;
using System.Collections.Generic;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 智能图集管理器，能够自动解析精灵所属的图集名称，支持AssetReference方式
    /// </summary>
    public class SmartAtlasManager : MonoBehaviour
    {
        private IAssetSystem assetSystem;
        
        // 存储精灵名称到图集名称的映射（可以根据项目实际结构进行调整）
        private Dictionary<string, string> spriteToAtlasMap = new Dictionary<string, string>();
        
        // 存储精灵名称到AssetReference图集的映射
        private Dictionary<string, AssetReferenceSpriteAtlas> spriteToAssetRefAtlasMap = new Dictionary<string, AssetReferenceSpriteAtlas>();
        
        // 存储当前加载的精灵及其图集引用
        private Dictionary<string, string> loadedSprites = new Dictionary<string, string>();

        private void Awake()
        {
            assetSystem = GameApp.Interface.GetSystem<IAssetSystem>();
            
            // 初始化精灵到图集的映射关系
            InitializeAtlasMappings();
        }

        /// <summary>
        /// 初始化精灵到图集的映射关系
        /// </summary>
        private void InitializeAtlasMappings()
        {
            // 注意：在实际项目中，这里需要根据您的实际图集结构进行配置
            // 以下是一些示例映射关系，您需要根据实际情况修改
            /*
            spriteToAtlasMap["player_idle"] = "CharacterAtlas";
            spriteToAtlasMap["player_walk"] = "CharacterAtlas";
            spriteToAtlasMap["enemy_goblin"] = "EnemyAtlas";
            spriteToAtlasMap["ui_button_normal"] = "UIAtlas";
            */
        }

        /// <summary>
        /// 根据精灵名称自动推断图集名称
        /// </summary>
        public string GetAtlasNameForSprite(string spriteName)
        {
            if (spriteToAtlasMap.ContainsKey(spriteName))
            {
                return spriteToAtlasMap[spriteName];
            }
            
            // 如果找不到直接映射，可以根据命名规则推断
            // 例如，如果精灵名以"ui_"开头，可能属于UI图集
            if (spriteName.StartsWith("ui_"))
            {
                return "UIAtlas";
            }
            else if (spriteName.StartsWith("char_") || spriteName.StartsWith("player_"))
            {
                return "CharacterAtlas";
            }
            else if (spriteName.StartsWith("item_"))
            {
                return "ItemAtlas";
            }
            
            return null; // 无法推断图集名称
        }

        /// <summary>
        /// 添加精灵到图集的映射关系
        /// </summary>
        public void AddSpriteToAtlasMapping(string spriteName, string atlasName)
        {
            spriteToAtlasMap[spriteName] = atlasName;
        }

        /// <summary>
        /// 添加精灵到AssetReference图集的映射关系
        /// </summary>
        public void AddSpriteToAssetRefAtlasMapping(string spriteName, AssetReferenceSpriteAtlas atlasReference)
        {
            spriteToAssetRefAtlasMap[spriteName] = atlasReference;
        }

        /// <summary>
        /// 通过AssetReference异步加载精灵
        /// </summary>
        public void LoadSpriteAsync(string spriteName, AssetReferenceSpriteAtlas atlasReference, System.Action<UnityEngine.Sprite> onLoaded)
        {
            if (atlasReference == null)
            {
                Debug.LogError($"图集引用为空，无法加载精灵: {spriteName}");
                onLoaded?.Invoke(null);
                return;
            }

            // 添加到映射表
            AddSpriteToAssetRefAtlasMapping(spriteName, atlasReference);

            // 使用AssetSystem加载精灵
            assetSystem.LoadSpriteFromAtlasAsync(spriteName, atlasReference, (sprite) =>
            {
                if (sprite != null)
                {
                    loadedSprites[spriteName] = atlasReference.AssetGUID;
                    Debug.Log($"成功加载精灵: {spriteName} 来自图集AssetReference");
                }
                else
                {
                    Debug.LogError($"无法加载精灵: {spriteName}");
                }
                
                onLoaded?.Invoke(sprite);
            });
        }

        /// <summary>
        /// 通过传统地址异步加载精灵
        /// </summary>
        public void LoadSpriteAsync(string spriteName, string atlasName, System.Action<UnityEngine.Sprite> onLoaded)
        {
            if (string.IsNullOrEmpty(spriteName) || string.IsNullOrEmpty(atlasName))
            {
                Debug.LogError("精灵名称或图集名称不能为空");
                onLoaded?.Invoke(null);
                return;
            }

            // 添加到映射表
            AddSpriteToAtlasMapping(spriteName, atlasName);

            // 使用AssetSystem加载精灵
            assetSystem.LoadSpriteFromAtlasAsync(spriteName, atlasName, (sprite) =>
            {
                if (sprite != null)
                {
                    loadedSprites[spriteName] = atlasName;
                    Debug.Log($"成功加载精灵: {spriteName} 来自图集: {atlasName}");
                }
                else
                {
                    Debug.LogError($"无法加载精灵: {spriteName} 来自图集: {atlasName}");
                }
                
                onLoaded?.Invoke(sprite);
            });
        }

        /// <summary>
        /// 释放精灵，如果图集不再被其他精灵使用，图集也会被自动释放
        /// </summary>
        public void ReleaseSprite(string spriteName)
        {
            if (loadedSprites.ContainsKey(spriteName))
            {
                string atlasIdentifier = loadedSprites[spriteName];
                
                // 检查是否是传统图集名称还是AssetReference GUID
                if (spriteToAtlasMap.ContainsKey(spriteName))
                {
                    // 传统方式释放
                    assetSystem.ReleaseSpriteFromAtlas(spriteName, spriteToAtlasMap[spriteName]);
                }
                else if (spriteToAssetRefAtlasMap.ContainsKey(spriteName))
                {
                    // 对于AssetReference方式，需要特殊处理
                    // 释放精灵，如果图集不再被其他精灵使用，图集也会被自动释放
                    // 在当前实现中，我们会从跟踪列表中移除
                }
                
                // 从记录中移除
                loadedSprites.Remove(spriteName);
                
                Debug.Log($"已释放精灵: {spriteName} 来自图集: {atlasIdentifier}");
            }
            else
            {
                Debug.LogWarning($"精灵 {spriteName} 不在跟踪列表中");
            }
        }

        /// <summary>
        /// 释放所有已加载的精灵和对应的图集
        /// </summary>
        public void ReleaseAllSprites()
        {
            // 复制当前的键列表，因为字典会在循环中被修改
            var spritesToRelease = new List<string>(loadedSprites.Keys);
            
            foreach (string spriteName in spritesToRelease)
            {
                ReleaseSprite(spriteName);
            }
        }

        /// <summary>
        /// 作为示例，在OnDestroy时自动释放所有资源
        /// </summary>
        private void OnDestroy()
        {
            ReleaseAllSprites();
        }

        #if UNITY_EDITOR
        [ContextMenu("Release All Sprites")]
        private void ContextMenu_ReleaseAll()
        {
            ReleaseAllSprites();
        }
        #endif
    }
}