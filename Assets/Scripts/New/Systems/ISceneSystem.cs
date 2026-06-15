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
        /// 是否正在换图（含场景与鸟的异步加载）。期间禁止再次切图、禁止覆盖写存档。
        /// </summary>
        bool IsLoading { get; set; }
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

        public bool IsLoading { get; set; }

        protected override void OnInit()
        {
            
        }

        public void LoadScene(int index, Action<float> onProgress = null, Action onComplete = null)
        {
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>($"Scene{index}", obj =>
            {
                HideCurrentScene();
                // 卸载旧场景遗留的纹理/精灵等原生资源（异步，不阻塞主线程）
                Resources.UnloadUnusedAssets();
                currentScene = GameObject.Instantiate(obj);
                sceneName = $"Scene{index}";
                Debug.Log("场景加载完成");
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