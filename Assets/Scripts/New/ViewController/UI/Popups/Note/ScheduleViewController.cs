using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ScheduleViewController : ViewControllerBase
    {
        public Transform content;
        public GameObject prefab;
        public Button addButton;

        private int currentIndex;
        private List<ScheduleItem> items = new List<ScheduleItem>();

        private void Start()
        {
            var data = this.GetModel<ISaveModel>().ScheduleData;
            
            addButton.onClick.AddListener(() =>
            {
                var item = GameObject.Instantiate(prefab, content).GetComponent<ScheduleItem>();
                int index = items.Count;
                item.transform.SetSiblingIndex(index);
                var itemData = new ScheduleItemData();
                data.scheduleList.Add(itemData);
                item.Init(index);
                items.Add(item);
            });

            this.RegisterEvent<DeleteScheduleItemEvent>(evt =>
            {
                var item = items[evt.index];
                items.RemoveAt(evt.index);
                data.scheduleList.RemoveAt(evt.index);
                int count = items.Count;
                for (int i = evt.index; i < count; i++)
                {
                    items[i].RefreshIndex(i);
                }
                GameObject.Destroy(item.gameObject);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            int count = data.scheduleList.Count;
            for (int i = 0; i < count; i++)
            {
                var item = GameObject.Instantiate(prefab, content).GetComponent<ScheduleItem>();
                item.transform.SetSiblingIndex(i);
                item.Init(i);
                items.Add(item);
            }
        }
    }
}