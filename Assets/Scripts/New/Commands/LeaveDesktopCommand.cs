using System.Collections;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BirdGame
{
    /// <summary>
    /// 离开桌面模式
    /// </summary>
    public class LeaveDesktopCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            this.GetSystem<IDesktopSystem>().DisableDesktopMode();
            int id = this.GetModel<ISaveModel>().SettingData.screenMode;
            if (id == 0)
            {
                this.GetUtility<IFullScreenUtility>().WindowedMode();
                Debug.Log("WindowedMode");
            }
            else if (id == 1)
            {
                this.GetUtility<IFullScreenUtility>().WallpaperMode();
                Debug.Log("WallpaperMode");
            }
            else if (id == 2)
            {
                this.GetUtility<IFullScreenUtility>().FullscreenMode();
                Debug.Log("FullscreenMode");
            }
            this.GetSystem<IMonoSystem>().StartCoroutine(LoadMapDelay());
        }

        private IEnumerator LoadMapDelay()
        {
            while (SceneManager.GetActiveScene().buildIndex != 0)
            {
                yield return null;
            }
            
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            Debug.Log("地图id：" + mapIndex);
            this.SendCommand(new LoadMapCommand(mapIndex));
        }
    }
}