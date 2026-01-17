using System.Collections.Generic;
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
        public LocalizationText priceText;
        public Button buyButton;
        public TextMeshProUGUI buyButtonText;
        public GameObject selectionPrefab;
        public UIButtonHoverScale uiButtonHoverScale;

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
            if (item.selections[0].type == ToolType.BirdMaxCount)
            {
                int current = this.GetModel<IConfigModel>().BirdConfig.maxBirdCount +
                              this.GetModel<IBirdModel>().AddedBirdCount;
                string str1 = this.GetSystem<ILocalizationSystem>().GetString("Upgrade forest bird capacity to");
                string str2 = this.GetSystem<ILocalizationSystem>().GetString("Current bird capacity");
                description.SetKey(
                    $"<i>{str2}:{current}</i>");
                selectName.SetKey("Capacity Upgrade");
            }
            else
            {
                selectName.SetKey(item.selections[gameModel.SelectedToolDic[index].Value].selectionName);
                // 优先使用descriptionKey，如果没有设置则使用description
                var selectedTool = item.selections[gameModel.SelectedToolDic[index].Value];
                if (!string.IsNullOrEmpty(selectedTool.descriptionKey))
                {
                    string str1 = this.GetSystem<ILocalizationSystem>().GetString(selectedTool.descriptionKey);

                    description.SetKey(selectedTool.description);
                }
            }

            if (item.selections[0].type == ToolType.Food)
            {
                while (saveModel.AccountData.tools.Count <= itemIndex)
                {
                    saveModel.AccountData.tools.Add(new ToolInfo());
                }

                if (!saveModel.AccountData.tools[itemIndex].unlockedList
                        .Contains(0))
                {
                    saveModel.AccountData.tools[itemIndex].unlockedList.Add(0);
                }

                bool isInitialPurchased = saveModel.AccountData.tools[itemIndex].unlockedList
                    .Contains(gameModel.SelectedToolDic[index].Value) || gameModel.SelectedToolDic[index].Value == 0;
                bool isInitialEquipped = saveModel.AccountData.tools[itemIndex].equipedId ==
                                         gameModel.SelectedToolDic[index].Value;

                if (isInitialEquipped)
                {
                    // 已装备：显示"equipped"
                    priceText.SetKey("equipped");
                    buyButton.targetGraphic.color = new Color32(159,159,159,255);
                    buyButton.interactable = false;
                }
                else if (isInitialPurchased)
                {
                    // 已购买但未装备：显示"equip"
                    priceText.SetKey("equip");
                    buyButton.targetGraphic.color =Color.white;
                }
                else
                {
                    // 未购买：显示价格
                    priceText.ThisText.text = $"${item.selections[gameModel.SelectedToolDic[index].Value].price}";
                    buyButton.interactable = true;
                    buyButton.targetGraphic.color =Color.white;
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
                while (saveModel.AccountData.tools.Count <= itemIndex)
                {
                    saveModel.AccountData.tools.Add(new ToolInfo());
                }

                bool isInitialPurchased = saveModel.AccountData.tools[itemIndex].unlockedList
                    .Contains(gameModel.SelectedToolDic[index].Value + 1);
                if (isInitialPurchased)
                {
                    priceText.SetKey("Purchased");
                    buyButton.targetGraphic.color = new Color32(159, 159, 159, 255);
                    buyButton.enabled = false;
                }
                else
                {

                    buyButton.targetGraphic.color =Color.white;
                    buyButton.enabled = true;
                    priceText.ThisText.text = $"${item.selections[gameModel.SelectedToolDic[index].Value].price}";
                }

                bool initFirst = false;
                for (int i = 0; i < item.selections.Length; i++)
                {
                    var select = GameObject.Instantiate(selectionPrefab, selectionPrefab.transform.parent)
                        .GetComponent<ShopToolSelection>();
                    selections.Add(select);
                    select.gameObject.SetActive(true);
                    select.Init(index, i);

                    if (saveModel.AccountData.tools[itemIndex].unlockedList.Contains(i))
                    {
                        continue;
                    }

                    if (!initFirst)
                    {
                        initFirst = true;
                    }
                    else
                    {
                        select.SetActive(false);
                        var toggle = select.GetComponent<Toggle>();
                        toggle.enabled = false;
                        toggle.graphic.gameObject.SetActive(false);
                    }
                }
            }

            gameModel.SelectedToolDic[itemIndex].Register(v =>
            {
                // 优先使用descriptionKey，如果没有设置则使用description
                var selectedTool = item.selections[v];
                if (item.selections[v].type == ToolType.Food)
                {
                    selectName.SetKey(item.selections[v].selectionName);
                    if (!string.IsNullOrEmpty(selectedTool.descriptionKey))
                    {
                        string str1 = this.GetSystem<ILocalizationSystem>().GetString(selectedTool.descriptionKey);
                        string str2 = this.GetSystem<ILocalizationSystem>().GetString(selectedTool.description);
                        //description.ThisText.text = $"{str1}";
                    }
                    else
                    {
                        //description.SetKey(selectedTool.description);
                    }
                }
                else
                {
                    selectName.SetKey("Capacity Upgrade");
                    int current = this.GetModel<IConfigModel>().BirdConfig.maxBirdCount +
                                  this.GetModel<IBirdModel>().AddedBirdCount;
                    string str1 = this.GetSystem<ILocalizationSystem>()
                        .GetString("Upgrade forest bird capacity to");
                    string str2 = this.GetSystem<ILocalizationSystem>().GetString("Current bird capacity");
                    description.SetKey(
                        $"<i>{str2}:{current}</i>");
                }

                if (item.selections[0].type == ToolType.Food)
                {
                    var sp = item.selections[v].icon;
                    icon.sprite = sp;
                    if (sp != null)
                        icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size;
                    // 检查食物状态，决定显示内容
                    bool isPurchased = saveModel.AccountData.tools[index].unlockedList.Contains(v) || v == 0;
                    bool isEquipped = saveModel.AccountData.tools[index].equipedId == v;

                    if (isEquipped)
                    {
                        // 已装备：显示"equipped"
                        priceText.SetKey("equipped");
                        buyButton.interactable = false;
                        buyButton.targetGraphic.color = new Color32(159, 159, 159, 255);
                    }
                    else if (isPurchased)
                    {
                        // 已购买但未装备：显示"equip"
                        priceText.SetKey("equip");
                        buyButton.interactable = true;
                        buyButton.targetGraphic.color = Color.white;
                    }
                    else
                    {
                        // 未购买：显示价格
                        priceText.ThisText.text = $"${selectedTool.price}";
                        buyButton.interactable = true;
                        buyButton.targetGraphic.color = Color.white;
                    }
                }
                else if (item.selections[0].type == ToolType.BirdMaxCount)
                {
                    bool isPurchased = saveModel.AccountData.tools[index].unlockedList.Contains(v + 1);
                    if (isPurchased)
                    {
                        priceText.SetKey("Purchased");
                        buyButton.enabled = false;
                        buyButton.targetGraphic.color = new Color32(159, 159, 159, 255);
                    }
                    else
                    {
                        priceText.ThisText.text = $"${selectedTool.price}";
                        buyButton.enabled = true;
                        buyButton.targetGraphic.color = Color.white;
                    }
                }

                UpdateButtonState();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.GetModel<IAccountModel>().Coins.Register(v =>
            {
                UpdateButtonState();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                itemName.SetKey(item.name);
                if (item.selections[0].type == ToolType.BirdMaxCount)
                {
                    int current = this.GetModel<IConfigModel>().BirdConfig.maxBirdCount +
                                  this.GetModel<IBirdModel>().AddedBirdCount;
                    string str1 = this.GetSystem<ILocalizationSystem>()
                        .GetString("Upgrade forest bird capacity to");
                    string str2 = this.GetSystem<ILocalizationSystem>().GetString("Current bird capacity");
                    description.SetKey(
                        $"{str1} {item.selections[gameModel.SelectedToolDic[index].Value].selectionName}.\n<i>{str2}:{current}</i>");
                    selectName.SetKey("Capacity Upgrade");
                }
                else
                {
                    selectName.SetKey(item.selections[gameModel.SelectedToolDic[index].Value].selectionName);
                    // 优先使用descriptionKey，如果没有设置则使用description
                    var selectedTool = item.selections[gameModel.SelectedToolDic[index].Value];
                    if (!string.IsNullOrEmpty(selectedTool.descriptionKey))
                    {
                        string str1 = this.GetSystem<ILocalizationSystem>().GetString(selectedTool.descriptionKey);
                        string str2 = this.GetSystem<ILocalizationSystem>().GetString(selectedTool.description);
                        description.ThisText.text = $"{str1}";
                    }
                    else
                    {
                        description.SetKey(selectedTool.description);
                    }
                }


                if (item.selections[0].type == ToolType.Food)
                {
                    bool isInitialPurchased = saveModel.AccountData.tools[itemIndex].unlockedList
                                                  .Contains(gameModel.SelectedToolDic[index].Value) ||
                                              gameModel.SelectedToolDic[index].Value == 0;
                    bool isInitialEquipped = saveModel.AccountData.tools[itemIndex].equipedId ==
                                             gameModel.SelectedToolDic[index].Value;

                    if (isInitialEquipped)
                    {
                        // 已装备：显示"equipped"
                        priceText.SetKey("equipped");
                    }
                    else if (isInitialPurchased)
                    {
                        // 已购买但未装备：显示"equip"
                        priceText.SetKey("equip");
                    }
                    else
                    {
                        // 未购买：显示价格
                        priceText.ThisText.text =
                            $"${item.selections[gameModel.SelectedToolDic[index].Value].price}";
                    }
                }
                else if (item.selections[0].type == ToolType.BirdMaxCount)
                {
                    bool isInitialPurchased = saveModel.AccountData.tools[itemIndex].unlockedList
                        .Contains(gameModel.SelectedToolDic[index].Value + 1);
                    if (isInitialPurchased)
                    {
                        priceText.SetKey("Purchased");
                    }
                    else
                    {
                        priceText.ThisText.text =
                            $"${item.selections[gameModel.SelectedToolDic[index].Value].price}";
                    }
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            var configModel = this.GetModel<IConfigModel>();
            var gameModel = this.GetModel<IGameModel>();
            var saveModel = this.GetModel<ISaveModel>();
            var selectedToolIndex = gameModel.SelectedToolDic[itemIndex].Value;
            var toolItem = configModel.ShopConfig.tools[itemIndex];
            var selectedTool = toolItem.selections[selectedToolIndex];
            var item = this.GetModel<IConfigModel>().ShopConfig.tools[itemIndex];

            if (item.selections[0].type == ToolType.Food)
            {
                while (saveModel.AccountData.tools.Count <= itemIndex)
                {
                    saveModel.AccountData.tools.Add(new ToolInfo());
                }

                if (!saveModel.AccountData.tools[itemIndex].unlockedList
                        .Contains(0))
                {
                    saveModel.AccountData.tools[itemIndex].unlockedList.Add(0);
                }

                bool isInitialPurchased = saveModel.AccountData.tools[itemIndex].unlockedList
                                              .Contains(gameModel.SelectedToolDic[itemIndex].Value) ||
                                          gameModel.SelectedToolDic[itemIndex].Value == 0;
                bool isInitialEquipped = saveModel.AccountData.tools[itemIndex].equipedId ==
                                         gameModel.SelectedToolDic[itemIndex].Value;

                if (isInitialEquipped)
                {
                    // 已装备：显示"equipped"
                    priceText.SetKey("equipped");
                    buyButton.interactable = false;
                    uiButtonHoverScale.enabled = true;
                    buyButton.targetGraphic.color = new Color32(159, 159, 159, 255);
                    uiButtonHoverScale.localizationKey = "hasbuy";
                    buyButton.GetComponent<HoverButton>().isLessCoin = false;
                }
                else if (isInitialPurchased)
                {
                    // 已购买但未装备：显示"equip"
                    priceText.SetKey("equip");
                    buyButton.interactable = true;
                    buyButton.targetGraphic.color = Color.white;
                    uiButtonHoverScale.enabled = true;
                    uiButtonHoverScale.localizationKey = "hasbuy";
                    buyButton.GetComponent<HoverButton>().isLessCoin = false;
                }
                else
                {
                    // 未购买：显示价格
                    priceText.ThisText.text = $"${item.selections[gameModel.SelectedToolDic[itemIndex].Value].price}";
                    if (item.selections[gameModel.SelectedToolDic[itemIndex].Value].price >
                        this.GetModel<IAccountModel>().Coins.Value)
                    {
                        uiButtonHoverScale.enabled = true;
                        uiButtonHoverScale.localizationKey = "Insufficient coins";
                        buyButton.GetComponent<HoverButton>().isLessCoin = true;
                        buyButton.interactable = false;
                        buyButton.targetGraphic.color = new Color32(159, 159, 159, 255);
                    }
                    else
                    {
                        buyButton.GetComponent<HoverButton>().isLessCoin = false;
                        uiButtonHoverScale.enabled = true;
                        uiButtonHoverScale.localizationKey = "can buy";
                        buyButton.interactable = true;
                        buyButton.targetGraphic.color = Color.white;
                    }

                }
            }
            else if (item.selections[0].type == ToolType.BirdMaxCount)
            {
                while (saveModel.AccountData.tools.Count <= itemIndex)
                {
                    saveModel.AccountData.tools.Add(new ToolInfo());
                }

                bool isInitialPurchased = saveModel.AccountData.tools[itemIndex].unlockedList
                    .Contains(gameModel.SelectedToolDic[itemIndex].Value + 1);
                if (isInitialPurchased)
                {
                    priceText.SetKey("Purchased");
                    buyButton.enabled = false;
                    buyButton.targetGraphic.color = new Color32(159, 159, 159, 255);
                    uiButtonHoverScale.enabled = true;
                    buyButton.GetComponent<HoverButton>().isLessCoin = false;
                    uiButtonHoverScale.localizationKey = "hasbuy";
                }
                else
                {
                    priceText.ThisText.text = $"${item.selections[gameModel.SelectedToolDic[itemIndex].Value].price}";
                    if (item.selections[gameModel.SelectedToolDic[itemIndex].Value].price >
                        this.GetModel<IAccountModel>().Coins.Value)
                    {
                        uiButtonHoverScale.enabled = true;
                        uiButtonHoverScale.localizationKey = "Insufficient coins";
                        buyButton.interactable = false;
                        buyButton.GetComponent<HoverButton>().isLessCoin = true;
                        buyButton.targetGraphic.color = new Color32(159, 159, 159, 255);
                    }
                    else
                    {
                        uiButtonHoverScale.enabled = true;
                        uiButtonHoverScale.localizationKey = "can buy";
                        buyButton.interactable = true;
                        buyButton.GetComponent<HoverButton>().isLessCoin = false;
                        buyButton.targetGraphic.color = Color.white;
                    }
                }
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
                if (toolItem.selections[0].type == ToolType.BirdMaxCount)
                {
                    isPurchased = saveModel.AccountData.tools[itemIndex].unlockedList.Contains(selectedToolIndex + 1);
                }

                if (!isPurchased)
                {
                    // 未购买，执行购买逻辑
                    int price = selectedTool.price;
                    if (price <= this.GetModel<IAccountModel>().Coins.Value)
                    {

                        // 扣除金币
                        this.GetModel<IAccountModel>().Coins.Value -= price;

                        // 添加到已购买列表


                        saveModel.AccountData.tools[itemIndex].equipedId = selectedToolIndex;
                        // 根据工具类型应用不同的效果
                        if (toolItem.name.ToLower() == "food")
                        {
                            // 设置当前食物类型（立即装备）
                            // string text = this.GetSystem<ILocalizationSystem>()
                            //     .GetString("Purchase successful! Food skins are equipped:");
                            // string equipName = this.GetSystem<ILocalizationSystem>()
                            //     .GetString(selectedTool.selectionName);
                            // this.GetSystem<IUISystem>().ShowPrompt($"{text} {equipName}");
                            saveModel.AccountData.tools[itemIndex].unlockedList.Add(selectedToolIndex);
                            //this.GetSystem<IUISystem>().ShowPrompt($"购买成功！食物皮肤已装备: {selectedTool.selectionName}");
                        }
                        else if (toolItem.name.ToLower() == "cursor")
                        {
                            // 应用光标类型
                            // string text = this.GetSystem<ILocalizationSystem>()
                            //     .GetString("Purchase successful! Cursor skins are equipped:");
                            // string equipName = this.GetSystem<ILocalizationSystem>()
                            //     .GetString(selectedTool.selectionName);
                            // this.GetSystem<IUISystem>().ShowPrompt($"{text} {equipName}");
                            //this.GetSystem<IUISystem>().ShowPrompt($"{text} {selectedTool.selectionName}");
                            saveModel.AccountData.tools[itemIndex].unlockedList.Add(selectedToolIndex);
                        }
                        else if (toolItem.selections[0].type == ToolType.BirdMaxCount)
                        {
                            saveModel.AccountData.tools[itemIndex].unlockedList.Add(selectedToolIndex + 1);
                            this.GetModel<IBirdModel>().AddedBirdCount += 10;
                            //this.GetModel<ISaveModel>().AccountData.addedMaxBirdValue += selectedTool.addCount;
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
                        //this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                        //this.GetSystem<IGameSystem>().SendEvent<OnShopCloseEvent>();

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
                        // string text = this.GetSystem<ILocalizationSystem>().GetString("Food skin equipped:");
                        // string equipName = this.GetSystem<ILocalizationSystem>()
                        //     .GetString(selectedTool.selectionName);
                        // this.GetSystem<IUISystem>().ShowPrompt($"{text} {equipName}");
                        //this.GetSystem<IUISystem>().ShowPrompt($"{text} {selectedTool.selectionName}");
                    }
                    else if (toolItem.name.ToLower() == "cursor")
                    {
                        // 应用光标类型
                        // string text = this.GetSystem<ILocalizationSystem>().GetString("Cursor skin equipped:");
                        // string equipName = this.GetSystem<ILocalizationSystem>()
                        //     .GetString(selectedTool.selectionName);
                        // this.GetSystem<IUISystem>().ShowPrompt($"{text} {equipName}");
                        //this.GetSystem<IUISystem>().ShowPrompt($"{text} {selectedTool.selectionName}");
                    }

                    UpdateButtonState();
                    //this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                    //this.GetSystem<IGameSystem>().SendEvent<OnShopCloseEvent>();
                }
            });
        }
    }
}