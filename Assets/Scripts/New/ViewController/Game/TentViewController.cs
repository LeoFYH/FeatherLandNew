using System;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class TentViewController : ViewControllerBase
    {
        public Transform[] enterPoses; 
        public Transform endPos;
        public Transform createPos;
        public Transform[] exitPoses;
        public TextMeshProUGUI waitingText;
        public Image progressFill;
        public Button finishedButton;

        private IGameModel gameModel;
        
        private void Start()
        {
            gameModel = this.GetModel<IGameModel>(); 
            gameModel.CurrentTent = this;
            waitingText.text = $"{gameModel.EnteredBirds.Value}/2";
            gameModel.EnteredBirds.Register(v =>
            {
                waitingText.text = $"{v}/2";
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            progressFill.fillAmount = gameModel.HatchingProgress.Value;
            gameModel.HatchingProgress.Register(v =>
            {
                progressFill.fillAmount = v > 1 ? 1f : v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            finishedButton.gameObject.SetActive(false);
            gameModel.IsHatchingFinished.Register(v =>
            {
                finishedButton.gameObject.SetActive(v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            finishedButton.onClick.AddListener(() =>
            {
                //this.GetSystem<IUISystem>().ShowPopup(UIPopup.HatchingBirdPopup);
                CreateEgg();
            });
        }

          private void CreateEgg()
        {
            int birdIndex = this.GetModel<IGameModel>().CurrentHatchingBirdIndex;
            int parentType = this.GetModel<IBirdModel>().BirdList[birdIndex].birdType;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int hatchedType = parentType;
            var parent = config.GetBird(parentType, mapIndex, out int classIndex);
            if (parent != null)
            {
                var variants = config.sceneBirds[mapIndex].birdClasses[classIndex].birds;
                if (variants != null && variants.Count > 0)
                    hatchedType = variants[UnityEngine.Random.Range(0, variants.Count)].id;
            }
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Egg", obj =>
            {
                GameObject go = GameObject.Instantiate(obj);
                go.GetComponent<Egg>().SetBirdIndex(hatchedType);
                go.transform.position = Vector3.zero;
                this.GetModel<IGameModel>().CurrentHatchingBirdIndex = -1;
                this.GetModel<IGameModel>().EnteredBirds.Value = 0;
                this.GetModel<IGameModel>().IsHatchingFinished.Value = false;
            });
            this.GetSystem<IUISystem>().ShowMask();
            this.GetSystem<IGameSystem>().SendEvent<DisableButtonEvent>();
        }
    }
}