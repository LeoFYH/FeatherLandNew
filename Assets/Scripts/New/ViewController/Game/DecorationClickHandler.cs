using System;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationClickHandler : ViewControllerBase
    {
        private int decorationId;
        public int decorationIndex;

        private SpriteRenderer sr;
        private Sequence weatherSequence; // Store sequence reference for cleanup

        public void Initialize(int id, int index)
        {
            decorationId = id;
            decorationIndex = index;
        }

        private void Start()
        {
            this.RegisterEvent<ClearDecorationsEvent>(evt =>
            {
                Destroy(transform.parent.gameObject);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            this.RegisterEvent<SwitchWeatherEvent>(evt =>
            {
                if (sr == null)
                    sr = GetComponentInChildren<SpriteRenderer>();
                // Kill previous sequence if exists
                weatherSequence?.Kill();
                weatherSequence = DOTween.Sequence();
                weatherSequence.Append(sr.DOColor(Color.black, 0.5f));
                weatherSequence.Append(sr.DOColor(Color.white, 0.5f));
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnMouseOver()
        {
            // 检查是否点击到UI元素
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 右键直接销毁装饰品
            if (Input.GetMouseButtonDown(1)) // 右键
            {
                DestroyDecoration();
            }
        }

        private void DestroyDecoration()
        {
            // 调用游戏系统的销毁方法
            //this.GetSystem<IGameSystem>().DestroyDecoration(decorationId, gameObject);
            this.GetSystem<IUISystem>().ShowMouseMenu(decorationId, decorationIndex, transform.parent.gameObject);
        }
        
        private void OnDestroy()
        {
            // Kill any active DOTween sequences to prevent memory leaks
            weatherSequence?.Kill();
            weatherSequence = null;
            
            // Kill any tweens on the sprite renderer
            if (sr != null)
                sr.DOKill();
        }
    }
} 