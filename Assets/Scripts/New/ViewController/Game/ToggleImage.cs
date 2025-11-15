using System;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ToggleImage : ViewControllerBase
    {
        public Image graphic;
        public Sprite backgroundUnselect;
        public Sprite backgroundSelected;
        public Sprite graphicUnselect;
        public Sprite graphicSelected;

        private Toggle thisToggle;
        
        private void Start()
        {
            thisToggle = GetComponent<Toggle>();
            
            thisToggle.onValueChanged.AddListener(InitToggle);
            InitToggle(thisToggle.isOn);
        }

        private void InitToggle(bool isOn)
        {
            graphic.sprite = isOn ? graphicSelected : graphicUnselect;
            thisToggle.image.sprite = isOn ? backgroundSelected : backgroundUnselect;
        }
    }
}