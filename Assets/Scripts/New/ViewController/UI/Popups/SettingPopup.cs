using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BirdGame
{
    public class SettingPopup : UIBase
    {
        [DllImport("kernel32.dll")]
        private static extern void ExitProcess(int ExitCode);
        
        public Button closeButton;
        public TMP_Dropdown screenDropdown;
        public TMP_Dropdown languageDropdown;
        public Button quitButton;
        public Button tutorialButton;
        public Button clearSaveButton; // 添加清除存档按钮
        public Sprite itemSprite;
        public Sprite[] dropSps;

        private List<SystemLanguage> languages = new List<SystemLanguage>();
        private bool isScreenExpend = false;
        private bool isLanguageExpend = false;
        private float moveHeight = 0;
        private float deleteY;
        private float tutorailY;
        private float quitY;
        private float languageY;
        private RectTransform deleteRect;
        private RectTransform tutorialRect;
        private RectTransform quitRect;
        private RectTransform languageRect;
        
        public void onClick()
        {
            // 显示确认对话框（使用简单的确认方式）
            Debug.Log("确认清除存档？这将删除所有游戏数据，包括鸟、金币、设置等。");
            this.GetSystem<ISteamSystem>().FirstPlayTime();
            // 直接执行清除操作（为了简化，暂时跳过确认对话框）
            ExecuteClearSave();
        }
        
        /// <summary>
        /// 执行清除存档操作
        /// </summary>
        private void ExecuteClearSave()
        {
            try
            {
                // 删除所有存档文件
                string[] saveFiles = {
                    "AccountData.save",
                    "BirdInfoData.save", 
                    "MusicSettingData.save",
                    "SettingData.save",
                    "NoteData.save",
                    "ScheduleData.save",
                    "IllustratedData.save",
                    "DecorationData.save"
                };
                
                string gameDataPath = Application.persistentDataPath + "/GameData/";
                
                foreach (string fileName in saveFiles)
                {
                    string filePath = gameDataPath + fileName;
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Debug.Log($"已删除存档文件: {fileName}");
                    }
                }
                
                // 清空内存中的数据
                var saveModel = this.GetModel<ISaveModel>();
                if (saveModel != null)
                {
                    saveModel.AccountData = new AccountData();
                    saveModel.BirdInfoData = new BirdInfoData();
                    saveModel.MusicSettingData = new MusicSettingData();
                    saveModel.SettingData = new SettingData();
                    saveModel.NoteData = new NoteData();
                    saveModel.ScheduleData = new ScheduleData();
                    saveModel.IllustratedData = new IllustratedData();
                }
                
                // 清空鸟模型中的数据
                var birdModel = this.GetModel<IBirdModel>();
                if (birdModel != null)
                {
                    // 清理所有鸟的监听器
                    this.GetSystem<IBirdSystem>().CleanupAllListeners();
                    
                    // 清空鸟列表
                    birdModel.BirdList.Clear();
                    birdModel.UnopenEggs = 0;
                    
                    Debug.Log("鸟模型数据已清空！");
                }
                
                Debug.Log("所有存档文件已清除！内存数据已清空！程序即将重启...");
                
                // 显示成功消息
                //this.GetSystem<IUISystem>().ShowPrompt("存档已清除！程序即将重启。");
                
                // 等待一帧确保消息显示
                this.GetSystem<IMonoSystem>().StartCoroutine(RestartApplication());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"清除存档失败: {e.Message}");
                //this.GetSystem<IUISystem>().ShowPrompt("清除失败！清除存档时发生错误，请重试。");
            }
        }
        
        /// <summary>
        /// 重启应用程序
        /// </summary>
        private System.Collections.IEnumerator RestartApplication()
        {
            // 等待一帧确保UI消息显示
            yield return new WaitForSeconds(1f);
            
            // 关闭设置弹窗
            this.GetSystem<IUISystem>().HidePopup(UIPopup.SettingPopup);
            
            // 等待UI关闭
            yield return new WaitForSeconds(0.5f);
            
            // 重启应用程序
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                yield return new WaitForSeconds(0.1f);
                UnityEditor.EditorApplication.isPlaying = true;
            #else
                // 在构建版本中重启应用
                ExitProcess(0);
                #if UNITY_STANDALONE_WIN
                    System.Diagnostics.Process.Start(Application.dataPath.Replace("_Data", ".exe"));
                #elif UNITY_STANDALONE_OSX
                    System.Diagnostics.Process.Start(Application.dataPath.Replace(".app/Contents", ".app"));
                #elif UNITY_STANDALONE_LINUX
                    System.Diagnostics.Process.Start(Application.dataPath.Replace("_Data", ".x86_64"));
                #endif
            #endif
        }

        private void Start()
        {
            deleteRect = clearSaveButton.GetComponent<RectTransform>();
            tutorialRect = tutorialButton.GetComponent<RectTransform>();
            quitRect = quitButton.GetComponent<RectTransform>();
            languageRect = languageDropdown.GetComponent<RectTransform>();

            deleteY = deleteRect.anchoredPosition.y;
            tutorailY = tutorialRect.anchoredPosition.y;
            quitY = quitRect.anchoredPosition.y;
            languageY = languageRect.anchoredPosition.y;
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.SettingPopup);
            });
            tutorialButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().ShowPopup(UIPopup.TutorialPopup);
            });
            quitButton.onClick.AddListener(() =>
            {
                this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
                this.GetSystem<ISteamSystem>().FirstPlayTime();
                ExitProcess(0);
            });
            
            // 添加清除存档按钮的点击监听器
            if (clearSaveButton != null)
            {
                clearSaveButton.onClick.AddListener(() =>
                {
                    this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
                    onClick(); // 调用清除存档功能
                });
            }
            
            // 初始化下拉菜单的默认值
            InitializeScreenDropdown();
            //初始化语言
            InitializeLanguageDropdown();
            
            screenDropdown.onValueChanged.AddListener(id =>
            {
                if (id == 0)
                {
                    this.GetUtility<IFullScreenUtility>().WindowedMode();
                    Debug.Log("WindowedMode");
                }
                else if (id == 1)
                {
                    // this.GetUtility<IFullScreenUtility>().WallpaperMode();
                    //暂时关闭壁纸模式
                  // WallpaperModeController.ins.EnterWallpaperMode();
                    Debug.Log("WallpaperMode");
                }
                else if (id == 2)
                {
                    this.GetUtility<IFullScreenUtility>().FullscreenMode();
                    Debug.Log("FullscreenMode");
                }

                this.GetModel<ISaveModel>().SettingData.screenMode = id;
            });
            languageDropdown.onValueChanged.AddListener(index =>
            {
                this.GetSystem<ILocalizationSystem>().ChangeLanguage(languages[index]);
            });
            screenDropdown.GetComponent<Image>().sprite = dropSps[0];
            languageDropdown.GetComponent<Image>().sprite = dropSps[0];

            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                screenDropdown.options[0].text = this.GetSystem<ILocalizationSystem>().GetString("Windowed");
                screenDropdown.options[1].text = this.GetSystem<ILocalizationSystem>().GetString("Wallpaper");
                screenDropdown.options[2].text = this.GetSystem<ILocalizationSystem>().GetString("Full Screen");
                screenDropdown.RefreshShownValue();
                int count = languages.Count;
                for (int i = 0; i < count; i++)
                {
                    languageDropdown.options[i].text =
                        this.GetSystem<ILocalizationSystem>().GetString(languages[i].ToString());
                }
                languageDropdown.RefreshShownValue();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            moveHeight = 0;
            
            if (!isScreenExpend && screenDropdown.IsExpanded)
            {
                isScreenExpend = true;
                screenDropdown.GetComponent<Image>().sprite = dropSps[1];
            }
            else if (isScreenExpend && !screenDropdown.IsExpanded)
            {
                isScreenExpend = false;
                screenDropdown.GetComponent<Image>().sprite = dropSps[0];
            }

            if (!isLanguageExpend && languageDropdown.IsExpanded)
            {
                isLanguageExpend = true;
                languageDropdown.GetComponent<Image>().sprite = dropSps[1];
            }
            else if (isLanguageExpend && !languageDropdown.IsExpanded)
            {
                isLanguageExpend = false;
                languageDropdown.GetComponent<Image>().sprite = dropSps[0];
            }

            if (isScreenExpend)
            {
                languageRect.anchoredPosition = new Vector2(0, languageY - 204);
            }
            else
            {
                languageRect.anchoredPosition = new Vector2(0, languageY);
            }

            moveHeight = (isScreenExpend ? 204 : 0) + (isLanguageExpend ? 570 : 0);

            deleteRect.anchoredPosition = new Vector2(0, deleteY - moveHeight);
            tutorialRect.anchoredPosition = new Vector2(0, tutorailY - moveHeight);
            quitRect.anchoredPosition = new Vector2(0, quitY - moveHeight);
        }

        private void InitializeScreenDropdown()
        {
            // 确保下拉菜单选项正确
            screenDropdown.options.Clear();
            string value = this.GetSystem<ILocalizationSystem>().GetString("Windowed");
            screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData()
            {
                text = value,
                image = itemSprite,
                color = Color.white
            });
            value = this.GetSystem<ILocalizationSystem>().GetString("Wallpaper");
            screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData()
            {
                text = value,
                image = itemSprite
            });
            value = this.GetSystem<ILocalizationSystem>().GetString("Full Screen");
            screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData()
            {
                text = value,
                image = itemSprite
            });
            // 从保存的设置中加载屏幕模式，如果没有保存过则默认为全屏模式
            int savedScreenMode = this.GetModel<ISaveModel>().SettingData.screenMode;
            // 如果保存的模式是Desktop(3)，则改为全屏模式(2)
            if (savedScreenMode >= 3)
            {
                savedScreenMode = 2;
            }
            screenDropdown.value = savedScreenMode;
        }

        private void InitializeLanguageDropdown()
        {
            languageDropdown.ClearOptions();
            var languageConfig = this.GetModel<IConfigModel>().LocalizationConfig;
            foreach (var language in languageConfig.languageDic)
            {
                string value = this.GetSystem<ILocalizationSystem>().GetString(language.Key.ToString());
                languageDropdown.options.Add(new TMP_Dropdown.OptionData(value, itemSprite, Color.white));
                languages.Add(language.Key);
                Debug.Log(value);
            }

            var currentLanguage = this.GetModel<ISaveModel>().SettingData.gameLanguage;
            Debug.Log($"存档中的语言设置: {currentLanguage}");
            Debug.Log($"本地化配置支持的语言: {string.Join(", ", languages)}");
            
            if (!languages.Contains(currentLanguage))
            {
                languageDropdown.value = 0;
                this.GetSystem<ILocalizationSystem>().ChangeLanguage(languages[0]);
                Debug.Log($"存档中选中的语言 {currentLanguage} 不在本地化配置列表中！强制设置为 {languages[0]}");
                return;
            }
            int index = languages.IndexOf(currentLanguage);
            languageDropdown.value = index;
        }
    }
}