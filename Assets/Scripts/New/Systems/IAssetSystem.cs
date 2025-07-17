using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace BirdGame
{
    /// <summary>
    /// 资源管理系统 - 游戏内所有资源在此管理
    /// </summary>
    public interface IAssetSystem : ISystem
    {
        void LoadAssetAsync<T>(string assetName, Action<T> onComplete) where T : Object;
    }

    public class AssetSystem : AbstractSystem, IAssetSystem
    {
        private Dictionary<string, AsyncOperationHandle> handleDic = new Dictionary<string, AsyncOperationHandle>();
        
        protected override void OnInit()
        {
            LoadAssetAsync<RadioConfig>("RadioConfig", config =>
            {
                this.GetModel<IConfigModel>().RadioConfig = config;
                var radioModel = this.GetModel<IRadioModel>();
                radioModel.SongName.Value = config.musics[radioModel.SongIndex].songName;
            });
            LoadAssetAsync<ShopConfig>("ShopConfig", config =>
            {
                this.GetModel<IConfigModel>().ShopConfig = config;
            });
            LoadAssetAsync<BirdConfig>("BirdConfig", config =>
            {
                this.GetModel<IConfigModel>().BirdConfig = config;
            });
        }

        public void LoadAssetAsync<T>(string assetName, Action<T> onComplete) where T : Object
        {
            if (handleDic.ContainsKey(assetName))
            {
                var handle = handleDic[assetName];
                onComplete?.Invoke(handle.Result as T);
                return;
            }

            handleDic.Add(assetName, new AsyncOperationHandle());
            Addressables.LoadAssetAsync<T>(assetName).Completed += handle =>
            {
                onComplete?.Invoke(handle.Result);
                handleDic[assetName] = handle;
            };
        }
    }
}