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
        public Transform enterPos;
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
                this.GetSystem<IUISystem>().ShowPopup(UIPopup.HatchingBirdPopup);
            });
        }
    }
}