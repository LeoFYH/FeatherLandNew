using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopToolSelection : ViewControllerBase
    {
        public TextMeshProUGUI indexText;

        private int itemId;
        private int selectId;
        
        public void Init(int itemIndex, int selectIndex)
        {
            itemId = itemIndex;
            selectId = selectIndex;
            indexText.text = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex]
                .selectionName;
        }

        private void Start()
        {
            var toggle = GetComponent<Toggle>();
            var gameModel = this.GetModel<IGameModel>();
            if (gameModel.SelectedToolDic[itemId].Value == selectId)
            {
                toggle.isOn = true;
            }

            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    gameModel.SelectedToolDic[itemId].Value = selectId;
            });
        }
    }
}