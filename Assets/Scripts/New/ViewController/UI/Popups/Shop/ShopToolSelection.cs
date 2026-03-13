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
        public TextMeshProUGUI text;
        public Image color;

        private int itemId;
        private int selectId;
        private bool isActive;
        private Toggle toggle;

        public void Init(int itemIndex, int selectIndex, bool isActivate = true)
        {
            itemId = itemIndex;
            selectId = selectIndex;
            isActive = isActivate;
            if (toggle != null)
            {
                toggle.interactable = isActivate;
            }
            // indexText.text = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex]
            //     .selectionName;
            if (this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].type ==
                ToolType.BirdMaxCount)
            {
                text.gameObject.SetActive(true);
                icon.gameObject.SetActive(false);
                color.gameObject.SetActive(false);
                text.text = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex]
                    .selectionName;
            }
            else if(this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].type ==
                    ToolType.Food)
            {
                text.gameObject.SetActive(false);
                icon.gameObject.SetActive(true);
                color.gameObject.SetActive(false);
                var sp = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].icon;
                icon.sprite = sp;
                if (sp != null)
                    icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size * 0.3f;
            }
            else
            {
                if (this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].type ==
                    ToolType.Radio ||
                    this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].type ==
                    ToolType.Note ||
                    this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].type ==
                    ToolType.Illustrated ||
                    this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].type ==
                    ToolType.Tomato)
                {
                    text.gameObject.SetActive(false);
                    icon.gameObject.SetActive(false);
                    color.gameObject.SetActive(true);
                    color.color = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex]
                        .uiColorItem.uiColor;
                    return;
                }
                else if (this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].type ==
                         ToolType.Cursor)
                {
                    text.gameObject.SetActive(false);
                    icon.gameObject.SetActive(true);
                    color.gameObject.SetActive(false);
                    var sprite = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].icon;
                    icon.sprite = sprite;
                    if (sprite != null)
                        icon.GetComponent<RectTransform>().sizeDelta = sprite.rect.size * 0.5f;
                    return;
                }

                text.gameObject.SetActive(true);
                icon.gameObject.SetActive(false);
                color.gameObject.SetActive(false);

                var sp = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex].selections[selectIndex].icon;
                icon.sprite = sp;
                if (sp != null)
                    icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size * 0.3f * 0.2f;
            }
        }

        public void SetActive(bool isActivate)
        {
            isActive = isActivate;
            if (toggle != null)
            {
                toggle.interactable = isActivate;
            }
        }

        private void Start()
        {
            toggle = GetComponent<Toggle>();
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

            toggle.interactable = isActive;
        }
    }
}