using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    [RequireComponent(typeof(Image))]
    public class ButtonHighlight : ViewControllerBase, IPointerEnterHandler, IPointerExitHandler
    {
        public Sprite normalSprite;
        public Sprite highlightSprite;

        private Image thisImage;
        
        private void Awake()
        {
            thisImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            thisImage.sprite = normalSprite;
        }

        private void OnDisable()
        {
            thisImage.sprite = normalSprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            thisImage.sprite = highlightSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            thisImage.sprite = normalSprite;
        }
    }
}