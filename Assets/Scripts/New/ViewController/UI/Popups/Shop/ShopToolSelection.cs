using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopToolSelection : ViewControllerBase
    {
        public Image icon;

        private int itemId;
        private int selectId;

        public void Init(int itemIndex, int selectIndex)
        {
            itemId = itemIndex;
            selectId = selectIndex;
            // indexText.text = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex]
            //     .selectionName;
            var sp = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].icon;
            icon.sprite = sp;
            if (sp != null)
                icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size * 0.3f;
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