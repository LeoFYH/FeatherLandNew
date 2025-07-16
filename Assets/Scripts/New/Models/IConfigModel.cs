using QFramework;
using UnityEngine.AddressableAssets;

namespace BirdGame
{
    public interface IConfigModel : IModel
    {
        RadioConfig RadioConfig { get; set; }
        ShopConfig ShopConfig { get; set; }
        BirdConfig BirdConfig { get; set; }
    }

    public class ConfigModel : AbstractModel, IConfigModel
    {
        
        protected override void OnInit()
        {
        }

        public RadioConfig RadioConfig { get; set; }
        public ShopConfig ShopConfig { get; set; }
        public BirdConfig BirdConfig { get; set; }
    }
}