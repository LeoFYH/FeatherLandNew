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
        _brid.isAte = true;
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
        
        Vector3 target = _brid.currFood.transform.position + new Vector3(_brid.sr.flipX ? -_brid.BirdEatDistance * _brid.BabyBirdSize : _brid.BirdEatDistance * _brid.BabyBirdSize, 0f, 0);
        if (Vector3.Distance(_brid.transform.position, target) > 0.01f)
        {
            _brid.sr.flipX = _brid.currFood.transform.position.x > _brid.transform.position.x;
            _brid.transform.position =
                Vector3.MoveTowards(_brid.transform.position, target, _brid.moveSpeed * Time.deltaTime);
            _brid.anim.SetFloat("MoveSpeed", 1f);
        }
        else
        {
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
