using System;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    public class GameConfigWindow : OdinMenuEditorWindow
    {
        [MenuItem("Tools/游戏配置")]
        private static void OpenWindow()
        {
            var window = GetWindow<GameConfigWindow>();
            window.titleContent = new GUIContent("游戏配置");
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
        }
        [Title("游戏全局配置", "游戏所有配置在此窗口进行配置", TitleAlignments.Centered)]
        [ReadOnly, LabelText("音乐列表配置文件")]
        public RadioConfig radioConfig;

        [ReadOnly, LabelText("商店配置文件")]
        public ShopConfig shopConfig;
        
        [ReadOnly, LabelText("鸟的配置文件")]
        public BirdConfig birdConfig;
        
        protected override OdinMenuTree BuildMenuTree()
        {
            radioConfig = Resources.FindObjectsOfTypeAll<RadioConfig>().FirstOrDefault();
            if (radioConfig == null)
            {
                var config = ScriptableObject.CreateInstance<RadioConfig>();
                AssetDatabase.CreateAsset(config, "Assets/Prefabs/Config/RadioConfig.asset");
                AssetDatabase.Refresh();
            }
            radioConfig = Resources.FindObjectsOfTypeAll<RadioConfig>().FirstOrDefault();

            shopConfig = Resources.FindObjectsOfTypeAll<ShopConfig>().FirstOrDefault();
            if (shopConfig == null)
            {
                var config = ScriptableObject.CreateInstance<ShopConfig>();
                AssetDatabase.CreateAsset(config, "Assets/Prefabs/Config/ShopConfig.asset");
                AssetDatabase.Refresh();
            }
            shopConfig = Resources.FindObjectsOfTypeAll<ShopConfig>().FirstOrDefault();

            birdConfig = Resources.FindObjectsOfTypeAll<BirdConfig>().FirstOrDefault();
            if (birdConfig == null)
            {
                var config = ScriptableObject.CreateInstance<BirdConfig>();
                AssetDatabase.CreateAsset(config, "Assets/Prefabs/Config/BirdConfig.asset");
                AssetDatabase.Refresh();
            }
            birdConfig = Resources.FindObjectsOfTypeAll<BirdConfig>().FirstOrDefault();

            OdinMenuTree tree = new OdinMenuTree(supportsMultiSelect: true)
            {
                {"首页", this, SdfIconType.House},
                {"音乐列表配置", radioConfig, SdfIconType.MusicNoteList },
                {"商店配置", shopConfig, SdfIconType.Shop},
                {"鸟的配置", birdConfig, SdfIconType.Egg}
            };
            
            return tree;
        }
    }
}