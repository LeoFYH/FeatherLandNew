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
            // if (!_brid.isSmall)
            // {
            //     DONext();
            //     return;
            // }

            // Check if the current food is null or destroyed before entering eat state
            if (_brid.currFood == null)
            {
                DONext();
                return;
            }

            if (_brid.currFood != null)
            {
                _brid.currFood.isTargeted = true;
            }
            
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
            if (_brid.anim.GetCurrentAnimatorStateInfo(0).IsName("Stroke"))
            {
                currMachine.ChangeState<BirdIdleState>();
                return;
            }
            
            if (_brid.currFood == null)
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
                // Additional null check before accessing transform
                if (_brid.currFood == null)
                {
                    DONext();
                    return;
                }

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
            // Check if the food still exists before accessing its transform
            if (_brid.currFood == null)
            {
                // Food was destroyed, exit eating state
                _brid.anim.SetBool("Eat", false);
                currMachine.ChangeState<BirdIdleState>();
                return;
            }

            dirX = _brid.currFood.transform.position.x - _brid.transform.position.x;
            _brid.sr.flipX = dirX >= 0;
            if (eatFoodTimer < _brid.eatFoodTime)
            {
                eatFoodTimer += Time.deltaTime;
            }
            else
            {
                eatFoodTimer = 0;
                int birdIndex = this.GetModel<IBirdModel>().BirdList[_brid.birdIndex].birdType;
                int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
                var conf = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex);
                float totalExp = conf.totalExp;
                _brid.currentExp.Value += conf.eatExp;
                // 安全检查：确保birdIndex在有效范围内
                if (_brid.birdIndex < 0 || _brid.birdIndex >= this.GetModel<IBirdModel>().BirdList.Count)
                {
                    Debug.LogWarning($"鸟的索引无效: {_brid.birdIndex}, BirdList.Count: {this.GetModel<IBirdModel>().BirdList.Count}");
                    currMachine.ChangeState<BirdIdleState>();
                    return;
                }
                
               
                if (_brid.currentExp.Value >= totalExp && _brid.isSmall)
                {
                    DOTween.To(v =>
                    {
                        _brid.animScale = v;
                    }, _brid.BabyBirdSize / _brid.AdultBirdSize, 1f, 0.5f).OnComplete(() =>
                    {
                        _brid.anim.SetTrigger("Stroke");
                    });
                    this.GetSystem<IAudioSystem>().PlayEffect(EffectType.GrowUp);
                    //_brid.transform.DOScale(_brid.AdultBirdSize, 0.2f);
                    //this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
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
            // if (_brid.isSmall)
            // {
                int random = Random.Range(0, 2);
                if (random == 0)
                {
                    currMachine.ChangeState<BirdIdleState>();
                }
                else
                {
                    currMachine.ChangeState<BirdRunState>();
                }
            // }
            // else
            // {
            //     currMachine.ChangeState<BirdIdleState>();
            // }
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