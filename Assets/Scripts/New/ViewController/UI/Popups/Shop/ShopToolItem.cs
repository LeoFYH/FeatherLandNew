using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopToolItem : ViewControllerBase
    {
        public Image icon;
        public TextMeshProUGUI itemName;
        public TextMeshProUGUI selectName;
        public TextMeshProUGUI description;
        public TextMeshProUGUI priceText;
        public Button buyButton;
        public TextMeshProUGUI buyButtonText;
        public GameObject selectionPrefab;

        private int itemIndex;
        
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
            if (!gameModel.SelectedToolDic.ContainsKey(index))
            {
                gameModel.SelectedToolDic.Add(index, new BindableProperty<int>());
            }
            icon.sprite = item.selections[gameModel.SelectedToolDic[index].Value].icon;
            itemName.text = item.name;
            selectName.text = item.selections[gameModel.SelectedToolDic[index].Value].selectionName;
            description.text = item.selections[gameModel.SelectedToolDic[index].Value].description;
            
            // 检查是否已购买，决定显示价格还是"equipped"
            var initialSelectedTool = item.selections[gameModel.SelectedToolDic[index].Value];
            bool isInitialPurchased = gameModel.PurchasedFoods.Contains(initialSelectedTool.selectionName);
            bool isInitialEquipped = gameModel.CurrentFoodType == initialSelectedTool.selectionName;
            
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
                priceText.text = initialSelectedTool.price.ToString();
            }
            for (int i = 0; i < item.selections.Length; i++)
            {
                var select = GameObject.Instantiate(selectionPrefab, selectionPrefab.transform.parent).GetComponent<ShopToolSelection>();
                select.gameObject.SetActive(true);
                select.Init(index, i);
            }

            gameModel.SelectedToolDic[itemIndex].Register(v =>
            {
                icon.sprite = item.selections[v].icon;
                selectName.text = item.selections[v].selectionName;
                selectName.text = item.selections[v].selectionName;
                description.text = item.selections[v].description;
                
                // 检查食物状态，决定显示内容
                var selectedTool = item.selections[v];
                bool isPurchased = this.GetModel<IGameModel>().PurchasedFoods.Contains(selectedTool.selectionName);
                bool isEquipped = this.GetModel<IGameModel>().CurrentFoodType == selectedTool.selectionName;
                
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
                
                UpdateButtonState();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            var gameModel = this.GetModel<IGameModel>();
            var configModel = this.GetModel<IConfigModel>();
            var selectedToolIndex = gameModel.SelectedToolDic[itemIndex].Value;
            var toolItem = configModel.ShopConfig.tools[itemIndex];
            var selectedTool = toolItem.selections[selectedToolIndex];
            
            // 检查是否已经购买过这个食物
            bool isPurchased = gameModel.PurchasedFoods.Contains(selectedTool.selectionName);
            
            Debug.Log($"更新按钮状态: {selectedTool.selectionName}, 已购买: {isPurchased}");
            
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
                var selectedToolIndex = gameModel.SelectedToolDic[itemIndex].Value;
                var toolItem = configModel.ShopConfig.tools[itemIndex];
                var selectedTool = toolItem.selections[selectedToolIndex];
                
                // 检查是否已经购买过
                bool isPurchased = gameModel.PurchasedFoods.Contains(selectedTool.selectionName);
                
                if (!isPurchased)
                {
                    // 未购买，执行购买逻辑
                    int price = selectedTool.price;
                    if (price <= this.GetModel<IAccountModel>().Coins.Value)
                    {
                        // 扣除金币
                        this.GetModel<IAccountModel>().Coins.Value -= price;
                        
                        // 添加到已购买列表
                        gameModel.PurchasedFoods.Add(selectedTool.selectionName);
                        
                        // 根据工具类型应用不同的效果
                        if (toolItem.name.ToLower() == "food")
                        {
                            // 设置当前食物类型（立即装备）
                            gameModel.CurrentFoodType = selectedTool.selectionName;
                            this.GetSystem<IUISystem>().ShowPrompt($"购买成功！食物皮肤已装备: {selectedTool.selectionName}");
                        }
                        else if (toolItem.name.ToLower() == "cursor")
                        {
                            // 应用光标类型
                            this.GetSystem<IUISystem>().ShowPrompt($"购买成功！光标皮肤已装备: {selectedTool.selectionName}");
                        }
                        
                        UpdateButtonState();
                        this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                    }
                    else
                    {
                        this.GetSystem<IUISystem>().ShowPrompt("Insufficient coins");
                    }
                }
                else
                {
                    // 已购买，执行装备逻辑
                    if (toolItem.name.ToLower() == "food")
                    {
                        // 设置当前食物类型
                        gameModel.CurrentFoodType = selectedTool.selectionName;
                        this.GetSystem<IUISystem>().ShowPrompt($"已装备食物皮肤: {selectedTool.selectionName}");
                    }
                    else if (toolItem.name.ToLower() == "cursor")
                    {
                        // 应用光标类型
                        this.GetSystem<IUISystem>().ShowPrompt($"已装备光标皮肤: {selectedTool.selectionName}");
                    }
                    
                    UpdateButtonState();
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                }
            });
        }
    }
}