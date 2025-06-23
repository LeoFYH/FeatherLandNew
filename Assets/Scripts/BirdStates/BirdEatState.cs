using DG.Tweening;
using FSM;
using UnityEngine;

public class BirdEatState : StateBase
{
    private Brid _brid;
    private float eatFoodTimer;

    public BirdEatState(StateMachine machine) : base(machine)
    {
        _brid = machine.currObj.GetComponent<Brid>();
    }

    public override void OnEnter()
    {
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
    }

    public override void OnUpdate()
    {
        if (!_brid.isSmall || _brid.currFood == null)
        {
            DONext();
            return;
        }
        
        // 计算精确的吃食物位置（鸟嘴对齐食物的位置）
        Vector3 eatPosition = _brid.currFood.transform.position + new Vector3(
            _brid.sr.flipX ? -_brid.BirdEatDistance * _brid.BabyBirdSize : _brid.BirdEatDistance * _brid.BabyBirdSize, 
            0f, 
            0
        );
        
        // 调整朝向
        bool shouldFlip = _brid.currFood.transform.position.x > _brid.transform.position.x;
        
        // 只有当朝向改变合理时才改变朝向（避免突然倒着走）
        if (_brid.sr.flipX != shouldFlip)
        {
            // 检查改变朝向是否合理（避免180度转向）
            float currentDirection = _brid.sr.flipX ? -1f : 1f;
            float targetDirection = shouldFlip ? 1f : -1f;
            
            // 如果食物在合理范围内，才允许改变朝向
            if (Mathf.Abs(_brid.currFood.transform.position.x - _brid.transform.position.x) > 0.5f)
            {
                _brid.sr.flipX = shouldFlip;
            }
        }
        
        // 重新计算精确的吃食物位置（因为朝向可能改变了）
        eatPosition = _brid.currFood.transform.position + new Vector3(
            _brid.sr.flipX ? -_brid.BirdEatDistance * _brid.BabyBirdSize : _brid.BirdEatDistance * _brid.BabyBirdSize, 
            0f, 
            0
        );
        
        // 向精确的吃食物位置移动
        if (Vector3.Distance(_brid.transform.position, eatPosition) > 0.01f)
        {
            _brid.transform.position = Vector3.MoveTowards(_brid.transform.position, eatPosition, _brid.moveSpeed * Time.deltaTime);
            _brid.anim.SetFloat("MoveSpeed", 1f);
        }
        else
        {
            // 到达计算位置后，检查鸟嘴是否真的对齐食物
            float beakToFoodDistance = Vector3.Distance(_brid.transform.position, _brid.currFood.transform.position);
            
            // 如果鸟嘴距离食物太远，说明计算有误，取消吃食物
            if (beakToFoodDistance > _brid.BirdEatDistance * _brid.BabyBirdSize + 0.2f)
            {
                _brid.anim.SetFloat("MoveSpeed", 0);
                _brid.anim.SetBool("Eat", false);
                DONext();
                return;
            }
            
            // 只有到达精确位置且鸟嘴对齐才开始吃食物
            _brid.anim.SetFloat("MoveSpeed", 0);
            _brid.anim.SetBool("Eat", true);
            EatFood();
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
                
                if (GameManager.Instance.nests.Count > 0)
                {
                    int index = Random.Range(0, GameManager.Instance.nests.Count);
                    _brid.nest = GameManager.Instance.nests[index];
                    if (_brid.nest != null)
                    {
                        _brid.nest.Init(_brid);
                        _brid.nestPos = _brid.nest.transform.position;
                        _brid.isSmall = false;
                        _brid.distance = Vector3.Distance(_brid.transform.position, _brid.nestPos);
                    }
                }
            }

            _brid.originalScale = _brid.transform.localScale;
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
}
