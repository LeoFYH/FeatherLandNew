using FSM;
using UnityEngine;

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
        _brid.isAte = false;
        // Release any existing food target when entering run state
        if (_brid.currFood != null)
        {
            _brid.currFood.isTargeted = false;
            _brid.currFood = null;
        }

        Vector2 currentPos = _brid.transform.position;
        Vector2 newTarget;

        if (_brid.walkableArea != null)
        {
            newTarget = _brid.walkableArea.GetRandomPoint(currentPos, _brid.radiusX);
        }
        else
        {
            float x = Random.Range(-_brid.radiusX, _brid.radiusX);
            float y = Random.Range(-_brid.radiusY, _brid.radiusY);
            newTarget = new Vector2(currentPos.x + x, currentPos.y + y);
        }

        target = new Vector3(newTarget.x, newTarget.y, _brid.transform.position.z);
    }

    public override void OnUpdate()
    {
        if (_brid.isSmall)
        {
            // Find closest untargeted food
            Food closestFood = null;
            float closestDistance = float.MaxValue;

            foreach (var food in GameManager.Instance.foods)
            {
                if (!food.isTargeted)
                {
                    float distance = Vector3.Distance(_brid.transform.position, food.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestFood = food;
                    }
                }
            }

            // If we found food, either switch to it or keep current target if it's closer
            if (closestFood != null)
            {
                float currentDistance = _brid.currFood != null ? 
                    Vector3.Distance(_brid.transform.position, _brid.currFood.transform.position) : 
                    float.MaxValue;

                if (closestDistance < currentDistance)
                {
                    
                        if (_brid.currFood != null)
                        {
                            _brid.currFood.isTargeted = false;
                        }
                        _brid.currFood = closestFood;
                        _brid.currFood.isTargeted = true;
                        currMachine.ChangeState<BirdEatState>();
                        return;
                    
                }
            }
        }
        
        // If no food to chase, continue random movement
        Vector3 nextPosition = Vector3.MoveTowards(
            _brid.transform.position, 
            target, 
            _brid.moveSpeed * Time.deltaTime
        );

        if (_brid.walkableArea != null)
        {
            Vector2 nextPos2D = new Vector2(nextPosition.x, nextPosition.y);
            if (!_brid.walkableArea.IsPointInside(nextPos2D))
            {
                nextPos2D = _brid.walkableArea.GetClosestValidPoint(nextPos2D);
                nextPosition = new Vector3(nextPos2D.x, nextPos2D.y, nextPosition.z);
            }
        }

        _brid.transform.position = nextPosition;
        _brid.sr.flipX = target.x > _brid.transform.position.x;
        _brid.anim.SetFloat("MoveSpeed", 1);
        if (Vector3.Distance(_brid.transform.position, target) <= 0.1f)
        {
            DONext();
        }
    }

    public override void OnExit()
    {
        _brid.anim.SetFloat("MoveSpeed", 0f);
        
        // Release any food target when leaving run state
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
