using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationClickHandler : ViewControllerBase
    {
        private int decorationId;

        public void Initialize(int id)
        {
            decorationId = id;
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
            this.GetSystem<IUISystem>().ShowMouseMenu(decorationId, gameObject);
        }
    }
} 