using System;
using System.Collections.Generic;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 智能图集管理器，能够自动解析精灵所属的图集名称
    /// </summary>
    public class SmartAtlasManager : MonoBehaviour
    {
        private IAssetSystem assetSystem;
        
        // 存储精灵名称到图集名称的映射（可以根据项目实际结构进行调整）
        private Dictionary<string, string> spriteToAtlasMap = new Dictionary<string, string>();
        
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
        private string GetAtlasNameFromSpriteName(string spriteName)
        {
            // 如果已经有映射关系，直接返回
            if (spriteToAtlasMap.ContainsKey(spriteName))
            {
                return spriteToAtlasMap[spriteName];
            }

            // 尝试根据命名约定自动推断图集名称
            // 例如：player_idle_sprite -> player_atlas, enemy_goblin -> enemy_atlas
            string[] parts = spriteName.Split('_');
            if (parts.Length > 0)
            {
                // 简单推断：取第一个单词作为图集名
                string atlasName = parts[0] + "_atlas";
                
                // 也可以根据实际项目结构制定更复杂的推断规则
                // 例如：如果精灵名包含特定关键词，映射到特定图集
                
                return atlasName;
            }

            // 默认返回通用图集名
            return "GeneralAtlas";
        }

        /// <summary>
        /// 异步加载精灵（自动解析图集名称）
        /// </summary>
        public void LoadSpriteAsync(string spriteName, Action<UnityEngine.Sprite> onLoaded, Action<float> onProgress = null)
        {
            string atlasName = GetAtlasNameFromSpriteName(spriteName);

            // 记录精灵和图集的关系
            loadedSprites[spriteName] = atlasName;

            // 使用AssetSystem的图集加载功能
            assetSystem.LoadSpriteFromAtlasAsync(
                spriteName,
                atlasName,
                (sprite) =>
                {
                    if (sprite != null)
                    {
                        Debug.Log($"成功加载精灵: {spriteName} 来自图集: {atlasName}");
                    }
                    else
                    {
                        Debug.LogError($"加载精灵失败: {spriteName}");
                    }
                    
                    onLoaded?.Invoke(sprite);
                },
                onProgress
            );
        }

        /// <summary>
        /// 释放精灵（自动释放不再使用的图集）
        /// </summary>
        public void ReleaseSprite(string spriteName)
        {
            if (loadedSprites.TryGetValue(spriteName, out string atlasName))
            {
                // 释放精灵，如果图集不再被其他精灵使用，图集也会被自动释放
                assetSystem.ReleaseSpriteFromAtlas(spriteName, atlasName);
                
                // 从记录中移除
                loadedSprites.Remove(spriteName);
                
                Debug.Log($"已释放精灵: {spriteName} 来自图集: {atlasName}");
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
        /// 添加精灵到图集的映射关系
        /// </summary>
        public void AddSpriteToAtlasMapping(string spriteName, string atlasName)
        {
            spriteToAtlasMap[spriteName] = atlasName;
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