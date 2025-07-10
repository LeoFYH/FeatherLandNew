using QFramework;
using UnityEngine.AddressableAssets;

namespace BirdGame
{
    public interface IConfigModel : IModel
    {
        RadioConfig RadioConfig { get; }
        ShopConfig ShopConfig { get; }
        BirdConfig BirdConfig { get; }
    }

    public class ConfigModel : AbstractModel, IConfigModel
    {
        protected override void OnInit()
        {
            Addressables.LoadAssetAsync<RadioConfig>("RadioConfig").Completed += handle =>
            {
                RadioConfig = handle.Result;
                handle.Release();
            };

            Addressables.LoadAssetAsync<ShopConfig>("ShopConfig").Completed += handle =>
            {
                ShopConfig = handle.Result;
                handle.Release();
            };

            Addressables.LoadAssetAsync<BirdConfig>("BirdConfig").Completed += handle =>
            {
                BirdConfig = handle.Result;
                handle.Release();
            };
        }

        public RadioConfig RadioConfig { get; private set; }
        public ShopConfig ShopConfig { get; private set; }
        public BirdConfig BirdConfig { get; private set; }
    }
}