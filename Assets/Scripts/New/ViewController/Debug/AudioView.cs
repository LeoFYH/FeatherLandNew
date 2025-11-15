using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame.DebugMode
{
    public class AudioView : ViewControllerBase
    {
        public Button closeButton;
        public GameObject itemPrefab;
        public Transform content;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
            });

            var config = this.GetModel<IConfigModel>().RadioConfig;
            foreach (var environment in config.environments)
            {
                var obj = GameObject.Instantiate(itemPrefab, content);
                obj.GetComponent<AudioMixerItem>().Init(environment);
                obj.SetActive(true);
            }
        }
    }
}