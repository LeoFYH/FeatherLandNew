using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Net;
using System.IO;
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
            翻译设置,
            Excel设置
        }

        [Title("本地化配置", Bold = true)] [HorizontalGroup("config"), ReadOnly, OnInspectorInit("InitConfig")]
        public LocalizationConfig config;

        [EnumToggleButtons, HideLabel] public Page page;

        private string searchingString = "";

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

        [LabelText("翻译服务"), ShowIf("@page==Page.翻译设置"), BoxGroup("Setting")]
        public TranslationService translationService = TranslationService.Ollama;
        
        [LabelText("ChatGPT API Key"), ShowIf("@translationService==TranslationService.ChatGPT&&page==Page.翻译设置"), BoxGroup("Setting")]
        public string chatGPTApiKey = "";
        
        [LabelText("Ollama模型"), ShowIf("@translationService==TranslationService.Ollama&&page==Page.翻译设置"), BoxGroup("Setting")]
        public string selectModel = "llama3:latest";
        
        public enum TranslationService
        {
            ChatGPT,
            Ollama
        }

        private int currentSelectedLanguage = 0;
        private Vector2 scrollPos;
        private bool translating;
        private bool progressing;
        private bool isTranslatingAll = false;

        [ShowIf("@page==Page.翻译设置"), BoxGroup("Setting"), Button("翻译服务连接测试")]
        private void OnTestTranslationConnection()
        {
            if (translationService == TranslationService.ChatGPT)
            {
                TestChatGPTConnectionAsync();
            }
            else
            {
                TestOllamaConnectionAsync();
            }
        }
        
        private void TestChatGPTConnectionAsync()
        {
            if (string.IsNullOrEmpty(chatGPTApiKey))
            {
                Debug.LogError("❌ ChatGPT API Key 未设置");
                Debug.LogError("请在设置中输入你的ChatGPT API Key");
                return;
            }

            // 验证API Key格式
            if (!chatGPTApiKey.StartsWith("sk-"))
            {
                Debug.LogError("❌ ChatGPT API Key 格式错误");
                Debug.LogError("API Key 应该以 'sk-' 开头");
                return;
            }

            Debug.Log("正在测试ChatGPT连接...");
            Debug.Log($"API Key 格式验证通过: {chatGPTApiKey.Substring(0, 10)}...");
            
            // 简单的连接测试
            Translate("Hello", SystemLanguage.Chinese, s => 
            {
                if (s != "Hello")
                {
                    Debug.Log("✅ ChatGPT连接成功！");
                    Debug.Log($"翻译测试结果: {s}");
                }
                else
                {
                    Debug.LogError("❌ ChatGPT连接失败");
                }
            });
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

        [ShowIf("@page==Page.Excel设置"), BoxGroup("Excel"), FolderPath(ParentFolder = "Assets/Scripts/New/Editor/Excels", RequireExistingPath = true, AbsolutePath = true)]
        public string excelPath;

        [ShowIf("@page==Page.Excel设置&&!string.IsNullOrEmpty(excelPath)"), BoxGroup("Excel"), Button("导出Excel")]
        private void OnExportExcel()
        {
            List<string[]> rowData = new List<string[]>();
            rowData.Add(new string[] { "key", "英文", "简体中文" });
            int wordCount = wordKeys.Count;
            if (wordCount == 0)
            {
                EditorUtility.DisplayDialog("错误", "language数据未加载！", "ok");
                return;
            }

            for (int i = 0; i < wordCount; i++)
            {
                rowData.Add(new string[]{words[0].keys[i], words[0].values[i], words[1].values[i]});
            }

            StringBuilder sb = new StringBuilder();
        
            foreach (string[] row in rowData)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    // 处理包含逗号或引号的内容
                    string cell = row[i];
                    if (cell.Contains(",") || cell.Contains("\"") || cell.Contains("\n"))
                    {
                        cell = $"\"{cell.Replace("\"", "\"\"")}\"";
                    }
                    sb.Append(cell);
                
                    if (i < row.Length - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.AppendLine();
            }
        
            // 保存文件
            string filePath = Path.Combine(excelPath, "data.csv");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
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

                currentSelectedLanguage = GUILayout.Toolbar(currentSelectedLanguage, texts, GUILayout.Width(600));
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
                searchingString = GUILayout.TextField(searchingString);
                if (GUILayout.Button("刷新", GUILayout.Width(50)))
                {
                    LoadWords();
                }
                
                // 一键翻译按钮
                if (isTranslatingAll)
                {
                    GUILayout.Label("翻译中...", GUILayout.Width(80));
                }
                else if (GUILayout.Button("一键翻译", GUILayout.Width(80)))
                {
                    TranslateAllKeys();
                }
            }
            GUILayout.EndHorizontal();
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);
            {
                for (int i = 0; i < wordKeys.Count; i++)
                {
                    if (!words[currentSelectedLanguage].keys[i].ToLower().Contains(searchingString.ToLower()))
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal();
                    {
                        wordKeys[i] = EditorGUILayout.TextField(wordKeys[i]);
                        GUILayout.Label("翻译", GUILayout.Width(30));
                        // if (!words[currentSelectedLanguage].isImageFlags[i])
                        // {
                            words[currentSelectedLanguage].values[i] = EditorGUILayout.TextField(words[currentSelectedLanguage].values[i]);
                        // }
                        // else
                        // {
                        //     words[currentSelectedLanguage].spValues[i] =
                        //         (Sprite)EditorGUILayout.ObjectField(words[currentSelectedLanguage].spValues[i], typeof(Sprite));
                        // }
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
                        //words[index].spValues.Add(word.Value.sprite);
                        words[index].values.Add(word.Value.text);
                        words[index].keys.Add(word.Key);
                        //words[index].isImageFlags.Add(word.Value.Type == Pattern.PatternType.Text);
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

            // 根据选择的翻译服务调用不同的方法
            if (translationService == TranslationService.ChatGPT)
            {
                EditorApplication.delayCall += () => TranslateWithChatGPTAsync(text, language, callback);
            }
            else
            {
                EditorApplication.delayCall += () => TranslateWithOllamaAsync(text, language, callback);
            }
        }
        
        private void TranslateWithOllamaAsync(string text, SystemLanguage language, Action<string> callback)
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

        private void TranslateWithChatGPTAsync(string text, SystemLanguage language, Action<string> callback)
        {
            if (string.IsNullOrEmpty(chatGPTApiKey))
            {
                Debug.LogError("❌ ChatGPT API Key 未设置");
                callback?.Invoke(text);
                return;
            }

            // 验证API Key格式
            if (!chatGPTApiKey.StartsWith("sk-"))
            {
                Debug.LogError("❌ ChatGPT API Key 格式错误");
                Debug.LogError("API Key 应该以 'sk-' 开头");
                callback?.Invoke(text);
                return;
            }

            string targetLanguage = GetLanguageName(language);
            string prompt = $"请将以下文字翻译为{targetLanguage}，只返回翻译结果，不要其他内容：{text}";

            // 手动构建JSON，因为Unity的JsonUtility不支持匿名类型
            string jsonData = $@"{{
                ""model"": ""gpt-3.5-turbo"",
                ""messages"": [
                    {{
                        ""role"": ""user"",
                        ""content"": ""{prompt.Replace("\"", "\\\"")}""
                    }}
                ],
                ""max_tokens"": 1000,
                ""temperature"": 0.3
            }}";

            byte[] data = Encoding.UTF8.GetBytes(jsonData);

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://api.openai.com/v1/chat/completions");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", $"Bearer {chatGPTApiKey}");
                request.ContentLength = data.Length;
                request.Timeout = 30000; // 30秒超时

                Debug.Log($"[ChatGPT请求] 发送翻译请求: {text} -> {targetLanguage}");

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string responseText = reader.ReadToEnd();
                    Debug.Log($"[ChatGPT响应] {responseText}");
                    
                    var responseData = JsonUtility.FromJson<ChatGPTResponse>(responseText);
                    
                    if (responseData != null && responseData.choices != null && responseData.choices.Length > 0)
                    {
                        string translation = responseData.choices[0].message.content.Trim();
                        Debug.Log($"[ChatGPT翻译] {translation}");
                        callback?.Invoke(CleanTranslation(translation));
                    }
                    else
                    {
                        Debug.LogError("❌ ChatGPT翻译失败：响应格式错误");
                        Debug.LogError($"响应内容: {responseText}");
                        callback?.Invoke(text);
                    }
                }
            }
            catch (WebException e)
            {
                Debug.LogError($"❌ ChatGPT翻译网络错误: {e.Message}");
                if (e.Response != null)
                {
                    using (var reader = new StreamReader(e.Response.GetResponseStream()))
                    {
                        string errorResponse = reader.ReadToEnd();
                        Debug.LogError($"错误详情: {errorResponse}");
                        
                        // 检查是否是配额不足错误
                        if (errorResponse.Contains("insufficient_quota") || errorResponse.Contains("quota"))
                        {
                            Debug.LogError("💡 解决方案：");
                            Debug.LogError("1. 访问 https://platform.openai.com/account/billing 检查余额");
                            Debug.LogError("2. 添加支付方式充值");
                            Debug.LogError("3. 或切换到Ollama翻译服务");
                        }
                    }
                }
                callback?.Invoke(text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ ChatGPT翻译异常: {e.Message}");
                callback?.Invoke(text);
            }
        }

        [System.Serializable]
        private class ChatGPTResponse
        {
            public Choice[] choices;
            public string id;
            public string object_type;
            public long created;
            public string model;
            public Usage usage;
        }

        [System.Serializable]
        private class Choice
        {
            public Message message;
            public string finish_reason;
            public int index;
        }

        [System.Serializable]
        private class Message
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        private class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
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
        
        private void TranslateAllKeys()
        {
            if (languages[currentSelectedLanguage].Language == SystemLanguage.English)
            {
                Debug.LogWarning("英文语言不需要翻译，请选择其他语言");
                return;
            }
            
            if (wordKeys.Count == 0)
            {
                Debug.LogWarning("没有可翻译的内容");
                return;
            }
            
            Debug.Log($"开始一键翻译 {wordKeys.Count} 个条目...");
            isTranslatingAll = true;
            
            // 查找英文语言索引
            int englishIndex = -1;
            for (int j = 0; j < languages.Count; j++)
            {
                if (languages[j].Language == SystemLanguage.English)
                {
                    englishIndex = j;
                    break;
                }
            }
            
            if (englishIndex == -1)
            {
                Debug.LogError("未找到英文语言，无法进行翻译");
                isTranslatingAll = false;
                return;
            }
            
            // 批量翻译
            int totalToTranslate = 0;
            int translatedCount = 0;
            
            // 先计算需要翻译的数量
            for (int i = 0; i < wordKeys.Count; i++)
            {
                if (!words[currentSelectedLanguage].isImageFlags[i] && 
                    string.IsNullOrEmpty(words[currentSelectedLanguage].values[i]))
                {
                    totalToTranslate++;
                }
            }
            
            if (totalToTranslate == 0)
            {
                Debug.Log("所有条目都已翻译完成或无需翻译");
                isTranslatingAll = false;
                return;
            }
            
            Debug.Log($"需要翻译 {totalToTranslate} 个条目...");
            
            for (int i = 0; i < wordKeys.Count; i++)
            {
                // 跳过图片翻译
                if (words[currentSelectedLanguage].isImageFlags[i])
                {
                    continue;
                }
                
                // 如果当前语言已有内容，跳过
                if (!string.IsNullOrEmpty(words[currentSelectedLanguage].values[i]))
                {
                    continue;
                }
                
                string textToTranslate = "";
                
                // 优先使用英文内容
                if (englishIndex < words.Count && i < words[englishIndex].values.Count && 
                    !string.IsNullOrEmpty(words[englishIndex].values[i]))
                {
                    textToTranslate = words[englishIndex].values[i];
                }
                // 否则使用key
                else if (!string.IsNullOrEmpty(wordKeys[i]))
                {
                    textToTranslate = wordKeys[i];
                }
                
                if (!string.IsNullOrEmpty(textToTranslate))
                {
                    int index = i; // 捕获循环变量
                    Translate(textToTranslate, languages[currentSelectedLanguage].Language, s => 
                    {
                        words[currentSelectedLanguage].values[index] = s;
                        translatedCount++;
                        Debug.Log($"翻译完成 ({translatedCount}/{totalToTranslate}): {textToTranslate} -> {s}");
                        
                        // 当所有翻译完成时保存
                        if (translatedCount >= totalToTranslate)
                        {
                            Save();
                            Debug.Log("✅ 一键翻译完成！");
                            isTranslatingAll = false;
                        }
                    });
                }
            }
            
            if (translatedCount == 0)
            {
                Debug.Log("所有条目都已翻译完成或无需翻译");
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
                        //sprite = words[i].spValues[j],
                        text = words[i].values[j],
                        //Type = words[i].isImageFlags[j] ? Pattern.PatternType.Image : Pattern.PatternType.Text
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
