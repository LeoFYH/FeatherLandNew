using DG.Tweening;
using UnityEngine;

namespace BirdGame
{

    public class BirdFlyHorizontalState : StateBase
    {
        private Brid _brid;
        private bool isInStartPosition = false;
        private const float REACH_DISTANCE = 0.1f; // 到达目标位置的判定距离
        private int originalSortingOrder;

        public BirdFlyHorizontalState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            _brid.agent.enabled = false;
            // 随机误差后的飞行高度
            float flyY = _brid.flyInAirStartPosition.y + Random.Range(-2f, 2f);
            // 检查是否已经到达目标高度
            float distanceToTargetY = Mathf.Abs(_brid.transform.position.y - flyY);
            if (distanceToTargetY <= REACH_DISTANCE)
            {
                isInStartPosition = true;
                Debug.Log("到达目标高度");
                StartHorizontalFlight();
            }
            else
            {
                // 先斜着飞到目标高度
                _brid.anim.SetBool("Fly", true);
                _brid.anim.Play("TakeOff");

                // 计算45度角飞行的目标点
                float deltaY = flyY - _brid.transform.position.y;
                float deltaX = deltaY; // 45度角，X和Y的变化量相同

                // 根据目标点的X坐标决定是向左还是向右飞
                if (_brid.flyInAirStartPosition.x < _brid.transform.position.x)
                {
                    deltaX = -deltaX; // 向左飞
                }

                Vector3 targetPosition = new Vector3(
                    _brid.transform.position.x + deltaX,
                    flyY,
                    _brid.transform.position.z
                );

                float flyTime = (distanceToTargetY / _brid.flySpeed) * 1.3f;

                // 设置朝向
                _brid.sr.flipX = deltaX > 0;

                // 斜向飞行时scale逐渐缩小到adultbridsize的0.6倍
                _brid.transform.DOScale(Vector3.one * (_brid.AdultBirdSize * 0.6f), flyTime).SetEase(Ease.Linear);
                _brid.transform.DOMove(targetPosition, flyTime)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        isInStartPosition = true;
                        // 这里直接根据下一步的目标点判断朝向
                        // 假设你一开始 always 从左往右飞
                        _brid.sr.flipX = false; // 先朝右
                        StartHorizontalFlight();
                    });
                DOTween.Sequence().AppendCallback(() => { _brid.sr.sortingOrder = -15; }).SetDelay(flyTime * 0.5f);
            }
        }

        private void StartHorizontalFlight()
        {
            // 保存原始sortingOrder，并设置为-15
            originalSortingOrder = _brid.sr.sortingOrder;
            _brid.anim.Play("FlyInAir");
            _brid.anim.SetBool("Fly", false);
            Fly();
        }

        private void Fly()
        {
            float outScreenOffset = 2f; // 飞出屏幕的距离，改为正数
            float y = _brid.transform.position.y;
            Camera cam = Camera.main;

            // 获取屏幕左右边界的世界坐标
            float leftEdge = cam.ViewportToWorldPoint(new Vector3(0, 0.5f, 0)).x;
            float rightEdge = cam.ViewportToWorldPoint(new Vector3(1, 0.5f, 0)).x;

            Vector3 target = Vector3.zero;
            bool isFacingLeft = _brid.sr.flipX;
            if (isFacingLeft)
            {
                // 向右飞，目标点在右边界之外
                target = new Vector3(rightEdge + outScreenOffset, y, 0);
                _brid.sr.flipX = false;
            }
            else
            {
                // 向左飞，目标点在左边界之外
                target = new Vector3(leftEdge - outScreenOffset, y, 0);
                _brid.sr.flipX = true;
            }

            float distance = Vector3.Distance(_brid.transform.position, target);
            float flyTime = distance / _brid.flySpeed;
            _brid.transform.DOMove(target, flyTime).SetEase(Ease.Linear).OnComplete(DoNext);
        }

        private void DoNext()
        {
            int random = Random.Range(0, 10);
            if (random < 4)
            {
                currMachine.ChangeState<BirdFlyDownState>();
            }
            else
            {
                Fly();
            }
        }

        public override void OnUpdate()
        {

        }

        public override void OnExit()
        {
            isInStartPosition = false;
            // 恢复原始sortingOrder
            _brid.sr.sortingOrder = originalSortingOrder;
        }
    }
}
