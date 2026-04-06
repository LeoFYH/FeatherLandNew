#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BirdGame.Editor
{
    public static class SwapSceneIndexTool
    {
        [MenuItem("Tools/一次性/交换 tundra(5) 和 Wetland(6)")]
        public static void SwapTundraAndWetland()
        {
            if (!EditorUtility.DisplayDialog("确认交换",
                "将交换 tundra(索引5) 和 Wetland(索引6) 在以下配置中的位置：\n\n" +
                "• MapConfig.maps\n" +
                "• BirdConfig.sceneBirds\n" +
                "• ShopConfig.sceneEggs\n" +
                "• ShopConfig.sceneDecorations\n\n" +
                "此操作不可撤销，建议先提交当前更改。",
                "确认交换", "取消"))
                return;

            const int indexA = 5;
            const int indexB = 6;

            // MapConfig
            var mapConfig = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Prefabs/Config/MapConfig.asset");
            if (mapConfig != null && mapConfig.maps != null && mapConfig.maps.Length > indexB)
            {
                (mapConfig.maps[indexA], mapConfig.maps[indexB]) = (mapConfig.maps[indexB], mapConfig.maps[indexA]);
                EditorUtility.SetDirty(mapConfig);
                Debug.Log("[Swap] MapConfig.maps 交换完成");
            }

            // BirdConfig
            var birdConfig = AssetDatabase.LoadAssetAtPath<BirdConfig>("Assets/Prefabs/Config/BirdConfig.asset");
            if (birdConfig != null && birdConfig.sceneBirds != null && birdConfig.sceneBirds.Count > indexB)
            {
                (birdConfig.sceneBirds[indexA], birdConfig.sceneBirds[indexB]) = (birdConfig.sceneBirds[indexB], birdConfig.sceneBirds[indexA]);
                EditorUtility.SetDirty(birdConfig);
                Debug.Log("[Swap] BirdConfig.sceneBirds 交换完成");
            }

            // ShopConfig
            var shopConfig = AssetDatabase.LoadAssetAtPath<ShopConfig>("Assets/Prefabs/Config/ShopConfig.asset");
            if (shopConfig != null)
            {
                if (shopConfig.sceneEggs != null && shopConfig.sceneEggs.Count > indexB)
                {
                    (shopConfig.sceneEggs[indexA], shopConfig.sceneEggs[indexB]) = (shopConfig.sceneEggs[indexB], shopConfig.sceneEggs[indexA]);
                    Debug.Log("[Swap] ShopConfig.sceneEggs 交换完成");
                }
                if (shopConfig.sceneDecorations != null && shopConfig.sceneDecorations.Count > indexB)
                {
                    (shopConfig.sceneDecorations[indexA], shopConfig.sceneDecorations[indexB]) = (shopConfig.sceneDecorations[indexB], shopConfig.sceneDecorations[indexA]);
                    Debug.Log("[Swap] ShopConfig.sceneDecorations 交换完成");
                }
                EditorUtility.SetDirty(shopConfig);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "tundra 和 Wetland 已交换位置。\n请在 Inspector 中确认结果。", "确定");
        }
    }
}
#endif
