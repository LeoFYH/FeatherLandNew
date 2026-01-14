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
        private const int MAX_TODO_COUNT = 9; // 最多可创建9个待办事项

        private void Start()
        {
            var data = this.GetModel<ISaveModel>().ScheduleData;
            
            addButton.onClick.AddListener(() =>
            {
                // 检查是否已达到待办事项上限
                if (items.Count >= MAX_TODO_COUNT)
                {
                    Debug.Log($"已达到待办事项上限：{MAX_TODO_COUNT}个");
                    // 可以在这里显示提示信息给玩家
                    return;
                }
                
                var item = GameObject.Instantiate(prefab, content).GetComponent<ScheduleItem>();
                int index = items.Count;
                item.transform.SetSiblingIndex(index);
                var itemData = new ScheduleItemData();
                data.scheduleList.Add(itemData);
                item.Init(index);
                items.Add(item);
                
                // 更新按钮状态
                UpdateAddButtonState();
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
                
                // 删除待办事项后更新按钮状态
                UpdateAddButtonState();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            int count = data.scheduleList.Count;
            for (int i = 0; i < count; i++)
            {
                var item = GameObject.Instantiate(prefab, content).GetComponent<ScheduleItem>();
                item.transform.SetSiblingIndex(i);
                item.Init(i);
                items.Add(item);
            }
            
            // 初始化时更新按钮状态
            UpdateAddButtonState();
        }
        
        /// <summary>
        /// 更新添加按钮的状态（根据待办事项数量）
        /// </summary>
        private void UpdateAddButtonState()
        {
            if (addButton != null)
            {
                // 如果已达到上限，禁用按钮
                addButton.interactable = items.Count < MAX_TODO_COUNT;
                
                // 可选：修改按钮的视觉效果
                if (items.Count >= MAX_TODO_COUNT)
                {
                    Debug.Log($"待办事项已达上限 {items.Count}/{MAX_TODO_COUNT}");
                }
            }
        }
    }
}