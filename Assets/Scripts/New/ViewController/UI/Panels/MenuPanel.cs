using System;
using System.Globalization;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BirdGame
{
    public class MenuPanel : UIBase
    {
        public Button weatherButton;

        public Image weatherImage;
        //public Sprite[] weatherSps;
        public Toggle noteToggle;
        public Toggle radioToggle;
        public Toggle settingButton;
        public Toggle shopButton;
        public Toggle clockToggle;
        public Toggle telescopeToggle;
        public Toggle cameraToggle;
        public Toggle illustratedButton;
        public Toggle mapButton;
        public Button externalLinkButton; // 新增外部链接按钮
        public TextMeshProUGUI coinsNum;
        public RectTransform timeItem;
        public TextMeshProUGUI timeText;
        public GameObject[] weatherItems;
        public RectTransform content;
        public Button debugButton;
        public GameObject viewGroup;
        public Toggle viewToggle;
        public RectTransform itemRect;

        private Sequence anim;
        private Tweener timeAnim;
        private bool isShowWeatherItems = false;
        private Tweener contentAnim;
        private bool showDebugButton;

        private float shopPosX;
        private float illustratedPosX;
        private float weatherPosX;
        private float mapPosX;
        private float currentCoins;
        private float originalCoinFontSize = -1f;
        
        private bool isSyncingShopButton = false; // 标志位：正在同步 shopButton 状态，避免重复调用 HidePopup
        private bool isSyncingIllustratedButton = false; // 标志位：正在同步 illustratedButton 状态
        private bool isSyncingMapButton = false; // 标志位：正在同步 mapButton 状态
        private bool isSyncingSettingButton = false; // 标志位：正在同步 settingButton 状态
        
        public override void OnShowPanel()
        {
            
        }

        public override void OnHidePanel(Action onComplete = null)
        {
            Destroy(gameObject);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 把其他工具 toggle 视觉状态同步为 off（不触发它们各自的 HidePopup 回调，
        /// 因为 popup 已经被 HideAllPopups 关掉了）。开启相机模式时调用。
        /// </summary>
        private void SyncOtherTogglesOff()
        {
            if (shopButton != null) { isSyncingShopButton = true; shopButton.isOn = false; isSyncingShopButton = false; }
            if (illustratedButton != null) { isSyncingIllustratedButton = true; illustratedButton.isOn = false; isSyncingIllustratedButton = false; }
            if (mapButton != null) { isSyncingMapButton = true; mapButton.isOn = false; isSyncingMapButton = false; }
            if (settingButton != null) { isSyncingSettingButton = true; settingButton.isOn = false; isSyncingSettingButton = false; }
            // 这几个没有sync flag，但其HidePopup回调对已关闭的popup是no-op，安全
            if (noteToggle != null) noteToggle.isOn = false;
            if (radioToggle != null) radioToggle.isOn = false;
            if (clockToggle != null) clockToggle.isOn = false;
        }

        /// <summary> 关闭与 Tutorial 互斥的 4 个弹窗并同步 Toggle（打开 Tutorial 前调用） </summary>
        public void CloseMutualPopupsForTutorial()
        {
            var ui = this.GetSystem<IUISystem>();
            if (ui == null) return;
            ui.HidePopup(UIPopup.ShopPopup);
            ui.HidePopup(UIPopup.IllustratedPopup);
            ui.HidePopup(UIPopup.MapPopup);
            ui.HidePopup(UIPopup.SettingPopup);
            isSyncingShopButton = true;
            shopButton.isOn = false;
            isSyncingShopButton = false;
            isSyncingIllustratedButton = true;
            illustratedButton.isOn = false;
            isSyncingIllustratedButton = false;
            isSyncingMapButton = true;
            mapButton.isOn = false;
            isSyncingMapButton = false;
            isSyncingSettingButton = true;
            settingButton.isOn = false;
            isSyncingSettingButton = false;
        }

        private void Start()
        {
            shopPosX = shopButton.GetComponent<RectTransform>().anchoredPosition.x;
            illustratedPosX = illustratedButton.GetComponent<RectTransform>().anchoredPosition.x;
            weatherPosX = weatherButton.GetComponent<RectTransform>().anchoredPosition.x;
            mapPosX = mapButton.GetComponent<RectTransform>().anchoredPosition.x;
            itemRect.anchoredPosition = new Vector2(36.5f, -220);
            
            Debug.Log("MenuPanel Start方法开始执行");
            Debug.Log($"MenuPanel GameObject名称: {gameObject.name}");
            Debug.Log($"MenuPanel 激活状态: {gameObject.activeInHierarchy}");
            debugButton.gameObject.SetActive(SceneManager.sceneCountInBuildSettings > 1 && showDebugButton);
            debugButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("DebugMode", LoadSceneMode.Additive);
            });

            var uiSystem = this.GetSystem<IUISystem>();
            if (uiSystem == null)
            {
                Debug.LogError("无法获取IUISystem，请检查系统初始化");
                return;
            }
            Debug.Log("IUISystem获取成功");
            
            noteToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    uiSystem.ShowPopup(UIPopup.NotePopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.NotePopup);
                }
            });
            
            radioToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    uiSystem.ShowPopup(UIPopup.RadioPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.RadioPopup);
                }
            });
            
            clockToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    uiSystem.ShowPopup(UIPopup.ClockPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.ClockPopup);
                }
            });

            if (telescopeToggle != null)
            {
                telescopeToggle.isOn = this.GetModel<IGameModel>().TelescopeEnabled.Value;
                telescopeToggle.onValueChanged.AddListener(isOn =>
                {
                    this.GetModel<IGameModel>().TelescopeEnabled.Value = isOn;
                });
            }

            if (cameraToggle != null)
            {
                cameraToggle.isOn = this.GetModel<IGameModel>().CameraCaptureEnabled.Value;
                cameraToggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        // 强制关闭所有popup并同步其他工具toggle的视觉状态，然后开启相机
                        uiSystem.HideAllPopups();
                        SyncOtherTogglesOff();
                    }
                    this.GetModel<IGameModel>().CameraCaptureEnabled.Value = isOn;
                });
                // 拍完照/点击其他UI后CameraCaptureController会把model置false，需要把toggle视觉也同步
                this.GetModel<IGameModel>().CameraCaptureEnabled.Register(v =>
                {
                    if (cameraToggle.isOn != v)
                        cameraToggle.isOn = v;
                }).UnRegisterWhenGameObjectDestroyed(gameObject);

                // 让 CameraCaptureController 知道 cameraToggle 的 GameObject，
                // 这样点击 cameraToggle 时不会被"点UI=关相机"逻辑误伤
                if (Camera.main != null)
                {
                    var cc = Camera.main.GetComponent<CameraCaptureController>();
                    if (cc != null)
                        cc.cameraToggleObj = cameraToggle.gameObject;
                }
            }

            settingButton.onValueChanged.AddListener(isOn =>
            {
                if (isSyncingSettingButton) return;
                // 开蛋流程期间设置完全不可用(开蛋就只能开蛋):静默回弹开关,不弹面板。
                // 覆盖:场上有未开的蛋(刚购买落地) + 点蛋后的开蛋动画
                if (Egg.IsHatching || this.GetModel<IBirdModel>().UnopenEggs > 0)
                {
                    isSyncingSettingButton = true;
                    settingButton.isOn = !isOn;
                    isSyncingSettingButton = false;
                    return;
                }
                if (isOn)
                {
                    uiSystem.HidePopup(UIPopup.ShopPopup);
                    uiSystem.HidePopup(UIPopup.IllustratedPopup);
                    uiSystem.HidePopup(UIPopup.MapPopup);
                    uiSystem.HidePopup(UIPopup.TutorialPopup);
                    isSyncingShopButton = true;
                    shopButton.isOn = false;
                    isSyncingShopButton = false;
                    isSyncingIllustratedButton = true;
                    illustratedButton.isOn = false;
                    isSyncingIllustratedButton = false;
                    isSyncingMapButton = true;
                    mapButton.isOn = false;
                    isSyncingMapButton = false;
                    uiSystem.ShowPopup(UIPopup.SettingPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.SettingPopup);
                }
            });
               
            shopButton.onValueChanged.AddListener(isOn =>
            {
                if (isSyncingShopButton)
                {
                    // 正在同步状态，跳过这次调用
                    return;
                }
                
                if (isOn)
                {
                    uiSystem.HidePopup(UIPopup.IllustratedPopup);
                    uiSystem.HidePopup(UIPopup.MapPopup);
                    uiSystem.HidePopup(UIPopup.SettingPopup);
                    uiSystem.HidePopup(UIPopup.TutorialPopup);
                    isSyncingIllustratedButton = true;
                    illustratedButton.isOn = false;
                    isSyncingIllustratedButton = false;
                    isSyncingMapButton = true;
                    mapButton.isOn = false;
                    isSyncingMapButton = false;
                    isSyncingSettingButton = true;
                    settingButton.isOn = false;
                    isSyncingSettingButton = false;
                    uiSystem.ShowPopup(UIPopup.ShopPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.ShopPopup);
                }
            });


            illustratedButton.onValueChanged.AddListener(isOn =>
            {
                if (isSyncingIllustratedButton)
                {
                    // 正在同步状态，跳过这次调用
                    return;
                }
                
                if (isOn)
                {
                    uiSystem.HidePopup(UIPopup.ShopPopup);
                    uiSystem.HidePopup(UIPopup.MapPopup);
                    uiSystem.HidePopup(UIPopup.SettingPopup);
                    uiSystem.HidePopup(UIPopup.TutorialPopup);
                    isSyncingShopButton = true;
                    shopButton.isOn = false;
                    isSyncingShopButton = false;
                    isSyncingMapButton = true;
                    mapButton.isOn = false;
                    isSyncingMapButton = false;
                    isSyncingSettingButton = true;
                    settingButton.isOn = false;
                    isSyncingSettingButton = false;
                    uiSystem.ShowPopup(UIPopup.IllustratedPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.IllustratedPopup);
                }
            });
                
            
            mapButton.onValueChanged.AddListener(isOn =>
            {
                if (isSyncingMapButton)
                {
                    // 正在同步状态，跳过这次调用
                    return;
                }
                
                if (isOn)
                {
                    uiSystem.HidePopup(UIPopup.ShopPopup);
                    uiSystem.HidePopup(UIPopup.IllustratedPopup);
                    uiSystem.HidePopup(UIPopup.SettingPopup);
                    uiSystem.HidePopup(UIPopup.TutorialPopup);
                    isSyncingShopButton = true;
                    shopButton.isOn = false;
                    isSyncingShopButton = false;
                    isSyncingIllustratedButton = true;
                    illustratedButton.isOn = false;
                    isSyncingIllustratedButton = false;
                    isSyncingSettingButton = true;
                    settingButton.isOn = false;
                    isSyncingSettingButton = false;
                    uiSystem.ShowPopup(UIPopup.MapPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.MapPopup);
                }
            });

            this.RegisterEvent<OnClockCloseEvent>(v =>
            {
                clockToggle.isOn = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnIllustratedCloseEvent>(v =>
            {
                // 设置标志位，避免在设置 isOn = false 时触发 onValueChanged 导致重复调用 HidePopup
                isSyncingIllustratedButton = true;
                
                // 关闭图鉴（如果存在）
                uiSystem.HidePopup(UIPopup.IllustratedPopup);
                
                // 同步 Toggle 状态（由于标志位，不会触发 onValueChanged 中的 HidePopup）
                illustratedButton.isOn = false;
                
                // 重置标志位
                isSyncingIllustratedButton = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnMapCloseEvent>(v =>
            {
                // 设置标志位，避免在设置 isOn = false 时触发 onValueChanged 导致重复调用 HidePopup
                isSyncingMapButton = true;
                
                // 关闭地图（如果存在）
                uiSystem.HidePopup(UIPopup.MapPopup);
                
                // 同步 Toggle 状态（由于标志位，不会触发 onValueChanged 中的 HidePopup）
                mapButton.isOn = false;
                
                // 重置标志位
                isSyncingMapButton = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnNoteCloseEvent>(evt =>
            {
                noteToggle.isOn = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnRadioCloseEvent>(evt =>
            {
                radioToggle.isOn = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnSettingCloseEvent>(evt =>
            {
                isSyncingSettingButton = true;
                uiSystem.HidePopup(UIPopup.SettingPopup);
                settingButton.isOn = false;
                isSyncingSettingButton = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnShopCloseEvent>(evt =>
            {
                // 设置标志位，避免在设置 isOn = false 时触发 onValueChanged 导致重复调用 HidePopup
                isSyncingShopButton = true;
                
                // 关闭商店（如果存在）
                uiSystem.HidePopup(UIPopup.ShopPopup);
                
                // 同步 Toggle 状态（由于标志位，不会触发 onValueChanged 中的 HidePopup）
                shopButton.isOn = false;
                
                // 重置标志位
                isSyncingShopButton = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            if (!viewGroup.activeSelf)
                viewGroup.gameObject.SetActive(true);
            viewToggle.isOn = true;
            this.GetModel<IGameModel>().ViewUI.Value = true;
            
            viewToggle.onValueChanged.AddListener(isOn =>
            {
                this.GetModel<IGameModel>().ViewUI.Value = isOn;
                viewGroup.SetActive(isOn);
            });
            
            // 外部链接按钮点击事件
            if (externalLinkButton != null)
            {
                externalLinkButton.onClick.AddListener(() =>
                {
                    OpenExternalLink();
                });
            }
            
            weatherButton.onClick.AddListener(() =>
            {
                contentAnim?.Kill();
                if (!isShowWeatherItems)
                {
                    // for (int i = 0; i < weatherItems.Length; i++)
                    // {
                    //     if (i == this.GetModel<IGameModel>().WeatherIndex.Value)
                    //     {
                    //         weatherItems[i].SetActive(false);
                    //     }
                    //     else if (!weatherItems[i].activeSelf)
                    //     {
                    //         weatherItems[i].SetActive(true);
                    //     }
                    // }

                    content.anchoredPosition = new Vector2(400, 0);
                    content.gameObject.SetActive(true);
                    contentAnim = content.DOAnchorPosX(0, 0.3f);
                    weatherImage.color= Color.white;
                    itemRect.gameObject.SetActive(true);
                }
                else
                {
                    content.anchoredPosition = new Vector2(0, 0);
                    contentAnim = content.DOAnchorPosX(400, 0.3f).OnComplete(() =>
                    {
                        content.gameObject.SetActive(false);
                    });
                    weatherImage.color = Color.clear;
                    itemRect.gameObject.SetActive(false);
                }

                isShowWeatherItems = !isShowWeatherItems;
            });
            weatherImage.color= Color.clear;
            itemRect.gameObject.SetActive(false);

            this.RegisterEvent<HideWeatherContentEvent>(evt =>
            {
                content.anchoredPosition = new Vector2(0, 0);
                contentAnim = content.DOAnchorPosX(400, 0.3f).OnComplete(() =>
                {
                    content.gameObject.SetActive(false);
                });
                isShowWeatherItems = false;
                weatherImage.color= Color.clear;
                itemRect.gameObject.SetActive(false);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            content.anchoredPosition = new Vector2(400, 0);
            
            var accountModel = this.GetModel<IAccountModel>();
            if (originalCoinFontSize < 0f) originalCoinFontSize = coinsNum.fontSize;
            UpdateCoinText(accountModel.Coins.Value);
            currentCoins = accountModel.Coins.Value;
            accountModel.Coins.Register(v =>
            {
                DOTween.To(n =>
                {
                    UpdateCoinText(n);
                }, currentCoins, v, 0.5f);
                currentCoins = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            //weatherIcon.sprite = weatherSps[this.GetModel<IGameModel>().WeatherIndex.Value];
            // this.GetModel<IGameModel>().WeatherIndex.Register(v =>
            // {
            //     //weatherIcon.sprite = weatherSps[v];
            // }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            if (!PlayerPrefs.HasKey("ShowedTutorial"))
            {
                PlayerPrefs.SetString("ShowedTutorial", "true");
                this.GetSystem<IUISystem>().ShowPopup(UIPopup.TutorialPopup);
            }

            // this.RegisterEvent<ShowBranchEvent>(evt =>
            // {
            //     if (isShowBranch)
            //     {
            //         HideBranch();
            //     }
            //     else
            //     {
            //         ShowBranch();
            //     }
            // }).UnRegisterWhenGameObjectDestroyed(gameObject);
            // ShowBranch();

            this.RegisterEvent<ChangeTimeViewEvent>(evt =>
            {
                timeAnim?.Kill();
                if (evt.show)
                {
                    timeAnim = timeItem.DOAnchorPosY(0f, 0.2f).SetEase(Ease.InSine);
                    shopButton.GetComponent<RectTransform>().DOAnchorPosX(shopPosX - 300, 0.3f);
                    illustratedButton.GetComponent<RectTransform>().DOAnchorPosX(illustratedPosX - 300, 0.3f);
                    weatherButton.GetComponent<RectTransform>().DOAnchorPosX(weatherPosX + 300, 0.3f);
                    mapButton.GetComponent<RectTransform>().DOAnchorPosX(mapPosX + 300, 0.3f);
                    itemRect.DOAnchorPosX(336.5f, 0.3f);
                }
                else
                {
                    timeAnim = timeItem.DOAnchorPosY(254f, 0.2f).SetEase(Ease.OutSine);
                    shopButton.GetComponent<RectTransform>().DOAnchorPosX(shopPosX, 0.3f);
                    illustratedButton.GetComponent<RectTransform>().DOAnchorPosX(illustratedPosX, 0.3f);
                    weatherButton.GetComponent<RectTransform>().DOAnchorPosX(weatherPosX, 0.3f);
                    mapButton.GetComponent<RectTransform>().DOAnchorPosX(mapPosX, 0.3f);
                    itemRect.DOAnchorPosX(36.5f, 0.3f);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            if (this.GetModel<IClockModel>().TimerType != TimerType.None)
            {
                timeAnim = timeItem.DOAnchorPosY(0f, 0.2f).SetEase(Ease.InSine);
                shopButton.GetComponent<RectTransform>().DOAnchorPosX(shopPosX - 300, 0.3f);
                illustratedButton.GetComponent<RectTransform>().DOAnchorPosX(illustratedPosX - 300, 0.3f);
                weatherButton.GetComponent<RectTransform>().DOAnchorPosX(weatherPosX + 300, 0.3f);
                mapButton.GetComponent<RectTransform>().DOAnchorPosX(mapPosX + 300, 0.3f);
                itemRect.DOAnchorPosX(336.5f, 0.3f);
                if (this.GetModel<IClockModel>().TimerType == TimerType.Tomato)
                {
                    timeText.text = this.GetModel<IClockModel>().TomatoItem.TimeString.Value;
                }
                else if (this.GetModel<IClockModel>().TimerType == TimerType.Timer)
                {
                    timeText.text = this.GetModel<IClockModel>().TimerItem.TimeString.Value;
                }
                else if (this.GetModel<IClockModel>().TimerType == TimerType.StopWatch)
                {
                    timeText.text = this.GetModel<IClockModel>().StopWatchItem.TimerString.Value;
                }
            }

            this.GetModel<IClockModel>().TomatoItem.TimeString.Register(v =>
            {
                if (this.GetModel<IClockModel>().TimerType == TimerType.Tomato)
                    timeText.text = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.GetModel<IClockModel>().TimerItem.TimeString.Register(v =>
            {
                if (this.GetModel<IClockModel>().TimerType == TimerType.Timer)
                    timeText.text = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.GetModel<IClockModel>().StopWatchItem.TimerString.Register(v =>
            {
                if (this.GetModel<IClockModel>().TimerType == TimerType.StopWatch)
                    timeText.text = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            timeItem.anchoredPosition = new Vector2(0f, 254f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A) && this.GetModel<IGameModel>().IsShortcutKeyOn.Value)
            {
                showDebugButton = !showDebugButton;
                debugButton.gameObject.SetActive(SceneManager.sceneCountInBuildSettings > 1 && showDebugButton);
            }
        }

        /// <summary>
        /// Helper method to check key press from both Unity Input and Windows Hook
        /// </summary>
        private bool GetKeyDownAny(KeyCode keyCode)
        {
            return (Input.GetKeyDown(keyCode) || MouseForwarder.GetKeyDown(keyCode)) && this.GetModel<IGameModel>().IsShortcutKeyOn.Value;
        }

        /// <summary>
        /// 打开外部链接
        /// </summary>
        private void OpenExternalLink()
        {
            // 这里可以替换为你想要跳转的网址
            string url = "https://itch.io/"; // 请替换为实际的网址
            this.GetSystem<IGameSystem>().OpenUrl(url);
        }

        private void UpdateCoinText(float value)
        {
            coinsNum.text = value.ToString("F1", CultureInfo.InvariantCulture);
            coinsNum.fontSize = value >= 1_000_000f ? originalCoinFontSize * 0.8f : originalCoinFontSize;
        }
    }
}