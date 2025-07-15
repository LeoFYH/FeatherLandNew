using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnimationTest
{
    public class AddItem : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public Toggle ThisToggle {
            get
            {
                if (toggle == null)
                    toggle = GetComponent<Toggle>();
                return toggle;
            }
        }
        public int Index { get; private set; }

        private Toggle toggle;
        
        public void Init(int index, string animName)
        {
            Index = index;
            nameText.text = animName;
        }
    }
}