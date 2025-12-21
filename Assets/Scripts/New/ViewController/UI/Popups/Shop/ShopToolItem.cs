using System;
using System.Collections.Generic;
using NUnit.Framework;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopToolItem : ViewControllerBase
    {
        public Image icon;
        public LocalizationText itemName;
        public LocalizationText selectName;
        public LocalizationText description;
        public TextMeshProUGUI priceText;
        public Button buyButton;
        public TextMeshProUGUI buyButtonText;
        public GameObject selectionPrefab;

        private int itemIndex;
        private List<ShopToolSelection> selections = new List<ShopToolSelection>();
        
        public void Init(int index)
        {
            itemIndex = index;
            
            // 自动获取按钮文本组件
            if (buyButtonText == null)
            {
                buyButtonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buyButtonText == null)
                {
                    // 如果还是null，尝试获取Button组件内的Text组件
                    buyButtonText = buyButton.GetComponent<TextMeshProUGUI>();
                }
                Debug.Log($"获取按钮文本组件: {(buyButtonText != null ? "成功" : "失败")}");
            }
            
            var item = this.GetModel<IConfigModel>().ShopConfig.tools[index];
            var gameModel = this.GetModel<IGameModel>();
            var saveModel = this.GetModel<ISaveModel>();
            if (!gameModel.SelectedToolDic.ContainsKey(index))
            {
                gameModel.SelectedToolDic.Add(index, new BindableProperty<int>());
            }

            var sp = item.selections[gameModel.SelectedToolDic[index].Value].icon;
            icon.sprite = sp;
            if (sp != null)
                icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size;
            Debug.Log("名称：" + item.name);
            itemName.SetKey(item.name);
            selectName.SetKey(item.selections[gameModel.SelectedToolDic[index].Value].selectionName);
            
            // 优先使用descriptionKey，如果没有设置则使用description
            var selectedTool = item.selections[gameModel.SelectedToolDic[index].Value];
            if (!string.IsNullOrEmpty(selectedTool.descriptionKey))
            {
                description.SetKey(selectedTool.descriptionKey);
            }
            else
            {
                description.SetKey(selectedTool.description);
            }
            if (item.selections[0].type == ToolType.Food)
            {
                bool isInitialPurchased = saveModel.AccountData.tools[itemIndex].unlockedList
                    .Contains(gameModel.SelectedToolDic[index].Value);
                bool isInitialEquipped = saveModel.AccountData.tools[itemIndex].equipedId ==
                                         gameModel.SelectedToolDic[index].Value;

                if (isInitialEquipped)
                {
                    // 已装备：显示"equipped"
                    priceText.text = "equipped";
                }
                else if (isInitialPurchased)
                {
                    // 已购买但未装备：显示"equip"
                    priceText.text = "equip";
                }
                else
                {
                    // 未购买：显示价格
                    priceText.text = item.selections[gameModel.SelectedToolDic[index].Value].price.ToString();
                }
                for (int i = 0; i < item.selections.Length; i++)
                {
                    var select = GameObject.Instantiate(selectionPrefab, selectionPrefab.transform.parent)
                        .GetComponent<ShopToolSelection>();
                    selections.Add(select);
                    select.gameObject.SetActive(true);
                    select.Init(index, i);
                }
            }
            else if (item.selections[0].type == ToolType.BirdMaxCount)
            {
                bool isInitialPurchased = saveModel.AccountData.tools[itemIndex].unlockedList
                    .Contains(gameModel.SelectedToolDic[index].Value);
                if (isInitialPurchased)
                {
                    priceText.text = "equipped";
                    buyButton.enabled = false;
                }
                else
                {
                    buyButton.enabled = true;
                    priceText.text = item.selections[gameModel.SelectedToolDic[index].Value].price.ToString();
                }

                bool initFirst = false;
                for (int i = 0; i < item.selections.Length; i++)
                {
                    var select = GameObject.Instantiate(selectionPrefab, selectionPrefab.transform.parent)
                        .GetComponent<ShopToolSelection>();
                    selections.Add(select);
                    select.gameObject.SetActive(true);
                    select.Init(index, i);
                    
                    if(saveModel.AccountData.tools[itemIndex].unlockedList.Contains(i))
                    {
                       continue; 
                    }

                    if (!initFirst)
                    {
                        initFirst = true;
                    }
                    else
                    {
                        var toggle = select.GetComponent<Toggle>();
                        toggle.enabled = false;
                        toggle.graphic.gameObject.SetActive(false);
                    }
                }
            }

            gameModel.SelectedToolDic[itemIndex].Register(v =>
            {
                var sp = item.selections[v].icon;
                icon.sprite = sp;
                if(sp != null)
                    icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size * 0.3f;
                selectName.SetKey(item.selections[v].selectionName);
                selectName.SetKey(item.selections[v].selectionName);
                
                // 优先使用descriptionKey，如果没有设置则使用description
                var selectedTool = item.selections[v];
                if (!string.IsNullOrEmpty(selectedTool.descriptionKey))
                {
                    description.SetKey(selectedTool.descriptionKey);
                }
                else
                {
                    description.SetKey(selectedTool.description);
                }
                if (item.selections[0].type == ToolType.Food)
                {
                    // 检查食物状态，决定显示内容
                    bool isPurchased = saveModel.AccountData.tools[index].unlockedList.Contains(v);
                    bool isEquipped = saveModel.AccountData.tools[index].equipedId == v;

                    if (isEquipped)
                    {
                        // 已装备：显示"equipped"
                        priceText.text = "equipped";
                    }
                    else if (isPurchased)
                    {
                        // 已购买但未装备：显示"equip"
                        priceText.text = "equip";
                    }
                    else
                    {
                        // 未购买：显示价格
                        priceText.text = selectedTool.price.ToString();
                    }
                }
                else if(item.selections[0].type == ToolType.BirdMaxCount)
                {
                    bool isPurchased = saveModel.AccountData.tools[index].unlockedList.Contains(v);
                    if (isPurchased)
                    {
                        priceText.text = "equipped";
                        buyButton.enabled = false;
                    }
                    else
                    {
                        priceText.text = selectedTool.price.ToString();
                        buyButton.enabled = true;
                    }
                }
                UpdateButtonState();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            //var gameModel = this.GetModel<IGameModel>();
            // var saveModel = this.GetModel<ISaveModel>();
            // var configModel = this.GetModel<IConfigModel>();
            // var selectedToolIndex = gameModel.SelectedToolDic[itemIndex].Value;
            // 检查是否已经购买过这个食物
            //bool isPurchased = saveModel.AccountData.tools[itemIndex].unlockedList.Contains(selectedToolIndex);
            
            //Debug.Log($"更新按钮状态: {selectedTool.selectionName}, 已购买: {isPurchased}");
            
            // 价格文本的显示/隐藏逻辑保持不变
            if (priceText != null)
            {
                priceText.gameObject.SetActive(true); // 始终显示价格文本区域
            }
        }

        private void Start()
        {
            buyButton.onClick.AddListener(() =>
            {
                var configModel = this.GetModel<IConfigModel>();
                var gameModel = this.GetModel<IGameModel>();
                var saveModel = this.GetModel<ISaveModel>();
                var selectedToolIndex = gameModel.SelectedToolDic[itemIndex].Value;
                var toolItem = configModel.ShopConfig.tools[itemIndex];
                var selectedTool = toolItem.selections[selectedToolIndex];
                
                // 检查是否已经购买过
                bool isPurchased = saveModel.AccountData.tools[itemIndex].unlockedList.Contains(selectedToolIndex);
                
                if (!isPurchased)
                {
                    // 未购买，执行购买逻辑
                    int price = selectedTool.price;
                    if (price <= this.GetModel<IAccountModel>().Coins.Value)
                    {
                        this.GetSystem<IUISystem>().ShowBuyConfirm(() =>
                        {
                            // 扣除金币
                            this.GetModel<IAccountModel>().Coins.Value -= price;

                            // 添加到已购买列表
                            saveModel.AccountData.tools[itemIndex].unlockedList.Add(selectedToolIndex);

                            saveModel.AccountData.tools[itemIndex].equipedId = selectedToolIndex;
                            // 根据工具类型应用不同的效果
                            if (toolItem.name.ToLower() == "food")
                            {
                                // 设置当前食物类型（立即装备）
                                string text = this.GetSystem<ILocalizationSystem>()
                                    .GetString("Purchase successful! Food skins are equipped:");
                                this.GetSystem<IUISystem>().ShowPrompt($"{text} {selectedTool.selectionName}");
                                //this.GetSystem<IUISystem>().ShowPrompt($"购买成功！食物皮肤已装备: {selectedTool.selectionName}");
                            }
                            else if (toolItem.name.ToLower() == "cursor")
                            {
                                // 应用光标类型
                                string text = this.GetSystem<ILocalizationSystem>()
                                    .GetString("Purchase successful! Cursor skins are equipped:");
                                this.GetSystem<IUISystem>().ShowPrompt($"{text} {selectedTool.selectionName}");
                            }
                            else if (toolItem.selections[0].type == ToolType.BirdMaxCount)
                            {
                                this.GetModel<ISaveModel>().AccountData.addedMaxBirdValue += selectedTool.addCount;
                                //this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
                                bool initFirst = false;
                                for (int i = 0; i < selections.Count; i++)
                                {
                                    var toggle = selections[i].GetComponent<Toggle>();
                                    if (saveModel.AccountData.tools[itemIndex].unlockedList.Contains(i))
                                    {
                                        toggle.enabled = true;
                                        toggle.graphic.gameObject.SetActive(true);
                                        continue;
                                    }

                                    if (!initFirst)
                                    {
                                        toggle.enabled = true;
                                        toggle.graphic.gameObject.SetActive(true);
                                        initFirst = true;
                                    }
                                    else
                                    {
                                        toggle.enabled = false;
                                        toggle.graphic.gameObject.SetActive(false);
                                    }
                                }
                            }
                            
                            UpdateButtonState();
                            this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                        });
                    }
                    else
                    {
                        string text = this.GetSystem<ILocalizationSystem>().GetString("Insufficient coins");
                        this.GetSystem<IUISystem>().ShowPrompt(text);
                    }
                }
                else
                {
                    // 已购买，执行装备逻辑
                    if (toolItem.name.ToLower() == "food")
                    {
                        // 设置当前食物类型
                        saveModel.AccountData.tools[itemIndex].equipedId = selectedToolIndex;
                        string text = this.GetSystem<ILocalizationSystem>().GetString("Food skin equipped:");
                        this.GetSystem<IUISystem>().ShowPrompt($"{text} {selectedTool.selectionName}");
                    }
                    else if (toolItem.name.ToLower() == "cursor")
                    {
                        // 应用光标类型
                        string text = this.GetSystem<ILocalizationSystem>().GetString("Cursor skin equipped:");
                        this.GetSystem<IUISystem>().ShowPrompt($"{text} {selectedTool.selectionName}");
                    }
                    
                    UpdateButtonState();
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                }
            });
        }
    }
}