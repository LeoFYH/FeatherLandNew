using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace BirdGame
{
    public interface ITextureOptimizationSystem : ISystem { }

    public class TextureOptimizationSystem : AbstractSystem, ITextureOptimizationSystem
    {
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private Queue<string> lruQueue = new Queue<string>();
        private const int MAX_CACHE_SIZE = 20; // 最大缓存纹理数量

        protected override void OnInit()
        {
            // 初始化纹理优化系统
        }

        /// <summary>
        /// 获取纹理资源，如果不在缓存中则加载
        /// </summary>
        public Texture2D GetTexture(string texturePath)
        {
            // 检查纹理是否在缓存中
            if (textureCache.ContainsKey(texturePath))
            {
                // 更新LRU队列
                MoveToFrontOfLRU(texturePath);
                return textureCache[texturePath];
            }

            // 如果缓存已满，移除最久未使用的纹理
            if (textureCache.Count >= MAX_CACHE_SIZE)
            {
                RemoveLRUTexture();
            }

            // 加载纹理（这里假设使用Resources.Load，实际项目中可能需要调整）
            Texture2D texture = LoadTexture(texturePath);

            if (texture != null)
            {
                textureCache[texturePath] = texture;
                lruQueue.Enqueue(texturePath);
            }

            return texture;
        }

        /// <summary>
        /// 释放指定路径的纹理
        /// </summary>
        public void ReleaseTexture(string texturePath)
        {
            if (textureCache.ContainsKey(texturePath))
            {
                Texture2D texture = textureCache[texturePath];
                if (texture != null)
                {
                    Object.DestroyImmediate(texture); // 立即销毁纹理资源
                }
                textureCache.Remove(texturePath);
                
                // 从LRU队列中移除
                var tempQueue = new Queue<string>();
                foreach (var path in lruQueue)
                {
                    if (path != texturePath)
                    {
                        tempQueue.Enqueue(path);
                    }
                }
                lruQueue = tempQueue;
            }
        }

        /// <summary>
        /// 清除所有缓存的纹理
        /// </summary>
        public void ClearAllTextures()
        {
            foreach (var texture in textureCache.Values)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
            }
            textureCache.Clear();
            lruQueue.Clear();
        }

        /// <summary>
        /// 移除LRU队列中最久未使用的纹理
        /// </summary>
        private void RemoveLRUTexture()
        {
            if (lruQueue.Count > 0)
            {
                string oldestPath = lruQueue.Dequeue();
                if (textureCache.ContainsKey(oldestPath))
                {
                    Texture2D texture = textureCache[oldestPath];
                    if (texture != null)
                    {
                        Object.DestroyImmediate(texture);
                    }
                    textureCache.Remove(oldestPath);
                }
            }
        }

        /// <summary>
        /// 将纹理移到LRU队列前面（表示最近使用）
        /// </summary>
        private void MoveToFrontOfLRU(string texturePath)
        {
            // 从队列中移除再添加到末尾，以更新使用顺序
            var tempQueue = new Queue<string>();
            bool found = false;
            
            foreach (var path in lruQueue)
            {
                if (path == texturePath)
                {
                    found = true;
                }
                else
                {
                    tempQueue.Enqueue(path);
                }
            }
            
            if (found)
            {
                tempQueue.Enqueue(texturePath);
                lruQueue = tempQueue;
            }
        }

        /// <summary>
        /// 实际加载纹理的方法（模拟）
        /// </summary>
        private Texture2D LoadTexture(string path)
        {
            // 在实际项目中，这里应该使用合适的加载方式
            // 比如Addressables、Resources.Load或其他资源加载系统
            // 为演示目的，返回一个空纹理
            return null;
        }
    }
}