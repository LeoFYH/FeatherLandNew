using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class MouseMenu : UIBase
    {
        public RectTransform menu;
        public Button deleteButton;
        public Button closeButton;

        private int decorationId;
        private GameObject deleteObject;

        public override void OnShowPanel()
        {
        }

        public override void OnHidePanel()
        {
            Destroy(gameObject);
        }

        private void Start()
        {
            deleteButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().DestroyDecoration(decorationId, deleteObject);
                this.GetSystem<IUISystem>().HideMouseMenu();
            });
        }

        public void Init(int id, GameObject obj)
        {
            decorationId = id;
            deleteObject = obj;
            
            Vector2 localPoint;
            Camera canvasCamera = null;

            var targetRectTransform = GetComponent<RectTransform>();
            // 根据Canvas渲染模式确定摄像机
            Canvas canvas = targetRectTransform.GetComponentInParent<Canvas>();
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            {
                canvasCamera = canvas.worldCamera;
            }

            // 执行坐标转换
            bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRectTransform,         // 目标UI
                Input.mousePosition,         // 鼠标屏幕坐标
                canvasCamera,               // 摄像机（Overlay模式传null）
                out localPoint              // 输出的局部坐标
            );

            if (success)
            {
                Debug.Log($"UI局部坐标: {localPoint}");
                float pivotX = 0f;
                float pivotY = 0f;
                if (localPoint.x < 0)
                {
                    pivotX = 0f;
                }
                else
                {
                    pivotX = 1f;
                }

                if (localPoint.y < 0)
                {
                    pivotY = 0;
                }
                else
                {
                    pivotY = 1f;
                }
                menu.pivot = new Vector2(pivotX, pivotY);
                menu.anchoredPosition = localPoint;
            }
        }
    }
}