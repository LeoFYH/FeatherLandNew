using DG.Tweening;
using FSM;
using UnityEngine;

public class BirdFlyWaitState : StateBase
{
    private Brid _brid;
    
    public BirdFlyWaitState(StateMachine machine) : base(machine)
    {
        _brid = machine.currObj.GetComponent<Brid>();
    }

    public override void OnEnter()
    {
        _brid.anim.SetBool("IsTakeOff", false);
        _brid.anim.SetBool("Fly", false);
        //_brid.anim.Play("FlyWait");

        if (_brid.nestTrans != null)
        {
            // 完全使用 target 的位置
            Vector3 alignedPosition = new Vector3(
                _brid.nestTrans.position.x,  // 使用 target 的 X
                _brid.nestTrans.position.y,  // 使用 target 的 Y
                _brid.transform.position.z
            );
            
            _brid.transform.position = alignedPosition;
        }

        float waitTime = Random.Range(3f, 8f);
        DOTween.Sequence().AppendCallback(() =>
        {
            currMachine.ChangeState<BirdFlyDownState>();
        }).SetDelay(waitTime);
    }

    public override void OnUpdate()
    {
        
    }

    public override void OnExit()
    {
        
    }
}
