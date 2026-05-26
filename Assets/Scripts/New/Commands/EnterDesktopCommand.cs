using System.Collections;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BirdGame
{
    /// <summary>
    /// 跳转到桌面模式
    /// </summary>
    public class EnterDesktopCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
#if !UNITY_STANDALONE_WIN
            Debug.LogWarning("Desktop mode is disabled on this platform.");
            this.GetUtility<IFullScreenUtility>().FullscreenMode();
            return;
#else
            this.GetModel<IDesktopBirdModel>().DesktopBirds.Clear();
            var birds = this.GetModel<IBirdModel>().BirdList;
            foreach (var birdData in birds)
            {
                if (birdData.isAddedToDesktop)
                {
                    this.GetModel<IDesktopBirdModel>().DesktopBirds.Add(new DesktopBird()
                    {
                        birdType = birdData.birdType,
                        isGrowUp = !birdData.bird.isSmall
                    });
                }
            }
            this.GetSystem<IBirdSystem>().ClearAllBirds();
            this.GetSystem<IUISystem>().HidePanel(UIPanel.MenuPanel);
            this.GetSystem<IUISystem>().HideAllPopups();
            var loadingModel = this.GetModel<ILoadingModel>();
            loadingModel.LoadingText.Value = "Enter Desktop";
            loadingModel.Progress.Value = 0;
            this.GetSystem<IMonoSystem>().StartCoroutine(LoadingDesktop());
#endif
        }

        private IEnumerator LoadingDesktop()
        {
            var op = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
            op.allowSceneActivation = false;
            while (!op.isDone)
            {
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                this.GetModel<ILoadingModel>().Progress.Value = progress;
                yield return null;
                
                if (op.progress >= 0.9f)
                {
                    op.allowSceneActivation = true;
                }
            }
        }
    }
}
