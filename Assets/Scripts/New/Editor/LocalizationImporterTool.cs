#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using BirdGame.Editor;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 从 xlsx 导入本地化数据，覆盖 LocalizationConfig.asset 里 7 个语言（保留 ChineseTraditional）。
    /// xlsx 列：key | 英文 | 简体中文 | German | French | Russian | Spanish | Portuguese
    /// 行为：完全按 xlsx 来——每个语言的 words 字典先清空再按 xlsx 重建；繁中不动。
    /// 重复 key：取第一次出现的；空 key 行：跳过。
    /// </summary>
    public static class LocalizationImporterTool
    {
        private const string ConfigPath = "Assets/Prefabs/Config/LocalizationConfig.asset";

        // (xlsx 列号 → SystemLanguage)。1-based 列号。
        private static readonly (int col, SystemLanguage lang)[] ColumnMap =
        {
            (2, SystemLanguage.English),
            (3, SystemLanguage.ChineseSimplified),
            (4, SystemLanguage.German),
            (5, SystemLanguage.French),
            (6, SystemLanguage.Russian),
            (7, SystemLanguage.Spanish),
            (8, SystemLanguage.Portuguese),
        };

        [MenuItem("Tools/本地化/从xlsx导入(覆盖7语言保留繁中)")]
        public static void ImportFromXlsx()
        {
            string path = EditorUtility.OpenFilePanel("选择本地化 xlsx", "", "xlsx");
            if (string.IsNullOrEmpty(path)) return;
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("出错", "文件不存在：" + path, "OK");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 LocalizationConfig：" + ConfigPath, "OK");
                return;
            }
            if (config.languageDic == null)
                config.languageDic = new Dictionary<SystemLanguage, LocalizationLanguage>();

            // 1) 读 xlsx (项目用的旧版 EPPlus，无需设置 LicenseContext)
            var perLang = new Dictionary<SystemLanguage, Dictionary<string, string>>();
            foreach (var (_, lang) in ColumnMap)
                perLang[lang] = new Dictionary<string, string>();

            int totalRows = 0;
            int dupRows = 0;
            int skippedEmptyKey = 0;

            using (var pkg = new ExcelPackage(new FileInfo(path)))
            {
                var ws = pkg.Workbook.Worksheets["data"];
                if (ws == null)
                {
                    EditorUtility.DisplayDialog("出错", "xlsx 里找不到名为 'data' 的 sheet", "OK");
                    return;
                }
                int rowCount = ws.Dimension?.End.Row ?? 0;
                for (int r = 2; r <= rowCount; r++)
                {
                    string key = (ws.Cells[r, 1].Value?.ToString() ?? "").Trim();
                    if (string.IsNullOrEmpty(key))
                    {
                        skippedEmptyKey++;
                        continue;
                    }
                    totalRows++;

                    bool isDupRow = false;
                    foreach (var (col, lang) in ColumnMap)
                    {
                        var dict = perLang[lang];
                        if (dict.ContainsKey(key))
                        {
                            isDupRow = true;
                            continue;   // 保留第一次出现
                        }
                        string val = (ws.Cells[r, col].Value?.ToString() ?? "").Trim();
                        dict[key] = val;
                    }
                    if (isDupRow) dupRows++;
                }
            }

            // 2) 写回 LocalizationConfig（只动 7 种语言；繁中保持原样）
            int totalWritten = 0;
            var summary = new System.Text.StringBuilder();
            var beforeCount = new Dictionary<SystemLanguage, int>();

            // 记录改动前的 count
            foreach (var kvp in config.languageDic)
                beforeCount[kvp.Key] = kvp.Value?.words?.Count ?? 0;

            // 准备每种语言的新 LocalizationLanguage 实例（含 fontAsset 保留）
            var rebuilt = new Dictionary<SystemLanguage, LocalizationLanguage>();
            foreach (var (_, lang) in ColumnMap)
            {
                LocalizationLanguage langData;
                if (config.languageDic.TryGetValue(lang, out var existing) && existing != null)
                {
                    langData = new LocalizationLanguage
                    {
                        fontAsset = existing.fontAsset,
                        words = new Dictionary<string, Pattern>()
                    };
                }
                else
                {
                    langData = new LocalizationLanguage { words = new Dictionary<string, Pattern>() };
                }
                foreach (var kv in perLang[lang])
                    langData.words[kv.Key] = new Pattern { text = kv.Value };
                rebuilt[lang] = langData;
                totalWritten += langData.words.Count;
            }

            // 3) 重建 config.languageDic，让顺序与 LocalizationGlobalConfig.languages 对齐，避免标签/数据错位
            var ordered = new Dictionary<SystemLanguage, LocalizationLanguage>();
            var globalCfg = LocalizationGlobalConfig.Instance;
            var orderRef = globalCfg != null && globalCfg.languages != null && globalCfg.languages.Count > 0
                ? globalCfg.languages.ConvertAll(le => le.Language)
                : new List<SystemLanguage>
                {
                    SystemLanguage.English,
                    SystemLanguage.ChineseSimplified,
                    SystemLanguage.ChineseTraditional,
                    SystemLanguage.Russian,
                    SystemLanguage.German,
                    SystemLanguage.Portuguese,
                    SystemLanguage.French,
                    SystemLanguage.Spanish,
                };

            foreach (var lang in orderRef)
            {
                if (rebuilt.TryGetValue(lang, out var langData))
                {
                    ordered[lang] = langData;
                }
                else if (config.languageDic.TryGetValue(lang, out var existing))
                {
                    // 不在 ColumnMap 里（比如繁中），保留原数据
                    ordered[lang] = existing;
                }
            }
            // 兜底：把 orderRef 没列出但 rebuilt 里有的，追加到末尾（防止丢数据）
            foreach (var kv in rebuilt)
            {
                if (!ordered.ContainsKey(kv.Key))
                    ordered[kv.Key] = kv.Value;
            }
            // 兜底：把 config.languageDic 里 orderRef 也没列出、rebuilt 也没有的（脏数据）保留
            foreach (var kv in config.languageDic)
            {
                if (!ordered.ContainsKey(kv.Key))
                    ordered[kv.Key] = kv.Value;
            }

            config.languageDic = ordered;

            // summary
            foreach (var kv in ordered)
            {
                int before = beforeCount.TryGetValue(kv.Key, out var b) ? b : 0;
                int after = kv.Value?.words?.Count ?? 0;
                bool wasInImport = rebuilt.ContainsKey(kv.Key);
                string note = wasInImport ? "" : "(未改动)";
                summary.AppendLine($"  {kv.Key}: {before} → {after} 个 key {note}");
            }

            EditorUtility.SetDirty(config);
            if (globalCfg != null) EditorUtility.SetDirty(globalCfg);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"✅ 导入完成\n\n" +
                         $"xlsx 数据行数：{totalRows}（跳过 {skippedEmptyKey} 个空 key 行，{dupRows} 行的 key 重复并被忽略）\n\n" +
                         $"覆盖的语言：\n{summary}\n" +
                         $"总计写入 {totalWritten} 个 key 翻译";
            Debug.Log("[LocalizationImporter]\n" + msg);
            EditorUtility.DisplayDialog("完成", msg, "OK");
        }
    }
}
#endif
