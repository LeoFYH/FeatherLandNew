using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnimationTest
{
    public class AnimationTest : MonoBehaviour
    {
        public TMP_Dropdown birdDropdown;
        //public TMP_Dropdown animDropdown;
        public Button playButton;
        public Button addButton;
        public Button deleteButton;
        public GameObject[] birds;
        public Transform content;
        public GameObject prefab;
        public AddPanel addPanel;
        public TextMeshProUGUI animStateText;
        
        private Animator currentBird;
        private int currAnimIndex;
        private List<AnimationItem> items = new List<AnimationItem>();
        private bool isPlaying;
        
        private void Start()
        {
            birdDropdown.onValueChanged.AddListener(LoadBird);
            addButton.onClick.AddListener(() =>
            {
                if(currentBird == null)
                    return;
                addPanel.ShowPanel(currentBird.runtimeAnimatorController.animationClips);
            });
            deleteButton.onClick.AddListener(() =>
            {
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    var item = items[i];
                    if (item.IsSelected)
                    {
                        items.RemoveAt(i);
                        Destroy(item.gameObject);
                    }
                }

                deleteButton.interactable = false;
            });
            addPanel.onAddAction += index =>
            {
                if (currentBird == null)
                    return;
                var clip = currentBird.runtimeAnimatorController.animationClips[index];
                var item = GameObject.Instantiate(prefab, content).GetComponent<AnimationItem>();
                item.Init(clip.name, index, 1);
                item.onRefreshToggleState += OnRefresh;
                items.Add(item);
            };
            //animDropdown.onValueChanged.AddListener(ShowAnim);
            playButton.onClick.AddListener(() =>
            {
                if (!isPlaying)
                {
                    isPlaying = true;
                    PlayAnimationList();
                    playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop";
                }
                else
                {
                    isPlaying = false;
                    currentBird.Play("Idle");
                    animStateText.text = "Idle";
                    playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
                }
            });
            birdDropdown.options.Clear();    
            for (int i = 0; i < birds.Length; i++)
            {
                birdDropdown.options.Add(new TMP_Dropdown.OptionData(birds[i].name));
            }
            birdDropdown.value = 1;
            deleteButton.interactable = false;
        }

        private void OnRefresh()
        {
            foreach (var item in items)
            {
                if (item.IsSelected)
                {
                    deleteButton.interactable = true;
                    return;
                }
            }

            deleteButton.interactable = false;
        }

        private void LoadBird(int index)
        {
            if (currentBird != null)
            {
                GameObject.Destroy(currentBird.transform.parent.gameObject);
                currentBird = null;
            }

            var obj = GameObject.Instantiate(birds[index]);
            obj.transform.localPosition = Vector3.zero;
            currentBird = obj.GetComponentInChildren<Animator>();
            ClearItems();
            isPlaying = false;
            playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
            currentBird.Play("Idle");
            animStateText.text = "Idle";
        }
        

        private void ClearItems()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                items.RemoveAt(i);
                Destroy(item.gameObject);
            }

            deleteButton.interactable = false;
        }

        private void PlayAnimationList()
        {
            Play(0);
        }

        private void Play(int index)
        {
            if (index >= items.Count)
            {
                Debug.Log("播放结束！");
                currentBird.Play("Idle");
                animStateText.text = "Idle";
                playButton.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
                isPlaying = false;
                return;
            }

            int count = items[index].PlayCount;
            Play(index, count);
        }

        private void Play(int index, int count)
        {
            if (count <= 0)
            {
                index++;
                Play(index);
                return;
            }

            count--;
            var clip = currentBird.runtimeAnimatorController.animationClips[items[index].AnimationIndex];
            float time = clip.length;
            currentBird.Play(clip.name);
            animStateText.text = clip.name;
            DOTween.Sequence().AppendCallback(() =>
            {
                Play(index, count);
            }).SetDelay(time);
        }
    }
}