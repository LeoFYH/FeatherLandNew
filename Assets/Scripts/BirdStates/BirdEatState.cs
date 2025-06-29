using DG.Tweening;
using FSM;
using UnityEngine;

public class BirdEatState : StateBase
{
    private Brid _brid;
    private float eatFoodTimer;
    private bool isOtherBirdEnter = false;

    public BirdEatState(StateMachine machine) : base(machine)
    {
        _brid = machine.currObj.GetComponent<Brid>();
    }

    public override void OnEnter()
    {
        _brid.onNearOtherBird = OnNearOtherBird;
        if (!_brid.isSmall)
        {
            DONext();
            return;
        }

        if (_brid.currFood != null)
        {
            _brid.currFood.isTargeted = true;
        }
        
        _brid.isAte = true;
        _brid.agent.SetDestination(_brid.currFood.transform.position);
        _brid.agent.isStopped = false;
        _brid.anim.SetFloat("MoveSpeed", 1f);
    }

    public override void OnUpdate()
    {
        if (!_brid.isSmall || _brid.currFood == null)
        {
            DONext();
            return;
        }
        
        if (!_brid.agent.pathPending && _brid.agent.remainingDistance <= 0.05f)
        {
            // // 到达计算位置后，检查鸟嘴是否真的对齐食物
            // float beakToFoodDistance = Vector3.Distance(_brid.transform.position, _brid.currFood.transform.position);
            //
            // // 如果鸟嘴距离食物太远，说明计算有误，取消吃食物
            // if (beakToFoodDistance > _brid.BirdEatDistance * _brid.BabyBirdSize + 0.2f)
            // {
            //     _brid.anim.SetFloat("MoveSpeed", 0);
            //     _brid.anim.SetBool("Eat", false);
            //     _brid.agent.isStopped = true;
            //     _brid.agent.velocity = Vector3.zero;
            //     DONext();
            //     return;
            // }
            
            _brid.anim.SetFloat("MoveSpeed", 0);
            _brid.anim.SetBool("Eat", true);
            _brid.agent.isStopped = true;
            _brid.agent.velocity = Vector3.zero;
            EatFood();
        }
        else
        {
            // 计算精确的吃食物位置（鸟嘴对齐食物的位置）
            Vector3 eatPosition = _brid.currFood.transform.position + new Vector3(
                _brid.sr.flipX ? -_brid.BirdEatDistance * _brid.BabyBirdSize : _brid.BirdEatDistance * _brid.BabyBirdSize, 
                0f, 
                0
            );
            _brid.agent.SetDestination(eatPosition);
            _brid.sr.flipX = _brid.agent.velocity.x >= 0;
            if (isOtherBirdEnter)
            {
                if (_brid.currFood != null)
                {
                    //GameManager.Instance.ReduceFood(_brid.currFood);
                    _brid.currFood.UntargetFood();
                    _brid.currFood = null;
                }
                DONext();
            }
        }
    }

    private void EatFood()
    {
        if (eatFoodTimer < _brid.eatFoodTime)
        {
            eatFoodTimer += Time.deltaTime;
        }
        else
        {
            eatFoodTimer = 0;
            _brid.eatFoodCount++;

            if (_brid.eatFoodCount == _brid.eatCountForBig)
            {
                //_brid.transform.localScale = Vector3.one * _brid.AdultBirdSize;
                _brid.transform.DOScale(_brid.AdultBirdSize, 0.2f);
                _brid.isSmall = false;

                // if (GameManager.Instance.nests.Count > 0)
                // {
                //     int index = Random.Range(0, GameManager.Instance.nests.Count);
                //     _brid.nest = GameManager.Instance.nests[index];
                //     if (_brid.nest != null)
                //     {
                //         _brid.nest.Init(_brid);
                //         _brid.nestPos = _brid.nest.transform.position;
                //         _brid.isSmall = false;
                //         _brid.distance = Vector3.Distance(_brid.transform.position, _brid.nestPos);
                //     }
                // }
            }
            
            if (_brid.currFood != null)
            {
                GameManager.Instance.ReduceFood(_brid.currFood);
                _brid.currFood = null;
            }
            
            _brid.anim.SetBool("Eat", false);
        }
    }

    public override void OnExit()
    {
        _brid.onNearOtherBird = null;
        _brid.anim.SetFloat("MoveSpeed", 0);
        _brid.anim.SetBool("Eat", false);
        eatFoodTimer = 0;
        
        // Release the food target when leaving the state
        if (_brid.currFood != null)
        {
            _brid.currFood.isTargeted = false;
            _brid.currFood = null;
        }
    }

    private void DONext()
    {
        if (_brid.isSmall)
        {
            int random = Random.Range(0, 2);
            if (random == 0)
            {
                currMachine.ChangeState<BirdIdleState>();
            }
            else
            {
                currMachine.ChangeState<BirdRunState>();
            }
        }
        else
        {
            currMachine.ChangeState<BirdIdleState>();
        }
    }

    private void OnNearOtherBird()
    {
        isOtherBirdEnter = true;
    }
}
