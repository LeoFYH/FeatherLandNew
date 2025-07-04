using DG.Tweening;
using FSM;
using UnityEngine;
using UnityEngine.AI;

public class BirdRunState : StateBase
{
    private Brid _brid;
    private Vector3 target;
    
    public BirdRunState(StateMachine machine) : base(machine)
    {
        _brid = machine.currObj.GetComponent<Brid>();
    }

    public override void OnEnter()
    {
        _brid.onNearOtherBird = OnNearOtherBird;
        
        _brid.isAte = false;
        // Release any existing food target when entering run state
        if (_brid.currFood != null)
        {
            _brid.currFood.isTargeted = false;
            _brid.currFood = null;
        }

        //Vector2 currentPos = _brid.transform.position;
        // Vector2 newTarget;
        //
        // var walkableArea = NavigationManager.Instance.GetWalkableArea(_brid.walkArea);
        // if (walkableArea != null)
        // {
        //     newTarget = walkableArea.GetRandomPoint(currentPos, _brid.radiusX);
        // }
        // else
        // {
        //     float x = Random.Range(-_brid.radiusX, _brid.radiusX);
        //     float y = Random.Range(-_brid.radiusY, _brid.radiusY);
        //     newTarget = new Vector2(currentPos.x + x, currentPos.y + y);
        // }

        target = NavigationManager.Instance.GetRandomTarget(_brid.walkArea);
        while (target == Vector3.zero)
        {
            target = NavigationManager.Instance.GetRandomTarget(_brid.walkArea);
        }
        if (_brid.agent.SetDestination(target))
        {
            _brid.agent.isStopped = false;
        }
        else
        {
            Debug.LogError("目标超出渲染地面范围！");
        }

        float distance = _brid.agent.remainingDistance;
        float time = distance / _brid.moveSpeed;
        DOTween.Sequence().AppendCallback(() =>
        {
            if (_brid.isSmall)
            {
                Food food;
                if (GameManager.Instance.TryGetUntargetedFood(_brid.transform.position, out food))
                {
                    // int random = Random.Range(1, 10);
                    // if(random == 1) // 10个数中随机到1时去吃食物
                    // {
                        _brid.currFood = food;
                        currMachine.ChangeState<BirdEatState>();
                    //}
                }
            }
        }).SetDelay(time * 0.5f);
    }

    public override void OnUpdate()
    {
        if (!_brid.agent.pathPending && _brid.agent.remainingDistance <= 0.05f)
        {
            _brid.agent.isStopped = true;
            _brid.agent.velocity = Vector3.zero;
            DONext();
        }
        else
        {
            _brid.sr.flipX = _brid.agent.velocity.x >= 0;
            _brid.anim.SetFloat("MoveSpeed", 1);
        }
    }

    public override void OnExit()
    {
        _brid.onNearOtherBird = null;
        _brid.anim.SetFloat("MoveSpeed", 0f);
        _brid.agent.isStopped = true;
        _brid.agent.velocity = Vector3.zero;
    }

    private void OnNearOtherBird()
    {
        currMachine.ChangeState<BirdIdleState>();
    }

    private void DONext()
    {
        if (_brid.isSmall)
        {
            currMachine.ChangeState<BirdIdleState>();
            return;
        }

        int random = Random.Range(0, 2);
        if (random == 0)
        {
            currMachine.ChangeState<BirdIdleState>();
        }
        else if (random == 1)
        {
            if (GameManager.Instance.flyPositions.Count == 0)
            {
                currMachine.ChangeState<BirdFlyState>();
                return;
            }

            int index = Random.Range(0, 2);
            if (index == 0)
            {
                currMachine.ChangeState<BirdFlyState>();
            }
            else
            {
                currMachine.ChangeState<BirdFlyState>();
            }
        }
    }
}
