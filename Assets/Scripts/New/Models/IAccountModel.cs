using QFramework;

namespace BirdGame
{
    public interface IAccountModel : IModel
    {
        /// <summary>
        /// 游戏货币
        /// </summary>
        BindableProperty<int> Coins { get; }
    }

    public class AccountModel : AbstractModel, IAccountModel
    {
        protected override void OnInit()
        {
            
        }

        public BindableProperty<int> Coins { get; } = new BindableProperty<int>(100);
    }
}