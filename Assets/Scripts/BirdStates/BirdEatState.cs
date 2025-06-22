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
    }

    public override void OnUpdate()
    {
        if (!_brid.isSmall || _brid.currFood == null)
        {
            DONext();
            return;
        }
        
        // 计算到食物的距离
        float distanceToFood = Vector3.Distance(_brid.transform.position, _brid.currFood.transform.position);
        
        // 计算精确的吃食物位置（鸟嘴对齐食物的位置）
        Vector3 eatPosition = _brid.currFood.transform.position + new Vector3(
            _brid.sr.flipX ? -_brid.BirdEatDistance * _brid.BabyBirdSize : _brid.BirdEatDistance * _brid.BabyBirdSize, 
            0f, 
            0
        );
        
        // 如果距离小于0.5，锁定当前食物目标，不再改变朝向
        if (distanceToFood < 0.5f)
        {
            // 锁定朝向，不再改变
            // 继续向精确的吃食物位置移动
            if (Vector3.Distance(_brid.transform.position, eatPosition) > 0.01f)
            {
                _brid.transform.position = Vector3.MoveTowards(_brid.transform.position, eatPosition, _brid.moveSpeed * Time.deltaTime);
                _brid.anim.SetFloat("MoveSpeed", 1f);
            }
            else
            {
                // 只有到达精确位置才开始吃食物
                _brid.anim.SetFloat("MoveSpeed", 0);
                _brid.anim.SetBool("Eat", true);
                EatFood();
            }
            return; // 直接返回，不执行后面的逻辑
        }
        else
        {
            // 距离较远时，可以调整朝向
            _brid.sr.flipX = _brid.currFood.transform.position.x > _brid.transform.position.x;
            
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
                // 只有到达精确位置才开始吃食物
                _brid.anim.SetFloat("MoveSpeed", 0);
                _brid.anim.SetBool("Eat", true);
                EatFood();
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
