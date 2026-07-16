using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 装饰物左右来回走动效果
    /// 挂载到装饰物子节点（与 DecorationDrag 同层级），会自动移动 transform.parent（装饰物根节点）。
    /// 拖动时自动暂停移动；拖动结束后以新位置为起点继续巡逻。
    /// </summary>
    public class DecorationWalk : ViewControllerBase
    {
        [Header("移动设置")]
        [Tooltip("移动速度（单位/秒）")]
        public float moveSpeed = 0.5f;

        [Tooltip("相对起点的最大移动距离，到达后自动切换方向")]
        public float moveDistance = 1f;

        [Header("朝向设置")]
        [Tooltip("初始移动方向：1 = 向右，-1 = 向左")]
        public int startDirection = 1;

        [Tooltip("移动时是否翻转 SpriteRenderer")]
        public bool flipSprite = true;

        [Tooltip("Sprite 默认是否朝左。勾选表示资源原图朝左，未勾选表示资源原图朝右。")]
        public bool spriteFacesLeft = false;

        private DecorationDrag decorationDrag;
        private SpriteRenderer spriteRenderer;
        private Vector3 startPosition;
        private int direction = 1; // 1 = 向右, -1 = 向左
        private bool wasDragging;

        private void Start()
        {
            decorationDrag = GetComponent<DecorationDrag>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            direction = startDirection > 0 ? 1 : -1;
            startPosition = transform.parent.position;
            UpdateSpriteDirection();
        }

        private void Update()
        {
            bool isDragging = decorationDrag != null && decorationDrag.IsDragging;

            // 拖动期间暂停移动，并记录下一帧需要更新起点
            if (isDragging)
            {
                wasDragging = true;
                return;
            }

            // 拖动刚结束时，把起点更新到当前位置，防止装饰物瞬移回旧起点
            if (wasDragging)
            {
                startPosition = transform.parent.position;
                wasDragging = false;
            }

            if (moveSpeed <= 0f || moveDistance <= 0f) return;

            Vector3 pos = transform.parent.position;
            pos.x += direction * moveSpeed * Time.deltaTime;

            // 限制在起点 ± moveDistance 范围内
            float offset = pos.x - startPosition.x;
            if (Mathf.Abs(offset) >= moveDistance)
            {
                pos.x = startPosition.x + direction * moveDistance;
                direction *= -1;
                UpdateSpriteDirection();
            }

            transform.parent.position = pos;
        }

        /// <summary>
        /// 根据当前移动方向更新 Sprite 朝向
        /// </summary>
        private void UpdateSpriteDirection()
        {
            if (!flipSprite || spriteRenderer == null) return;

            // 向右移动：朝右；向左移动：朝左
            // spriteFacesLeft 用于兼容原图朝左的资源
            spriteRenderer.flipX = (direction < 0) != spriteFacesLeft;
        }

        private void OnValidate()
        {
            if (moveSpeed < 0f) moveSpeed = 0f;
            if (moveDistance < 0f) moveDistance = 0f;
            startDirection = startDirection >= 0 ? 1 : -1;
        }
    }
}
