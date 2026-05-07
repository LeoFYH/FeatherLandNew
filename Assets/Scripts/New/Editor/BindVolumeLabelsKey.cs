#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 给 SettingPopup prefab 里 6 个音量 label 加 LocalizationText 组件 + 对应 key。
    /// </summary>
    public static class BindVolumeLabelsKey
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Popups/SettingPopup.prefab";

        // Label 当前文本 → 本地化 key
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            {"Effect",      "VolumeEffect"},
            {"Environment", "VolumeEnvironment"},
            {"Music",       "VolumeMusic"},
            {"Petting",     "VolumePetting"},
            {"Alarm",       "VolumeAlarm"},
            {"Master",      "VolumeMaster"},
        };

        [MenuItem("Tools/本地化/给音量label绑定key")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 prefab：" + PrefabPath, "OK");
                return;
            }

            var sb = new System.Text.StringBuilder();
            int totalScanned = 0, bound = 0, alreadyHad = 0;
            try
            {
                var allTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in allTexts)
                {
                    if (t == null) continue;
                    totalScanned++;
                    string raw = (t.text ?? "").Trim();
                    if (!Map.TryGetValue(raw, out var key)) continue;

                    var existing = t.GetComponent<LocalizationText>();
                    if (existing != null)
                    {
                        existing.Key = key;
                        EditorUtility.SetDirty(existing);
                        sb.AppendLine($"  已有 LocalizationText：{t.gameObject.name} key 设为 {key}");
                        alreadyHad++;
                    }
                    else
                    {
                        var lt = t.gameObject.AddComponent<LocalizationText>();
                        lt.Key = key;
                        // LocalizationText [RequireComponent(FontAssetExchange)]，确保有 FontAssetExchange
                        if (t.GetComponent<FontAssetExchange>() == null)
                            t.gameObject.AddComponent<FontAssetExchange>();
                        EditorUtility.SetDirty(lt);
                        sb.AppendLine($"  ✅ 加 LocalizationText：{t.gameObject.name} key={key}");
                        bound++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            string msg = $"扫描 {totalScanned} 个 TMP，新加 {bound} 个 LocalizationText，已有 {alreadyHad} 个更新 key\n\n{sb}";
            Debug.Log("[BindVolumeKey]\n" + msg);
            EditorUtility.DisplayDialog("完成", msg, "OK");
        }
    }
}
#endif
