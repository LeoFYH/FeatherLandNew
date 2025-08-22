using System;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
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
            var uiSystem = this.GetSystem<IUISystem>();
            toDoButton.onClick.AddListener(() =>
            {
                uiSystem.ShowPopup(UIPopup.NotePopup);
            });
            
            radioButton.onClick.AddListener(() =>
            {
                uiSystem.ShowPopup(UIPopup.RadioPopup);
            });
            
            settingButton.onClick.AddListener(() =>
            {
                uiSystem.ShowPopup(UIPopup.SettingPopup);
            });
            
            shopButton.onClick.AddListener(() =>
            {
                uiSystem.ShowPopup(UIPopup.ShopPopup);
            });
            
            tomatoButton.onClick.AddListener(() =>
            {
                uiSystem.ShowPopup(UIPopup.ClockPopup);
            });
            
            illustratedButton.onClick.AddListener(() =>
            {
                uiSystem.ShowPopup(UIPopup.IllustratedPopup);
            });
            illustratedButton1.onClick.AddListener(() =>
            {
                uiSystem.ShowPopup(UIPopup.IllustratedPopup);
            });
            mapButton.onClick.AddListener(() =>
            {
                
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
            coinsNum.text = accountModel.Coins.Value.ToString();
            accountModel.Coins.Register(v =>
            {
                coinsNum.text = v.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            weatherIcon.sprite = weatherSps[this.GetModel<IGameModel>().WeatherIndex.Value];
            this.GetModel<IGameModel>().WeatherIndex.Register(v =>
            {
                weatherIcon.sprite = weatherSps[v];
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

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
            try
            {
                // 这里可以替换为你想要跳转的网址
                string url = "https://itch.io/"; // 请替换为实际的网址
                
                Debug.Log($"正在打开外部链接: {url}");
                
                // 使用系统默认浏览器打开链接
                Application.OpenURL(url);
            }
            catch (Exception ex)
            {
                Debug.LogError($"打开外部链接失败: {ex.Message}");
            }
        }
    }
}