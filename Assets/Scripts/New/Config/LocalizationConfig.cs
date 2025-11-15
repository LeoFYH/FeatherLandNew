using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;

namespace BirdGame
{
    public class LocalizationConfig : SerializedScriptableObject
    {
        [Title("本地化配置"), ReadOnly]
        public Dictionary<SystemLanguage, LocalizationLanguage> languageDic =
            new Dictionary<SystemLanguage, LocalizationLanguage>();
    }

    [Serializable]
    public class LocalizationLanguage
    {
        [LabelText("字体文件")]
        public TMP_FontAsset fontAsset;
        [OdinSerialize, DictionaryDrawerSettings(KeyLabel = "标识", ValueLabel = "翻译")]
        public Dictionary<string, Pattern> words = new Dictionary<string, Pattern>();
    }

    [Serializable]
    public class Pattern
    {
        public enum PatternType
        {
            Image,
            Text,
        }

        [ShowInInspector] public PatternType Type { get; set; } = PatternType.Text;

        [ShowIf("@Type==PatternType.Image"), PreviewField(30, ObjectFieldAlignment.Left)]
        public Sprite sprite;
        [ShowIf("@Type==PatternType.Text")]
        public string text;
    }
}