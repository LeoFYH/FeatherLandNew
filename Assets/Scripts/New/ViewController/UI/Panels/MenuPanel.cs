using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class MenuPanel : UIBase
    {
        public Button toDoButton;
        public Button radioButton;
        public Button settingButton;
        public Button shopButton;
        public Button tomatoButton;
        public Text coinsNum;
        
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

            var accountModel = this.GetModel<IAccountModel>();
            coinsNum.text = accountModel.Coins.Value.ToString();
            accountModel.Coins.Register(v =>
            {
                coinsNum.text = v.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}