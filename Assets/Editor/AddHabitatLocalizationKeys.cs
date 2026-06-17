#if UNITY_EDITOR
using System.Collections.Generic;
using BirdGame;
using UnityEditor;
using UnityEngine;

namespace BirdGameEditor
{
    /// <summary>
    /// 把地图栖息地缺失的本地化 key（Sea / Tundra）写进 LocalizationConfig。
    /// 栖息地名字直接当本地化 key 用（见 MapItem.GetString(mapName)），这两个之前没加，
    /// 运行时回退显示英文。这里通过真实类型由 Odin 序列化补齐，不手改 .asset 引用 ID。
    /// 菜单：Tools/Localization/Add Habitat Keys
    /// </summary>
    public static class AddHabitatLocalizationKeys
    {
        private const string AssetPath = "Assets/Prefabs/Config/LocalizationConfig.asset";

        // key（= MapConfig 里的 mapName） -> (语言 -> 翻译)
        private static readonly Dictionary<string, Dictionary<SystemLanguage, string>> Translations =
            new Dictionary<string, Dictionary<SystemLanguage, string>>
            {
                ["Sea"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Sea",
                    [SystemLanguage.ChineseSimplified] = "海洋",
                    [SystemLanguage.ChineseTraditional] = "海洋",
                    [SystemLanguage.German] = "Meer",
                    [SystemLanguage.Portuguese] = "Mar",
                    [SystemLanguage.French] = "Mer",
                    [SystemLanguage.Spanish] = "Mar",
                    [SystemLanguage.Russian] = "Море",
                },
                ["Tundra"] = new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = "Tundra",
                    [SystemLanguage.ChineseSimplified] = "苔原",
                    [SystemLanguage.ChineseTraditional] = "苔原",
                    [SystemLanguage.German] = "Tundra",
                    [SystemLanguage.Portuguese] = "Tundra",
                    [SystemLanguage.French] = "Toundra",
                    [SystemLanguage.Spanish] = "Tundra",
                    [SystemLanguage.Russian] = "Тундра",
                },
            };

        [MenuItem("Tools/Localization/Add Habitat Keys")]
        public static void AddKeys()
        {
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(AssetPath);
            if (config == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LocalizationConfig");
                if (guids.Length > 0)
                    config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (config == null)
            {
                Debug.LogError($"[AddHabitatLocalizationKeys] 找不到 LocalizationConfig（{AssetPath}）");
                return;
            }

            int added = 0, updated = 0;
            foreach (var langPair in config.languageDic)
            {
                SystemLanguage lang = langPair.Key;
                LocalizationLanguage langData = langPair.Value;
                if (langData.words == null)
                    langData.words = new Dictionary<string, Pattern>();

                foreach (var keyPair in Translations)
                {
                    string key = keyPair.Key;
                    if (!keyPair.Value.TryGetValue(lang, out string text))
                        continue;

                    if (langData.words.TryGetValue(key, out var existing) && existing != null)
                    {
                        existing.text = text;
                        updated++;
                    }
                    else
                    {
                        langData.words[key] = new Pattern { text = text };
                        added++;
                    }
                }
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AddHabitatLocalizationKeys] 完成：新增 {added} 条，覆盖 {updated} 条，" +
                      $"覆盖 {config.languageDic.Count} 种语言 × {Translations.Count} 个 key（Sea / Tundra）。");
        }
    }
}
#endif
