using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class ScheduleItem : ViewControllerBase
    {
        public Toggle markToggle;
        public TMP_InputField scheduleInput;
        public Button deleteButton;

        private int scheduleIndex;

        public void Init(int index)
        {
            scheduleIndex = index;
            var data = this.GetModel<ISaveModel>().ScheduleData.scheduleList[index];
            scheduleInput.textComponent.fontStyle = data.isCompleted ? FontStyles.Bold | FontStyles.Strikethrough : FontStyles.Bold;
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
                scheduleInput.textComponent.fontStyle = isOn ? FontStyles.Bold | FontStyles.Strikethrough : FontStyles.Bold;
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