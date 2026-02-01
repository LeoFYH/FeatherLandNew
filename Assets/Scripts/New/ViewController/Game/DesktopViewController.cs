using System;
using QFramework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace BirdGame
{
    public class DesktopViewController : ViewControllerBase
    {
        public GameObject exitPop;

        private void Start()
        {
            if (exitPop.activeSelf)
                exitPop.SetActive(false);
#if !UNITY_EDITOR
            this.GetSystem<IDesktopSystem>().EnableDesktopMode();
            this.GetSystem<IDesktopSystem>().SetClickThrough(true);
#endif
            int count = this.GetModel<IDesktopBirdModel>().DesktopBirds.Count;
            for (int i = 0; i < count; i++)
            {
                CreateBird(this.GetModel<IDesktopBirdModel>().DesktopBirds[i].birdType, this.GetModel<IDesktopBirdModel>().DesktopBirds[i].isGrowUp);
            }
        }
        
        private void CreateBird(int birdIndex, bool isGrowUp)
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var asset = config.GetBird(birdIndex, mapIndex).prefab;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>(asset.AssetGUID, obj =>
            {
                GameObject go = GameObject.Instantiate(obj);
                var agent = go.GetComponent<NavMeshAgent>();
                agent.enabled = false;

                var point = NavigationManager.Instance.GetRandomTarget(3);
                go.transform.position = new Vector3(point.x, point.y, 0);
                agent.enabled = true;
                go.GetComponent<Brid>().isDesktopBird = true;
                if (isGrowUp)
                {
                    go.GetComponent<Brid>().isSmall = false;
                    // 成鸟：保持原始大小
                    go.transform.localScale = Vector3.one * go.GetComponent<Brid>().AdultBirdSize;
                }
            });
            
        }

        public void OnExitClick()
        {
            exitPop.SetActive(!exitPop.activeSelf);
            if(exitPop.activeSelf)
                exitPop.GetComponent<UIBase>().OnShowPanel();
            this.GetSystem<IDesktopSystem>().SetClickThrough(!exitPop.activeSelf);
        }
    }
}