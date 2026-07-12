#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
                "• ShopConfig.sceneDecorations\n" +
                "• 本地存档 BirdInfoData.save（mapBirds）\n" +
                "• 本地存档 AccountData.save（sceneDecorationInfos）\n\n" +
                "此操作不可撤销，建议先提交当前更改并备份存档。",
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

            // 本地存档：BirdInfoData.mapBirds / AccountData.sceneDecorationInfos
            SwapSaveData(indexA, indexB);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "tundra 和 Wetland 已交换位置（包含本地存档）。\n请在 Inspector 中确认结果。", "确定");
        }

        private static void SwapSaveData(int indexA, int indexB)
        {
            string saveDir = Path.Combine(Application.persistentDataPath, "GameData");
            SwapBirdInfoData(saveDir, indexA, indexB);
            SwapAccountData(saveDir, indexA, indexB);
        }

        private static void SwapBirdInfoData(string saveDir, int indexA, int indexB)
        {
            string path = Path.Combine(saveDir, "BirdInfoData.save");
            if (!File.Exists(path)) return;

            try
            {
                byte[] allBytes = File.ReadAllBytes(path);
                if (allBytes.Length < 16) return;

                byte[] jsonBytes = new byte[allBytes.Length - 16];
                Buffer.BlockCopy(allBytes, 0, jsonBytes, 0, jsonBytes.Length);
                string json = Encoding.UTF8.GetString(jsonBytes);

                var data = JsonUtility.FromJson<BirdInfoData>(json);
                if (data?.mapBirds == null) return;

                while (data.mapBirds.Count <= indexB) data.mapBirds.Add(new MapBirdList());

                (data.mapBirds[indexA], data.mapBirds[indexB]) = (data.mapBirds[indexB], data.mapBirds[indexA]);

                WriteSaveFile(path, data);
                Debug.Log("[Swap] BirdInfoData.save 交换完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Swap] BirdInfoData.save 交换失败: {e.Message}");
            }
        }

        private static void SwapAccountData(string saveDir, int indexA, int indexB)
        {
            string path = Path.Combine(saveDir, "AccountData.save");
            if (!File.Exists(path)) return;

            try
            {
                byte[] allBytes = File.ReadAllBytes(path);
                if (allBytes.Length < 16) return;

                byte[] jsonBytes = new byte[allBytes.Length - 16];
                Buffer.BlockCopy(allBytes, 0, jsonBytes, 0, jsonBytes.Length);
                string json = Encoding.UTF8.GetString(jsonBytes);

                var data = JsonUtility.FromJson<AccountData>(json);
                if (data?.sceneDecorationInfos == null) return;

                while (data.sceneDecorationInfos.Count <= indexB) data.sceneDecorationInfos.Add(new SceneDecorationInfo());

                (data.sceneDecorationInfos[indexA], data.sceneDecorationInfos[indexB]) = (data.sceneDecorationInfos[indexB], data.sceneDecorationInfos[indexA]);

                WriteSaveFile(path, data);
                Debug.Log("[Swap] AccountData.save 交换完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Swap] AccountData.save 交换失败: {e.Message}");
            }
        }

        private static void WriteSaveFile<T>(string path, T data) where T : SavableData
        {
            string jsonData = JsonUtility.ToJson(data);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);
            byte[] hash = ComputeMD5(jsonBytes);

            byte[] finalData = new byte[jsonBytes.Length + hash.Length];
            Buffer.BlockCopy(jsonBytes, 0, finalData, 0, jsonBytes.Length);
            Buffer.BlockCopy(hash, 0, finalData, jsonBytes.Length, hash.Length);

            File.WriteAllBytes(path, finalData);
        }

        private static byte[] ComputeMD5(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        [MenuItem("Tools/一次性/同步存档到 tundra(5)/Wetland(6) 当前配置")]
        public static void SyncSaveDataToCurrentConfig()
        {
            if (!EditorUtility.DisplayDialog("确认同步",
                "将只交换本地存档中的 BirdInfoData.mapBirds[5]/[6] 和 AccountData.sceneDecorationInfos[5]/[6]，\n" +
                "使其与当前配置顺序保持一致。\n\n" +
                "如果你已经运行过 '交换 tundra(5) 和 Wetland(6)' 但存档没跟上，使用此项可修复因存档不同步导致的 NullReferenceException。\n\n" +
                "此操作不可撤销，建议先备份存档。",
                "确认同步", "取消"))
                return;

            SwapSaveData(5, 6);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "本地存档已与当前配置顺序同步。\n请重新进入游戏测试。", "确定");
        }
    }
}
#endif
