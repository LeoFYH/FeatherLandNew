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
        int _previousRightClickCount;
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
                _previousClickCount = MouseForwarder.clickCount;
                _previousRightClickCount = MouseForwarder.rightClickCount;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (this.GetSystem<IGameSystem>().IsPlacingDecoration()) return;

            bool leftClicked = Input.GetMouseButtonDown(0) || MouseForwarder.clickCount > _previousClickCount;
            bool rightClicked = Input.GetMouseButtonDown(1) || MouseForwarder.rightClickCount > _previousRightClickCount;
            if ((leftClicked || rightClicked) && IsClickingOnBird())
            {
                if (leftClicked)
                    _previousClickCount = MouseForwarder.clickCount;
                if (rightClicked)
                    _previousRightClickCount = MouseForwarder.rightClickCount;

                StopAutoFeedingForBirdInteraction();
                return;
            }

            if (this.GetSystem<IUISystem>().HasAnyPopupOpen()) return;

            bool autoFeedingEnabled = this.GetModel<ISaveModel>().SettingData.autoFeeding;
            if (!autoFeedingEnabled && _isAutoFeeding)
            {
                SetAutoFeeding(false);
            }

            bool clicked = leftClicked;
            if (IsClickingOnDecoration(clicked))
            {
                _previousClickCount = MouseForwarder.clickCount;
                return;
            }

            if (clicked)
            {
                _previousClickCount = MouseForwarder.clickCount;
                if (autoFeedingEnabled)
                {
                    SetAutoFeeding(!_isAutoFeeding);
                    _foodTimer = 0f;
                    if (_isAutoFeeding)
                        this.GetSystem<IGameSystem>().CreateFood(true);
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
                    this.GetSystem<IGameSystem>().CreateFood(true);
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

        void StopAutoFeedingForBirdInteraction()
        {
            SetAutoFeeding(false);
            this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
        }

        void SetAutoFeeding(bool isAutoFeeding)
        {
            _isAutoFeeding = isAutoFeeding;
            this.GetSystem<IGameSystem>().IsAutoFeeding = isAutoFeeding;
            if (!isAutoFeeding)
            {
                _foodTimer = 0f;
            }
        }

        bool IsClickingOnBird()
        {
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            if (_cachedCamera == null) return false;

            Vector2 mousePosition = _cachedCamera.ScreenToWorldPoint(Input.mousePosition);
            var birdList = this.GetModel<IBirdModel>().BirdList;
            foreach (var birdData in birdList)
            {
                if (birdData?.bird == null || birdData.bird.gameObject == null) continue;

                Collider2D collider2D = birdData.bird.birdCollider;
                if (collider2D != null)
                {
                    if (collider2D.OverlapPoint(mousePosition))
                    {
                        return true;
                    }
                }
                else
                {
                    Vector2 diff = mousePosition - (Vector2)birdData.bird.transform.position;
                    if (diff.sqrMagnitude < 0.25f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
