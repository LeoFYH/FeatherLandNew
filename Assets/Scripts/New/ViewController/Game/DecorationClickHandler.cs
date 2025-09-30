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

        public void Initialize(int id, int index)
        {
            decorationId = id;
            decorationIndex = index;
        }

        private void Start()
        {
            this.RegisterEvent<ClearDecorationsEvent>(evt =>
            {
                Destroy(gameObject);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            this.RegisterEvent<SwitchWeatherEvent>(evt =>
            {
                if (sr == null)
                    sr = GetComponentInChildren<SpriteRenderer>();
                var ani = DOTween.Sequence();
                ani.Append(sr.DOColor(Color.black, 0.5f));
                ani.Append(sr.DOColor(Color.white, 0.5f));
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
            this.GetSystem<IUISystem>().ShowMouseMenu(decorationId, decorationIndex, gameObject);
        }
    }
} 