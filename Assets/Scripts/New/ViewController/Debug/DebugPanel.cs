using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BirdGame.DebugMode
{
    public class DebugPanel : ViewControllerBase
    {
        public Button closeButton;
        public Toggle tog_Bird;
        public Toggle tog_Shop;
        public GameObject birdEdit;
        public GameObject shopEdit;
        public TMP_InputField coinsInput;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                SceneManager.UnloadSceneAsync("DebugMode");
            });
            tog_Bird.isOn = true;
            tog_Shop.isOn = false;
            birdEdit.SetActive(true);
            shopEdit.SetActive(false);
            
            tog_Bird.onValueChanged.AddListener(isOn =>
            {
                birdEdit.SetActive(isOn);
            });
            
            tog_Shop.onValueChanged.AddListener(isOn =>
            {
                shopEdit.SetActive(isOn);
            });

            coinsInput.text = this.GetModel<IAccountModel>().Coins.Value.ToString();
            this.GetModel<IAccountModel>().Coins.Register(v =>
            {
                coinsInput.text = v.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            coinsInput.onEndEdit.AddListener(v=>
            {
                try
                {
                    this.GetModel<IAccountModel>().Coins.Value = int.Parse(v);
                    this.GetModel<ISaveModel>().AccountData.coins = int.Parse(v);
                }
                catch (Exception e)
                {
                    coinsInput.text = this.GetModel<IAccountModel>().Coins.Value.ToString();
                }
            });
        }
    }
}