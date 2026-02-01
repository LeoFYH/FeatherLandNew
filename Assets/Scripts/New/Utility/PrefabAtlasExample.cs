using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using QFramework;

namespace BirdGame
{
    /// <summary>
    /// Prefab图集管理器的使用示例
    /// 展示如何在实际项目中使用PrefabAtlasManager
    /// </summary>
    public class PrefabAtlasExample : MonoBehaviour
    {
        [Header("示例配置")]
        [SerializeField] private AssetReferenceSpriteAtlas exampleAtlas = null; // 示例图集
        [SerializeField] private string[] exampleSpriteNames = new string[] { "character_idle", "character_walk", "ui_button" }; // 示例精灵名称

        private void Start()
        {
            // 这个脚本只是一个示例，实际使用时不需要在这里做任何事情
            // PrefabAtlasManager 会自动在Start时加载图集，并在OnDestroy时释放图集
        }
    }
}