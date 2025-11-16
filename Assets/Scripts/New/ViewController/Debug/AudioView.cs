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
            foreach (var item in config.environments)
            {
                var obj = GameObject.Instantiate(itemPrefab, content);
                obj.GetComponent<AudioMixerItem>().Init(item);
                obj.SetActive(true);
            }

            foreach (var item in config.musicItems)
            {
                var obj = GameObject.Instantiate(itemPrefab, content);
                obj.GetComponent<AudioMixerItem>().Init(item);
                obj.SetActive(true);
            }

            var clickObj = GameObject.Instantiate(itemPrefab, content);
            clickObj.GetComponent<AudioMixerItem>().Init(config.click);
            clickObj.SetActive(true);
            
            var dropFoodObj = GameObject.Instantiate(itemPrefab, content);
            dropFoodObj.GetComponent<AudioMixerItem>().Init(config.dropFood);
            dropFoodObj.SetActive(true);
            
            var strokeObj = GameObject.Instantiate(itemPrefab, content);
            strokeObj.GetComponent<AudioMixerItem>().Init(config.stroke);
            strokeObj.SetActive(true);
            
            var growUpObj = GameObject.Instantiate(itemPrefab, content);
            growUpObj.GetComponent<AudioMixerItem>().Init(config.growUp);
            growUpObj.SetActive(true);
        }
    }
}