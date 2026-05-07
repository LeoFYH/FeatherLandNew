#if UNITY_EDITOR
using System.Collections.Generic;
using BirdGame.Editor;
using UnityEditor;
using UnityEngine;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 修复工具：把 LocalizationGlobalConfig.languages 列表的顺序同步到 LocalizationConfig.languageDic 实际插入顺序。
    /// 解决 tool 标签显示错位问题（标签位置和字典位置不一致 → 显示错语言的数据）。
    /// </summary>
    public static class LocalizationSyncLanguagesTool
    {
        private const string ConfigPath = "Assets/Prefabs/Config/LocalizationConfig.asset";

        [MenuItem("Tools/本地化/诊断-同步languages列表到字典顺序")]
        public static void Sync()
        {
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(ConfigPath);
            if (config == null || config.languageDic == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 LocalizationConfig 或 languageDic 为 null", "OK");
                return;
            }

            var globalCfg = LocalizationGlobalConfig.Instance;
            if (globalCfg == null)
            {
                EditorUtility.DisplayDialog("出错", "LocalizationGlobalConfig.Instance 为 null", "OK");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("操作前：");
            sb.AppendLine("  字典顺序：");
            int i = 0;
            foreach (var kvp in config.languageDic)
                sb.AppendLine($"    [{i++}] {kvp.Key}");
            sb.AppendLine("  languages 顺序：");
            for (int j = 0; j < globalCfg.languages.Count; j++)
                sb.AppendLine($"    [{j}] {globalCfg.languages[j].Language}");

            // 重建 languages 列表，与字典顺序一致
            var newList = new List<LanguageEncoding>();
            foreach (var kvp in config.languageDic)
            {
                // 尝试保留原有 LanguageEncoding 的 encoding 字段
                var existing = globalCfg.languages.Find(le => le.Language == kvp.Key);
                if (existing != null)
                {
                    newList.Add(existing);
                }
                else
                {
                    newList.Add(new LanguageEncoding { Language = kvp.Key });
                }
            }
            globalCfg.languages = newList;

            sb.AppendLine();
            sb.AppendLine("操作后 languages 顺序：");
            for (int j = 0; j < globalCfg.languages.Count; j++)
                sb.AppendLine($"    [{j}] {globalCfg.languages[j].Language}");

            EditorUtility.SetDirty(globalCfg);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LocalizationSync]\n" + sb);
            EditorUtility.DisplayDialog("完成", sb.ToString(), "OK");
        }
    }
}
#endif
