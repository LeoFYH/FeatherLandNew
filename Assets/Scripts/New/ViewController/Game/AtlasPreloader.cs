using UnityEngine;
using UnityEngine.U2D;
using System.Collections;
using System.Collections.Generic;

namespace BirdGame
{


    public class AtlasPreloader : MonoBehaviour
    {
        [Header("图集配置")] [SerializeField] private string atlasLabel = "Atlas"; // 用于标记所有图集的Label

        private Dictionary<string, SpriteAtlas> loadedAtlases = new Dictionary<string, SpriteAtlas>();

        private Dictionary<string, UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<SpriteAtlas>>
            atlasHandles =
                new Dictionary<string,
                    UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<SpriteAtlas>>();

        private static AtlasPreloader _instance;
        public static AtlasPreloader Instance => _instance;

        // 加载进度事件
        public System.Action<float> OnLoadingProgress;
        public System.Action<bool> OnLoadingComplete;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 注册图集请求事件
            SpriteAtlasManager.atlasRequested += HandleAtlasRequest;
        }

        private void Start()
        {
            // 开始预加载所有图集
            StartCoroutine(PreloadAllAtlases());
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SpriteAtlasManager.atlasRequested -= HandleAtlasRequest;
                ReleaseAllAtlases();
            }
        }

        /// <summary>
        /// 预加载所有标记了指定Label的图集
        /// </summary>
        private IEnumerator PreloadAllAtlases()
        {
            Debug.Log("开始预加载所有图集...");

            // 通过Label加载所有图集
            var loadHandle = UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync<SpriteAtlas>(
                atlasLabel,
                OnSingleAtlasLoaded
            );

            // 更新加载进度
            while (!loadHandle.IsDone)
            {
                OnLoadingProgress?.Invoke(loadHandle.PercentComplete);
                yield return null;
            }

            if (loadHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"成功预加载 {loadHandle.Result.Count} 个图集");
                OnLoadingComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError("图集预加载失败");
                OnLoadingComplete?.Invoke(false);
            }
        }

        /// <summary>
        /// 单个图集加载完成时的回调
        /// </summary>
        private void OnSingleAtlasLoaded(SpriteAtlas atlas)
        {
            if (atlas != null)
            {
                string atlasName = atlas.name;
                if (!loadedAtlases.ContainsKey(atlasName))
                {
                    loadedAtlases[atlasName] = atlas;
                    Debug.Log($"已加载图集: {atlasName}");
                }
            }
        }

        /// <summary>
        /// 处理图集请求事件
        /// </summary>
        private void HandleAtlasRequest(string atlasTag, System.Action<SpriteAtlas> callback)
        {
            Debug.Log($"收到图集请求: {atlasTag}");

            // 如果图集已经加载，直接返回
            if (loadedAtlases.ContainsKey(atlasTag))
            {
                callback(loadedAtlases[atlasTag]);
                return;
            }

            // 如果图集未加载，尝试通过Addressables加载
            StartCoroutine(LoadSingleAtlasOnDemand(atlasTag, callback));
        }

        /// <summary>
        /// 按需加载单个图集
        /// </summary>
        private IEnumerator LoadSingleAtlasOnDemand(string atlasTag, System.Action<SpriteAtlas> callback)
        {
            Debug.LogWarning($"按需加载图集: {atlasTag} (建议预加载所有图集)");

            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<SpriteAtlas>(atlasTag);
            atlasHandles[atlasTag] = handle;

            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                loadedAtlases[atlasTag] = handle.Result;
                callback(handle.Result);
                Debug.Log($"按需加载成功: {atlasTag}");
            }
            else
            {
                Debug.LogError($"按需加载失败: {atlasTag}");
                callback(null);
            }
        }

        /// <summary>
        /// 获取已加载的图集
        /// </summary>
        public SpriteAtlas GetAtlas(string atlasName)
        {
            if (loadedAtlases.ContainsKey(atlasName))
            {
                return loadedAtlases[atlasName];
            }

            Debug.LogWarning($"图集未加载: {atlasName}");
            return null;
        }

        /// <summary>
        /// 从图集获取精灵
        /// </summary>
        public Sprite GetSpriteFromAtlas(string atlasName, string spriteName)
        {
            var atlas = GetAtlas(atlasName);
            if (atlas != null)
            {
                var sprite = atlas.GetSprite(spriteName);
                if (sprite == null)
                {
                    Debug.LogError($"在图集 {atlasName} 中找不到精灵: {spriteName}");
                }

                return sprite;
            }

            return null;
        }

        /// <summary>
        /// 检查图集是否已加载
        /// </summary>
        public bool IsAtlasLoaded(string atlasName)
        {
            return loadedAtlases.ContainsKey(atlasName);
        }

        /// <summary>
        /// 获取所有已加载图集的名称
        /// </summary>
        public List<string> GetLoadedAtlasNames()
        {
            return new List<string>(loadedAtlases.Keys);
        }

        /// <summary>
        /// 释放所有图集资源
        /// </summary>
        private void ReleaseAllAtlases()
        {
            foreach (var handle in atlasHandles.Values)
            {
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                }
            }

            atlasHandles.Clear();
            loadedAtlases.Clear();
        }
    }
}