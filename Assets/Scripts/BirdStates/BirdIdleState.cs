using System.Collections;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace BirdGame
{

    public class BirdIdleState : StateBase
    {
        private Brid _brid;
        private Coroutine coroutine;
        private int random;
        private bool isLicking = false; // 标记是否正在舔毛

        public BirdIdleState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            float time = Random.Range(6f, 10f);
            random = Random.Range(1, 10);
            float lickingTime = Random.Range(1f, 4f);
            if (!_brid.agent.enabled)
                _brid.agent.enabled = true;
            _brid.sr.sortingOrder = 3;

            // 重置被抚摸标志
            _brid.isBeingPetted = false;

            coroutine = _brid.StartCoroutine(WaitForNext(time));
            if (!_brid.isSmall)
            {
                DOTween.Sequence().AppendCallback(() =>
                {
                    isLicking = true; // 标记开始舔毛
                    _brid.anim.SetTrigger(AnimatorHashes.Licking);
                }).SetDelay(lickingTime);
            }
        }

        public override void OnUpdate()
        {
            // 如果正在舔毛，不检测食物，保持idle状态
            if (isLicking)
            {
                return;
            }

            // 检查是否应该跟随鼠标（优先级最高）
            // if (_brid.shouldFollowMouse)
            // {
            //     Debug.Log("IdleState: 检测到跟随鼠标标志，切换到RunState");
            //     currMachine.ChangeState<BirdRunState>();
            //     return;
            // }

            // 检查是否在抚摸后的锁定期间
            float timeSinceLastPet = Time.time - _brid.lastPetTime;
            if (timeSinceLastPet < _brid.idleLockDuration)
            {
                return; // 在锁定期间，不检测食物，保持idle状态
            }

            // 检查是否在进食后的等待期间（随机0-3秒）
            // 如果鸟从未吃过食物（lastEatTime为0），则不需要等待
            if (_brid.lastEatTime > 0)
            {
                float timeSinceLastEat = Time.time - _brid.lastEatTime;
                if (timeSinceLastEat < _brid.eatWaitDuration)
                {
                    return; // 在进食等待期间，不检测食物，保持idle状态
                }
            }

            if (_brid.walkArea == 3)
            {
                if (_brid.currFood == null)
                {
                    Food food;
                    if (this.GetSystem<IGameSystem>().TryGetUntargetedFood(_brid.transform.position, out food))
                    {
                        // if(random == 1) // 10个数中随机到1时去吃食物
                        // {
                        _brid.currFood = food;
                        food.isTargeted = true;
                        if(_brid.isSmall)
                            currMachine.ChangeState<BirdEatState>();
                        else
                            currMachine.ChangeState<BirdRunState>();
                        //}
                    }
                }
            }
        }

        public override void OnExit()
        {
            if (coroutine == null)
                return;
            _brid.StopCoroutine(coroutine);
        }

        private void DONext()
        {
            // 如果正在舔毛，不切换状态
            if (isLicking)
            {
                return;
            }
            

            // if (_brid.anim.GetCurrentAnimatorStateInfo(0).shortNameHash == AnimatorHashes.StrokeState)
            // {
            //     if (_brid.shouldFollowMouse)
            //     {
            //         Debug.Log("IdleState: 检测到跟随鼠标标志，切换到RunState");
            //         currMachine.ChangeState<BirdRunState>();
            //     }
            //     return;
            // }

            if (_brid.isDesktopBird)
            {
                currMachine.ChangeState<BirdRunState>();
                return;
            }

            // 安全检查：确保birdIndex在有效范围内
            if (_brid.birdIndex < 0 || _brid.birdIndex >= this.GetModel<IBirdModel>().BirdList.Count)
            {
                Debug.LogWarning($"鸟的索引无效: {_brid.birdIndex}, BirdList.Count: {this.GetModel<IBirdModel>().BirdList.Count}");
                currMachine.ChangeState<BirdRunState>();
                return;
            }
            
            int birdIndex = this.GetModel<IBirdModel>().BirdList[_brid.birdIndex].birdType;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var birdConfig = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex);
            if (birdConfig == null)
            {
                currMachine.ChangeState<BirdRunState>();
                return;
            }

            if (!birdConfig.canFly)
            {
                if (_brid.isSmall)
                    currMachine.ChangeState<BirdRunState>();
                else
                {
                    int random = Random.Range(0, 2);
                    if (random == 0)
                        currMachine.ChangeState<BirdRunState>();
                    else 
                        currMachine.ChangeState<BirdHatchingEggState>();
                }
                return;
            }

            if (_brid.isSmall)
            {
                currMachine.ChangeState<BirdRunState>();
                return;
            }
            else
            {
                // 长大的鸟优先选择飞行动作
                bool canFlyWait = birdConfig.canFlyWait;
                bool canFlyHorizontal = birdConfig.canFlyHorizontal;
                bool hasFlyPositions = this.GetModel<IBirdModel>().FlyPositions.Count > 0;
                
                // 优先选择飞行动作的逻辑
                
                if (hasFlyPositions && canFlyWait && Random.Range(0f, 1f) < 0.8f)
                {
                    // 如果有飞行位置且支持飞行等待，优先飞到树上
                    currMachine.ChangeState<BirdFlyState>();
                    return;
                }
                else if (canFlyHorizontal && Random.Range(0f, 1f) < 0.8f)
                {
                    // 如果支持水平飞行，优先选择水平飞行
                    currMachine.ChangeState<BirdFlyHorizontalState>();
                    return;
                }
                // 如果都不支持飞行，则使用备选的随机逻辑
                
                // 原有的飞行选择逻辑（作为备选）
                if (this.GetModel<IBirdModel>().FlyPositions.Count > 0)
                {
                    // 检查是否能飞行等待
                    if (!birdConfig.canFlyWait)
                    {
                        // 如果不能飞行等待，只进行远处飞行
                        int index = Random.Range(0, 3);
                        if (index == 0)
                        {
                            currMachine.ChangeState<BirdRunState>();
                        }
                        else if(index == 1)
                        {
                            if (!birdConfig.canFlyHorizontal)
                            {
                                currMachine.ChangeState<BirdRunState>();
                            }
                            else
                            {
                                currMachine.ChangeState<BirdFlyHorizontalState>();
                            }
                            //currMachine.ChangeState<BirdFlyHorizontalState>();
                        }
                        else
                        {
                            currMachine.ChangeState<BirdHatchingEggState>();
                        }
                    }
                    else
                    {
                        // 如果可以飞行等待，正常选择飞行方式
                        int index = Random.Range(0, 4);
                        if (index == 0)
                        {
                            currMachine.ChangeState<BirdRunState>();
                        }
                        else if (index == 1)
                        {
                            currMachine.ChangeState<BirdFlyState>();
                        }
                        else if(index == 2)
                        {
                            if (!birdConfig.canFlyHorizontal)
                            {
                                currMachine.ChangeState<BirdFlyState>();
                            }
                            else
                            {
                                currMachine.ChangeState<BirdFlyHorizontalState>();
                            }
                        }
                        else
                        {
                            currMachine.ChangeState<BirdHatchingEggState>();
                        }
                    }
                }
                else //基本不会触发
                {
                    int index = Random.Range(0, 3);
                    if (index == 0)
                    {
                        currMachine.ChangeState<BirdRunState>();
                    }
                    else
                    {
                        if (!birdConfig.canFlyHorizontal)
                        {
                            currMachine.ChangeState<BirdRunState>();
                        }
                        else
                        {
                            currMachine.ChangeState<BirdFlyHorizontalState>();
                        }
                        //currMachine.ChangeState<BirdFlyHorizontalState>();
                    }
                }
            }
        }

        private IEnumerator WaitForNext(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            
            // 等待舔毛动画完成（大约0.7秒）
            if (isLicking)
            {
                yield return new WaitForSeconds(0.7f);
                isLicking = false; // 标记舔毛完成
            }
            
            DONext();
        }
    }
}
