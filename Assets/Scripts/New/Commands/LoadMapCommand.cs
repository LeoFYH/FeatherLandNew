using QFramework;

namespace BirdGame
{
    /// <summary>
    /// 加载场景
    /// </summary>
    public class LoadMapCommand : AbstractCommand
    {
        private int mapIndex;
        
        public LoadMapCommand(int index)
        {
            mapIndex = index;
        }

        protected override void OnExecute()
        {
            //保存当前的场景鸟的信息并清除场景中的鸟
            this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
            //清除当前场景的鸟
            this.GetSystem<IBirdSystem>().ClearAllBirds();
            //显示加载界面
            this.GetModel<ILoadingModel>().LoadingText.Value = "Loading Scene";
            this.GetModel<ILoadingModel>().Progress.Value = 0;
            this.GetSystem<IUISystem>().ShowPanel(UIPanel.LoadingPanel);
            //加载新的地图
            this.GetSystem<ISceneSystem>().LoadScene(mapIndex, progress =>
            {
                this.GetModel<ILoadingModel>().Progress.Value = progress;
            }, OnLoadingComplete);
        }

        private void OnLoadingComplete()
        {
            //更新当前地图数据
            this.GetModel<ISaveModel>().BirdInfoData.currentMap = mapIndex;
            this.GetSystem<ISaveSystem>().SaveData();
            //加载新地图的鸟的信息
            this.GetSystem<IBirdSystem>().GenerateBirdsFromSave();
            this.GetSystem<IGameSystem>().CreateDecorations();
            //展示MenuPanel
            this.GetSystem<IUISystem>().ShowPanel(UIPanel.MenuPanel);
        }
    }
}