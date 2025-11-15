using DG.Tweening;
using QFramework;
using UnityEngine;
using UnityEngine.AI;

namespace BirdGame
{
    public class BirdHatchingEggState : StateBase
    {
        private Brid _brid;
        private float dirX;
        private int currentPathIndex = 0; // 当前路径点索引
        private bool isEnd = false;
        private bool isExit = false;
        private float startTime;
        private float totalTime = 10f;
        
        public BirdHatchingEggState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            int areaToAdd = NavMesh.GetAreaFromName("Limit");
            _brid.agent.areaMask |= (1 << areaToAdd);
            if (this.GetModel<IGameModel>().CurrentTent == null || this.GetModel<IGameModel>().HatchingBirds.Count >= 2 || this.GetModel<IGameModel>().CurrentHatchingBirdIndex != -1 || _brid.walkArea != 3)
            {
                DONext();
                return;
            }
            
            // 检查品种：如果帐篷里已经有一只鸟，检查品种是否相同
            if (this.GetModel<IGameModel>().HatchingBirds.Count == 1)
            {
                int firstBirdIndex = this.GetModel<IGameModel>().HatchingBirds[0];
                int firstBirdType = this.GetModel<IBirdModel>().BirdList[firstBirdIndex].birdType;
                int currentBirdType = this.GetModel<IBirdModel>().BirdList[_brid.birdIndex].birdType;
                
                if (firstBirdType != currentBirdType)
                {
                    DONext();
                    return;
                }
            }
            
            this.GetModel<IGameModel>().HatchingBirds.Add(_brid.birdIndex);
            
            // 开始从第一个点进入
            currentPathIndex = 0;
            _brid.agent.SetDestination(this.GetModel<IGameModel>().CurrentTent.enterPoses[0].position);
            _brid.agent.isStopped = false;
        }

        public override void OnUpdate()
        {
            if (!_brid.agent.pathPending && _brid.agent.remainingDistance <= 0.01f)
            {
                var tent = this.GetModel<IGameModel>().CurrentTent;
                
                // 依次经过所有enterPos点，然后到endPos 规定路径
                if (currentPathIndex < tent.enterPoses.Length)
                {
                    currentPathIndex++;
                    if (currentPathIndex < tent.enterPoses.Length)
                    {
                        _brid.agent.SetDestination(tent.enterPoses[currentPathIndex].position);
                        _brid.agent.isStopped = false;
                    }
                    else
                    {
                        _brid.agent.SetDestination(tent.endPos.position);
                        _brid.agent.isStopped = false;
                    }
                    return;
                }
                else if (!isEnd)
                {
                    Debug.Log("End");
                    isEnd = true;
                    _brid.agent.isStopped = true;
                    _brid.agent.velocity = Vector3.zero;
                    _brid.lineRenderer.positionCount = 0;
                    startTime = 0;
                    this.GetModel<IGameModel>().EnteredBirds.Value++;
                }
                else if(!isExit)
                {
                    if (this.GetModel<IGameModel>().EnteredBirds.Value == 2)
                    {
                        startTime += Time.deltaTime;
                        this.GetModel<IGameModel>().HatchingProgress.Value = startTime / totalTime;
                        if (startTime >= totalTime)
                        {
                            if (this.GetModel<IGameModel>().HatchingBirds.Count == 2 &&
                                this.GetModel<IGameModel>().HatchingBirds[0] == _brid.birdIndex)
                            {
                                //CreateEgg();
                                this.GetModel<IGameModel>().CurrentHatchingBirdIndex = _brid.birdIndex;
                                this.GetModel<IGameModel>().HatchingBirds.Clear();
                                this.GetModel<IGameModel>().HatchingProgress.Value = 0;
                                this.GetModel<IGameModel>().IsHatchingFinished.Value = true;
                                ExitTent(0);
                            }
                            else
                            { 
                                ExitTent(1);
                            }

                            isExit = true;
                            return;
                        }

                        return;
                    }
                    else
                    {
                        startTime = 0;
                    }
                }
                else
                {
                    currMachine.ChangeState<BirdRunState>();
                }
            }
            else
            {
                _brid.sr.flipX = _brid.agent.velocity.x >= 0;
                _brid.anim.SetFloat("MoveSpeed", 1);
            }
        }

        public override void OnExit()
        {
            int areaToRemove = NavMesh.GetAreaFromName("Limit");
            _brid.agent.areaMask &= ~(1 << areaToRemove);
        }

        private void ExitTent(int index)
        {
            Debug.Log("Exit");
            _brid.agent.isStopped = false;
            this.GetModel<IGameModel>().HatchingProgress.Value = 0;
            var target = this.GetModel<IGameModel>().CurrentTent.exitPoses[index].position;
            if (_brid.agent.SetDestination(target))
            {
                _brid.agent.isStopped = false;
            }
            else
            {
                Debug.LogError("目标超出渲染地面范围！");
            }
        }

        private void DONext()
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

        
    }
}