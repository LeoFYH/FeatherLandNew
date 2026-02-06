using DG.Tweening;
using QFramework;
using UnityEngine;

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
            this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
            
            // 切换地图时关闭所有popup界面
            this.GetSystem<IUISystem>().HideAllPopups();
            foreach (var select in this.GetModel<IGameModel>().SelectedToolDic)
            {
                select.Value.Value = 0;
            }
            this.SendEvent<ClearFoodEvent>();
            var gameModel = this.GetModel<IGameModel>();
            gameModel.CurrentTent = null;
            gameModel.EnteredBirds.Value = 0;
            gameModel.IsHatchingFinished.Value = false;
            gameModel.HatchingProgress.Value = 0;
            gameModel.HatchingBirds.Clear();
            
            //清除当前场景的鸟
            this.GetModel<IDesktopBirdModel>().DesktopBirds.Clear();
          
            this.GetSystem<IBirdSystem>().ClearAllBirds();

            this.SendEvent<ClearDecorationsEvent>();
            //显示加载界面
            this.GetModel<ILoadingModel>().LoadingText.Value = "Loading Scene";
            this.GetModel<ILoadingModel>().Progress.Value = 0;
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
            //加载新地图的鸟的信息
            this.GetSystem<IBirdSystem>().GenerateBirdsFromSave();
            this.GetSystem<IGameSystem>().CreateDecorations();
            //展示MenuPanel
            this.GetSystem<IUISystem>().ShowPanel(UIPanel.MenuPanel);
            DOTween.Sequence().AppendCallback(() =>
            {
                this.GetSystem<IGameSystem>().SendEvent<EnableHoverScaleEvent>();
            }).SetDelay(0.2f);
        }
        
     
    }
}