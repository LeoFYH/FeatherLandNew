using System.Text;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ScheduleItem : ViewControllerBase
    {
        public Toggle markToggle;
        public TMP_InputField scheduleInput;
        public Button deleteButton;
        public RectTransform line;

        private int scheduleIndex;

        public void Init(int index)
        {
            scheduleIndex = index;
            var data = this.GetModel<ISaveModel>().ScheduleData.scheduleList[index];
            if (data.isCompleted)
            {
                line.sizeDelta = new Vector2(1062, 5f);
            }
            else
            {
                line.sizeDelta = new Vector2(0, 5f);
            }
            //scheduleInput.textComponent.fontStyle = data.isCompleted ? FontStyles.Bold | FontStyles.Strikethrough : FontStyles.Bold;
            // 初始化时也设置字体粗细
            if (data.isCompleted)
            {
                scheduleInput.textComponent.fontWeight = FontWeight.Bold;
                scheduleInput.textComponent.outlineWidth = 0.2f;
            }
            else
            {
                scheduleInput.textComponent.fontWeight = FontWeight.Medium;
                scheduleInput.textComponent.outlineWidth = 0f;
            }
            markToggle.isOn = data.isCompleted;
            scheduleInput.text = data.scheduleText;
        }

        public void RefreshIndex(int index)
        {
            scheduleIndex = index;
        }

        private void Start()
        {
            markToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    line.sizeDelta = new Vector2(1062, 5f);
                }
                else
                {
                    line.sizeDelta = new Vector2(0, 5f);
                }
                //scheduleInput.textComponent.fontStyle = isOn ? FontStyles.Bold | FontStyles.Strikethrough : FontStyles.Bold;
                // 增加文字粗细使删除线更明显
                if (isOn)
                {
                    scheduleInput.textComponent.fontWeight = FontWeight.Bold; // 更粗的字体权重
                    scheduleInput.textComponent.outlineWidth = 0.2f; // 添加轻微描边让删除线更粗
                }
                else
                {
                    scheduleInput.textComponent.fontWeight = FontWeight.Medium; // 正常粗体
                    scheduleInput.textComponent.outlineWidth = 0f; // 移除描边
                }
                this.GetModel<ISaveModel>().ScheduleData.scheduleList[scheduleIndex].isCompleted = isOn;
            });
            
            deleteButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().SendEvent(new DeleteScheduleItemEvent()
                {
                    index = scheduleIndex
                });
            });
            
            scheduleInput.onEndEdit.AddListener(text =>
            {
                this.GetModel<ISaveModel>().ScheduleData.scheduleList[scheduleIndex].scheduleText = text;
            });
        }
    }
}