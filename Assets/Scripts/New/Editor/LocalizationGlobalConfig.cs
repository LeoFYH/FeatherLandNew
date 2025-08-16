using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BirdGame.Editor
{
    public class LocalizationGlobalConfig : GlobalConfig<LocalizationGlobalConfig>
    {
        public enum Page
        {
            语言设置,
            翻译设置
        }

        [Title("本地化配置", Bold = true)] [HorizontalGroup("config"), ReadOnly, OnInspectorInit("InitConfig")]
        public LocalizationConfig config;

        [EnumToggleButtons, HideLabel] public Page page;

        private void InitConfig()
        {
            if (config != null)
                return;
            config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(
                "Assets/Prefabs/Config/LocalizationConfig.asset");
            if (config == null)
            {
                var conf = ScriptableObject.CreateInstance<LocalizationConfig>();
                AssetDatabase.CreateAsset(conf, "Assets/Prefabs/Config/LocalizationConfig.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>(
                    "Assets/Prefabs/Config/LocalizationConfig.asset");
            }
            
            LoadWords();
        }

        [LabelText("语言"), ShowIf("@page==Page.语言设置")]
        public List<LanguageEncoding> languages = new List<LanguageEncoding>();

        private List<string> wordKeys = new List<string>();
        private List<LanguageWordItem> words = new List<LanguageWordItem>();
        //private Dictionary<SystemLanguage, LanguageWordItem> words = new Dictionary<SystemLanguage, LanguageWordItem>();

        [LabelText("大语言模型"), ShowIf("@page==Page.翻译设置"), BoxGroup("Setting"),
         InfoBox("<color=green>若电脑没有安装Ollama，无法使用翻译功能！如果使用请在电脑安装ollama软件，并且下载对应的大语言模型。</color>")]
        public string selectModel = "llama3";

        private int currentSelectedLanguage = 0;
        private Vector2 scrollPos;
        private bool translating;
        private bool progressing;

        [ShowIf("@page==Page.翻译设置"), BoxGroup("Setting"), Button("Ollama连接测试")]
        private void OnTestOllamaConnection()
        {
            Translate("你好", SystemLanguage.English, s => Debug.Log($"测试翻译结果: {s}"));
        }

        [OnInspectorGUI]
        private void OnGUI()
        {
            if (config == null)
                return;

            if (languages.Count == 0)
                return;
            //语言选项
            GUILayout.BeginHorizontal();
            {
                string[] texts = new string[languages.Count];
                for (int i = 0; i < languages.Count; i++)
                {
                    texts[i] = languages[i].Language.ToString();
                }

                currentSelectedLanguage = GUILayout.Toolbar(currentSelectedLanguage, texts);
            }
            GUILayout.EndHorizontal();
            if (words.Count != languages.Count)
            {
                if (words.Count < languages.Count)
                {
                    while (words.Count<languages.Count)
                    {
                        int index = words.Count;
                        words.Add(new LanguageWordItem(wordKeys));
                    }
                }
                else
                {
                    words.Clear();
                    foreach (var vLanguage in languages)
                    {
                        words.Add(new LanguageWordItem(wordKeys));
                    }
                }
            }

            words[currentSelectedLanguage].fontAsset =
                (TMP_FontAsset)EditorGUILayout.ObjectField(words[currentSelectedLanguage].fontAsset,
                    typeof(TMP_FontAsset));
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Words");
                if (GUILayout.Button("刷新", GUILayout.Width(50)))
                {
                    LoadWords();
                }
            }
            GUILayout.EndHorizontal();
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
            {
                for (int i = 0; i < wordKeys.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("图片翻译", GUILayout.Width(60));
                        words[currentSelectedLanguage].isImageFlags[i] =
                            EditorGUILayout.Toggle(words[currentSelectedLanguage].isImageFlags[i]);
                        wordKeys[i] = EditorGUILayout.TextField(wordKeys[i]);
                        GUILayout.Label("翻译", GUILayout.Width(30));
                        if (!words[currentSelectedLanguage].isImageFlags[i])
                        {
                            words[currentSelectedLanguage].values[i] = EditorGUILayout.TextField(words[currentSelectedLanguage].values[i]);
                        }
                        else
                        {
                            words[currentSelectedLanguage].spValues[i] =
                                (Sprite)EditorGUILayout.ObjectField(words[currentSelectedLanguage].spValues[i], typeof(Sprite));
                        }
                        foreach (var word in words)
                        {
                            word.keys[i] = wordKeys[i];
                        }

                        if (GUILayout.Button("翻译", GUILayout.Width(35)))
                        {
                            if (languages[currentSelectedLanguage].Language == SystemLanguage.English)
                            {
                                words[currentSelectedLanguage].values[i] = wordKeys[i];
                            }
                            else
                            {
                                int index = i;
                                Translate(wordKeys[i], languages[currentSelectedLanguage].Language, s => { words[currentSelectedLanguage].values[index] = s; });
                            }
                        }

                        if (GUILayout.Button("删除", GUILayout.Width(35)))
                        {
                            RemoveWordIndex(i);
                            i--;
                            Save();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            //操作
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("添加一条语句"))
                {
                    AddWord();
                    Save();
                }

                if (GUILayout.Button("删除最后一条"))
                {
                    RemoveWordIndex(wordKeys.Count - 1);
                    Save();
                }

                if (GUILayout.Button("保存配置"))
                {
                    // TODO
                    SaveToGameConfig();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void LoadWords()
        {
            int index = 0;
            wordKeys.Clear();
            foreach (var language in config.languageDic)
            {
                words[index].spValues.Clear();
                words[index].keys.Clear();
                words[index].values.Clear();
                words[index].isImageFlags.Clear();
                words[index].fontAsset = language.Value.fontAsset;
                foreach (var word in language.Value.words)
                {
                    if (index == 0)
                    {
                        wordKeys.Add(word.Key);
                    }
                    words[index].spValues.Add(word.Value.sprite);
                    words[index].values.Add(word.Value.text);
                    words[index].keys.Add(word.Key);
                    words[index].isImageFlags.Add(word.Value.Type == Pattern.PatternType.Text);
                }
                index++;
            }
        }

        private string GetLanguageName(SystemLanguage lang)
        {
            switch (lang)
            {
                case SystemLanguage.Chinese: return "中文";
                case SystemLanguage.ChineseSimplified: return "简体中文";
                case SystemLanguage.ChineseTraditional: return "繁體中文";
                case SystemLanguage.English: return "英文";
                case SystemLanguage.Japanese: return "日文";
                case SystemLanguage.Korean: return "韓文";
                case SystemLanguage.French: return "法文";
                case SystemLanguage.German: return "德文";
                case SystemLanguage.Spanish: return "西班牙文";
                default: return lang.ToString();
            }
        }

        public void Translate(string text, SystemLanguage language, Action<string> callback)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.Log("key不能为空");
                return;
            }

            string targetLanguage = GetLanguageName(language);
            var startInfo = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments =  $"run {selectModel} \"请翻译一段文字为{targetLanguage}，并且只回复我结果，文字：{text}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Debug.Log($"[输出] {output}");
                
                callback?.Invoke(CleanTranslation(output.Trim())); // 不要返回空内容
            }
        }

        private string CleanTranslation(string translation)
        {
            translation = translation.Trim();
            string[] prefixes = { "翻译结果：", "Translation:", "結果：" };
            foreach (var prefix in prefixes)
            {
                if (translation.StartsWith(prefix))
                {
                    translation = translation.Substring(prefix.Length).Trim();
                    break;
                }
            }
            return translation;
        }

        private void RemoveWordIndex(int index)
        {
            wordKeys.RemoveAt(index);
            foreach (var word in words)
            {
                word.keys.RemoveAt(index);
                word.values.RemoveAt(index);
                word.spValues.RemoveAt(index);
                word.isImageFlags.RemoveAt(index);
            }
        }

        private void AddWord()
        {
            wordKeys.Add("");
            foreach (var word in words)
            {
                word.keys.Add("");
                word.values.Add("");
                word.spValues.Add(null);
                word.isImageFlags.Add(false);
            }
        }

        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SaveToGameConfig()
        {
            config.languageDic.Clear();
            int count = words.Count;
            for (int i = 0; i < count; i++)
            {
                config.languageDic.Add(languages[i].Language, new LocalizationLanguage());
                config.languageDic[languages[i].Language].fontAsset = words[i].fontAsset;
                int wordCount = words[i].values.Count;
                for (int j = 0; j < wordCount; j++)
                {
                    config.languageDic[languages[i].Language].words.Add(wordKeys[j], new Pattern()
                    {
                        sprite = words[i].spValues[j],
                        text = words[i].values[j],
                        Type = words[i].isImageFlags[j] ? Pattern.PatternType.Image : Pattern.PatternType.Text
                    });
                }
            }
            
            EditorUtility.SetDirty(config);
            Save();
        }
    }

    [Serializable]
    public class LanguageEncoding
    {
        public SystemLanguage Language;
        public string encoding = "GBK";
    }

    [Serializable]
    public class LanguageWordItem
    {
        public TMP_FontAsset fontAsset;
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();
        public List<Sprite> spValues = new List<Sprite>();
        public List<bool> isImageFlags = new List<bool>();

        public LanguageWordItem(List<string> keys)
        {
            int count = keys.Count;
            for (int i = 0; i < count; i++)
            {
                this.keys.Add(keys[i]);
                values.Add("");
                spValues.Add(null);
                isImageFlags.Add(false);
            }
        }
    }
}
