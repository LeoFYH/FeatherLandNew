using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

namespace BirdGame
{
    public class AtlasManager : MonoBehaviour
    {
        private IAssetSystem _assetSystem;

        private static AtlasManager _instance;
        public static AtlasManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化AssetSystem
            _assetSystem = GameApp.Interface.GetSystem<IAssetSystem>();
        }

        /// <summary>
        /// 异步加载图集中的精灵，自动管理图集的加载和释放
        /// </summary>
        /// <param name="spriteAddress">精灵地址</param>
        /// <param name="atlasAddress">图集地址</param>
        /// <param name="onCompleted">加载完成回调</param>
        /// <param name="onProgress">加载进度回调</param>
        public void LoadSpriteFromAtlasAsync(string spriteAddress, string atlasAddress, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null)
        {
            _assetSystem.LoadSpriteFromAtlasAsync(spriteAddress, atlasAddress, onCompleted, onProgress);
        }

        /// <summary>
        /// 使用AssetReference异步加载图集中的精灵，自动管理图集的加载和释放
        /// </summary>
        /// <param name="spriteName">精灵名称</param>
        /// <param name="atlasReference">图集AssetReference</param>
        /// <param name="onCompleted">加载完成回调</param>
        /// <param name="onProgress">加载进度回调</param>
        public void LoadSpriteFromAtlasAsync(string spriteName, AssetReferenceSpriteAtlas atlasReference, Action<UnityEngine.Sprite> onCompleted, Action<float> onProgress = null)
        {
            _assetSystem.LoadSpriteFromAtlasAsync(spriteName, atlasReference, onCompleted, onProgress);
        }

        /// <summary>
        /// 释放图集中的精灵，当图集不再被引用时自动释放图集
        /// </summary>
        /// <param name="spriteAddress">精灵地址</param>
        /// <param name="atlasAddress">图集地址</param>
        public void ReleaseSpriteFromAtlas(string spriteAddress, string atlasAddress)
        {
            _assetSystem.ReleaseSpriteFromAtlas(spriteAddress, atlasAddress);
        }

        // 保留原有的手动获取图集方法，以备不时之需
        public SpriteAtlas GetAtlas(string atlasName)
        {
            // 如果需要手动获取已加载的图集，可以调用AssetSystem的相关方法
            // 由于当前AssetSystem内部没有提供直接获取图集的方法，这里留作扩展
            return null;
        }
    }
}