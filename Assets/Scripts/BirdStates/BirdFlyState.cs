using DG.Tweening;
using QFramework;
using UnityEngine;

namespace BirdGame
{

    public class BirdFlyState : StateBase
    {
        private Brid _brid;

        //飞向巢穴 自带缩小功能 完成后触发flywait
        public BirdFlyState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            _brid.anim.SetBool("IsTakeOff", true);
            _brid.anim.SetBool("Fly", true);
            _brid.agent.enabled = false;
            if (this.GetModel<IBirdModel>().FlyPositions.Count > 0)
            {
                int random = Random.Range(0, this.GetModel<IBirdModel>().FlyPositions.Count);
                _brid.flyIndex = random;
                _brid.nestTrans = this.GetModel<IBirdModel>().FlyPositions[random];
                this.GetModel<IBirdModel>().FlyPositions.RemoveAt(random);

                // 创建目标位置，完全使用 target 的坐标
                Vector3 target = new Vector3(
                    _brid.nestTrans.position.x, // 使用 target 的 X
                    _brid.nestTrans.position.y, // 使用 target 的 Y
                    _brid.transform.position.z
                );

                // 计算飞行方向向量
                Vector3 direction = target - _brid.transform.position;
                float distance = direction.magnitude;

                // 如果距离太近，直接回到Idle状态
                if (distance < 1f)
                {
                    currMachine.ChangeState<BirdIdleState>();
                    return;
                }

                // 计算飞行角度（相对于水平面的角度）
                float horizontalDistance = Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z);
                float verticalDistance = Mathf.Abs(direction.y);
                float flightAngle = Mathf.Atan2(verticalDistance, horizontalDistance) * Mathf.Rad2Deg;

                // 如果角度太陡（接近垂直），调整目标位置让飞行更自然
                // if (flightAngle > 60f)
                // {
                //     // 计算一个更平缓的飞行路径
                //     float maxHeight = horizontalDistance * 0.5f; // 最大高度为水平距离的一半
                //     float adjustedY = _brid.transform.position.y + Mathf.Min(direction.y, maxHeight);
                //
                //     // 创建中间点，让小鸟先飞到合适的高度
                //     Vector3 intermediateTarget = new Vector3(
                //         _brid.transform.position.x + direction.x * 0.3f, // 先飞30%的水平距离
                //         adjustedY,
                //         _brid.transform.position.z
                //     );
                //
                //     // 设置朝向
                //     _brid.sr.flipX = intermediateTarget.x > _brid.transform.position.x;
                //
                //     // 第一阶段：斜着飞到中间点
                //     float firstDistance = Vector3.Distance(_brid.transform.position, intermediateTarget);
                //     float firstFlyTime = firstDistance / _brid.flySpeed;
                //
                //     // 飞向中间点时逐渐缩小
                //     Vector3 targetScale = Vector3.one * _brid.AdultBirdSize * 0.8f;
                //     _brid.transform.DOScale(targetScale, firstFlyTime).SetEase(Ease.Linear);
                //
                //     _brid.transform.DOMove(intermediateTarget, firstFlyTime).SetEase(Ease.Linear).OnComplete(() =>
                //     {
                //         // 第二阶段：从中间点飞到最终目标
                //         float secondDistance = Vector3.Distance(intermediateTarget, target);
                //         float secondFlyTime = secondDistance / _brid.flySpeed;
                //
                //         _brid.transform.DOMove(target, secondFlyTime).SetEase(Ease.Linear).OnComplete(() =>
                //         {
                //             currMachine.ChangeState<BirdFlyWaitState>();
                //         });
                //     });
                // }
                if(flightAngle <= 60)
                {
                    // 角度合适，直接斜着飞向目标
                    _brid.sr.flipX = target.x > _brid.transform.position.x;

                    float flyTime = distance / _brid.flySpeed;

                    // 飞向目标时逐渐缩小
                    Vector3 targetScale = Vector3.one * _brid.AdultBirdSize * 0.8f;
                    _brid.transform.DOScale(targetScale, flyTime).SetEase(Ease.Linear);

                    _brid.transform.DOMove(target, flyTime).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        currMachine.ChangeState<BirdFlyWaitState>();
                    });
                }
                else
                {
                    currMachine.ChangeState<BirdIdleState>();
                }
            }
            else
            {
                // 如果没有可用的飞行位置，直接回到 Idle 状态
                currMachine.ChangeState<BirdIdleState>();
            }
        }

        public override void OnUpdate()
        {

        }

        public override void OnExit()
        {

        }
    }
}