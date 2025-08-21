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
            if (Input.GetMouseButtonDown(0))
            {
                this.GetSystem<IGameSystem>().CreateFood();
            }
        }

        private bool IsClickingOnDecoration()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                
                // 使用OverlapPointAll来检测触发器，这样可以检测到IsTrigger的Collider2D
                Collider2D[] colliders = Physics2D.OverlapPointAll(mousePosition);
                
                foreach (var collider in colliders)
                {
                    // 检查是否点击到装饰物（通过检查是否有DecorationClickHandler或DecorationDrag组件）
                    if (collider.GetComponent<DecorationClickHandler>() != null || 
                        collider.GetComponent<DecorationDrag>() != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}