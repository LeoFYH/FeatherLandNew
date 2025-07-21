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

        private OdinMenuTree tree;
        
        [Title("游戏全局配置", "游戏所有配置在此窗口进行配置", TitleAlignments.Centered)]
        [Space]
        
        [ReadOnly, LabelText("音乐列表配置文件"), OnInspectorInit("OnRadioInit"), HorizontalGroup("音频配置")]
        public RadioConfig radioConfig;

        [ShowIf("@radioConfig==null"), Button("新建"), HorizontalGroup("音频配置")]
        private void OnCreateRadioConfig()
        {
            var config = ScriptableObject.CreateInstance<RadioConfig>();
            AssetDatabase.CreateAsset(config, "Assets/Prefabs/Config/RadioConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            OnRadioInit();
        }

        [ReadOnly, LabelText("商店配置文件"), OnInspectorInit("OnShopInit"), HorizontalGroup("商店配置")]
        public ShopConfig shopConfig;
        
        [ShowIf("@shopConfig==null"), Button("新建"), HorizontalGroup("商店配置")]
        private void OnCreateShopConfig()
        {
            var config = ScriptableObject.CreateInstance<ShopConfig>();
            AssetDatabase.CreateAsset(config, "Assets/Prefabs/Config/ShopConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            OnShopInit();
        }
        
        [ReadOnly, LabelText("鸟的配置文件"), OnInspectorInit("OnBirdInit"), HorizontalGroup("鸟配置")]
        public BirdConfig birdConfig;
        
        [ShowIf("@birdConfig==null"), Button("新建"), HorizontalGroup("鸟配置")]
        private void OnCreateBirdConfig()
        {
            var config = ScriptableObject.CreateInstance<BirdConfig>();
            AssetDatabase.CreateAsset(config, "Assets/Prefabs/Config/BirdConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            OnBirdInit();
        }
        
        protected override OdinMenuTree BuildMenuTree()
        {
            tree = new OdinMenuTree(supportsMultiSelect: true)
            {
                {"首页", this, SdfIconType.House},
                {"音乐列表配置", radioConfig, SdfIconType.MusicNoteList },
                {"商店配置", shopConfig, SdfIconType.Shop},
                {"鸟的配置", birdConfig, SdfIconType.Egg}
            };
            
            return tree;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AssetDatabase.SaveAssetIfDirty(radioConfig);
            AssetDatabase.SaveAssetIfDirty(shopConfig);
            AssetDatabase.SaveAssetIfDirty(birdConfig);
        }

        private void OnRadioInit()
        {
            radioConfig = AssetDatabase.LoadAssetAtPath<RadioConfig>("Assets/Prefabs/Config/RadioConfig.asset");
            tree.MenuItems[1].Value = radioConfig;
        }

        private void OnShopInit()
        {
            shopConfig = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Prefabs/Config/ShopConfig.asset");
            tree.MenuItems[2].Value = shopConfig;
        }

        private void OnBirdInit()
        {
            birdConfig = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            tree.MenuItems[3].Value = birdConfig;
        }
    }
}