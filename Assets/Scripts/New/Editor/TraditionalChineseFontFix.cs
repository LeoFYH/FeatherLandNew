#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 修繁体字显示：
    /// 1. 撤销之前的污染（移除 ARIALI SDF 里的 ChineseTraditional.asset fallback）
    /// 2. 从 ChironGoRoundTC-Medium.ttf 生成 ChironGoRoundTC SDF
    /// 3. 把 TC 语言的 fontAsset 直接设为 ChironGoRoundTC SDF（繁体全部用这个字体）
    /// 4. 把 ChironGoRoundTC SDF 加到 ARIALI SDF 的 fallback（其他语言模式遇到繁体字也能显示）
    /// </summary>
    public static class TraditionalChineseFontFix
    {
        private const string LocalizationConfigPath = "Assets/Prefabs/Config/LocalizationConfig.asset";
        private const string MainFontPath = "Assets/Fonts/English/ARIALI SDF.asset";
        private const string OldBrokenFontPath = "Assets/Fonts/ChineseTra/ChineseTraditional.asset";
        private const string ChironTtfPath = "Assets/Fonts/ChineseTra/ChironGoRoundTC-Medium.ttf";
        private const string ChironSdfPath = "Assets/Fonts/ChineseTra/ChironGoRoundTC SDF.asset";

        [MenuItem("Tools/本地化/修繁体字显示")]
        public static void Apply()
        {
            var sb = new System.Text.StringBuilder();

            var mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
            if (mainFont == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 " + MainFontPath, "OK");
                return;
            }

            // ========== 1) 撤销污染 ==========

            // 1a) ARIALI SDF 的 fallback 里移除空的 ChineseTraditional.asset
            var brokenTc = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldBrokenFontPath);
            if (mainFont.fallbackFontAssetTable != null && brokenTc != null
                && mainFont.fallbackFontAssetTable.Remove(brokenTc))
            {
                sb.AppendLine("✅ 从 ARIALI SDF fallback 移除了空的 ChineseTraditional.asset");
            }

            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(LocalizationConfigPath);

            // ========== 2) 生成 Chiron GoRound TC SDF ==========
            var srcFont = AssetDatabase.LoadAssetAtPath<Font>(ChironTtfPath);
            if (srcFont == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 " + ChironTtfPath, "OK");
                return;
            }

            // 删旧的（如果之前有）
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChironSdfPath) != null)
                AssetDatabase.DeleteAsset(ChironSdfPath);

            int samplingPointSize = 90;
            int padding = 9;
            int atlasSize = 2048; // 中文字符多，用大点的 atlas

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                srcFont,
                samplingPointSize,
                padding,
                GlyphRenderMode.SDFAA,
                atlasSize, atlasSize,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("出错", "TMP_FontAsset.CreateFontAsset 失败", "OK");
                return;
            }
            fontAsset.name = "ChironGoRoundTC SDF";

            AssetDatabase.CreateAsset(fontAsset, ChironSdfPath);

            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    var tex = fontAsset.atlasTextures[i];
                    if (tex == null) continue;
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                    {
                        tex.name = "ChironGoRoundTC SDF Atlas " + i;
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }
            }
            if (fontAsset.atlasTexture != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fontAsset.atlasTexture)))
            {
                fontAsset.atlasTexture.name = "ChironGoRoundTC SDF Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }
            if (fontAsset.material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fontAsset.material)))
            {
                fontAsset.material.name = "ChironGoRoundTC SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ChironSdfPath, ImportAssetOptions.ForceUpdate);

            sb.AppendLine("✅ ChironGoRoundTC SDF 已生成 (" + ChironSdfPath + ")");

            var saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChironSdfPath);

            // ========== 3) TC 语言 fontAsset 直接设为 ChironGoRoundTC SDF ==========
            if (saved != null && config != null && config.languageDic != null
                && config.languageDic.TryGetValue(SystemLanguage.ChineseTraditional, out var tcLang)
                && tcLang != null)
            {
                tcLang.fontAsset = saved;
                EditorUtility.SetDirty(config);
                sb.AppendLine($"✅ TC 语言 fontAsset 设为 {saved.name}（繁体直接用这个字体）");
            }

            // ========== 4) 加到 ARIALI SDF fallback ==========
            if (saved != null)
            {
                if (mainFont.fallbackFontAssetTable == null)
                    mainFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!mainFont.fallbackFontAssetTable.Contains(saved))
                {
                    mainFont.fallbackFontAssetTable.Add(saved);
                    EditorUtility.SetDirty(mainFont);
                    sb.AppendLine($"✅ ChironGoRoundTC SDF 加入 ARIALI SDF fallback (现共 {mainFont.fallbackFontAssetTable.Count} 项)");
                }
                else
                {
                    sb.AppendLine("ℹ️ ChironGoRoundTC SDF 已在 fallback 里");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TCFontFix]\n" + sb);
            EditorUtility.DisplayDialog("完成", sb.ToString(), "OK");
        }
    }
}
#endif
