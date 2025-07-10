using System;
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
    }

    public class SceneSystem : AbstractSystem, ISceneSystem
    {
        private GameObject currentScene = null;
        
        protected override void OnInit()
        {
            
        }

        public void LoadScene(int index, Action<float> onProgress = null, Action onComplete = null)
        {
            Addressables.LoadAssetAsync<GameObject>($"Scene{index}").Completed += handle =>
            {
                if (currentScene != null)
                {
                    GameObject.Destroy(currentScene);
                }

                currentScene = GameObject.Instantiate(handle.Result);
                handle.Release();
                onComplete?.Invoke();
            };
        }
    }
}