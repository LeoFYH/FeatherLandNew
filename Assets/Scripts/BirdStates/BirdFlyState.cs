using DG.Tweening;
using FSM;
using UnityEngine;

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
        _brid.anim.SetBool("Fly", true);
        
        if (GameManager.Instance.flyPositions.Count > 0)
        {
            int random = Random.Range(0, GameManager.Instance.flyPositions.Count);
            _brid.flyIndex = random;
            _brid.nestTrans = GameManager.Instance.flyPositions[random];
            GameManager.Instance.flyPositions.RemoveAt(random);

            // 创建目标位置，完全使用 target 的坐标
            Vector3 target = new Vector3(
                _brid.nestTrans.position.x,  // 使用 target 的 X
                _brid.nestTrans.position.y,  // 使用 target 的 Y
                _brid.transform.position.z
            );

            _brid.sr.flipX = target.x > _brid.transform.position.x;

            float distance = Vector3.Distance(_brid.transform.position, target);
            float flyTime = distance / _brid.flySpeed;

            // 飞向目标时逐渐缩小为当前scale的0.8倍
            Vector3 targetScale = _brid.originalScale * _brid.lastPerspectiveScale;
            _brid.transform.DOScale(targetScale, flyTime).SetEase(Ease.Linear);

            _brid.transform.DOMove(target, flyTime).SetEase(Ease.Linear).OnComplete(() =>
            {
                currMachine.ChangeState<BirdFlyWaitState>();
            });
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
