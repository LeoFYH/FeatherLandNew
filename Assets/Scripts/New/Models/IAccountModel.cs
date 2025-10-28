using QFramework;

namespace BirdGame
{
    public interface IAccountModel : IModel
    {
        /// <summary>
        /// 游戏货币
        /// </summary>
        BindableProperty<float> Coins { get; }
    }

    public class AccountModel : AbstractModel, IAccountModel
    {
        protected override void OnInit()
        {
        }

        public BindableProperty<float> Coins { get; } = new BindableProperty<float>(600f);
    }
}