using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class GameManager : ViewControllerBase
    {
        public List<Transform> flyPositions;
        [Header("自动投喂")]
        public float createFoodTime = 1f;

        float _foodTimer;
        int _previousClickCount;
        bool _isAutoFeeding;

        // Memory optimization: pre-allocate raycast buffer to avoid GC allocations
        private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[16];
        private Camera _cachedCamera;

        void Start()
        {
            _cachedCamera = Camera.main;
            this.GetModel<IBirdModel>().FlyPositions = flyPositions;
            this.RegisterEvent<OnSettingCloseEvent>(evt =>
            {
                _previousClickCount = SimpleMouseForwarder.clickCount;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (this.GetSystem<IGameSystem>().IsPlacingDecoration()) return;

            bool autoFeedingEnabled = this.GetModel<ISaveModel>().SettingData.autoFeeding;
            bool clicked = Input.GetMouseButtonDown(0) || SimpleMouseForwarder.clickCount > _previousClickCount;
            if (IsClickingOnDecoration(clicked))
            {
                _previousClickCount = SimpleMouseForwarder.clickCount;
                return;
            }

            if (clicked)
            {
                _previousClickCount = SimpleMouseForwarder.clickCount;
                if (autoFeedingEnabled)
                {
                    _isAutoFeeding = !_isAutoFeeding;
                    _foodTimer = 0f;
                    if (_isAutoFeeding)
                        this.GetSystem<IGameSystem>().CreateFood();
                }
                else
                {
                    this.GetSystem<IGameSystem>().CreateFood();
                }
            }

            if (autoFeedingEnabled && _isAutoFeeding)
            {
                _foodTimer += Time.deltaTime;
                if (_foodTimer >= createFoodTime)
                {
                    _foodTimer = 0f;
                    this.GetSystem<IGameSystem>().CreateFood();
                }
            }
        }

        bool IsClickingOnDecoration(bool clicked)
        {
            if (!clicked) return false;

            if (_cachedCamera == null) _cachedCamera = Camera.main;
            Vector2 mousePosition = _cachedCamera.ScreenToWorldPoint(Input.mousePosition);
            int hitCount = Physics2D.RaycastNonAlloc(mousePosition, Vector2.zero, _hitBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                var collider = _hitBuffer[i].collider;
                var handler = collider.GetComponent<DecorationClickHandler>();
                var hasDrag = collider.GetComponent<DecorationDrag>() != null;
                if (handler == null && !hasDrag) continue;
                return true;
            }
            return false;
        }
    }
}
