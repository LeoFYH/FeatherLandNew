using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    /// <summary>
    /// 壁纸模式专用的输入嗅探器 —— 把 Unity 侧观察到的鼠标 / EventSystem 状态
    /// 写到 Player.log，便于和原生层的日志 (FLWallpaperBridge.mm 的 [FLLOG] 前缀)
    /// 一起对照排查 "点击为什么没到 Unity"。
    ///
    /// 通过 FLClickProbe.Install() 自动创建一个 DontDestroyOnLoad 的 GameObject，
    /// 进入壁纸模式时由 FullScreenUtility 调用。
    /// </summary>
    public class FLClickProbe : MonoBehaviour
    {
        private static FLClickProbe _instance;

        public static void Install()
        {
            if (_instance != null) return;
            var go = new GameObject("FLClickProbe");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FLClickProbe>();
            Debug.Log("[FLLOG-CS] FLClickProbe installed");
        }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [DllImport("FLWallpaperBridge")] private static extern void _FLDiagnose();
#endif

        private float _nextDiagnoseAt = 0f;
        private const float DIAGNOSE_INTERVAL = 5f;

        private bool _prevL, _prevR;
        private Vector3 _lastMousePos;

        private readonly List<RaycastResult> _raycastBuf = new List<RaycastResult>(16);

        private void Update()
        {
            // 鼠标按下检测 —— 如果 Unity 这里看到了 down,说明 NSEvent 流通了
            bool curL = Input.GetMouseButton(0);
            bool curR = Input.GetMouseButton(1);
            bool downL = Input.GetMouseButtonDown(0);
            bool downR = Input.GetMouseButtonDown(1);
            bool upL = Input.GetMouseButtonUp(0);
            bool upR = Input.GetMouseButtonUp(1);

            if (downL) Debug.Log($"[FLLOG-CS] *** Input.GetMouseButtonDown(0)=TRUE pos={Input.mousePosition} ***");
            if (downR) Debug.Log($"[FLLOG-CS] *** Input.GetMouseButtonDown(1)=TRUE pos={Input.mousePosition} ***");
            if (upL)   Debug.Log($"[FLLOG-CS] Input.GetMouseButtonUp(0) pos={Input.mousePosition}");
            if (upR)   Debug.Log($"[FLLOG-CS] Input.GetMouseButtonUp(1) pos={Input.mousePosition}");

            // 状态切换日志
            if (curL != _prevL) { Debug.Log($"[FLLOG-CS] mouse-left state {_prevL}->{curL}"); _prevL = curL; }
            if (curR != _prevR) { Debug.Log($"[FLLOG-CS] mouse-right state {_prevR}->{curR}"); _prevR = curR; }

            // 鼠标移动 (每秒最多 2 条, 避免刷屏)
            Vector3 mp = Input.mousePosition;
            if ((mp - _lastMousePos).sqrMagnitude > 1f && Time.time - _nextDiagnoseAt < -4.5f)
            {
                _lastMousePos = mp;
            }

            // 滚轮
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                Debug.Log($"[FLLOG-CS] Input.mouseScrollDelta={Input.mouseScrollDelta}");

            // 在按下瞬间额外打 raycast 命中情况,看 EventSystem 是否能 hit UI
            if (downL || downR)
            {
                LogRaycastAt(Input.mousePosition, downL ? "LEFT" : "RIGHT");
            }

            // 周期性触发原生 diagnose,保证 log 一直有新鲜状态
            if (Time.time >= _nextDiagnoseAt)
            {
                _nextDiagnoseAt = Time.time + DIAGNOSE_INTERVAL;
                Debug.Log($"[FLLOG-CS] heartbeat mousePos={mp} keyboard={GetPressedKeysShort()}");
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
                try { _FLDiagnose(); }
                catch (System.Exception e) { Debug.LogWarning($"[FLLOG-CS] _FLDiagnose threw {e.Message}"); }
#endif
            }
        }

        private void LogRaycastAt(Vector2 screenPos, string btn)
        {
            var es = EventSystem.current;
            if (es == null)
            {
                Debug.LogWarning("[FLLOG-CS] EventSystem.current = null !!!");
                return;
            }

            var ped = new PointerEventData(es) { position = screenPos };
            _raycastBuf.Clear();
            es.RaycastAll(ped, _raycastBuf);

            if (_raycastBuf.Count == 0)
            {
                Debug.Log($"[FLLOG-CS] [{btn}] RaycastAll at {screenPos} -> 0 hits");
                return;
            }

            for (int i = 0; i < _raycastBuf.Count && i < 5; i++)
            {
                var r = _raycastBuf[i];
                Debug.Log($"[FLLOG-CS] [{btn}] raycast#{i} -> '{PathOf(r.gameObject)}' depth={r.depth} dist={r.distance}");
            }

            // 也看一眼 currentSelectedGameObject / pointerEnter
            Debug.Log($"[FLLOG-CS] [{btn}] EventSystem currentSelected={(es.currentSelectedGameObject != null ? es.currentSelectedGameObject.name : "null")} firstSelected={(es.firstSelectedGameObject != null ? es.firstSelectedGameObject.name : "null")} alreadySelecting={es.alreadySelecting}");
        }

        private static string PathOf(GameObject go)
        {
            if (go == null) return "<null>";
            string p = go.name;
            var t = go.transform.parent;
            int safety = 0;
            while (t != null && safety++ < 10)
            {
                p = t.name + "/" + p;
                t = t.parent;
            }
            return p;
        }

        private static string GetPressedKeysShort()
        {
            // 仅采几个常用键避免 log 太长
            var keys = new[] { KeyCode.Escape, KeyCode.Space, KeyCode.Return, KeyCode.LeftCommand, KeyCode.LeftAlt };
            var sb = new System.Text.StringBuilder();
            foreach (var k in keys)
            {
                if (Input.GetKey(k))
                {
                    if (sb.Length > 0) sb.Append(',');
                    sb.Append(k.ToString());
                }
            }
            return sb.Length > 0 ? sb.ToString() : "(none)";
        }
    }
}
