using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class InputProxy : ViewControllerBase
    {
        private Vector2 lastMousePos;
        private bool isMouseDown = false;
        private GameObject lastPressedUI;
        private GameObject lastHoveredObject;
        private Camera mainCam;

        void Start()
        {
            mainCam = Camera.main;
        }

        void Update()
        {
            if(!this.GetUtility<IFullScreenUtility>().EnableWallpaperMode)
                return;
            
            Vector2 currentMousePos = Input.mousePosition;
            
            // 处理鼠标移动
            if (currentMousePos != lastMousePos)
            {
                HandleMouseMove(currentMousePos);
                lastMousePos = currentMousePos;
            }
            
            // 处理鼠标按下
            if (Input.GetMouseButtonDown(0) && !isMouseDown)
            {
                isMouseDown = true;
                HandleMouseDown(currentMousePos);
            }
            
            // 处理鼠标抬起
            if (Input.GetMouseButtonUp(0) && isMouseDown)
            {
                isMouseDown = false;
                HandleMouseUp(currentMousePos);
            }
        }

        // 外部调用入口（来自透明输入捕获进程）
        public void HandleMessage(string msg)
        {
            string[] parts = msg.Split(' ');
            string cmd = parts[0];

            switch (cmd)
            {
                case "MOUSEMOVE":
                    lastMousePos = ToUnityPos(parts[1], parts[2]);
                    HandleMouseMove(lastMousePos);
                    break;

                case "MOUSEDOWN":
                    lastMousePos = ToUnityPos(parts[1], parts[2]);
                    isMouseDown = true;
                    HandleMouseDown(lastMousePos);
                    break;

                case "MOUSEUP":
                    lastMousePos = ToUnityPos(parts[1], parts[2]);
                    isMouseDown = false;
                    HandleMouseUp(lastMousePos);
                    break;
            }
        }

        private Vector2 ToUnityPos(string x, string y)
        {
            return new Vector2(float.Parse(x), Screen.height - float.Parse(y));
        }

        // ---------------- UI 处理 ----------------
        private void HandleMouseDown(Vector2 pos)
        {
            var eventData = new PointerEventData(EventSystem.current) { position = pos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count > 0)
            {
                eventData.pointerCurrentRaycast = results[0];
                eventData.pointerPressRaycast = results[0];
                lastPressedUI = results[0].gameObject;
                ExecuteEvents.Execute(lastPressedUI, eventData, ExecuteEvents.pointerDownHandler);
            }

            // 3D / 2D 检测
            DoPhysicsRaycast(pos, true);
        }

        private void HandleMouseUp(Vector2 pos)
        {
            var eventData = new PointerEventData(EventSystem.current) { position = pos };

            if (lastPressedUI != null)
            {
                ExecuteEvents.Execute(lastPressedUI, eventData, ExecuteEvents.pointerUpHandler);

                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);
                if (results.Count > 0 && results[0].gameObject == lastPressedUI)
                {
                    ExecuteEvents.Execute(lastPressedUI, eventData, ExecuteEvents.pointerClickHandler);
                }

                lastPressedUI = null;
            }

            DoPhysicsRaycast(pos, false);
        }

        private void HandleMouseMove(Vector2 pos)
        {
            var eventData = new PointerEventData(EventSystem.current) { position = pos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count > 0)
            {
                eventData.pointerCurrentRaycast = results[0];
                ExecuteEvents.Execute(results[0].gameObject, eventData, ExecuteEvents.pointerEnterHandler);
            }

            DoPhysicsHover(pos);
        }

        // ---------------- Physics 处理 ----------------
        private void DoPhysicsRaycast(Vector2 pos, bool isDown)
        {
            Ray ray = mainCam.ScreenPointToRay(pos);

            // 先 2D
            RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit2D.collider != null)
            {
                if (isDown)
                    hit2D.collider.gameObject.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
                else
                {
                    hit2D.collider.gameObject.SendMessage("OnMouseUp", SendMessageOptions.DontRequireReceiver);
                    hit2D.collider.gameObject.SendMessage("OnMouseClick", SendMessageOptions.DontRequireReceiver);
                }

                return;
            }

            // 再 3D
            if (Physics.Raycast(ray, out RaycastHit hit3D))
            {
                if (isDown)
                    hit3D.collider.gameObject.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
                else
                {
                    hit3D.collider.gameObject.SendMessage("OnMouseUp", SendMessageOptions.DontRequireReceiver);
                    hit3D.collider.gameObject.SendMessage("OnMouseClick", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        private void DoPhysicsHover(Vector2 pos)
        {
            Ray ray = mainCam.ScreenPointToRay(pos);

            GameObject currentHover = null;
            RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit2D.collider != null) currentHover = hit2D.collider.gameObject;
            else if (Physics.Raycast(ray, out RaycastHit hit3D)) currentHover = hit3D.collider.gameObject;

            if (currentHover != lastHoveredObject)
            {
                if (lastHoveredObject != null)
                    lastHoveredObject.SendMessage("OnMouseExit", SendMessageOptions.DontRequireReceiver);
                if (currentHover != null)
                    currentHover.SendMessage("OnMouseEnter", SendMessageOptions.DontRequireReceiver);
                lastHoveredObject = currentHover;
            }
        }
    }
}