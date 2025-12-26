using System;
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
        //public Sprite[] weatherSps;
        public Toggle noteToggle;
        public Toggle radioToggle;
        public Toggle settingButton;
        public Toggle shopButton;
        public Toggle clockToggle;
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

        private float shopPosX;
        private float illustratedPosX;
        private float weatherPosX;
        private float mapPosX;
        
        public override void OnShowPanel()
        {
            
        }

        public override void OnHidePanel(Action onComplete = null)
        {
            Destroy(gameObject);
            onComplete?.Invoke();
        }

        private void Start()
        {
            shopPosX = shopButton.GetComponent<RectTransform>().anchoredPosition.x;
            illustratedPosX = illustratedButton.GetComponent<RectTransform>().anchoredPosition.x;
            weatherPosX = weatherButton.GetComponent<RectTransform>().anchoredPosition.x;
            mapPosX = mapButton.GetComponent<RectTransform>().anchoredPosition.x;
            itemRect.anchoredPosition = new Vector2(0, -220);
            
            Debug.Log("MenuPanel Start方法开始执行");
            Debug.Log($"MenuPanel GameObject名称: {gameObject.name}");
            Debug.Log($"MenuPanel 激活状态: {gameObject.activeInHierarchy}");
            debugButton.gameObject.SetActive(SceneManager.sceneCountInBuildSettings > 1);
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

            settingButton.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    uiSystem.ShowPopup(UIPopup.SettingPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.SettingPopup);
                }
            });
               
            shopButton.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    uiSystem.ShowPopup(UIPopup.ShopPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.ShopPopup);
                }
            });


            illustratedButton.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    uiSystem.ShowPopup(UIPopup.IllustratedPopup);
                }
                else
                {
                    uiSystem.HidePopup(UIPopup.IllustratedPopup);
                }
            });
                
            
            mapButton.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
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
                illustratedButton.isOn = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnMapCloseEvent>(v =>
            {
                mapButton.isOn = false;
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
                settingButton.isOn = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnShopCloseEvent>(evt =>
            {
                shopButton.isOn = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            if (!viewGroup.activeSelf)
                viewGroup.gameObject.SetActive(true);
            viewToggle.isOn = true;
            
            viewToggle.onValueChanged.AddListener(isOn =>
            {
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
                    for (int i = 0; i < weatherItems.Length; i++)
                    {
                        if (i == this.GetModel<IGameModel>().WeatherIndex.Value)
                        {
                            weatherItems[i].SetActive(false);
                        }
                        else if (!weatherItems[i].activeSelf)
                        {
                            weatherItems[i].SetActive(true);
                        }
                    }

                    content.anchoredPosition = new Vector2(400, 0);
                    content.gameObject.SetActive(true);
                    contentAnim = content.DOAnchorPosX(0, 0.3f);
                }
                else
                {
                    content.anchoredPosition = new Vector2(0, 0);
                    contentAnim = content.DOAnchorPosX(400, 0.3f).OnComplete(() =>
                    {
                        content.gameObject.SetActive(false);
                    });
                }

                isShowWeatherItems = !isShowWeatherItems;
            });

            this.RegisterEvent<HideWeatherContentEvent>(evt =>
            {
                content.anchoredPosition = new Vector2(0, 0);
                contentAnim = content.DOAnchorPosX(400, 0.3f).OnComplete(() =>
                {
                    content.gameObject.SetActive(false);
                });
                isShowWeatherItems = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            content.anchoredPosition = new Vector2(400, 0);
            
            var accountModel = this.GetModel<IAccountModel>();
            coinsNum.text = accountModel.Coins.Value.ToString("F1");
            accountModel.Coins.Register(v =>
            {
                coinsNum.text = v.ToString("F1");
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            //weatherIcon.sprite = weatherSps[this.GetModel<IGameModel>().WeatherIndex.Value];
            this.GetModel<IGameModel>().WeatherIndex.Register(v =>
            {
                //weatherIcon.sprite = weatherSps[v];
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
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
                    itemRect.DOAnchorPosX(470, 0.3f);
                }
                else
                {
                    timeAnim = timeItem.DOAnchorPosY(254f, 0.2f).SetEase(Ease.OutSine);
                    shopButton.GetComponent<RectTransform>().DOAnchorPosX(shopPosX, 0.3f);
                    illustratedButton.GetComponent<RectTransform>().DOAnchorPosX(illustratedPosX, 0.3f);
                    weatherButton.GetComponent<RectTransform>().DOAnchorPosX(weatherPosX, 0.3f);
                    mapButton.GetComponent<RectTransform>().DOAnchorPosX(mapPosX, 0.3f);
                    itemRect.DOAnchorPosX(0, 0.3f);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                this.GetSystem<IUISystem>().HideAllPopups();
                noteToggle.isOn = false;
                radioToggle.isOn = false;
                shopButton.isOn = false;
                clockToggle.isOn = false;
                illustratedButton.isOn = false;
                mapButton.isOn = false;
                settingButton.isOn = false;
                //this.GetSystem<IUISystem>().ShowPopup(UIPopup.SettingPopup);
            }
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
    }
}