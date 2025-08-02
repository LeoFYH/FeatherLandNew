using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationClickHandler : MonoBehaviour
    {
        private int decorationId;
        private IGameSystem gameSystem;

        public void Initialize(int id, IGameSystem system)
        {
            decorationId = id;
            gameSystem = system;
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
            gameSystem.DestroyDecoration(decorationId, gameObject);
        }
    }
} 