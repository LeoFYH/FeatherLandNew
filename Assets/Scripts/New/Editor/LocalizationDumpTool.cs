#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 诊断工具：直接 dump LocalizationConfig.asset 里每种语言的几条 sample，
    /// 验证 import 是否真的写对了。结果在 Console 输出。
    /// </summary>
    public static class LocalizationDumpTool
    {
        private const string ConfigPath = "Assets/Prefabs/Config/LocalizationConfig.asset";

        // 验证用的 sample key（每种语言都有内容的那种）
        private static readonly string[] SampleKeys = { "Sale Price:", "English", "ChineseSimplified", "ChineseTraditional", "German", "French", "Russian", "Spanish", "Portuguese" };

        [MenuItem("Tools/本地化/诊断-Dump各语言sample")]
        public static void Dump()
        {
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(ConfigPath);
            if (config == null || config.languageDic == null)
            {
                Debug.LogError("找不到 LocalizationConfig 或 languageDic 为 null");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== LocalizationConfig 实际数据 =====");
            sb.AppendLine($"字典共 {config.languageDic.Count} 个语言（按插入顺序）：");

            int idx = 0;
            foreach (var kvp in config.languageDic)
            {
                var lang = kvp.Key;
                var data = kvp.Value;
                int wordCount = data?.words?.Count ?? 0;
                sb.AppendLine();
                sb.AppendLine($"[{idx}] {lang} ({wordCount} 个 key):");
                if (data?.words != null)
                {
                    foreach (var sk in SampleKeys)
                    {
                        if (data.words.TryGetValue(sk, out var p))
                        {
                            sb.AppendLine($"    \"{sk}\" → \"{p?.text}\"");
                        }
                        else
                        {
                            sb.AppendLine($"    \"{sk}\" → <key 不存在>");
                        }
                    }
                }
                idx++;
            }

            // 同时 dump LocalizationGlobalConfig.languages 的顺序
            sb.AppendLine();
            sb.AppendLine("===== LocalizationGlobalConfig.languages 列表（标签顺序）=====");
            var globalCfg = BirdGame.Editor.LocalizationGlobalConfig.Instance;
            if (globalCfg != null && globalCfg.languages != null)
            {
                for (int i = 0; i < globalCfg.languages.Count; i++)
                {
                    sb.AppendLine($"  [{i}] {globalCfg.languages[i].Language}");
                }
            }
            else
            {
                sb.AppendLine("  (LocalizationGlobalConfig.Instance 或 .languages 为 null)");
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Dump 完成", "结果已输出到 Console，请打开 Console 窗口查看。", "OK");
        }
    }
}
#endif
