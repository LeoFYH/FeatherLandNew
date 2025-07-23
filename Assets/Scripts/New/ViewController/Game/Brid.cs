using System;
using BirdGame;
using QFramework;
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
        public int smallPrice = 20;
        public int bigPrice = 30;
        public int level = 1;
        public string title = "Bird";
        public string desc = "It's a bird";
        public Transform heartPos;
        public Vector3 flyInAirStartPosition; // 横向飞行的起始位置

        [Header("Click count for following mouse movement")]
        public int clickCount = 5;

        [Header("好感度")] public int totalFavorability = 10;
        public BindableProperty<int> currentFavorability = new BindableProperty<int>(0);

        [Header("Food count needed for large size")]
        public int eatCountForBig = 2;

        [Header("Income for large size")] public int incomeForBig;

        public float distance;
        public BindableProperty<int> eatFoodCount = new BindableProperty<int>();
        public float eatFoodTime = 1;

        bool isEnter;

        /// <summary>
        /// 是否已经吃过米粒
        /// </summary>
        public bool isAte = false;

        public Food currFood;

        private StateMachine _stateMachine;
        private float startTimer = 0;
        private float petTime = 0;
        private float lastClickTime = 0; // 添加最后点击时间记录
        private float clickInterval = 0.2f; // 点击间隔时间
        public NavMeshAgent agent;
        public Action onNearOtherBird;

        public Vector3 originalScale;
        public float lastPerspectiveScale = 1f;

        void Start()
        {
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

            // if (walkableArea == null)
            // {
            //     walkableArea = FindObjectOfType<WalkableArea>();
            //     if (walkableArea == null)
            //     {
            //         Debug.LogWarning("No WalkableArea found, bird will not be restricted!");
            //     }
            // }
            originalPos = transform.position;
            anim = GetComponentInChildren<Animator>();
            sr = GetComponentInChildren<SpriteRenderer>();

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

        private void OnMouseEnter()
        {
            isEnter = true;
        }

        private void OnMouseExit()
        {
            isEnter = false;
        }

        void Update()
        {
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
                    if (_stateMachine.CurrentState == typeof(BirdIdleState))
                    {
                        // 检查是否达到点击间隔时间
                        if (Time.time - lastClickTime >= clickInterval)
                        {
                            lastClickTime = Time.time; // 更新最后点击时间
                            Debug.Log("Feather!");
                            // GameObject go = Instantiate(heartPre); 
                            // Vector2 newPosition = new Vector2(transform.position.x - 0.2f, transform.position.y + 0.2f);
                            // go.transform.position = newPosition;
                            petTime += 0.1f;
                            //GameManager.Instance.coin += 1;
                            this.GetModel<IAccountModel>().Coins.Value += 1;
                            if (currentFavorability.Value < totalFavorability && !isSmall)
                            {
                                currentFavorability.Value++;
                            }

                            this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Stroke);
                            anim.SetTrigger("Stroke");
                            this.GetSystem<ICursorSystem>().Stroke();
                            if (petTime > 0.5)
                            {
                                this.GetModel<IAccountModel>().Coins.Value += 3;
                            }
                        }
                    }
                }
            }

            _stateMachine.OnUpdate();

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
                        transform.localScale = Vector3.one * AdultBirdSize * scaleFactor;
                    }
                }
            }

            // Generate income every minute
            if (Time.time - startTimer >= 60)
            {
                startTimer = Time.time;
                AddCoins();
            }
        }

        /// Generates income based on bird's size
        private void AddCoins()
        {
            Debug.Log("Adding coins");
            if (!isSmall)
            {
                this.GetModel<IAccountModel>().Coins.Value += incomeForBig;
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