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
        public TextMeshProUGUI coinsNum;
        public RectTransform branch;
        public CanvasGroup group1;
        public CanvasGroup group2;
        public GameObject timeItem;
        public TextMeshProUGUI timeText;

        private Sequence anim;
        private bool isShowBranch;
        
        public override void OnShowPanel()
        {
            
        }

        public override void OnHidePanel()
        {
            Destroy(gameObject);
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
            
            weatherButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().SendEvent<SwitchWeatherEvent>();
            });

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

            this.RegisterEvent<ShowBranchEvent>(evt =>
            {
                if (isShowBranch)
                {
                    HideBranch();
                }
                else
                {
                    ShowBranch();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            ShowBranch();

            this.RegisterEvent<ChangeTimeViewEvent>(evt =>
            {
                timeItem.SetActive(evt.show);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.GetModel<IClockModel>().TomatoItem.TimeString.Register(v =>
            {
                timeText.text = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            timeItem.SetActive(false);
        }

        private void ShowBranch()
        {
            isShowBranch = true;
            anim?.Kill();
            group1.alpha = 0;
            group2.alpha = 0;
            var rect1 = group1.transform as RectTransform;
            var rect2 = group2.transform as RectTransform;
            branch.anchoredPosition = new Vector2(1432f, -166.61f);
            rect1.anchoredPosition = new Vector2(50f, -2f);
            rect2.anchoredPosition = new Vector2(50f, -115f);
            anim = DOTween.Sequence();
            anim.Append(branch.DOAnchorPosX(1028f, 0.5f).SetEase(Ease.InSine));
            anim.Append(rect1.DOAnchorPosY(-22f, 0.3f).SetEase(Ease.Linear));
            anim.Join(group1.DOFade(1f, 0.3f).SetEase(Ease.Linear));
            anim.Append(rect2.DOAnchorPosY(-135f, 0.3f).SetEase(Ease.Linear));
            anim.Join(group2.DOFade(1f, 0.3f).SetEase(Ease.Linear));
        }

        private void HideBranch()
        {
            isShowBranch = false;
            anim?.Kill();
            anim = DOTween.Sequence();
            anim.Append(branch.DOAnchorPosX(1432f, 0.5f).SetEase(Ease.OutSine));
        }
    }
}