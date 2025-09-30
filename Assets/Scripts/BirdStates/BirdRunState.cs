using DG.Tweening;
using QFramework;
using UnityEngine;
using UnityEngine.AI;

namespace BirdGame
{

    public class BirdRunState : StateBase
    {
        private Brid _brid;
        private Vector3 target;
        private NavMeshPath currentPath = new NavMeshPath();
        private bool isFollowingMouse = false;
        private float followMouseStartTime = 0;
        private float followMouseDuration = 8f; // 跟随时间延长到8秒

        public BirdRunState(StateMachine machine) : base(machine)
        {
            _brid = machine.currObj.GetComponent<Brid>();
        }

        public override void OnEnter()
        {
            _brid.onNearOtherBird = OnNearOtherBird;
            if (!_brid.agent.enabled)
                _brid.agent.enabled = true;
            // Release any existing food target when entering run state
            if (_brid.currFood != null)
            {
                _brid.currFood.isTargeted = false;
                _brid.currFood = null;
            }

            // 检查是否应该跟随鼠标
            if (_brid.shouldFollowMouse)
            {
                isFollowingMouse = true;
                followMouseStartTime = Time.time;
                _brid.shouldFollowMouse = false; // 重置标志
                Debug.Log("RunState: 开始跟随鼠标！");
                return; // 不设置随机目标，直接跟随鼠标
            }

            //Vector2 currentPos = _brid.transform.position;
            // Vector2 newTarget;
            //
            // var walkableArea = NavigationManager.Instance.GetWalkableArea(_brid.walkArea);
            // if (walkableArea != null)
            // {
            //     newTarget = walkableArea.GetRandomPoint(currentPos, _brid.radiusX);
            // }
            // else
            // {
            //     float x = Random.Range(-_brid.radiusX, _brid.radiusX);
            //     float y = Random.Range(-_brid.radiusY, _brid.radiusY);
            //     newTarget = new Vector2(currentPos.x + x, currentPos.y + y);
            // }

            target = NavigationManager.Instance.GetRandomTarget(_brid.walkArea);
            while (target == Vector3.zero)
            {
                target = NavigationManager.Instance.GetRandomTarget(_brid.walkArea);
            }

            if (_brid.agent.SetDestination(target))
            {
                _brid.agent.isStopped = false;
            }
            else
            {
                Debug.LogError("目标超出渲染地面范围！");
            }

            float distance = _brid.agent.remainingDistance;
            float time = distance / _brid.moveSpeed;
            DOTween.Sequence().AppendCallback(() =>
            {
                if (_brid.walkArea == 3)
                {
                    Food food;
                    if (this.GetSystem<IGameSystem>().TryGetUntargetedFood(_brid.transform.position, out food))
                    {
                        // int random = Random.Range(1, 10);
                        // if(random == 1) // 10个数中随机到1时去吃食物
                        // {
                        _brid.currFood = food;
                        currMachine.ChangeState<BirdEatState>();
                        //}
                    }
                }
            }).SetDelay(time * 0.5f);
        }

        public override void OnUpdate()
        {
            if (_brid.anim.GetCurrentAnimatorStateInfo(0).IsName("Stroke") && !isFollowingMouse)
            {
                currMachine.ChangeState<BirdIdleState>();
                return;
            }

            // 处理跟随鼠标逻辑
            if (isFollowingMouse)
            {
                float followDuration = Time.time - followMouseStartTime;
                if (followDuration >= followMouseDuration)
                {
                    // 跟随时间结束，切换到idle状态
                    isFollowingMouse = false;
                    currMachine.ChangeState<BirdIdleState>();
                    Debug.Log("跟随鼠标结束");
                    return;
                }
                else
                {
                    // 跟随鼠标移动
                    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorldPos.z = _brid.transform.position.z; // 保持Z轴不变
                    
                    _brid.agent.SetDestination(mouseWorldPos);
                    _brid.agent.isStopped = false;
                    
                    // 根据移动方向设置sprite朝向
                    if (Mathf.Abs(_brid.agent.velocity.x) > 0.001f)
                    {
                        _brid.sr.flipX = _brid.agent.velocity.x >= 0;
                    }
                    
                    _brid.anim.SetFloat("MoveSpeed", 1);
                    
                    // 跟随时冒爱心（每0.5秒一次）
                    if (Mathf.FloorToInt(followDuration * 2) != Mathf.FloorToInt((followDuration - Time.deltaTime) * 2))
                    {
                        this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Heart", obj =>
                        {
                            GameObject.Instantiate(obj, _brid.heartPos);
                        });
                    }
                    
                    return;
                }
            }

            // 正常的跑步逻辑
            if (!_brid.agent.pathPending && _brid.agent.remainingDistance <= 0.01f)
            {
                _brid.agent.isStopped = true;
                _brid.agent.velocity = Vector3.zero;
                _brid.lineRenderer.positionCount = 0;
                DONext();
            }
            else
            {
                if(this.GetModel<IConfigModel>().BirdConfig.isDrawPathLine)
                    DrawPath();
                _brid.sr.flipX = _brid.agent.velocity.x >= 0;
                _brid.anim.SetFloat("MoveSpeed", 1);
            }
        }
        
        private void DrawPath()
        {
            _brid.agent.CalculatePath(target, currentPath);
            int pathLength = currentPath.corners.Length;
            if (pathLength < 2)
            {
                _brid.lineRenderer.positionCount = 2;
                _brid.lineRenderer.SetPosition(0, _brid.transform.position);
                _brid.lineRenderer.SetPosition(1, target);
            }
            else
            {
                _brid.lineRenderer.positionCount = pathLength + 1;
                for (int i = 0; i < pathLength; i++)
                {
                    _brid.lineRenderer.SetPosition(i, currentPath.corners[i]);
                }
                _brid.lineRenderer.SetPosition(pathLength, target);
            }
        }

        public override void OnExit()
        {
            _brid.onNearOtherBird = null;
            _brid.lineRenderer.positionCount = 0;
            _brid.anim.SetFloat("MoveSpeed", 0f);
            _brid.agent.isStopped = true;
            _brid.agent.velocity = Vector3.zero;
        }

        private void OnNearOtherBird()
        {
            currMachine.ChangeState<BirdIdleState>();
        }

        private void DONext()
        {
            // 安全检查：确保birdIndex在有效范围内
            if (_brid.birdIndex < 0 || _brid.birdIndex >= this.GetModel<IBirdModel>().BirdList.Count)
            {
                Debug.LogWarning($"鸟的索引无效: {_brid.birdIndex}, BirdList.Count: {this.GetModel<IBirdModel>().BirdList.Count}");
                currMachine.ChangeState<BirdIdleState>();
                return;
            }

            if (_brid.isDesktopBird)
            {
                currMachine.ChangeState<BirdRunState>();
                return;
            }

            int birdIndex = this.GetModel<IBirdModel>().BirdList[_brid.birdIndex].birdType;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            if (!this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex).canFly)
            {
                currMachine.ChangeState<BirdIdleState>();
                return;
            }
            if (_brid.isSmall)
            {
                currMachine.ChangeState<BirdIdleState>();
                return;
            }

            // 检查是否能飞行等待
            if (!this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex).canFlyWait)
            {
                // 如果不能飞行等待，只进行远处飞行
                int random = Random.Range(0, 2);
                if (random == 0)
                {
                    currMachine.ChangeState<BirdIdleState>();
                }
                else
                {
                    if (!this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex).canFlyHorizontal)
                    {
                        currMachine.ChangeState<BirdIdleState>();
                    }
                    else
                    {
                        currMachine.ChangeState<BirdFlyHorizontalState>();
                    }
                    //currMachine.ChangeState<BirdFlyHorizontalState>();
                }
            }
            else
            {
                // 如果可以飞行等待，正常选择飞行方式
                int random = Random.Range(0, 2);
                if (random == 0)
                {
                    currMachine.ChangeState<BirdIdleState>();
                }
                else if (random == 1)
                {
                    if (this.GetModel<IBirdModel>().FlyPositions.Count == 0)
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
    }
}
