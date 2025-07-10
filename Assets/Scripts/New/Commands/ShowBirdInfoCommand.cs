using QFramework;

namespace BirdGame
{
    /// <summary>
    /// 展示鸟信息命令
    /// </summary>
    public class ShowBirdInfoCommand : AbstractCommand
    {
        private int _price;
        private string _title;
        private string _desc;
        private int _incomeForBig;
        private float _progressForBig;
        private float _progressForFavorability;
        private bool _cursorOn;
        
        public ShowBirdInfoCommand(int price, string title, string description, int incomeForBig, float progressForBig,
            float progressForFavorability, bool cursorOn)
        {
            _price = price;
            _desc = description;
            _title = title;
            _incomeForBig = incomeForBig;
            _progressForBig = progressForBig;
            _progressForFavorability = progressForFavorability;
            _cursorOn = cursorOn;
        }

        protected override void OnExecute()
        {
            var uiSystem = this.GetSystem<IUISystem>();
            uiSystem.ShowPopup(UIPopup.InfoPopup);
            uiSystem.GetPopup<InfoPopup>(UIPopup.InfoPopup).Init(_price, _title, _desc, _incomeForBig, _progressForBig, _progressForFavorability, _cursorOn);
        }
    }
}