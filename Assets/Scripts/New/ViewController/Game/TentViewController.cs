using System;
using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame
{
    public class TentViewController : ViewControllerBase
    {
        public Transform enterPos;
        public Transform endPos;
        public Transform createPos;
        public GameObject waitingUI;
        public TextMeshProUGUI waitingText;

        private IGameModel gameModel;
        
        private void Start()
        {
            gameModel = this.GetModel<IGameModel>(); 
            gameModel.CurrentTent = this;
            waitingText.text = $"{gameModel.HatchingBirds.Count}/2";
        }

        private void Update()
        {
            waitingText.text = $"{gameModel.HatchingBirds.Count}/2";
        }
    }
}