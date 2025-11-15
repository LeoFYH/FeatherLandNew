using UnityEngine;

namespace BirdGame
{
    public class Test : MonoBehaviour
    {
        [Header("检测设置")]
        public Camera targetCamera;
        public LayerMask detectionLayers = -1; // 默认检测所有层
        public float maxDetectionDistance = 100f;
        public bool debugMode = true;

        [Header("可视化")]
        public Color rayColor = Color.yellow;

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                DetectObject();
            }

            // 在编辑器中绘制射线（调试用）
            if (debugMode && Input.GetMouseButton(0))
            {
                DrawDebugRay();
            }
        }

        void DetectObject()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDetectionDistance, detectionLayers))
            {
                GameObject clickedObject = hit.collider.gameObject;
                Vector3 hitPoint = hit.point;
            
                Debug.Log($"点击了: {clickedObject.name}，位置: {hitPoint}");

                // 发送消息给被点击的物体
                clickedObject.SendMessage("OnMouseClick", SendMessageOptions.DontRequireReceiver);
            
                // 或者使用接口
                IClickable clickable = clickedObject.GetComponent<IClickable>();
                if (clickable != null)
                {
                    clickable.OnClick();
                }
            }
        }

        void DrawDebugRay()
        {
            if (targetCamera == null) return;

            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * maxDetectionDistance, rayColor);
        }

        // 可选的接口定义
        public interface IClickable
        {
            void OnClick();
        }
    }
}