using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BirdGame
{
    public class SettingPopup : UIBase
    {
        [DllImport("kernel32.dll")]
        private static extern void ExitProcess(int ExitCode);
        
        public TMP_Dropdown screenDropdown;
        public TMP_Dropdown volumeDropdown;
        public TMP_Dropdown languageDropdown;
        public Button quitButton;
        public Button tutorialButton;
        public Button clearSaveButton; // 添加清除存档按钮
        public Sprite itemSprite;
        public Sprite[] dropSps;
        public Button closeButton;
        [Header("喂食")]
        public Toggle autoFeedingToggle;
        public LocalizationText autoFeedingLabelText;

        private List<SystemLanguage> languages = new List<SystemLanguage>();
        private bool isScreenExpend = false;
        private bool isVloumeExpend = false;
        private bool isLanguageExpend = false;
        private float moveHeight = 0;
        private float deleteY;
        private float tutorailY;
        private float quitY;
        private float languageY;
        private float volumeY;
        private float autoFeedY;
        private RectTransform deleteRect;
        private RectTransform tutorialRect;
        private RectTransform quitRect;
        private RectTransform languageRect;
        private RectTransform volumeRect;
        private RectTransform autoFeedRect;
        private bool isChangingMode = false; // 防止键盘切换时触发onValueChanged导致循环
        private Canvas canvas;
        
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
                
                PlayerPrefs.DeleteAll();
                
                // 清空内存中的数据
                this.GetSystem<ISaveSystem>().InitData();
                this.GetModel<IAccountModel>().Coins.Value = this.GetModel<IConfigModel>().ShopConfig.startCoins;
                this.GetModel<ISaveModel>().SettingData.gameLanguage = this.GetSystem<ISteamSystem>().GetUserLanguage();
                
                // 应用默认屏幕模式（全屏模式）
                int defaultScreenMode = this.GetModel<ISaveModel>().SettingData.screenMode;
                if (defaultScreenMode >= 3)
                {
                    defaultScreenMode = 2; // 如果模式无效，使用全屏模式
                }
                SwitchScreenMode(defaultScreenMode);
                
                // // 清空鸟模型中的数据
                // var birdModel = this.GetModel<IBirdModel>();
                // if (birdModel != null)
                // {
                //     // 清理所有鸟的监听器
                //     this.GetSystem<IBirdSystem>().CleanupAllListeners();
                //     foreach (var birdItem in birdModel.BirdList)
                //     {
                //         GameObject.Destroy(birdItem.bird.gameObject);
                //     }
                //     // 清空鸟列表
                //     birdModel.BirdList.Clear();
                //     birdModel.UnopenEggs = 0;
                //     
                //     Debug.Log("鸟模型数据已清空！");
                // }

                
                this.SendCommand(new LoadMapCommand(0));
                this.GetSystem<IUISystem>().HidePopup(UIPopup.SettingPopup);
                
                //Debug.Log("所有存档文件已清除！内存数据已清空！程序即将重启...");

                // 显示成功消息
                //this.GetSystem<IUISystem>().ShowPrompt("存档已清除！程序即将重启。");

                // 等待一帧确保消息显示
                //this.GetSystem<IMonoSystem>().StartCoroutine(RestartApplication());
                var stopWatch = this.GetModel<IClockModel>().StopWatchItem;
                var timer = this.GetModel<IClockModel>().TimerItem;
                var tomato = this.GetModel<IClockModel>().TomatoItem;

                if(stopWatch.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(stopWatch.TimerCoroutine);
                    stopWatch.TimerCoroutine = null;
                    stopWatch.Hours.Value=0;
                    stopWatch.IsPause =false;
                    stopWatch.Minutes.Value = 0;
                    stopWatch.Seconds.Value = 0;
                }
                if(timer.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(timer.TimerCoroutine);
                    timer.TimerCoroutine = null;
                    timer.Hours.Value = 0;
                    timer.IsPause = false;
                    timer.Minutes.Value = 5;
                    timer.Seconds.Value =0;
                }
                if(tomato.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(tomato.TimerCoroutine);
                    tomato.TimerCoroutine = null;
                    tomato.IsPause=false;
                    tomato.IsSkip =false;
                    tomato.Number.Value= 1;
                    tomato.SessionMinutes.Value =5;
                    tomato.Timer.Value = 0;
                    tomato.BreakMinutes.Value =5;
                    tomato.TimerType.Value = TomatoTimerType.Session;
                }
                this.GetModel<IClockModel>().TimerType = TimerType.None;
                DOTween.Sequence().AppendCallback(() =>
                {
                    ClockPopup.barPos = new Vector2(10000, 10000);
                    ClockPopup.barScale = 0;
                    NotePopup.barPos = new Vector2(10000, 10000);
                    NotePopup.barScale = 0;
                    RadioPopup.barPos = new Vector2(10000, 10000);
                    RadioPopup.barScale = 0;
                }).SetDelay(0.5f);
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
            this.GetModel<IGameModel>().IsSettingOpen = true;
            deleteRect = clearSaveButton.GetComponent<RectTransform>();
            tutorialRect = tutorialButton.GetComponent<RectTransform>();
            quitRect = quitButton.GetComponent<RectTransform>();
            languageRect = languageDropdown.GetComponent<RectTransform>();
            volumeRect = volumeDropdown.GetComponent<RectTransform>();
            autoFeedRect = autoFeedingToggle.GetComponent<RectTransform>();

            deleteY = deleteRect.anchoredPosition.y;
            tutorailY = tutorialRect.anchoredPosition.y;
            quitY = quitRect.anchoredPosition.y;
            languageY = languageRect.anchoredPosition.y;
            volumeY = volumeRect.anchoredPosition.y;
            autoFeedY = autoFeedRect.anchoredPosition.y;
            
            tutorialButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().SendEvent<OnSettingCloseEvent>();
                this.GetSystem<IUISystem>().ShowPopup(UIPopup.TutorialPopup);
            });
            quitButton.onClick.AddListener(() =>
            {
                // 显示退出确认弹窗，询问是否填写问卷
                this.GetSystem<IUISystem>().ShowExitConfirm();
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().SendEvent<OnSettingCloseEvent>();
            });
            canvas = GetComponent<Canvas>();
            // 添加清除存档按钮的点击监听器
            if (clearSaveButton != null)
            {
                clearSaveButton.onClick.AddListener(() =>
                {
                    canvas.sortingOrder = 9;
                    this.GetSystem<IUISystem>().ShowConfirm("Are you sure you want to delete this save file?",
                        () =>
                        {
                            canvas.sortingOrder = 10;
                            this.GetModel<IBirdModel>().UnopenEggs = 0;
                            this.GetSystem<IUISystem>().HideMask();
                            this.GetSystem<IGameSystem>().SendEvent<DestroyEggEvent>();
                            this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
                            onClick(); // 调用清除存档功能
                        });
                });
            }
            
            // 初始化下拉菜单的默认值
            InitializeScreenDropdown();
            //初始化语言
            InitializeLanguageDropdown();
            
            volumeDropdown.ClearOptions();
            volumeDropdown.AddOptions(new List<TMP_Dropdown.OptionData>()
            {
                new TMP_Dropdown.OptionData("Effect Volume"),
            });
            
            screenDropdown.onValueChanged.AddListener(id =>
            {
                // 如果正在通过代码切换模式，跳过处理（避免循环触发）
                if (isChangingMode)
                    return;
                    
                SwitchScreenMode(id);
            });
            languageDropdown.onValueChanged.AddListener(index =>
            {
                this.GetSystem<ILocalizationSystem>().ChangeLanguage(languages[index]);
            });
            screenDropdown.GetComponent<Image>().sprite = dropSps[0];
            languageDropdown.GetComponent<Image>().sprite = dropSps[0];

            if (autoFeedingToggle != null)
            {
                autoFeedingToggle.isOn = this.GetModel<ISaveModel>().SettingData.autoFeeding;
                autoFeedingToggle.onValueChanged.AddListener(isOn =>
                {
                    this.GetModel<ISaveModel>().SettingData.autoFeeding = isOn;
                    RefreshAutoFeedingLabel(isOn);
                });
                RefreshAutoFeedingLabel(autoFeedingToggle.isOn);
            }

            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                if (autoFeedingToggle != null)
                    RefreshAutoFeedingLabel(autoFeedingToggle.isOn);
                screenDropdown.options[0].text = this.GetSystem<ILocalizationSystem>().GetString("Windowed");
                screenDropdown.options[1].text = this.GetSystem<ILocalizationSystem>().GetString("Wallpaper");
                screenDropdown.options[2].text = this.GetSystem<ILocalizationSystem>().GetString("Full Screen");
                screenDropdown.RefreshShownValue();
                int count = languages.Count;
                var currentLang = this.GetModel<ISaveModel>().SettingData.gameLanguage;
                for (int i = 0; i < count; i++)
                {
                    string langText = this.GetSystem<ILocalizationSystem>().GetString(languages[i].ToString());
                    if (languages[i] == SystemLanguage.ChineseSimplified && currentLang == SystemLanguage.ChineseSimplified)
                    {
                        langText = "中文";
                    }
                    languageDropdown.options[i].text = langText;
                }
                languageDropdown.RefreshShownValue();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            // 监听全局屏幕模式切换事件（用于键盘快捷键切换时更新UI）
            this.RegisterEvent<ChangeScreenModeEvent>(evt =>
            {
                if (screenDropdown != null && screenDropdown.value != evt.mode)
                {
                    isChangingMode = true; // 设置标志，防止触发onValueChanged
                    screenDropdown.value = evt.mode;
                    isChangingMode = false; // 重置标志
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void RefreshAutoFeedingLabel(bool isAutoFeeding)
        {
            if (autoFeedingLabelText == null) return;
            autoFeedingLabelText.SetKey(isAutoFeeding ? "AutoFeeding" : "ManualFeeding");
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
            
            if (!isVloumeExpend && volumeDropdown.IsExpanded)
            {
                isVloumeExpend = true;
            }
            else if (isVloumeExpend && !volumeDropdown.IsExpanded)
            {
                isVloumeExpend = false;
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
                volumeRect.anchoredPosition = new Vector2(0, volumeY - 204);
            }
            else
            {
                volumeRect.anchoredPosition = new Vector2(0, volumeY);
            }
            
            
            if (isVloumeExpend)
            {
                languageRect.anchoredPosition = new Vector2(0, languageY - 384 - (isScreenExpend ? 204 : 0));
            }
            else
            {
                languageRect.anchoredPosition = new Vector2(0, languageY- (isScreenExpend ? 204 : 0));
            }


            moveHeight = (isScreenExpend ? 204 : 0) + (isVloumeExpend? 384 : 0) + (isLanguageExpend ? 105 : 0);

            deleteRect.anchoredPosition = new Vector2(0, deleteY - moveHeight); 
            tutorialRect.anchoredPosition = new Vector2(0, tutorailY - moveHeight);
            quitRect.anchoredPosition = new Vector2(0, quitY - moveHeight);
            autoFeedRect.anchoredPosition = new Vector2(0, autoFeedY - moveHeight);
        }

        /// <summary>
        /// 切换屏幕模式（统一方法，供点击和键盘快捷键调用）
        /// </summary>
        /// <param name="mode">模式：0=窗口模式, 1=壁纸模式, 2=全屏模式</param>
        private void SwitchScreenMode(int mode)
        {
            // Clear keyboard state before mode change to prevent lingering key states
            SimpleMouseForwarder.ClearKeyboardState();
            
            switch (mode)
            {
                case 0:
                    this.GetUtility<IFullScreenUtility>().WindowedMode();
                    // Performance optimization: 45 FPS for windowed mode
                    // Application.targetFrameRate = 45;
                    // OnDemandRendering.renderFrameInterval = 1;
                    Debug.Log("WindowedMode (45 FPS)");
                    break;
                case 1:
                    this.GetUtility<IFullScreenUtility>().WallpaperMode();
                    if (this.GetUtility<IFullScreenUtility>().HasMultipleMonitors)
                        this.GetSystem<IUISystem>().ShowPrompt(this.GetSystem<ILocalizationSystem>().GetString("WallpaperSingleMonitorOnly"));
                    Debug.Log("WallpaperMode (60 FPS - 流畅光标)");
                    break;
                case 2:
                    this.GetUtility<IFullScreenUtility>().FullscreenMode();
                    // Performance optimization: 45 FPS for fullscreen mode
                    // Application.targetFrameRate = 45;
                    // OnDemandRendering.renderFrameInterval = 1;
                    Debug.Log("FullscreenMode (45 FPS)");
                    break;
            }
            
            // Clear keyboard state after mode change as well
            SimpleMouseForwarder.ClearKeyboardState();

            // 保存设置
            this.GetModel<ISaveModel>().SettingData.screenMode = mode;
            
            // 更新下拉菜单显示（如果是通过键盘切换）
            if (screenDropdown.value != mode)
            {
                isChangingMode = true; // 设置标志，防止触发onValueChanged
                screenDropdown.value = mode;
                isChangingMode = false; // 重置标志
            }
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
            
            // 设置下拉菜单值（使用标志防止触发onValueChanged）
            isChangingMode = true;
            screenDropdown.value = savedScreenMode;
            isChangingMode = false;
            
            // 应用屏幕模式（仅在初始化时应用，避免重复应用）
            // 注意：这里不调用SwitchScreenMode，因为GameEntry会在启动时应用屏幕模式
            // 但如果SettingPopup在游戏运行中打开，且屏幕模式不匹配，则需要应用
            // 为了安全，我们只在必要时应用（例如清除存档后）
        }

        private void InitializeLanguageDropdown()
        {
            languageDropdown.ClearOptions();
            var languageConfig = this.GetModel<IConfigModel>().LocalizationConfig;
            // foreach (var language in languageConfig.languageDic)
            // {
            //     string value = this.GetSystem<ILocalizationSystem>().GetString(language.Key.ToString());
            //     languageDropdown.options.Add(new TMP_Dropdown.OptionData(value, itemSprite, Color.white));
            //     languages.Add(language.Key);
            //     Debug.Log(value);
            // }
            var currentLanguage = this.GetModel<ISaveModel>().SettingData.gameLanguage;
            if (currentLanguage == SystemLanguage.Unknown)
            {
                currentLanguage = this.GetSystem<ISteamSystem>().GetUserLanguage();
            }

            if (currentLanguage != SystemLanguage.ChineseSimplified && currentLanguage != SystemLanguage.English)
            {
                currentLanguage = SystemLanguage.English;
            }

            string value = this.GetSystem<ILocalizationSystem>().GetString("English");
            languageDropdown.options.Add(new TMP_Dropdown.OptionData(value,itemSprite, Color.white));
            value = this.GetSystem<ILocalizationSystem>().GetString("Chinese");
            if (currentLanguage == SystemLanguage.ChineseSimplified)
            {
                value = "中文";
            }
            languageDropdown.options.Add(new TMP_Dropdown.OptionData(value, itemSprite, Color.white));
            languages.Add(SystemLanguage.English);
            languages.Add(SystemLanguage.ChineseSimplified);
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

        private void OnDestroy()
        {
            this.GetModel<IGameModel>().IsSettingOpen = false;
        }
    }
}