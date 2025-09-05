using System;
using BirdGame;
using DG.Tweening;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;


namespace BirdGame
{

    /// Controls bird behavior including movement, growth stages, and interactions
    public class Brid : ViewControllerBase
    {
        // [Header("Activity Area Settings")]
        // public WalkableArea walkableArea;    // Area for limiting activity range
        public int birdIndex;

        public int walkArea = 3;

        [Header("Baby Bird Size")] public float BabyBirdSize = 0.01f;

        [Header("Adult Bird Size")] public float AdultBirdSize = 0.12f;
        [Header("Bird Ead Distance")] public float BirdEatDistance = 0.35f;

        [Header("Background Bird Size")] public float BackgroundBirdSize;
        public Transform nestTrans;
        public Vector3 originalPos;
        public Vector3 nestPos;
        public float radiusX = 2f;
        public float radiusY = 0.8f;
        public float moveSpeed = 1;
        public float flySpeed = 3;
        public float waitTime = 3;
        public bool isSmall = true;
        public Animator anim;
        public SpriteRenderer sr;
        public int flyIndex = -1;
        public Nest nest;
        public bool isInNest;
        float eatTimer;
        public GameObject heartPre;
        public int level = 1;
        public string title = "Bird";
        public string desc = "It's a bird";
        public Transform heartPos;
        public Vector3 flyInAirStartPosition; // 横向飞行的起始位置
        public LineRenderer lineRenderer;

        [Header("Click count for following mouse movement")]
        public int clickCount = 5;

        [Header("好感度")] public int totalFavorability = 10;
        public BindableProperty<int> currentFavorability = new BindableProperty<int>(0);
        
        public float distance;
        public BindableProperty<float> currentExp = new BindableProperty<float>();
        public float eatFoodTime = 1;

        bool isEnter;
        public Food currFood;

        private StateMachine _stateMachine;
        private float startTimer = 0;
        private float petTime = 0;
        private float lastClickTime = 0; // 添加最后点击时间记录
        private float clickInterval = 0.2f; // 点击间隔时间
        public bool isBeingPetted = false; // 是否正在被抚摸
        public float lastPetTime = 0; // 最后抚摸时间
        public float idleLockDuration = 1f; // 抚摸后锁定idle状态的时间（秒）
        private float continuousPetStartTime = 0; // 连续抚摸开始时间
        public bool shouldFollowMouse = false; // 是否应该跟随鼠标
        public NavMeshAgent agent;
        public Action onNearOtherBird;

        public Vector3 originalScale;
        public float lastPerspectiveScale = 1f;
        
        // 飞行状态碰撞体调整相关
        private Collider2D birdCollider;
        private Vector2 originalColliderSize;
        private bool isFlying = false;
        
        // 高亮效果相关
        private Color originalOutlineColor;
        private bool hasOriginalColor = false;

        [ReadOnly]
        public float animScale = 1f;

        void Start()
        {
            lineRenderer.startColor = new Color(0, 1, 0, 0); // 绿色，透明度为0
            lineRenderer.endColor = new Color(0, 1, 0, 0); // 绿色，透明度为0
            // Initialize walkable area and basic components
            transform.localRotation = Quaternion.identity;
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.updateUpAxis = false;
            agent.updateRotation = false;
            // 关键参数设置
            agent.acceleration = 1000f; // 极大加速度（瞬间达到最大速度）
            agent.autoBraking = false; // 禁用自动减速
            agent.stoppingDistance = 0f; // 零停止距离
            
            originalPos = transform.position;
            anim = GetComponentInChildren<Animator>();
            sr = GetComponentInChildren<SpriteRenderer>();
            
            // 初始化碰撞体
            birdCollider = GetComponent<Collider2D>();
            if (birdCollider != null)
            {
                originalColliderSize = birdCollider.bounds.size;
            }
            
            // 保存原始轮廓颜色
            SaveOriginalOutlineColor();
            
            // 监听信息栏关闭事件
            this.RegisterEvent<InfoPopupClosedEvent>(OnInfoPopupClosed).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            // 动态获取吃饭动画的时长
            if (anim != null && anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.animationClips.Length > 3)
            {
                eatFoodTime = anim.runtimeAnimatorController.animationClips[3].length;
                Debug.Log($"动态设置吃饭动画时长: {eatFoodTime}秒");
            }
            else
            {
                Debug.LogWarning("无法获取吃饭动画时长，使用默认值1秒");
            }

            eatFoodTime = anim.runtimeAnimatorController.animationClips[3].length;

            eatFoodTime = anim.runtimeAnimatorController.animationClips[3].length;

            // Setup state machine for bird behavior
            _stateMachine = new StateMachine(gameObject);
            _stateMachine.AddState(new BirdIdleState(_stateMachine));
            _stateMachine.AddState(new BirdRunState(_stateMachine));
            _stateMachine.AddState(new BirdFlyState(_stateMachine));
            _stateMachine.AddState(new BirdEatState(_stateMachine));
            _stateMachine.AddState(new BirdFlyWaitState(_stateMachine));
            _stateMachine.AddState(new BirdFlyDownState(_stateMachine));
            _stateMachine.AddState(new BirdFlyHorizontalState(_stateMachine));
            startTimer = Time.time;

            transform.localScale = Vector3.one * BabyBirdSize;
        }

        /// Handles bird interaction when clicked
        private void OnMouseDown()
        {
            // if (!isSmall)
            // {
            //     level++;
            //     GameObject go = Instantiate(heartPre);
            //     go.transform.SetParent(transform);
            //     go.transform.position = heartPos.position;
            //     go.transform.localScale = Vector3.one * BabyBirdSize;
            //     
            // }
        }

        public void OnMouseEnter()
        {
            isEnter = true;
        }

        public void OnMouseExit()
        {
            isEnter = false;
            
            // 检查是否连续抚摸超过3秒
            if (continuousPetStartTime > 0)
            {
                float continuousPetDuration = Time.time - continuousPetStartTime;
                if (continuousPetDuration >= 0.3f)
                {
                    // 设置跟随鼠标标志
                    shouldFollowMouse = true;
                    Debug.Log("连续抚摸超过1秒，准备跟随鼠标！");
                }
                
                // 重置连续抚摸计时
                continuousPetStartTime = 0;
            }
            
            // 重置被抚摸状态
            isBeingPetted = false;
        }

        void Update()
        {
            // 检测飞行状态并调整碰撞体
            CheckFlyingStateAndAdjustCollider();
            
            
            if (isEnter)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    if (!isSmall)
                    {
                        title = "Adult bird";
                        desc = "It's an adult bird";
                    }

                    this.GetModel<IGameModel>().CurrentSelectedBirdIndex = birdIndex;
                    this.GetSystem<IUISystem>().ShowPopup(UIPopup.InfoPopup);
                    
                    // 先恢复所有鸟的材质颜色，然后设置当前鸟为白色轮廓
                    RestoreAllBirdsOutlineColor();
                    SetBirdOutlineToWhite();
                    // if (isSmall)
                    // {
                    //     // UIManager.Instance.ShowInfoPanel(gameObject, smallPrice, title, desc, 0,
                    //     //     eatFoodCount * 1f / eatCountForBig, currentFavorability * 1f / totalFavorability, false);
                    //     //this.SendCommand(new ShowBirdInfoCommand(smallPrice, title, desc, 0, eatFoodCount.Value * 1f / eatCountForBig, currentFavorability * 1f / totalFavorability, false));
                    // }
                    // else
                    // {
                    //     //UIManager.Instance.infoPanel.IntimacyFill.gameObject.SetActive(true);
                    //     // UIManager.Instance.ShowInfoPanel(gameObject, bigPrice, title, desc, incomeForBig,
                    //     //     1, currentFavorability * 1f / totalFavorability, true);
                    //     //this.SendCommand(new ShowBirdInfoCommand(bigPrice, title, desc, incomeForBig,
                    //         //1, currentFavorability * 1f / totalFavorability, true));
                    // }
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (_stateMachine.CurrentState == typeof(BirdIdleState) || _stateMachine.CurrentState == typeof(BirdRunState)||_stateMachine.CurrentState == typeof(BirdEatState))
                    {
                        // 检查是否达到点击间隔时间
                        if (Time.time - lastClickTime >= clickInterval)
                        {
                            lastClickTime = Time.time; // 更新最后点击时间
                            Debug.Log("Feather!");
                            petTime += 0.1f;
                            int index = this.GetModel<IBirdModel>().BirdList[birdIndex].birdType;
                            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
                            var birdConf = this.GetModel<IConfigModel>().BirdConfig.GetBird(index, mapIndex);
                            this.GetModel<IAccountModel>().Coins.Value += birdConf.clickEarning;
                            if (currentFavorability.Value < totalFavorability && !isSmall)
                            {
                                currentFavorability.Value++;
                            }

                            // 设置被抚摸标志和记录抚摸时间
                            isBeingPetted = true;
                            lastPetTime = Time.time;
                            
                            // 开始或继续连续抚摸计时
                            if (continuousPetStartTime == 0)
                            {
                                continuousPetStartTime = Time.time;
                            }
                            
                            // 如果当前是跑步状态，切换到idle状态
                            if (_stateMachine.CurrentState == typeof(BirdRunState))
                            {
                                agent.isStopped = true;
                                agent.velocity = Vector3.zero;
                                _stateMachine.ChangeState<BirdIdleState>();
                            }
                            // 如果当前是吃东西状态，直接处理抚摸
                            else if (_stateMachine.CurrentState == typeof(BirdEatState))
                            {
                                agent.isStopped = true;
                                agent.velocity = Vector3.zero;
                                if (currFood != null)
                                {
                                    currFood.UntargetFood();
                                    currFood = null;
                                }
                                anim.SetBool("Eat", false);
                                _stateMachine.ChangeState<BirdIdleState>();
                            }

                            this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Stroke);
                            this.GetSystem<IAudioSystem>().PlayBirdEffect(index);
                            anim.SetTrigger("Stroke");
                            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Heart", obj =>
                            {
                                GameObject.Instantiate(obj, heartPos);
                            });
                            if (petTime > 0.5)
                            {
                                this.GetModel<IAccountModel>().Coins.Value += birdConf.clickEarningForFiveTimes;
                            }
                        }
                    }
                }
            }

            _stateMachine.OnUpdate();

            // 检查是否应该跟随鼠标（在任何状态下都可以触发）
            if (shouldFollowMouse)
            {
                Debug.Log("Brid: 检测到跟随鼠标标志，强制切换到RunState");
                _stateMachine.ChangeState<BirdRunState>();
            }

            // 统一处理走路动画 - 只要在移动就播放走路动画
            if (agent != null && agent.enabled)
            {
                if (agent.velocity.magnitude > 0.01f)
                {
                    anim.SetFloat("MoveSpeed", 1f);
                }
                else
                {
                    anim.SetFloat("MoveSpeed", 0f);
                }
            }

            //检查是否在WalkableArea中并做透视缩放
            var walkableArea = NavigationManager.Instance.GetWalkableArea(walkArea);
            if (walkableArea != null && walkArea == 3)
            {
                Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
                if (walkableArea.IsPointInside(currentPos))
                {
                    // 获取WalkableArea的Y轴范围
                    var bounds = walkableArea.GetComponent<PolygonCollider2D>().bounds;
                    float minY = bounds.min.y;
                    float maxY = bounds.max.y;

                    // 归一化当前Y位置（0=最上，1=最下）
                    float t = Mathf.InverseLerp(maxY, minY, transform.position.y);

                    // 计算scale（最上0.8，中间递增，最下1.1）
                    float scaleFactor = Mathf.Lerp(0.8f, 1.2f, t);

                    if (isSmall)
                    {
                        transform.localScale = Vector3.one * BabyBirdSize * scaleFactor;
                    }
                    else
                    {
                        transform.localScale = Vector3.one * AdultBirdSize * scaleFactor * animScale;
                    }
                }
            }

            // Generate income every minute
            if (Time.time - startTimer >= 60)
            {
                startTimer = Time.time;
                AddCoins();
                AutoExp();
            }
        }

        /// <summary>
        /// 每分钟成长值
        /// </summary>
        private void AutoExp()
        {
            if (isSmall)
            {
                int index = this.GetModel<IBirdModel>().BirdList[birdIndex].birdType;
                int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
                var conf = this.GetModel<IConfigModel>().BirdConfig.GetBird(index, mapIndex);
                currentExp.Value += conf.autoExp;
                if (currentExp.Value >= conf.totalExp)
                {
                    transform.DOScale(AdultBirdSize, 0.2f);
                    isSmall = false;
                }
            }
        }

        /// Generates income based on bird's size
        private void AddCoins()
        {
            Debug.Log("Adding coins");
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            if (!isSmall)
            {
                int index = this.GetModel<IBirdModel>().BirdList[birdIndex].birdType;
                int income = this.GetModel<IConfigModel>().BirdConfig.GetBird(index, mapIndex).eraningForBig;
                this.GetModel<IAccountModel>().Coins.Value += income;
            }
            else
            {
                int index = this.GetModel<IBirdModel>().BirdList[birdIndex].birdType;
                int income = this.GetModel<IConfigModel>().BirdConfig.GetBird(index, mapIndex).eraningForSmall;
                this.GetModel<IAccountModel>().Coins.Value += income;
            }
        }

        /// <summary>
        /// 检测飞行状态并调整碰撞体大小，让飞行中的鸟更容易被点击
        /// </summary>
        private void CheckFlyingStateAndAdjustCollider()
        {
            if (birdCollider == null) return;
            
            // 检测当前是否在飞行状态
            bool currentlyFlying = _stateMachine.CurrentState == typeof(BirdFlyState) || 
                                 _stateMachine.CurrentState == typeof(BirdFlyHorizontalState) || 
                                 _stateMachine.CurrentState == typeof(BirdFlyDownState) ||
                                 _stateMachine.CurrentState == typeof(BirdFlyWaitState);
            
            // 如果飞行状态发生变化
            if (currentlyFlying != isFlying)
            {
                isFlying = currentlyFlying;
                
                if (isFlying)
                {
                    // 进入飞行状态，增大碰撞体
                    AdjustColliderForFlying(true);
                }
                else
                {
                    // 退出飞行状态，恢复原始碰撞体
                    AdjustColliderForFlying(false);
                }
            }
        }
        
        /// <summary>
        /// 调整碰撞体大小以适应飞行状态
        /// </summary>
        /// <param name="isFlying">是否在飞行</param>
        private void AdjustColliderForFlying(bool isFlying)
        {
            if (birdCollider == null || !(birdCollider is BoxCollider2D)) return;
            
            BoxCollider2D boxCollider = birdCollider as BoxCollider2D;
            
            if (isFlying)
            {
                // 飞行时增大碰撞体，让点击更容易
                // 根据当前scale计算合适的碰撞体大小
                float currentScale = transform.localScale.x;
                float scaleMultiplier = Mathf.Max(2.5f, 1.0f / currentScale); // 至少2倍，或者根据scale反向调整
                
                boxCollider.size = originalColliderSize * scaleMultiplier;
            }
            else
            {
                // 恢复原始碰撞体大小
                boxCollider.size = originalColliderSize;
            }
        }

        /// <summary>
        /// 保存原始轮廓颜色
        /// </summary>
        private void SaveOriginalOutlineColor()
        {
            if (sr == null || sr.material == null) return;
            
            Material currentMaterial = sr.material;
            
            // 尝试不同的可能属性名称
            string[] possibleNames = { "_SolidOutline", "_GradientOutline1", "_GradientOutline2" };
            
            foreach (string propName in possibleNames)
            {
                if (currentMaterial.HasProperty(propName))
                {
                    originalOutlineColor = currentMaterial.GetColor(propName);
                    hasOriginalColor = true;
                    Debug.Log($"保存原始轮廓颜色: {propName} = {originalOutlineColor}");
                    break;
                }
            }
        }

        /// <summary>
        /// 设置鸟的轮廓为白色
        /// </summary>
        private void SetBirdOutlineToWhite()
        {
            if (sr == null)
            {
                Debug.LogError("SpriteRenderer为空！");
                return;
            }

            Material currentMaterial = sr.material;
            if (currentMaterial == null)
            {
                Debug.LogError("鸟的材质为空！");
                return;
            }

            Debug.Log("=== 开始设置鸟的轮廓为白色 ===");
            Debug.Log($"材质名称: {currentMaterial.name}");
            Debug.Log($"Shader名称: {currentMaterial.shader.name}");
            
            // 尝试不同的可能属性名称
            string[] possibleNames = { "_SolidOutline", "_GradientOutline1", "_GradientOutline2" };
            bool foundProperty = false;
            
            foreach (string propName in possibleNames)
            {
                if (currentMaterial.HasProperty(propName))
                {
                    Color currentColor = currentMaterial.GetColor(propName);
                    Debug.Log($"找到属性 {propName}，当前颜色: {currentColor}");
                    
                    currentMaterial.SetColor(propName, Color.white);
                    Debug.Log($"成功设置 {propName} 为白色");
                    foundProperty = true;
                }
            }
            
            if (!foundProperty)
            {
                Debug.LogWarning("没有找到任何轮廓颜色属性！");
                // 打印所有材质属性用于调试
                Shader shader = currentMaterial.shader;
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    string propName = shader.GetPropertyName(i);
                    Debug.Log($"可用属性: {propName}");
                }
            }
            
            
        }
        
        /// <summary>
        /// 恢复原始轮廓颜色
        /// </summary>
        private void RestoreOriginalOutlineColor()
        {
            if (sr == null || sr.material == null || !hasOriginalColor) return;
            
            Material currentMaterial = sr.material;
            string[] possibleNames = { "_SolidOutline", "_GradientOutline1", "_GradientOutline2" };
            
            foreach (string propName in possibleNames)
            {
                if (currentMaterial.HasProperty(propName))
                {
                    currentMaterial.SetColor(propName, originalOutlineColor);
                    Debug.Log($"恢复 {propName} 为原始颜色: {originalOutlineColor}");
                    break;
                }
            }
        }

        /// <summary>
        /// 恢复所有鸟的材质颜色
        /// </summary>
        private void RestoreAllBirdsOutlineColor()
        {
            GameObject[] birds = GameObject.FindGameObjectsWithTag("Bird");
            foreach (var bird in birds)
            {
                if (bird != null)
                {
                    Brid birdScript = bird.GetComponent<Brid>();
                    if (birdScript != null)
                    {
                        birdScript.RestoreOriginalOutlineColor();
                    }
                }
            }
        }

        /// <summary>
        /// 信息栏关闭时恢复材质颜色
        /// </summary>
        private void OnInfoPopupClosed(InfoPopupClosedEvent evt)
        {
            if (evt.popupType == UIPopup.InfoPopup)
            {
                RestoreOriginalOutlineColor();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Bird"))
            {
                onNearOtherBird?.Invoke();
            }
        }
    }
}