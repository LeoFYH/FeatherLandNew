using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class BirdHatchingEggState : StateBase
    {
        private Brid _brid;
        private float dirX;
        private bool isEnter = false;
        private bool isEnd = false;
        private float startTime;
        private float totalTime = 10f;
        
        public BirdHatchingEggState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            if (this.GetModel<IGameModel>().CurrentTent == null || this.GetModel<IGameModel>().HatchingBirds.Count >= 2)
            {
                DONext();
                return;
            }
            
            this.GetModel<IGameModel>().HatchingBirds.Add(_brid.birdIndex);
            var target = this.GetModel<IGameModel>().CurrentTent.enterPos.position;
            if (_brid.agent.SetDestination(target))
            {
                _brid.agent.isStopped = false;
            }
            else
            {
                Debug.LogError("目标超出渲染地面范围！");
            }
        }

        public override void OnUpdate()
        {
            if (!_brid.agent.pathPending && _brid.agent.remainingDistance <= 0.01f)
            {
                if (!isEnter)
                {
                    Debug.Log("Enter");
                    isEnter = true;
                    var target = this.GetModel<IGameModel>().CurrentTent.endPos.position;
                    if (_brid.agent.SetDestination(target))
                    {
                        _brid.agent.isStopped = false;
                    }
                    else
                    {
                        Debug.LogError("目标超出渲染地面范围！");
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
                }
                else
                {
                    if (this.GetModel<IGameModel>().HatchingBirds.Count == 2)
                    {
                        startTime += Time.deltaTime;
                        if (startTime >= totalTime)
                        {
                            if (this.GetModel<IGameModel>().HatchingBirds[0] == _brid.birdIndex)
                            {
                                CreateEgg();
                                this.GetModel<IGameModel>().HatchingBirds.Clear();
                            }
                            currMachine.ChangeState<BirdRunState>();
                            return;
                        }
                        return;
                    }
                    else
                    {
                        startTime = 0;
                    }
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

        private void CreateEgg()
        {
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Egg", obj =>
            {
                GameObject go = GameObject.Instantiate(obj);
                go.GetComponent<Egg>().SetBirdIndex(this.GetModel<IBirdModel>().BirdList[_brid.birdIndex].birdType);
                go.transform.position = this.GetModel<IGameModel>().CurrentTent.createPos.position;
            });
            this.GetSystem<IUISystem>().ShowMask();
            this.GetSystem<IGameSystem>().SendEvent<DisableButtonEvent>();
        }
    }
}