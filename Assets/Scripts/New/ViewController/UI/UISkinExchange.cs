using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class UISkinExchange : ViewControllerBase
    {
        public ToolType thisType;
        public Image barImage;
        
        private void Start()
        {
            int toolIndex = (int)thisType;
            SetBar(this.GetModel<ISaveModel>().AccountData.sceneTools[0].tools[toolIndex].equipedId);
            this.RegisterEvent<EquipUISkin>(evt =>
            {
                if (evt.type == thisType)
                {
                    SetBar(evt.index);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void SetBar(int index)
        {
            if (index == 0)
            {
                barImage.color = Color.white;
            }
            else
            {
                barImage.color = Color.cyan;
            }
        }
    }
}