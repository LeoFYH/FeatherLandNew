using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class HatchingBirdPopup : UIBase
    {
        public Button closeButton;
        public Button openButton;

        private void Start()
        {
            openButton.onClick.AddListener(CreateEgg);
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.HatchingBirdPopup);
            });
        }
        
        private void CreateEgg()
        {
            int birdIndex = this.GetModel<IGameModel>().CurrentHatchingBirdIndex;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Egg", obj =>
            {
                GameObject go = GameObject.Instantiate(obj);
                go.GetComponent<Egg>().SetBirdIndex(this.GetModel<IBirdModel>().BirdList[birdIndex].birdType);
                go.transform.position = Vector3.zero;
                this.GetModel<IGameModel>().CurrentHatchingBirdIndex = -1;
                this.GetModel<IGameModel>().EnteredBirds.Value = 0;
                this.GetModel<IGameModel>().IsHatchingFinished.Value = false;
                this.GetSystem<IUISystem>().HidePopup(UIPopup.HatchingBirdPopup);
            });
            this.GetSystem<IUISystem>().ShowMask();
            this.GetSystem<IGameSystem>().SendEvent<DisableButtonEvent>();
        }
    }
}