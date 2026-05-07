#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 把 Comfortaa 字体应用到俄语（仅此一项）。
    /// </summary>
    public static class NunitoForRussian
    {
        private const string TtfPath = "Assets/Fonts/Russian/Comfortaa-VariableFont_wght.ttf";
        private const string SdfPath = "Assets/Fonts/Russian/Comfortaa SDF.asset";
        private const string FontDisplayName = "Comfortaa SDF";
        private const string LocalizationConfigPath = "Assets/Prefabs/Config/LocalizationConfig.asset";

        [MenuItem("Tools/本地化/把Comfortaa应用到俄语")]
        public static void Apply()
        {
            var srcFont = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (srcFont == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 Comfortaa TTF：" + TtfPath, "OK");
                return;
            }

            // 1) 删旧的
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath) != null)
                AssetDatabase.DeleteAsset(SdfPath);

            // 2) 用 DYNAMIC 模式创建 TMP_FontAsset（运行时按需加字形，不需要预烤）
            int samplingPointSize = 90;
            int padding = 9;
            int atlasSize = 1024;

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
            fontAsset.name = "Comfortaa SDF";

            // 3) 保存为资源 —— 关键：先 CreateAsset，再立刻 AddObjectToAsset 把 atlas + material 作为子资源
            AssetDatabase.CreateAsset(fontAsset, SdfPath);

            // 把所有 atlas texture 加入为子资源
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    var tex = fontAsset.atlasTextures[i];
                    if (tex == null) continue;
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                    {
                        tex.name = "Comfortaa SDF Atlas " + i;
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }
            }
            // 单数版兜底（某些 TMP 版本只用 atlasTexture）
            if (fontAsset.atlasTexture != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fontAsset.atlasTexture)))
            {
                fontAsset.atlasTexture.name = "Comfortaa SDF Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }
            // material 也作为子资源
            if (fontAsset.material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fontAsset.material)))
            {
                fontAsset.material.name = "Comfortaa SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(SdfPath, ImportAssetOptions.ForceUpdate);

            // 4) 重新加载 saved 引用
            var saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
            if (saved == null)
            {
                EditorUtility.DisplayDialog("出错", "保存后无法加载 Comfortaa SDF", "OK");
                return;
            }

            // 5) 把 saved 设为 Russian 语言的 fontAsset
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(LocalizationConfigPath);
            if (config == null || config.languageDic == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 LocalizationConfig", "OK");
                return;
            }

            if (config.languageDic.TryGetValue(SystemLanguage.Russian, out var russianLang) && russianLang != null)
            {
                russianLang.fontAsset = saved;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Comfortaa] 完成：Comfortaa SDF 已生成（Dynamic 模式）并设为 Russian fontAsset。运行时会按需烤字形。");
                EditorUtility.DisplayDialog("完成",
                    "Comfortaa SDF 已生成（Dynamic 模式）。\n" +
                    "已设为 Russian 语言的 fontAsset。\n\n" +
                    "运行时碰到俄文字符会自动烤进 atlas。\n" +
                    "进游戏切到 Russian 测试。", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("注意",
                    "LocalizationConfig 里没有 SystemLanguage.Russian 这一项。\n" +
                    "Comfortaa SDF 已生成在：" + SdfPath, "OK");
            }
        }
    }
}
#endif
