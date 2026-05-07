#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// 把 Comfortaa SDF 加到 ARIALI SDF 的 fallback 列表，让其他语言模式下也能渲染俄文字符
    /// （比如 dropdown 里的 "Русский" 在中文/西语模式下也能显示）。
    /// </summary>
    public static class AddCyrillicFallback
    {
        private const string MainFontPath = "Assets/Fonts/English/ARIALI SDF.asset";
        private const string ComfortaaPath = "Assets/Fonts/Russian/Comfortaa SDF.asset";

        [MenuItem("Tools/本地化/给ARIALI SDF加Comfortaa作为Cyrillic-fallback")]
        public static void Apply()
        {
            var mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
            var comfortaa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ComfortaaPath);

            if (mainFont == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 " + MainFontPath, "OK");
                return;
            }
            if (comfortaa == null)
            {
                EditorUtility.DisplayDialog("出错", "找不到 " + ComfortaaPath + "\n请先跑【把Comfortaa应用到俄语】生成它", "OK");
                return;
            }

            if (mainFont.fallbackFontAssetTable == null)
                mainFont.fallbackFontAssetTable = new List<TMP_FontAsset>();

            if (mainFont.fallbackFontAssetTable.Contains(comfortaa))
            {
                EditorUtility.DisplayDialog("无变化",
                    $"{mainFont.name} 的 fallback 已包含 {comfortaa.name}，无需重复添加。", "OK");
                return;
            }

            mainFont.fallbackFontAssetTable.Add(comfortaa);
            EditorUtility.SetDirty(mainFont);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"已把 {comfortaa.name} 加到 {mainFont.name} 的 fallback 列表。\n" +
                         $"现在 {mainFont.name} 的 fallback 共 {mainFont.fallbackFontAssetTable.Count} 项。\n\n" +
                         "进游戏切到任意非俄语，dropdown 里的 \"Русский\" 现在应该能显示了。";
            Debug.Log("[CyrillicFallback] " + msg);
            EditorUtility.DisplayDialog("完成", msg, "OK");
        }
    }
}
#endif
