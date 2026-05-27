using System;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;


namespace BirdGame
{
    /// <summary>
    /// 动画参数哈希值缓存类，避免每次都使用字符串比较
    /// </summary>
    public static class AnimatorHashes
    {
        // 动画状态名称哈希值
        public static readonly int StrokeState = Animator.StringToHash("Stroke");
        
        // 动画参数哈希值
        public static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        public static readonly int Eat = Animator.StringToHash("Eat");
        public static readonly int Fly = Animator.StringToHash("Fly");
        public static readonly int IsTakeOff = Animator.StringToHash("IsTakeOff");
        
        // 触发器哈希值
        public static readonly int StrokeTrigger = Animator.StringToHash("Stroke");
        public static readonly int Licking = Animator.StringToHash("Licking");
        
        // 动画片段名称哈希值
        public static readonly int TakeOffAnim = Animator.StringToHash("TakeOff");
        public static readonly int FlyInAirAnim = Animator.StringToHash("FlyInAir");
        public static readonly int FlyFromBranchAnim = Animator.StringToHash("FlyFromBranch");
    }

    /// <summary>
    /// Memory optimization: static cached Color values to avoid repeated allocations
    /// </summary>
    public static class CachedColors
    {
        public static readonly Color TransparentGreen = new Color(0, 1, 0, 0);
    }

    /// Controls bird behavior including movement, growth stages, and interactions
    public class Brid : ViewControllerBase
    {
        // [Header("Activity Area Settings")]
        // public WalkableArea walkableArea;    // Area for limiting activity range
        public int birdIndex;

        public int walkArea = 3;
        private int previousClickCount = 0;
        private int previousRightClickCount = 0;
        [Header("Baby Bird Size")] public float BabyBirdSize = 0.01f;

        [Header("Adult Bird Size")] public float AdultBirdSize = 0.12f;
        [Header("Bird Eat DistanceX")] public float BirdEatDistance = 0.35f;
        [Header("Bird Eat DistanceY")] public float BirdEatDistanceY = 0f;

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
        public List<DepthMask> maskList = new List<DepthMask>();

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
        public float lastEatTime = 0; // 最后进食时间
        public float eatWaitDuration = 0; // 进食后等待时间（随机0-3秒）
        public float idleLockDuration = 1f; // 抚摸后锁定idle状态的时间（秒）
        private float continuousPetStartTime = 0; // 连续抚摸开始时间
        //public bool shouldFollowMouse = false; // 是否应该跟随鼠标
        public NavMeshAgent agent;
        public Action onNearOtherBird;

        public Vector3 originalScale;
        public float lastPerspectiveScale = 1f;
        
        // 飞行状态碰撞体调整相关 — CPU优化：公开collider供外部直接使用，避免GetComponent
        public Collider2D birdCollider { get; private set; }
        private Vector2 originalColliderSize;
        public bool isFlying = false;
        
        // 高亮效果相关
        private Color originalOutlineColor;
        private bool hasOriginalColor = false;
        private float originalThickness;
        private bool hasOriginalThickness = false;
        private Material materialNormal;

        // Memory optimization: use MaterialPropertyBlock to avoid material instancing
        private static MaterialPropertyBlock _sharedMPB;
        private static readonly int _colorPropertyId = Shader.PropertyToID("_Color");
    
        
        // 静态变量跟踪当前高亮的鸟，避免每次查找所有鸟
        private static Brid currentHighlightedBird = null;

        [ReadOnly]
        public float animScale = 1f;
        private float lastAppliedScale = -1f; // CPU优化：缓存上次应用的scale，避免每帧设置
        private float lastMoveSpeed = -1f; // CPU优化：缓存上次animator speed，仅变化时SetFloat

        public bool isDesktopBird;
        private GameObject heart;
        private int weatherIndex = 0;

        void Start()
        {
            heart.localPosition = new Vector3(Vector3(-0.032, 0.96, 0));
            this.RegisterEvent<SwitchWeatherEvent>(evt =>
            {
                if(weatherIndex == evt.index)
                    return;
                weatherIndex = evt.index;
                var ani = DOTween.Sequence();
                ani.Append(sr.DOColor(Color.black, 0.5f));
                ani.Append(sr.DOColor(Color.white, 0.5f));
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            
            // Memory optimization: use static cached Color to avoid allocation per bird
            lineRenderer.startColor = CachedColors.TransparentGreen;
            lineRenderer.endColor = CachedColors.TransparentGreen;
            // Initialize walkable area and basic components
            transform.localRotation = Quaternion.identity;
            agent = GetComponent<NavMeshAgent>();
            agent.autoRepath = true;
            agent.speed = moveSpeed;
            agent.updateUpAxis = false;
            agent.updateRotation = false;
            // 关键参数设置
            agent.acceleration = 1000f; // 极大加速度（瞬间达到最大速度）
            agent.autoBraking = false; // 禁用自动减速
            agent.stoppingDistance = 0f; // 零停止距离
            
            agent.areaMask = agent.areaMask = (1 << NavMesh.GetAreaFromName("Walkable")) | 
                                              (1 << NavMesh.GetAreaFromName("Ground")) |
                                              (1 << NavMesh.GetAreaFromName("LeftArea")) |
                                              (1 << NavMesh.GetAreaFromName("Rock1")) |
                                              (1 << NavMesh.GetAreaFromName("Rock2")) |
                                              (1 << NavMesh.GetAreaFromName("RightArea")) |
                                              (1 << NavMesh.GetAreaFromName("Ground1"));
            
            originalPos = transform.position;
            anim = GetComponentInChildren<Animator>();
            anim.speed = 0.8f;
            sr = GetComponentInChildren<SpriteRenderer>();
            // Memory optimization: use MaterialPropertyBlock to set color without creating material instances
            if (_sharedMPB == null) _sharedMPB = new MaterialPropertyBlock();
            _sharedMPB.SetColor(_colorPropertyId, this.GetModel<IBirdModel>().BirdColor.Value);
            sr.SetPropertyBlock(_sharedMPB);
            this.GetModel<IBirdModel>().BirdColor.Register(v =>
            {
                if (_sharedMPB == null) _sharedMPB = new MaterialPropertyBlock();
                _sharedMPB.SetColor(_colorPropertyId, v);
                sr.SetPropertyBlock(_sharedMPB);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            // 初始化碰撞体
            birdCollider = GetComponent<Collider2D>();
            if (birdCollider != null)
            {
                originalColliderSize = birdCollider.bounds.size;
            }

            materialNormal = sr.sharedMaterial;
            if (this.GetModel<IBirdModel>().MaterialHighlight == null)
            {
                this.GetSystem<IAssetSystem>().LoadAssetAsync<Material>("MaterialHighlight",
                    v =>
                    {
                        if (v != null)
                        {
                            this.GetModel<IBirdModel>().MaterialHighlight = v;
                        }
                        else
                        {
                            this.GetModel<IBirdModel>().MaterialHighlight = materialNormal;
                        }
                    });
            }

            // 保存原始轮廓颜色
            SaveOriginalOutlineColor();
            
            // 监听信息栏关闭事件
            this.RegisterEvent<InfoPopupClosedEvent>(OnInfoPopupClosed).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            // 动态获取吃饭动画的时长
            if (anim != null && anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.animationClips.Length > 3)
            {
                eatFoodTime = anim.runtimeAnimatorController.animationClips[3].length;
            }
            else
            {
                // 无法获取吃饭动画时长，使用默认值1秒
            }

            eatFoodTime = anim.runtimeAnimatorController.animationClips[3].length;

            // eatFoodTime = anim.runtimeAnimatorController.animationClips[3].length;

            // Setup state machine for bird behavior
            _stateMachine = new StateMachine(gameObject);
            _stateMachine.AddState(new BirdIdleState(_stateMachine));
            _stateMachine.AddState(new BirdRunState(_stateMachine));
            _stateMachine.AddState(new BirdFlyState(_stateMachine));
            _stateMachine.AddState(new BirdEatState(_stateMachine));
            _stateMachine.AddState(new BirdFlyWaitState(_stateMachine));
            _stateMachine.AddState(new BirdFlyDownState(_stateMachine));
            _stateMachine.AddState(new BirdFlyHorizontalState(_stateMachine));
            _stateMachine.AddState(new BirdHatchingEggState(_stateMachine));
            startTimer = Time.time;

            animScale = isSmall ? BabyBirdSize : AdultBirdSize;
            //transform.localScale = Vector3.one * BabyBirdSize;
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
                if (continuousPetDuration >= 1f)
                {
                    // 设置跟随鼠标标志
                    //shouldFollowMouse = true;
                    // Debug.Log("连续抚摸超过1秒，准备跟随鼠标！");
                }
                
                // 重置连续抚摸计时
                continuousPetStartTime = 0;
            }
            
            // 重置被抚摸状态
            isBeingPetted = false;
        }

        void Update()
        {
            if (!isDesktopBird && maskList.Count == 0)
            {
                if (isEnter)
                {
                    if (Input.GetMouseButtonDown(1) || SimpleMouseForwarder.rightClickCount > previousRightClickCount)
                    {
                        previousRightClickCount = SimpleMouseForwarder.rightClickCount;
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
                    }

                    if (Input.GetMouseButtonDown(0) || SimpleMouseForwarder.clickCount > previousClickCount)
                    {
                        previousClickCount = SimpleMouseForwarder.clickCount;
                        if (_stateMachine.CurrentState == typeof(BirdIdleState) ||
                            _stateMachine.CurrentState == typeof(BirdRunState) ||
                            _stateMachine.CurrentState == typeof(BirdEatState))
                        {
                            // 检查是否达到点击间隔时间
                            if (Time.time - lastClickTime >= clickInterval)
                            {
                                lastClickTime = Time.time; // 更新最后点击时间
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

                                    anim.SetBool(AnimatorHashes.Eat, false);
                                    _stateMachine.ChangeState<BirdIdleState>();
                                }

                                //this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Stroke);
                                //this.GetSystem<IAudioSystem>().PlayBirdEffect(index);
                                anim.SetTrigger(AnimatorHashes.StrokeTrigger);
                                var bodyType = this.GetModel<IConfigModel>().BirdConfig.GetBirdBodyType(index, mapIndex);
                                this.GetSystem<IAudioSystem>().RandomPlayPetting(bodyType);
                                // 使用对象池获取心形特效
                                if (heart != null && heart.activeSelf)
                                {
                                    this.GetSystem<IObjectPoolSystem>().Recycle("Heart", heart);
                                    heart = null;
                                }

                                this.GetSystem<IObjectPoolSystem>().Get("Heart", heartPos, obj => { heart = obj; });
                                if (petTime > 0.5)
                                {
                                    this.GetModel<IAccountModel>().Coins.Value += birdConf.clickEarningForFiveTimes;
                                }
                            }
                        }
                    }
                }
            }

            _stateMachine.OnUpdate();

            // 检查是否应该跟随鼠标（在任何状态下都可以触发）
            // if (shouldFollowMouse)
            // {
            //     Debug.Log("Brid: 检测到跟随鼠标标志，强制切换到RunState");
            //     _stateMachine.ChangeState<BirdRunState>();
            // }
            
            // CPU优化：仅在animScale实际变化时设置localScale
            if (animScale != lastAppliedScale)
            {
                lastAppliedScale = animScale;
                transform.localScale = new Vector3(animScale, animScale, 1f);
            }
            // CPU优化：仅在移动状态变化时调用SetFloat，避免每帧调用Animator
            if (agent != null && agent.enabled)
            {
                float newMoveSpeed = agent.velocity.sqrMagnitude > 0.0001f ? 1f : 0f;
                if (newMoveSpeed != lastMoveSpeed)
                {
                    lastMoveSpeed = newMoveSpeed;
                    anim.SetFloat(AnimatorHashes.MoveSpeed, newMoveSpeed);
                }
            }

            //检查是否在WalkableArea中并做透视缩放
            // var walkableArea = NavigationManager.Instance.GetWalkableArea(walkArea);
            // if (walkableArea != null && walkArea == 8)
            // {
            //     Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
            //     if (walkableArea.IsPointInside(currentPos))
            //     {
            //         // 获取WalkableArea的Y轴范围
            //         var bounds = walkableArea.GetComponent<PolygonCollider2D>().bounds;
            //         float minY = bounds.min.y;
            //         float maxY = bounds.max.y;

            //         // 归一化当前Y位置（0=最上，1=最下）
            //         float t = Mathf.InverseLerp(maxY, minY, transform.position.y);

            //         // 计算scale（最上0.8，中间递增，最下1.1）
            //         float scaleFactor = Mathf.Lerp(0.4f, 1.2f, t);

            //         if (isSmall)
            //         {
            //             transform.localScale = Vector3.one * BabyBirdSize * scaleFactor;
            //         }
            //         else
            //         {
            //             transform.localScale = Vector3.one * AdultBirdSize * scaleFactor * animScale;
            //         }
            //     }
            // }

            // if (!isDesktopBird)
            // {
            // 每分钟成长（金币收益已改为全局所有地图结算，见 BirdSystem.AddAllMapsIncome）
            if (Time.time - startTimer >= 60)
            {
                startTimer = Time.time;
                AutoExp();
            }
            // }

            if (SimpleMouseForwarder.clickCount > previousClickCount)
            {
                previousClickCount = SimpleMouseForwarder.clickCount;
            }

            if (SimpleMouseForwarder.rightClickCount > previousRightClickCount)
            {
                previousRightClickCount = SimpleMouseForwarder.rightClickCount;
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
                    //transform.DOScale(AdultBirdSize, 0.2f);
                    isSmall = false;
                    this.GetSystem<IAchievementSystem>().OnBirdGrewToAdult();
                    // 立即同步状态到存档数据，确保 SaleBirdViewController 显示正确的状态
                    this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
                }
            }
        }

        /// Generates income based on bird's size
        private void AddCoins()
        {
            var birdData = this.GetModel<IBirdModel>().BirdList[birdIndex];
            
            if (!isSmall)
            {
                // 使用实例化时计算的个体化收入
                this.GetModel<IAccountModel>().Coins.Value += birdData.individualEarningBig;
            }
            else
            {
                // 使用实例化时计算的个体化收入
                this.GetModel<IAccountModel>().Coins.Value += birdData.individualEarningSmall;
            }
        }

        /// <summary>
        /// 调整碰撞体大小以适应飞行状态
        /// </summary>
        /// <param name="isFlying">是否在飞行</param>
        public void AdjustColliderForFlying(bool isFlying)
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
        /// 保存原始轮廓颜色和宽度
        /// </summary>
        private void SaveOriginalOutlineColor()
        {
            if (sr == null || sr.sharedMaterial == null) return;

            sr.sharedMaterial = materialNormal;
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

            sr.sharedMaterial = this.GetModel<IBirdModel>().MaterialHighlight;

            // Material currentMaterial = sr.material;
            // if (currentMaterial == null)
            // {
            //     Debug.LogError("鸟的材质为空！");
            //     return;
            // }

            // // 直接使用 _SolidOutline 属性（对应 Inspector 中的 "Outline Color Base"）
            // if (currentMaterial.HasProperty("_SolidOutline"))
            // {
            //     currentMaterial.SetColor("_SolidOutline", Color.white);
            // }
            // else
            // {
            //     Debug.LogWarning($"材质 {currentMaterial.name} 没有找到 _SolidOutline 属性！");
            // }
            
            // // 设置 Width 属性（对应 Inspector 中的 "Width (Max recommended 100)"）
            // if (currentMaterial.HasProperty("_Thickness"))
            // {
            //     currentMaterial.SetFloat("_Thickness", 3.8f);
            // }
            
            // // 更新静态变量，记录当前高亮的鸟
            // currentHighlightedBird = this;
        }
        
        /// <summary>
        /// 恢复原始轮廓颜色和宽度
        /// </summary>
        private void RestoreOriginalOutlineColor()
        {
            if (sr == null || sr.sharedMaterial == null) return;

            sr.sharedMaterial = materialNormal;
            // Material currentMaterial = sr.material;
            
            // // 使用 _SolidOutline 属性（对应 Inspector 中的 "Outline Color Base"）
            // if (currentMaterial.HasProperty("_SolidOutline") && hasOriginalColor)
            // {
            //     currentMaterial.SetColor("_SolidOutline", originalOutlineColor);
            // }
            
            // // 恢复原始 Width 属性（对应 Inspector 中的 "Width (Max recommended 100)"）
            // if (currentMaterial.HasProperty("_Thickness") && hasOriginalThickness)
            // {
            //     currentMaterial.SetFloat("_Thickness", originalThickness);
            // }
        }

        /// <summary>
        /// 恢复所有鸟的材质颜色（优化版：只恢复上一次高亮的鸟）
        /// </summary>
        private void RestoreAllBirdsOutlineColor()
        {
            // 优化：只恢复上一次高亮的鸟，避免遍历所有鸟
            if (currentHighlightedBird != null && currentHighlightedBird != this)
            {
                currentHighlightedBird.RestoreOriginalOutlineColor();
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
        
        private void OnDestroy()
        {
            // 清除静态引用，避免悬空引用
            if (currentHighlightedBird == this)
            {
                currentHighlightedBird = null;
            }
        }
    }
}