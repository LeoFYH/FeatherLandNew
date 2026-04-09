using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using QFramework;

namespace BirdGame
{
    public class Food : ViewControllerBase
    {
        // Memory optimization: cache WaitForSeconds to avoid repeated allocations
        private static readonly WaitForSeconds _wait8s = new WaitForSeconds(8f);
        private static readonly WaitForFixedUpdate _waitFixed = new WaitForFixedUpdate();

        public bool isTargeted = false;
        public bool isDisabling = false;
        public int hp = 1;
        public float addValue;
        float y;
        private SpriteRenderer spriteRenderer;
        private float fadeDuration = 4f; // 总淡出时间
        private float timer = 0f; // 淡出计时器
        // CPU优化：缓存Color避免协程中每帧new Color分配
        private Color fadeColor = Color.white;
        

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            this.RegisterEvent<ClearFoodEvent>(evt =>
            {
                this.GetSystem<IGameSystem>().RecycleFood(this);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            // 启动8秒后自动消失的检测
            
        }

        public void Init()
        {
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            isTargeted = false;
            isDisabling = false;
            y = transform.position.y;
            StartCoroutine(DelayedStart());
            StartCoroutine(nameof(AutoDestroyIfUntargeted));
        }

        /// <summary>
        /// 如果食物没有被 target 超过 8 秒，自动消失
        /// </summary>
        private IEnumerator AutoDestroyIfUntargeted()
        {
            yield return _wait8s;
            
            // 8秒后检查是否还被 target
            if (!isTargeted && !isDisabling)
            {
                // 启动淡出效果
                StartCoroutine(nameof(DestroyDelay));
            }
        }

        public void UntargetFood()
        {
            if (isTargeted)
            {
                isTargeted = false;
                isDisabling = false;
                StartCoroutine(nameof(DestroyDelay));
            }
        }

        private IEnumerator DelayedStart()
        {
            yield return null;
            transform.DOMoveY(y - 0.2f, 0.2f).SetEase(Ease.OutQuad);
        }

        private IEnumerator DestroyDelay()
        {
            timer = 0f;

            while (timer < 5)
            {
                if (isTargeted)
                    yield break;
                timer += Time.deltaTime;
                yield return _waitFixed;
            }

            isDisabling = true;
            timer = 0;
            while (timer < fadeDuration)
            {
                if (isTargeted)
                {
                    // 如果被目标选中，恢复完全不透明
                    if (spriteRenderer != null)
                    {
                        fadeColor.a = 1f;
                        spriteRenderer.color = fadeColor;
                    }
                    isDisabling = false;

                    yield break;
                }

                timer += Time.deltaTime;
                // CPU优化：复用fadeColor，仅修改alpha，避免每帧new Color
                float alpha = 1f - (timer / fadeDuration);
                if (spriteRenderer != null)
                {
                    fadeColor.a = alpha;
                    spriteRenderer.color = fadeColor;
                }

                yield return _waitFixed;
            }

            // 完全透明后销毁
            this.GetSystem<IGameSystem>().RecycleFood(this);
        }

        // private void OnDisable()
        // {
        //     if (delayCoroutine != null)
        //     {
        //         this.GetSystem<IMonoSystem>()?.StopCoroutine(delayCoroutine);
        //         delayCoroutine = null;
        //     }
        //     if (autoDestroyCoroutine != null)
        //     {
        //         this.GetSystem<IMonoSystem>()?.StopCoroutine(autoDestroyCoroutine);
        //         autoDestroyCoroutine = null;
        //     }
        //     if (delayDestroyCoroutine != null)
        //     {
        //         this.GetSystem<IMonoSystem>()?.StopCoroutine(delayDestroyCoroutine);
        //         delayDestroyCoroutine = null;
        //     }
        // }
    }
}
