using System;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 场景管理 -- 后续地图系统切换场景
    /// </summary>
    public interface ISceneSystem : ISystem
    {
        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="index"></param>
        void LoadScene(int index, Action<float> onProgress = null, Action onComplete = null);
        /// <summary>
        /// 隐藏当前场景
        /// </summary>
        void HideCurrentScene();
    }

    public class SceneSystem : AbstractSystem, ISceneSystem
    {
        private GameObject currentScene = null;
        private string sceneName;
        
        protected override void OnInit()
        {
            
        }

        public void LoadScene(int index, Action<float> onProgress = null, Action onComplete = null)
        {
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>($"Scene{index}", obj =>
            {
                HideCurrentScene();
                currentScene = GameObject.Instantiate(obj);
                sceneName = $"Scene{index}";
                onComplete?.Invoke();
            }, onProgress);
        }

        public void HideCurrentScene()
        {
            if (currentScene != null)
            {
                GameObject.Destroy(currentScene);
                this.GetSystem<IAssetSystem>().ReleaseAsset(sceneName);
            }
        }
    }
}