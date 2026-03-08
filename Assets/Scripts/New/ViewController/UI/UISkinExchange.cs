using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class UISkinExchange : ViewControllerBase
    {
        public ToolType thisType;
        public int spIndex = 0;
        public Image barImage;
        
        private void Start()
        {
            int toolIndex = (int)thisType;
            var accountData = this.GetModel<ISaveModel>().AccountData;
            int equipedId = 0;
            if (accountData.sceneTools != null && accountData.sceneTools.Count > 0 &&
                accountData.sceneTools[0].tools != null && toolIndex < accountData.sceneTools[0].tools.Count)
            {
                equipedId = accountData.sceneTools[0].tools[toolIndex].equipedId;
            }
            SetBar(equipedId);
            this.RegisterEvent<EquipUISkin>(evt =>
            {
                if (evt.type == thisType)
                    SetBar(evt.index);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void SetBar(int index)
        {
            if (barImage == null) return;
            var config = this.GetModel<IConfigModel>().ShopConfig;
            if (config?.tools == null || (int)thisType >= config.tools.Length) return;
            var toolItem = config.tools[(int)thisType];
            if (toolItem?.selections == null || toolItem.selections.Length == 0) return;
            int safeIndex = Mathf.Clamp(index, 0, toolItem.selections.Length - 1);
            var sel = toolItem.selections[safeIndex];
            barImage.sprite = sel?.uiColorItem?.uiSprites[spIndex];
        }
    }
}