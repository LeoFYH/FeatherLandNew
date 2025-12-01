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
        public Image weatherIcon;
        public Button weatherButton;
        public Sprite[] weatherSps;
        public Button toDoButton;
        public Button radioButton;
        public Button settingButton;
        public Button shopButton;
        public Button tomatoButton;
        public Button illustratedButton;
        public Button illustratedButton1;
        public Button mapButton;
        public Button externalLinkButton; // 新增外部链接按钮
        public TextMeshProUGUI coinsNum;
        public RectTransform branch;
        public CanvasGroup group1;
        public CanvasGroup group2;
        public RectTransform timeItem;
        public TextMeshProUGUI timeText;
        public GameObject[] weatherItems;
        public RectTransform content;
        public Button debugButton;

        private Sequence anim;
        private Tweener timeAnim;
        private bool isShowBranch;
        private bool isShowWeatherItems = false;
        private Tweener contentAnim;
        
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
            
            if (toDoButton != null)
            {
                Debug.Log($"绑定toDoButton事件 - 按钮名称: {toDoButton.name}, 激活状态: {toDoButton.gameObject.activeInHierarchy}");
                toDoButton.onClick.AddListener(() =>
                {
                    Debug.Log("toDoButton被点击");
                    uiSystem.ShowPopup(UIPopup.NotePopup);
                });
            }
            else
            {
                Debug.LogWarning("toDoButton未分配");
            }
            
            if (radioButton != null)
            {
                Debug.Log("绑定radioButton事件");
                radioButton.onClick.AddListener(() =>
                {
                    Debug.Log("radioButton被点击");
                    uiSystem.ShowPopup(UIPopup.RadioPopup);
                });
            }
            else
            {
                Debug.LogWarning("radioButton未分配");
            }
            
            if (settingButton != null)
            {
                Debug.Log($"绑定settingButton事件 - 按钮名称: {settingButton.name}, 激活状态: {settingButton.gameObject.activeInHierarchy}");
                settingButton.onClick.AddListener(() =>
                {
                    Debug.Log("设置按钮被点击，尝试打开设置弹窗");
                    uiSystem.ShowPopup(UIPopup.SettingPopup);
                });
            }
            else
            {
                Debug.LogError("设置按钮未分配，请检查MenuPanel预制体");
            }
            
            if (shopButton != null)
            {
                Debug.Log("绑定shopButton事件");
                shopButton.onClick.AddListener(() =>
                {
                    Debug.Log("shopButton被点击");
                    uiSystem.ShowPopup(UIPopup.ShopPopup);
                });
            }
            else
            {
                Debug.LogWarning("shopButton未分配");
            }
            
            if (tomatoButton != null)
            {
                Debug.Log("绑定tomatoButton事件");
                tomatoButton.onClick.AddListener(() =>
                {
                    Debug.Log("tomatoButton被点击");
                    uiSystem.ShowPopup(UIPopup.ClockPopup);
                });
            }
            else
            {
                Debug.LogWarning("tomatoButton未分配");
            }
            
            if (illustratedButton != null)
            {
                Debug.Log("绑定illustratedButton事件");
                illustratedButton.onClick.AddListener(() =>
                {
                    Debug.Log("illustratedButton被点击");
                    uiSystem.ShowPopup(UIPopup.IllustratedPopup);
                });
            }
            else
            {
                Debug.LogWarning("illustratedButton未分配");
            }
            
            if (illustratedButton1 != null)
            {
                Debug.Log("绑定illustratedButton1事件");
                illustratedButton1.onClick.AddListener(() =>
                {
                    Debug.Log("illustratedButton1被点击");
                    uiSystem.ShowPopup(UIPopup.IllustratedPopup);
                });
            }
            else
            {
                Debug.LogWarning("illustratedButton1未分配");
            }
            
            if (mapButton != null)
            {
                Debug.Log("绑定mapButton事件");
                mapButton.onClick.AddListener(() =>
                {
                    Debug.Log("mapButton被点击");
                    
                });
            }
            else
            {
                Debug.LogWarning("mapButton未分配");
            }
            
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
            coinsNum.text = accountModel.Coins.Value.ToString("F2");
            accountModel.Coins.Register(v =>
            {
                coinsNum.text = v.ToString("F2");
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            weatherIcon.sprite = weatherSps[this.GetModel<IGameModel>().WeatherIndex.Value];
            this.GetModel<IGameModel>().WeatherIndex.Register(v =>
            {
                weatherIcon.sprite = weatherSps[v];
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
                }
                else
                {
                    timeAnim = timeItem.DOAnchorPosY(254f, 0.2f).SetEase(Ease.OutSine);
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
        
        private void OnDestroy()
        {
            // Kill all DOTween animations to prevent memory leaks
            anim?.Kill();
            timeAnim?.Kill();
            contentAnim?.Kill();
            anim = null;
            timeAnim = null;
            contentAnim = null;
            
            // Remove all event listeners to prevent memory leaks
            if (debugButton != null)
                debugButton.onClick.RemoveAllListeners();
            if (toDoButton != null)
                toDoButton.onClick.RemoveAllListeners();
            if (radioButton != null)
                radioButton.onClick.RemoveAllListeners();
            if (settingButton != null)
                settingButton.onClick.RemoveAllListeners();
            if (shopButton != null)
                shopButton.onClick.RemoveAllListeners();
            if (tomatoButton != null)
                tomatoButton.onClick.RemoveAllListeners();
            if (illustratedButton != null)
                illustratedButton.onClick.RemoveAllListeners();
            if (illustratedButton1 != null)
                illustratedButton1.onClick.RemoveAllListeners();
            if (mapButton != null)
                mapButton.onClick.RemoveAllListeners();
            if (externalLinkButton != null)
                externalLinkButton.onClick.RemoveAllListeners();
            if (weatherButton != null)
                weatherButton.onClick.RemoveAllListeners();
        }

        private void ShowBranch()
        {
            isShowBranch = true;
            anim?.Kill();
            group1.alpha = 0;
            group2.alpha = 0;
            var rect1 = group1.transform as RectTransform;
            var rect2 = group2.transform as RectTransform;
            branch.anchoredPosition = new Vector2(403.8f, -166.61f);
            rect1.anchoredPosition = new Vector2(50f, -2f);
            rect2.anchoredPosition = new Vector2(50f, -115f);
            anim = DOTween.Sequence();
            anim.Append(branch.DOAnchorPosX(0, 0.5f).SetEase(Ease.InSine));
            anim.Append(rect1.DOAnchorPosY(-22f, 0.3f).SetEase(Ease.Linear));
            anim.Join(group1.DOFade(1f, 0.3f).SetEase(Ease.Linear));
            anim.Append(rect2.DOAnchorPosY(-135f, 0.3f).SetEase(Ease.Linear));
            anim.Join(group2.DOFade(1f, 0.3f).SetEase(Ease.Linear));
            anim.OnComplete(() =>
            {
                this.GetSystem<IAudioSystem>().InitEnvironments();
            });
        }

        private void HideBranch()
        {
            isShowBranch = false;
            anim?.Kill();
            anim = DOTween.Sequence();
            anim.Append(branch.DOAnchorPosX(403.8f, 0.5f).SetEase(Ease.OutSine));
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