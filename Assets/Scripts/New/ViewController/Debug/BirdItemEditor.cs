using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace BirdGame.DebugMode
{
    public class BirdItemEditor : ViewControllerBase
    {
        public TMP_InputField idInput;
        public Toggle tog_canFly;
        public Toggle tog_canFlyHorizontal;
        public Toggle tog_canFlyWait;
        public TMP_InputField incomeSmall;
        public TMP_InputField incomeBig;
        public TMP_InputField priceSmall;
        public TMP_InputField priceBig;
        public TMP_InputField clickIncome;
        public TMP_InputField clickIncomeForFiveTimes;
        public TMP_InputField descriptionKey;
        public TMP_InputField habitatKey;
        public TMP_InputField growthForBig;
        public TMP_InputField growthPerFood;
        public TMP_InputField growthPerMinute;
        public Image icon;
        public Button createButton;
        
        public void Init(BirdItem item)
        {
            icon.sprite = item.preview;
            idInput.text = item.id.ToString();
            tog_canFly.isOn = item.canFly;
            tog_canFlyHorizontal.isOn = item.canFlyHorizontal;
            tog_canFlyWait.isOn = item.canFlyWait;
            incomeSmall.text = item.eraningForSmall.ToString();
            incomeBig.text = item.eraningForBig.ToString();
            priceSmall.text = item.priceForSmall.ToString();
            priceBig.text = item.priceForBig.ToString();
            clickIncome.text = item.clickEarning.ToString();
            clickIncomeForFiveTimes.text = item.clickEarningForFiveTimes.ToString();
            descriptionKey.text = item.description;
            habitatKey.text = item.habitat;
            growthForBig.text = item.totalExp.ToString();
            growthPerFood.text = item.eatExp.ToString();
            growthPerMinute.text = item.autoExp.ToString();
            
            createButton.onClick.AddListener(() =>
            {
                CreateBird(item.id);
            });
            
            idInput.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.id = int.Parse(v);
                }
                catch (Exception e)
                {
                    idInput.text = item.id.ToString();
                }
            });
            tog_canFly.onValueChanged.AddListener(isOn =>
            {
                item.canFly = isOn;
            });
            tog_canFlyHorizontal.onValueChanged.AddListener(isOn =>
            {
                item.canFlyHorizontal = isOn;
            });
            tog_canFlyWait.onValueChanged.AddListener(isOn =>
            {
                item.canFlyWait = isOn;
            });
            
            incomeSmall.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.eraningForSmall = float.Parse(v);
                }
                catch (Exception e)
                {
                    incomeSmall.text = item.eraningForSmall.ToString();
                }
            });
            incomeBig.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.eraningForBig = float.Parse(v);
                }
                catch (Exception e)
                {
                    incomeBig.text = item.eraningForBig.ToString();
                }
            });
            priceSmall.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.priceForSmall = float.Parse(v);
                }
                catch (Exception e)
                {
                    priceSmall.text = item.priceForSmall.ToString();
                }
            });
            priceBig.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.priceForBig = float.Parse(v);
                }
                catch (Exception e)
                {
                    priceBig.text = item.priceForBig.ToString();
                }
            });
            
            clickIncome.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.clickEarning = float.Parse(v);
                }
                catch (Exception e)
                {
                    clickIncome.text = item.clickEarning.ToString();
                }
            });
            
            clickIncomeForFiveTimes.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.clickEarningForFiveTimes = float.Parse(v);
                }
                catch (Exception e)
                {
                    clickIncomeForFiveTimes.text = item.clickEarningForFiveTimes.ToString();
                }
            });
            
            descriptionKey.onValueChanged.AddListener(v =>
            {
                item.description = v;
            });
            habitatKey.onValueChanged.AddListener(v =>
            {
                item.habitat = v;
            });
            
            growthForBig.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.totalExp = float.Parse(v);
                }
                catch (Exception e)
                {
                    growthForBig.text = item.totalExp.ToString();
                }
            });
            
            growthPerFood.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.eatExp = float.Parse(v);
                }
                catch (Exception e)
                {
                    growthPerFood.text = item.eatExp.ToString();
                }
            });
            growthPerMinute.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.autoExp = float.Parse(v);
                }
                catch (Exception e)
                {
                    growthPerMinute.text = item.autoExp.ToString();
                }
            });
            
        }
        
        private void CreateBird(int birdIndex)
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            GameObject go = GameObject.Instantiate(config.GetBird(birdIndex, mapIndex).prefab);
            this.GetModel<IBirdModel>().AddBird(birdIndex, go.GetComponent<Brid>());
            this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
            if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].eggList.Count > 0)
                this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].eggList.RemoveAt(0);
            var agent = go.GetComponent<NavMeshAgent>();
            agent.enabled = false;

            var point = NavigationManager.Instance.GetRandomTarget(3);
            go.transform.position = new Vector3(point.x, point.y, 0);
            agent.enabled = true;
        }
    }
}