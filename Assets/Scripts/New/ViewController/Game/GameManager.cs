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

        void Start()
        {
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

            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, Vector2.zero);

            foreach (var hit in hits)
            {
                var handler = hit.collider.GetComponent<DecorationClickHandler>();
                var hasDrag = hit.collider.GetComponent<DecorationDrag>() != null;
                if (handler == null && !hasDrag) continue;
                return true;
            }
            return false;
        }
    }
}
