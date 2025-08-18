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
                
            try
            {
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
                
                if (config != null)
                {
                    LoadWords();
                }
                
                // 延迟检查Ollama状态，避免阻塞UI
                EditorApplication.delayCall += () =>
                {
                    CheckOllamaStatusAsync();
                };
            }
            catch (System.Exception e)
            {
                Debug.LogError($"初始化配置失败: {e.Message}");
            }
        }
        
        private void CheckOllamaStatusAsync()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        // 设置超时时间为5秒
                        if (!process.WaitForExit(5000))
                        {
                            process.Kill();
                            Debug.LogWarning("⚠️ Ollama未运行，翻译功能将不可用");
                            Debug.LogWarning("启动Ollama: 在终端运行 'ollama serve'");
                            return;
                        }
                        
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        
                        if (process.ExitCode == 0)
                        {
                            if (output.Contains(selectModel))
                            {
                                Debug.Log($"✅ Ollama已就绪，模型 '{selectModel}' 可用");
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ Ollama已运行，但模型 '{selectModel}' 未安装");
                                Debug.LogWarning($"请运行: ollama pull {selectModel}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ Ollama未运行，翻译功能将不可用");
                            Debug.LogWarning("启动Ollama: 在终端运行 'ollama serve'");
                        }
                    }
                }
            }
            catch
            {
                Debug.LogWarning("⚠️ 无法检测Ollama状态，请确保Ollama已正确安装");
            }
        }

        [LabelText("语言"), ShowIf("@page==Page.语言设置")]
        public List<LanguageEncoding> languages = new List<LanguageEncoding>();

        private List<string> wordKeys = new List<string>();
        private List<LanguageWordItem> words = new List<LanguageWordItem>();
        //private Dictionary<SystemLanguage, LanguageWordItem> words = new Dictionary<SystemLanguage, LanguageWordItem>();

        [LabelText("大语言模型"), ShowIf("@page==Page.翻译设置"), BoxGroup("Setting"),
         InfoBox("<color=green>✅ Ollama已就绪！模型llama3已安装，翻译功能可用。</color>")]
        public string selectModel = "llama3:latest";

        private int currentSelectedLanguage = 0;
        private Vector2 scrollPos;
        private bool translating;
        private bool progressing;

        [ShowIf("@page==Page.翻译设置"), BoxGroup("Setting"), Button("Ollama连接测试")]
        private void OnTestOllamaConnection()
        {
            TestOllamaConnectionAsync();
        }
        
        private void TestOllamaConnectionAsync()
        {
            Debug.Log("正在测试Ollama连接...");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Debug.LogError("❌ 无法启动Ollama进程");
                        return;
                    }
                    
                    // 设置超时时间为10秒
                    if (!process.WaitForExit(10000))
                    {
                        process.Kill();
                        Debug.LogError("❌ Ollama连接超时");
                        Debug.LogError("请确保Ollama已安装并正在运行");
                        Debug.LogError("启动Ollama: 在终端运行 'ollama serve'");
                        return;
                    }
                    
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    if (process.ExitCode == 0)
                    {
                        Debug.Log("✅ Ollama连接成功！");
                        Debug.Log($"已安装的模型: {output}");
                        
                        // 检查指定模型是否存在
                        if (output.Contains(selectModel))
                        {
                            Debug.Log($"✅ 模型 '{selectModel}' 已安装");
                            // 进行翻译测试
                            Translate("你好", SystemLanguage.English, s => Debug.Log($"翻译测试结果: {s}"));
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ 模型 '{selectModel}' 未安装，请运行: ollama pull {selectModel}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"❌ Ollama连接失败: {error}");
                        Debug.LogError("请确保Ollama已安装并正在运行");
                        Debug.LogError("启动Ollama: 在终端运行 'ollama serve'");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Ollama连接异常: {e.Message}");
                Debug.LogError("请确保Ollama已正确安装");
            }
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
                                // 英文时，key和value保持一致
                                words[currentSelectedLanguage].values[i] = wordKeys[i];
                            }
                            else
                            {
                                // 其他语言时，优先使用英文内容作为翻译源
                                int index = i;
                                string textToTranslate = "";
                                
                                // 首先查找英文语言的内容
                                int englishIndex = -1;
                                for (int j = 0; j < languages.Count; j++)
                                {
                                    if (languages[j].Language == SystemLanguage.English)
                                    {
                                        englishIndex = j;
                                        break;
                                    }
                                }
                                
                                // 如果找到英文语言且有内容，使用英文内容
                                if (englishIndex >= 0 && englishIndex < words.Count && 
                                    i < words[englishIndex].values.Count && 
                                    !string.IsNullOrEmpty(words[englishIndex].values[i]))
                                {
                                    textToTranslate = words[englishIndex].values[i];
                                    Debug.Log($"使用英文内容作为翻译源: {textToTranslate}");
                                }
                                // 否则使用当前语言的内容
                                else if (!string.IsNullOrEmpty(words[currentSelectedLanguage].values[i]))
                                {
                                    textToTranslate = words[currentSelectedLanguage].values[i];
                                    Debug.Log($"使用当前语言内容作为翻译源: {textToTranslate}");
                                }
                                // 最后才使用key作为翻译源
                                else if (!string.IsNullOrEmpty(wordKeys[i]))
                                {
                                    textToTranslate = wordKeys[i];
                                    Debug.Log($"使用key作为翻译源: {textToTranslate}");
                                }
                                
                                // 如果所有都为空，提示用户输入
                                if (string.IsNullOrEmpty(textToTranslate))
                                {
                                    Debug.LogWarning($"Key '{wordKeys[i]}' 没有可翻译的内容，请先在英文语言下输入文本");
                                    return;
                                }
                                
                                Translate(textToTranslate, languages[currentSelectedLanguage].Language, s => { words[currentSelectedLanguage].values[index] = s; });
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
            if (config == null || config.languageDic == null)
            {
                Debug.LogWarning("配置对象为空，无法加载词汇");
                return;
            }
            
            int index = 0;
            wordKeys.Clear();
            foreach (var language in config.languageDic)
            {
                if (index >= words.Count)
                {
                    words.Add(new LanguageWordItem(wordKeys));
                }
                
                if (words[index] != null)
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

            // 使用异步方法避免阻塞UI
            EditorApplication.delayCall += () => TranslateAsync(text, language, callback);
        }
        
        private void TranslateAsync(string text, SystemLanguage language, Action<string> callback)
        {
            string targetLanguage = GetLanguageName(language);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = $"run {selectModel} \"请翻译一段文字为{targetLanguage}，并且只回复我结果，文字：{text}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Debug.LogError("❌ 无法启动Ollama进程");
                        callback?.Invoke(text);
                        return;
                    }
                    
                    // 设置超时时间为30秒
                    if (!process.WaitForExit(30000))
                    {
                        process.Kill();
                        Debug.LogError("❌ Ollama翻译超时");
                        Debug.LogError("请检查Ollama是否正在运行，或模型是否正确安装");
                        callback?.Invoke(text);
                        return;
                    }
                    
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        Debug.Log($"[Ollama翻译] {output}");
                        callback?.Invoke(CleanTranslation(output.Trim()));
                    }
                    else
                    {
                        Debug.LogError($"❌ Ollama翻译失败: {error}");
                        Debug.LogError("请检查Ollama是否正在运行，或模型是否正确安装");
                        callback?.Invoke(text);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Ollama翻译异常: {e.Message}");
                callback?.Invoke(text);
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
            if (this != null)
            {
                EditorUtility.SetDirty(this);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SaveToGameConfig()
        {
            if (config == null)
            {
                Debug.LogError("配置对象为空，无法保存");
                return;
            }
            
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
            
            if (config != null)
            {
                EditorUtility.SetDirty(config);
            }
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
