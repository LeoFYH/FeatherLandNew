using QFramework;
using System.Collections;
using UnityEngine;

namespace BirdGame
{
    public interface IPeriodicCleanupSystem : ISystem
    {
        void StartCleanupCycle();
        void StopCleanupCycle();
    }

    public class PeriodicCleanupSystem : AbstractSystem, IPeriodicCleanupSystem
    {
        private static readonly WaitForSeconds _cleanupInterval = new WaitForSeconds(60f);
        private bool isRunning = false;

        private IObjectPoolSystem objectPoolSystem;
        private IMemoryOptimizationSystem memoryOptimizationSystem;

        protected override void OnInit()
        {
    
            objectPoolSystem = this.GetSystem<IObjectPoolSystem>();
            memoryOptimizationSystem = this.GetSystem<IMemoryOptimizationSystem>();
        }

        public void StartCleanupCycle()
        {
            if (!isRunning)
            {
                isRunning = true;
                this.GetSystem<IMonoSystem>()?.StartCoroutine(RunCleanupCycle());
            }
        }

        public void StopCleanupCycle()
        {
            isRunning = false;
        }

        private IEnumerator RunCleanupCycle()
        {
            while (isRunning)
            {
                // 每60秒执行一次轻度清理，减少卡顿
                yield return _cleanupInterval;

                PerformCleanup();
            }
        }

        private void PerformCleanup()
        {
            // 不调用 ClearAll()：清空所有对象池会导致后续首次使用重新加载预制体并产生卡顿，且不释放 Asset 内存
            // 改为仅限制对象池中非活跃对象数量，释放多余实例
            if (memoryOptimizationSystem != null)
            {
                memoryOptimizationSystem.CleanupObjectPools();
                memoryOptimizationSystem.OptimizeTextures();
            }

            // 仅卸载未使用资源；避免每帧 GC 造成 100–500ms 卡顿
            Resources.UnloadUnusedAssets();

            // 不在此处调用 GC.Collect() / WaitForPendingFinalizers()，由 Unity 自动 GC，或在场景切换时手动调用 PerformFullOptimization()
            Debug.Log("执行周期性内存清理完成");
        }
    }
}