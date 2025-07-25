using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class NoteItem : ViewControllerBase
    {
        public TextMeshProUGUI titleText;
        public Toggle thisToggle;

        private int noteIndex;
        
        public void Init(int index, ToggleGroup group)
        {
            noteIndex = index;
            titleText.text = $"Book{index + 1}";
            thisToggle.group = group;
        }

        public void ResetIndex(int index)
        {
            noteIndex = index;
        }

        private void Start()
        {
            thisToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    this.GetSystem<IGameSystem>().SendEvent(new RefreshNoteIndexEvent()
                    {
                        index = noteIndex
                    });
                }
            });
        }
    }
}