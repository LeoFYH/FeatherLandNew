using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BirdGame
{


    public class AtlasManager : MonoBehaviour
    {
        // 指定用于加载图集的Label
        public string atlasLabel = "Atlas";

        private Dictionary<string, SpriteAtlas> _loadedAtlases = new Dictionary<string, SpriteAtlas>();
        private List<AsyncOperationHandle<SpriteAtlas>> _atlasHandles = new List<AsyncOperationHandle<SpriteAtlas>>();

        private static AtlasManager _instance;
        public static AtlasManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // 注册图集请求事件
            SpriteAtlasManager.atlasRequested += OnAtlasRequested;
        }

        private void OnDisable()
        {
            SpriteAtlasManager.atlasRequested -= OnAtlasRequested;
        }

        private void OnAtlasRequested(string atlasName, System.Action<SpriteAtlas> callback)
        {
            // 检查图集是否已经预加载
            if (_loadedAtlases.ContainsKey(atlasName))
            {
                callback(_loadedAtlases[atlasName]);
            }
            else
            {
                // 如果没有预加载，则通过Addressables按名称加载
                Debug.LogWarning($"Atlas {atlasName} was not preloaded, loading on demand.");
                StartCoroutine(LoadAtlasOnDemand(atlasName, callback));
            }
        }

        private IEnumerator LoadAtlasOnDemand(string atlasName, System.Action<SpriteAtlas> callback)
        {
            var handle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasName);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedAtlases[atlasName] = handle.Result;
                callback(handle.Result);
                _atlasHandles.Add(handle);
            }
            else
            {
                Debug.LogError($"Failed to load atlas on demand: {atlasName}");
                callback(null);
            }
        }

        // 提供方法手动获取图集（如果已经加载）
        public SpriteAtlas GetAtlas(string atlasName)
        {
            if (_loadedAtlases.ContainsKey(atlasName))
            {
                return _loadedAtlases[atlasName];
            }

            return null;
        }

        private void OnDestroy()
        {
            // 释放所有Addressables加载的图集
            foreach (var handle in _atlasHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _atlasHandles.Clear();
            _loadedAtlases.Clear();
        }
    }
}