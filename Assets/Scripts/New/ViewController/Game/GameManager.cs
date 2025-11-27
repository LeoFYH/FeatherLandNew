using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class GameManager : ViewControllerBase
    {
        public List<Transform> flyPositions;
        public float createFoodTime = 0.5f;
        float foodTimer;
        private float lastClickTime = 0f; // 记录上次点击时间
        private float clickInterval = 1f; // 点击间隔时间（1秒）
        private int previousClickCount = 0;

        private void Start()
        {
            this.GetModel<IBirdModel>().FlyPositions = flyPositions;
        }

        private void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            // 检查是否正在放置装饰物
            if (this.GetSystem<IGameSystem>().IsPlacingDecoration()) return;

            // 检查是否点击到装饰物
            if (IsClickingOnDecoration()) return;

            // 取消撒食物冷却，每次点击都能撒
            if (Input.GetMouseButtonDown(0) || (SimpleMouseForwarder.clickCount > previousClickCount) )
            {
                previousClickCount = SimpleMouseForwarder.clickCount;
                this.GetSystem<IGameSystem>().CreateFood();
            }

            if (SimpleMouseForwarder.clickCount > previousClickCount)
            {
                previousClickCount = SimpleMouseForwarder.clickCount;
            }
        }

        private bool IsClickingOnDecoration()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, Vector2.zero);
                
                foreach (var hit in hits)
                {
                    // 检查是否点击到装饰物（通过检查是否有DecorationClickHandler或DecorationDrag组件）
                    if (hit.collider.GetComponent<DecorationClickHandler>() != null || 
                        hit.collider.GetComponent<DecorationDrag>() != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}