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
                // 每30秒执行一次清理
                yield return new WaitForSeconds(30f);

                // 执行清理任务
                PerformCleanup();
            }
        }

        private void PerformCleanup()
        {
            // 清理对象池
            if (objectPoolSystem != null)
            {
                ((ObjectPoolSystem)objectPoolSystem).ClearAll();
            }

            // 执行内存优化
            if (memoryOptimizationSystem != null)
            {
                memoryOptimizationSystem.PerformFullOptimization();
            }

            // 强制垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            
            // 卸载未使用的资源
            Resources.UnloadUnusedAssets();

            Debug.Log("执行周期性内存清理完成");
        }
    }
}