using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class MenuButton : ViewControllerBase
    {
        public UIPopup popup;

        private bool isOn = false;

        public void OnClick()
        {
            Debug.Log("Click");
            isOn = !isOn;
            // // 判断如果是地图按钮，显示开发提示
            // if (popup == UIPopup.MapPopup)
            // {
            //     ShowDevelopingTip();
            //     //this.GetSystem<IUISystem>().ShowPopup(popup);
            // }
            // else
            // {
            // 其他按钮保持原来的弹窗功能
            if (isOn)
            {
                this.GetSystem<IUISystem>().ShowPopup(popup);
            }
            else
            {
                this.GetSystem<IUISystem>().HidePopup(popup);
            }
            //}
        }

        public void ShowDevelopingTip()
        {
            string text = this.GetSystem<ILocalizationSystem>().GetString("DevelopingMap");
            this.GetSystem<IUISystem>().ShowPrompt(text);
        }
    }
}