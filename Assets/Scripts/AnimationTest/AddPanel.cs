using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnimationTest
{
    public class AddPanel : MonoBehaviour
    {
        public Action<int> onAddAction;
        public Transform content;
        public GameObject prefab;
        public Button addButton;
        public Button closeButton;

        private List<AddItem> items = new List<AddItem>();
        
        public void ShowPanel(AnimationClip[] clips)
        {
            Clear();
            for (int i = 0; i < clips.Length; i++)
            {
                var item = GameObject.Instantiate(prefab, content).GetComponent<AddItem>();
                item.Init(i, clips[i].name);
                item.ThisToggle.group = content.GetComponent<ToggleGroup>();
                items.Add(item);
            }
            gameObject.SetActive(true);
        }

        private void Start()
        {
            addButton.onClick.AddListener(() =>
            {
                foreach (var item in items)
                {
                    if (item.ThisToggle.isOn)
                    {
                        onAddAction?.Invoke(item.Index);
                        break;
                    }
                }
                gameObject.SetActive(false);
            });
            
            closeButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
            });
        }

        private void Clear()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                items.RemoveAt(i);
                Destroy(item.gameObject);
            }
        }
    }
}