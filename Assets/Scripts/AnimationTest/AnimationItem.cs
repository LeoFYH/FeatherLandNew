using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnimationTest
{
    public class AnimationItem : MonoBehaviour
    {
        public Action onRefreshToggleState;
        public int AnimationIndex { get; private set; }
        public int PlayCount { get; private set; }
        public bool IsSelected { get; private set; }

        public TextMeshProUGUI nameText;
        public TMP_InputField countInput;
        private Toggle thisToggle;

        private void Start()
        {
            thisToggle = GetComponent<Toggle>();
            countInput.onValueChanged.AddListener(v =>
            {
                PlayCount = int.Parse(v);
            });
            
            thisToggle.onValueChanged.AddListener(isOn =>
            {
                IsSelected = isOn;
                onRefreshToggleState?.Invoke();
            });
            thisToggle.isOn = false;
        }

        public void Init(string animName, int index, int count)
        {
            nameText.text = animName;
            AnimationIndex = index;
            countInput.text = count.ToString();
            PlayCount = count;
        }
    }
}