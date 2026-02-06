using System;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    [RequireComponent(typeof(Image))]
    public class ButtonAnimSp : ViewControllerBase
    {
        public Sprite normal;
        public Sprite higLight;

        private Image thisImage;
        
        private void Awake()
        {
            thisImage = GetComponent<Image>();
            OnNormal();
        }

        public void OnNormal()
        {
            thisImage.sprite = normal;
            GetComponent<RectTransform>().sizeDelta = normal.rect.size;
        }

        public void OnHigLight()
        {
            thisImage.sprite = higLight;
            GetComponent<RectTransform>().sizeDelta = higLight.rect.size;
        }
    }
}