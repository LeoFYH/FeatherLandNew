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

            // 取消撒食物冷却，每次点击都能撒
            if (Input.GetMouseButtonDown(0))
            {
                this.GetSystem<IGameSystem>().CreateFood();
            }
        }
    }
}