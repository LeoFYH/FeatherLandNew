using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace BirdGame
{
    public interface IMemoryOptimizationSystem : ISystem
    {
        void OptimizeAudioSystem();

        void CleanupObjectPools();
        void OptimizeTextures();
        void PerformFullOptimization();
    }

    public class MemoryOptimizationSystem : AbstractSystem, IMemoryOptimizationSystem
    {
        private IAudioSystem audioSystem;
        private IObjectPoolSystem objectPoolSystem;
        private List<AudioSource> reusableAudioSources = new List<AudioSource>();

        protected override void OnInit()
        {
            audioSystem = this.GetSystem<IAudioSystem>();
            objectPoolSystem = this.GetSystem<IObjectPoolSystem>();
            
            // ✅ 优化：移除强制定时GC，Unity的GC会自动管理
            // 强制GC会造成100-500ms的明显卡顿，这是导致"偶发卡顿"的主要原因
            // this.GetSystem<IMonoSystem>().RegisterUpdate(CleanupRoutine);
        }

        /// <summary>
        /// 优化音频系统，限制同时播放的音频源数量
        /// </summary>
        public void OptimizeAudioSystem()
        {
            // 限制环境音效数量，只保留必要的环境音
            LimitEnvironmentAudioSources();
            
            // 优化音效播放，使用对象池管理AudioSource
            OptimizeEffectAudioSources();
        }

        private void LimitEnvironmentAudioSources()
        {
            // 获取AudioSystem中的私有字段，减少环境音效数量
            var audioSys = (AudioSystem)audioSystem;
            var environmentAudiosField = typeof(AudioSystem).GetField("environmentAudios", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (environmentAudiosField != null)
            {
                var environmentAudios = (List<AudioSource>)environmentAudiosField.GetValue(audioSys);
                
                // 限制环境音效的最大数量
                if (environmentAudios.Count > 3) // 限制最多3个环境音效
                {
                    for (int i = environmentAudios.Count - 1; i >= 3; i--)
                    {
                        var audioSource = environmentAudios[i];
                        if (audioSource != null)
                        {
                            Object.DestroyImmediate(audioSource);
                        }
                        environmentAudios.RemoveAt(i);
                    }
                }
            }
        }

        private void OptimizeEffectAudioSources()
        {
            // 获取AudioSystem中的effectAudios字段
            var audioSys = (AudioSystem)audioSystem;
            var effectAudiosField = typeof(AudioSystem).GetField("effectAudios", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (effectAudiosField != null)
            {
                var effectAudios = (List<AudioSource>)effectAudiosField.GetValue(audioSys);
                
                // 将多余的AudioSource移到可重用列表中
                if (effectAudios.Count > 5) // 最多保留5个音效AudioSource
                {
                    for (int i = effectAudios.Count - 1; i >= 5; i--)
                    {
                        var audioSource = effectAudios[i];
                        if (audioSource != null)
                        {
                            audioSource.clip = null; // 清除引用的音频剪辑
                            audioSource.Stop();
                            reusableAudioSources.Add(audioSource);
                        }
                        effectAudios.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// 定期清理资源
        /// ✅ 优化：移除强制GC调用，避免卡顿
        /// 如果确实需要清理，建议在场景切换或游戏暂停时手动调用
        /// </summary>
        private void CleanupRoutine()
        {
            // ❌ 移除：每30秒强制GC会造成100-500ms的卡顿
            // if (Time.time % 30 < Time.deltaTime)
            // {
            //     System.GC.Collect();
            //     Resources.UnloadUnusedAssets();
            // }
            
            // ✅ 如果需要清理，建议在适当时机手动调用PerformFullOptimization()
        }

        /// <summary>
        /// 清理对象池中过多的对象
        /// </summary>
        public void CleanupObjectPools()
        {
            // 这里可以添加具体的对象池清理逻辑
            // 检查并清理长时间未使用的对象
            var poolsField = typeof(ObjectPoolSystem).GetField("pools",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (poolsField != null)
            {
                var pools = (Dictionary<string, object>)poolsField.GetValue(objectPoolSystem);
                
                foreach (var poolKVP in pools)
                {
                    var poolType = poolKVP.Value.GetType();
                    var inactiveField = poolType.GetField("inactive",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var inactiveStack = (System.Collections.Stack)inactiveField.GetValue(poolKVP.Value);
                    
                    // 限制非活跃对象数量，防止无限增长
                    while (inactiveStack.Count > 20) // 最多保留20个非活跃对象
                    {
                        var obj = (GameObject)inactiveStack.Pop();
                        if (obj != null)
                        {
                            GameObject.DestroyImmediate(obj);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 优化纹理资源
        /// </summary>
        public void OptimizeTextures()
        {
            // 主动卸载不需要的纹理资源
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 完整内存优化
        /// ✅ 优化：只在场景切换或游戏暂停等合适时机调用
        /// 不应该定时自动调用，会造成卡顿
        /// </summary>
        public void PerformFullOptimization()
        {
            // ✅ 可以在以下时机手动调用此方法：
            // 1. 场景切换前
            // 2. 游戏暂停时
            // 3. 进入后台时
            
            OptimizeAudioSystem();
            CleanupObjectPools();
            OptimizeTextures();

            // ⚠️ 注意：GC.Collect会造成卡顿，只在必要时调用
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }
    }
}