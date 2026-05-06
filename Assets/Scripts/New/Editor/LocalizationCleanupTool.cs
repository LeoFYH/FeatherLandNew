using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 本地化清理工具：把 Italian 槽位重命名为 Russian，并移除 Japanese / Korean。
    /// 最终保留 8 种语言：English、ChineseSimplified、ChineseTraditional、Russian、German、Portuguese、French、Spanish。
    /// </summary>
    public static class LocalizationCleanupTool
    {
        private const string ConfigPath = "Assets/Prefabs/Config/LocalizationConfig.asset";

        [MenuItem("Tools/本地化/Italian改Russian并清理为8种语言")]
        public static void RenameItalianToRussianAndCleanup()
        {
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("出错", $"找不到 LocalizationConfig: {ConfigPath}", "OK");
                return;
            }

            if (config.languageDic == null)
            {
                EditorUtility.DisplayDialog("出错", "languageDic 为空", "OK");
                return;
            }

            // 列出当前内容
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("当前 LocalizationConfig 包含的语言：");
            foreach (var pair in config.languageDic)
            {
                int wordCount = pair.Value?.words?.Count ?? 0;
                summary.AppendLine($"  - {pair.Key} ({wordCount} 个 key)");
            }
            summary.AppendLine();
            summary.AppendLine("将执行以下操作：");
            summary.AppendLine("  1. 把 Italian 槽位的所有数据搬到 Russian (相当于改标签)");
            summary.AppendLine("  2. 删除 Japanese / Korean 槽位（若存在）");
            summary.AppendLine("  3. 保留 ChineseTraditional（繁体中文）");
            summary.AppendLine();
            summary.AppendLine("最终保留 8 种语言：");
            summary.AppendLine("  English / ChineseSimplified / ChineseTraditional / Russian / German / Portuguese / French / Spanish");
            summary.AppendLine();
            summary.AppendLine("继续吗？");

            if (!EditorUtility.DisplayDialog("确认操作", summary.ToString(), "执行", "取消"))
                return;

            int changes = 0;
            var log = new System.Text.StringBuilder();

            // Step 1: Italian -> Russian (rename key, preserve data)
            if (config.languageDic.TryGetValue(SystemLanguage.Italian, out var italianData))
            {
                if (config.languageDic.ContainsKey(SystemLanguage.Russian))
                {
                    int existing = config.languageDic[SystemLanguage.Russian]?.words?.Count ?? 0;
                    log.AppendLine($"⚠️ Russian 槽已存在 ({existing} 个 key)，被 Italian 数据覆盖");
                }
                config.languageDic[SystemLanguage.Russian] = italianData;
                config.languageDic.Remove(SystemLanguage.Italian);
                int n = italianData?.words?.Count ?? 0;
                log.AppendLine($"✅ Italian → Russian (搬移 {n} 个 key)");
                changes++;
            }
            else
            {
                log.AppendLine("ℹ️ 没有 Italian 槽位，跳过重命名");
            }

            // Step 2: Remove unwanted languages (ChineseTraditional 保留)
            foreach (var lang in new[] { SystemLanguage.Japanese, SystemLanguage.Korean })
            {
                if (config.languageDic.ContainsKey(lang))
                {
                    int n = config.languageDic[lang]?.words?.Count ?? 0;
                    config.languageDic.Remove(lang);
                    log.AppendLine($"🗑️ 已删除 {lang} ({n} 个 key)");
                    changes++;
                }
                else
                {
                    log.AppendLine($"ℹ️ 没有 {lang} 槽位，跳过");
                }
            }

            log.AppendLine();
            log.AppendLine("当前剩余语言：");
            foreach (var pair in config.languageDic)
            {
                int n = pair.Value?.words?.Count ?? 0;
                log.AppendLine($"  - {pair.Key} ({n} 个 key)");
            }

            if (changes > 0)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[LocalizationCleanup] 完成：\n" + log);
                EditorUtility.DisplayDialog("完成", log.ToString() + "\n已保存。", "OK");
            }
            else
            {
                Debug.Log("[LocalizationCleanup] 无变化：\n" + log);
                EditorUtility.DisplayDialog("无变化", log.ToString(), "OK");
            }
        }
    }
}
