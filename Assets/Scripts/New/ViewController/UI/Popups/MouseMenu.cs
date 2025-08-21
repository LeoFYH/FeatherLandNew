using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

namespace BirdGame
{
    public class MouseMenu : UIBase
    {
        public RectTransform menu;
        public Button deleteButton;
        public Button closeButton;

        [Header("菜单偏移设置")]
        [LabelText("菜单偏移")]
        public Vector2 menuOffset = Vector2.zero;

        private int decorationId;
        private int decorationIndex;
        private GameObject deleteObject;

        public override void OnShowPanel()
        {
        }

        public override void OnHidePanel(Action onComplete = null)
        {
            Destroy(gameObject);
            onComplete?.Invoke();
        }

        private void Start()
        {
            deleteButton.onClick.AddListener(() =>
            {
                this.GetSystem<IGameSystem>().DestroyDecoration(decorationId, decorationIndex, deleteObject);
                this.GetSystem<IUISystem>().HideMouseMenu();
            });
        }

        private void Update()
        {
            // 检测左键点击
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击在UI元素上
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    // 检查是否点击在菜单内部
                    if (!IsClickInsideMenu())
                    {
                        // 点击在菜单外部，隐藏菜单
                        this.GetSystem<IUISystem>().HideMouseMenu();
                    }
                }
                else
                {
                    // 点击在非UI区域，隐藏菜单
                    this.GetSystem<IUISystem>().HideMouseMenu();
                }
            }
        }

        private bool IsClickInsideMenu()
        {
            if (menu == null) return false;

            Vector2 localPoint;
            Camera canvasCamera = null;

            var targetRectTransform = GetComponent<RectTransform>();
            Canvas canvas = targetRectTransform.GetComponentInParent<Canvas>();
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            {
                canvasCamera = canvas.worldCamera;
            }

            // 检查鼠标位置是否在菜单区域内
            bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                menu,                    // 菜单的RectTransform
                Input.mousePosition,     // 鼠标屏幕坐标
                canvasCamera,           // 摄像机
                out localPoint          // 输出的局部坐标
            );

            if (isInside)
            {
                // 检查是否在菜单的边界内
                Rect menuRect = menu.rect;
                return localPoint.x >= menuRect.xMin && localPoint.x <= menuRect.xMax &&
                       localPoint.y >= menuRect.yMin && localPoint.y <= menuRect.yMax;
            }

            return false;
        }

        public void Init(int id, int index, GameObject obj)
        {
            decorationId = id;
            decorationIndex = index;
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

            // 获取装饰物的世界坐标，转换为屏幕坐标
            Vector3 decorationWorldPos = deleteObject.transform.position;
            Vector3 decorationScreenPos = Camera.main.WorldToScreenPoint(decorationWorldPos);
            
            // 将屏幕坐标转换为UI局部坐标
            bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRectTransform,         // 目标UI
                decorationScreenPos,         // 装饰物的屏幕坐标
                canvasCamera,               // 摄像机（Overlay模式传null）
                out localPoint              // 输出的局部坐标
            );

            if (success)
            {
                Debug.Log($"装饰物UI局部坐标: {localPoint}");
                
                // 设置菜单在装饰物正下方
                menu.pivot = new Vector2(0.5f, 1f); // 顶部中心对齐
                menu.anchoredPosition = localPoint + menuOffset;
            }
        }
    }
}