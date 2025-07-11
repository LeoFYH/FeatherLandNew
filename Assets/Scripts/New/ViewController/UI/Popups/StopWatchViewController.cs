using System.Collections;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class StopWatchViewController : ViewControllerBase
    {
        public TextMeshProUGUI hourText;
        public TextMeshProUGUI minuteText;
        public TextMeshProUGUI secondText;
        public Button refreshButton;
        public Button startButton;
        public Button stopButton;

        private float timer = 0f;
        
        private void Start()
        {
            refreshButton.onClick.AddListener(() =>
            {
                timer = 0;
            });
            
            startButton.onClick.AddListener(() =>
            {
                this.GetModel<IGameModel>().StopWatchCoroutine = StartCoroutine(nameof(StartTimer));
                startButton.interactable = false;
                stopButton.interactable = true;
            });
            stopButton.onClick.AddListener(() =>
            {
                StopCoroutine(this.GetModel<IGameModel>().StopWatchCoroutine);
                this.GetModel<IGameModel>().StopWatchCoroutine = null;
                startButton.interactable = true;
                stopButton.interactable = false;
            });

            startButton.interactable = this.GetModel<IGameModel>().StopWatchCoroutine == null;
            stopButton.interactable = this.GetModel<IGameModel>().StopWatchCoroutine != null;
        }

        private IEnumerator StartTimer()
        {
            var frame = new WaitForFixedUpdate();
            while (true)
            {
                int totalSeconds = (int)timer;
                hourText.text = string.Format("{0:00}", totalSeconds / 3600);
                minuteText.text = string.Format("{0:00}", totalSeconds / 60 % 60);
                secondText.text = string.Format("{0:00}", totalSeconds % 60);
                yield return frame;
                timer += Time.fixedDeltaTime;
            }
        }
    }
}