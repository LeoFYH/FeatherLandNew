using DG.Tweening;
using QFramework;
using UnityEngine;
using UnityEngine.AI;

namespace BirdGame
{

    public class BirdEatState : StateBase
    {
        private Brid _brid;
        private float eatFoodTimer;
        private bool isOtherBirdEnter = false;
        private NavMeshPath currentPath = new NavMeshPath();
        private Vector3 eatPosition;
        private float dirX;

        public BirdEatState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            //_brid.onNearOtherBird = OnNearOtherBird;
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
            var endPath = _brid.transform.position;
            if (_brid.agent.path.corners.Length > 1)
            {
                endPath = _brid.agent.path.corners[^1];
            }
            dirX = _brid.currFood.transform.position.x - endPath.x;
            if (dirX == 0)
            {
                dirX = _brid.currFood.transform.position.x - _brid.transform.position.x;
            }

            Debug.Log($"Direction: {dirX}");
            // 计算精确的吃食物位置（鸟嘴对齐食物的位置）
            eatPosition = _brid.currFood.transform.position + new Vector3(
                dirX >= 0
                    ? -_brid.BirdEatDistance * _brid.BabyBirdSize
                    : _brid.BirdEatDistance * _brid.BabyBirdSize,
                0f,
                0
            );
            _brid.agent.SetDestination(eatPosition);
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

            if (!_brid.agent.pathPending && _brid.agent.remainingDistance <= 0.01f)
            {
                if (this.GetModel<IConfigModel>().BirdConfig.isDrawPathLine)
                    _brid.lineRenderer.positionCount = 0;
                
                _brid.anim.SetFloat("MoveSpeed", 0);
                _brid.anim.SetBool("Eat", true);
                _brid.agent.isStopped = true;
                _brid.agent.velocity = Vector3.zero;
                EatFood();
            }
            else
            {
                var endPath = _brid.transform.position;
                if (_brid.agent.path.corners.Length > 1)
                {
                    endPath = _brid.agent.path.corners[^1];
                }
                dirX = _brid.currFood.transform.position.x - endPath.x;
                if (dirX == 0)
                {
                    dirX = _brid.currFood.transform.position.x - _brid.transform.position.x;
                }
                eatPosition = _brid.currFood.transform.position + new Vector3(
                    dirX >= 0
                        ? -_brid.BirdEatDistance * _brid.BabyBirdSize
                        : _brid.BirdEatDistance * _brid.BabyBirdSize,
                    0f,
                    0
                );
                _brid.agent.SetDestination(eatPosition);
                if (this.GetModel<IConfigModel>().BirdConfig.isDrawPathLine)
                    DrawPath();
                if (Mathf.Abs(_brid.agent.velocity.x) > 0.001f)
                    _brid.sr.flipX = _brid.agent.velocity.x >= 0;
                
                // 只要在移动就播放走路动画
                // if (_brid.agent.velocity.magnitude > 0.001f)
                // {
                //     //_brid.anim.SetFloat("MoveSpeed", 1f);
                //     _brid.sr.flipX = _brid.agent.velocity.x >= 0;
                // }
                // else
                // {
                //     //_brid.anim.SetFloat("MoveSpeed", 0f);
                // }
                
                if (isOtherBirdEnter)
                {
                    if (_brid.currFood != null)
                    {
                        _brid.currFood.UntargetFood();
                        _brid.currFood = null;
                    }

                    DONext();
                }
            }
        }

        private void EatFood()
        {
            dirX = _brid.currFood.transform.position.x - _brid.transform.position.x;
            _brid.sr.flipX = dirX >= 0;
            if (eatFoodTimer < _brid.eatFoodTime)
            {
                eatFoodTimer += Time.deltaTime;
            }
            else
            {
                eatFoodTimer = 0;
                _brid.eatFoodCount.Value++;

                if (_brid.eatFoodCount.Value == _brid.eatCountForBig)
                {
                    _brid.transform.DOScale(_brid.AdultBirdSize, 0.2f);
                    _brid.isSmall = false;
                }

                if (_brid.currFood != null)
                {
                    this.GetSystem<IGameSystem>().ReduceFood(_brid.currFood);
                    _brid.currFood = null;
                }

                _brid.anim.SetBool("Eat", false);
                
                // 吃完后直接切换到Idle状态，防止动画和行为混乱
                currMachine.ChangeState<BirdIdleState>();
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

        private void DrawPath()
        {
            _brid.agent.CalculatePath(eatPosition, currentPath);
            int pathLength = currentPath.corners.Length;
            if (pathLength < 2)
            {
                _brid.lineRenderer.positionCount = 2;
                _brid.lineRenderer.SetPosition(0, _brid.transform.position);
                _brid.lineRenderer.SetPosition(1, eatPosition);
            }
            else
            {
                _brid.lineRenderer.positionCount = pathLength + 1;
                for (int i = 0; i < pathLength; i++)
                {
                    _brid.lineRenderer.SetPosition(i, currentPath.corners[i]);
                }
                _brid.lineRenderer.SetPosition(pathLength, eatPosition);
            }
        }
    }
}