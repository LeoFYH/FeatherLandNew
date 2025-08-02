using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BirdGame
{
    public interface IGameSystem : ISystem
    {
        Vector3 FoodDropOffset { get; }

        void CreateNum(string s, Vector3 pos);
        void CreateFood();
        void ReduceFood(Food food);
        void RecycleFood(Food food);
        bool TryGetUntargetedFood(Vector3 position, out Food food);
        bool IsCoverGround();
        bool IsCoverBird();
        bool IsCoverUI();
        void CreateDecoration(int decorationId);
        void PlaceDecoration();
    }

    public class GameSystem : AbstractSystem, IGameSystem
    {
        private GameObject foodPrefab;
        private GameObject numPrefab;
        private IBirdModel birdModel;
        private GameObject currentPlacingDecoration; // 当前正在放置的装饰品
        private int currentPlacingDecorationId; // 当前正在放置的装饰品ID
        
        [Header("食物位置偏移")]
        [Tooltip("食物落下位置相对于鼠标的偏移量")]
        private Vector3 foodDropOffset = new Vector3(0.3f, 0, 0f); // 基础偏移量
        
        [Header("随机偏移设置")]
        [Tooltip("X轴随机偏移范围")]
        private float randomXOffset = 0.3f; // X轴随机偏移范围
        [Tooltip("Y轴随机偏移范围")]
        private float randomYOffset = 0.3f; // Y轴随机偏移范围
        [Tooltip("食物最小间距")]
        private float minFoodDistance = 0.3f; // 食物之间的最小距离

        protected override void OnInit()
        {
            birdModel = this.GetModel<IBirdModel>();
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Food", obj =>
            {
                foodPrefab = obj;
            });
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Num", obj =>
            {
                numPrefab = obj;
            });
        }

        public Vector3 FoodDropOffset {
            get
            {
                return foodDropOffset;
            }
        }

        public void CreateNum(string s, Vector3 pos)
        {
            GameObject go = GameObject.Instantiate(numPrefab);
            go.transform.position = pos;
            go.GetComponent<NumPanel>().Init(s);
        }

        public void CreateFood()
        {
            if (IsCoverGround())
            {
                this.GetSystem<ICursorSystem>().Feed();
                this.GetSystem<IAudioSystem>().PlayEffect(EffectType.DropFood);
                Food food = GameObject.Instantiate(foodPrefab).GetComponent<Food>();
                food.isTargeted = false;

                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0; // 确保Z轴位置正确
                
                // 生成有间距的随机位置
                Vector3 finalPosition = GetValidFoodPosition(mouseWorldPos + foodDropOffset);
                
                // 设置位置
                food.transform.position = finalPosition;
            
                // 如果有Rigidbody2D，确保它不会移动
                Rigidbody2D rb = food.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Static;
                    // 或者
                    // rb.constraints = RigidbodyConstraints2D.FreezeAll;
                }
                
                birdModel.Foods.Add(food);

                if (birdModel.Foods.Count > 8)
                {
                    // 删除最早的食物
                    var foodToRemove = birdModel.Foods[0];
                    birdModel.Foods.RemoveAt(0);
                    GameObject.Destroy(foodToRemove.gameObject);
                }
            }
        }

        public void ReduceFood(Food food)
        {
            food.hp--;
            if (food.hp <= 0)
            {
                CreateNum("+1", food.transform.position);
                birdModel.Foods.Remove(food);
                GameObject.Destroy(food.gameObject);
            }
        }

        public void RecycleFood(Food food)
        {
            birdModel.Foods.Remove(food);
            GameObject.Destroy(food.gameObject);
        }

        public bool TryGetUntargetedFood(Vector3 position, out Food food)
        {
            food = null;
            float closestDistance = float.MaxValue;

            foreach (var temp in birdModel.Foods)
            {
                if (!temp.isTargeted && !temp.isDisabling)
                {
                    float distance = Vector3.Distance(position, temp.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        food = temp;
                    }
                }
            }

            return food != null;
        }

        public bool IsCoverGround()
        {
            if (NavigationManager.Instance == numPrefab)
                return false;
            
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; // 确保Z轴位置正确
        
            //检测是否点击到鸟，如果点击到鸟，则不生成食物
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, Vector2.zero);
            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider.CompareTag("Bird"))
                    {
                        return false;
                    }
                }
            }

            // 检查基础偏移位置是否在可导航区域
            if (NavigationManager.Instance.IsPointInNavMeshArea(3, mousePosition + (Vector2)foodDropOffset))
                return true;

            return false;
        }

        public bool IsCoverBird()
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0; // 确保Z轴位置正确
        
            //检测是否点击到鸟，如果点击到鸟，则不生成食物
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, Vector2.zero);
            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider.CompareTag("Bird") || hit.collider.CompareTag("Egg"))
                    {
                            return true;
                    }
                }
            }

            return false;
        }

        public bool IsCoverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
        
        public bool IsOnGround()
        {
            if (NavigationManager.Instance == null)
                return false;
                
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // 检查鼠标位置是否在可导航区域（地面）
            return NavigationManager.Instance.IsPointInNavMeshArea(3, mousePosition);
        }

        /// <summary>
        /// 获取有效的食物位置，确保与其他食物有足够间距
        /// </summary>
        /// <param name="basePosition">基础位置</param>
        /// <returns>有效的食物位置</returns>
        private Vector3 GetValidFoodPosition(Vector3 basePosition)
        {
            Vector3 finalPosition = basePosition;
            int maxAttempts = 100; // 增加最大尝试次数
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                // 生成随机偏移量（Y轴只向下偏移）
                float randomX = Random.Range(-randomXOffset, randomXOffset);
                float randomY = Random.Range(-randomYOffset, 0f); // 只生成负值，确保在下方
                Vector3 randomOffset = new Vector3(randomX, randomY, 0f);
                
                finalPosition = basePosition + randomOffset;

                // 严格检查是否与现有食物有足够间距
                bool isValidPosition = true;
                foreach (var existingFood in birdModel.Foods)
                {
                    if (existingFood != null && existingFood.gameObject != null)
                    {
                        float distance = Vector3.Distance(finalPosition, existingFood.transform.position);
                        if (distance < minFoodDistance)
                        {
                            isValidPosition = false;
                            break;
                        }
                    }
                }

                if (isValidPosition)
                {
                    return finalPosition;
                }

                attempts++;
            }

            // 如果尝试次数过多，强制偏移到更远的位置（Y轴只向下）
            Vector3 forcedOffset = new Vector3(
                Random.Range(-randomXOffset * 1.5f, randomXOffset * 1.5f),
                Random.Range(-randomYOffset * 1.5f, 0f), // 只生成负值，确保在下方
                0f
            );
            return basePosition + forcedOffset;
        }

        public void CreateDecoration(int decorationId)
        {
            var decorationItem = this.GetModel<IConfigModel>().ShopConfig.decorations[decorationId];
            
            // 优先使用场景Sprite，如果没有则使用icon
            Sprite spriteToUse = decorationItem.sceneSprite != null ? decorationItem.sceneSprite : decorationItem.icon;
            
            if (spriteToUse != null)
            {
                // 创建一个 GameObject 来承载 Sprite
                GameObject decoration = new GameObject("Decoration");
                
                // 添加 SpriteRenderer 组件
                SpriteRenderer spriteRenderer = decoration.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = spriteToUse;  // 设置 Sprite
                
                // 设置大小
                decoration.transform.localScale = Vector3.one * decorationItem.scale;
                
                // 添加跟随鼠标组件
                DecorationFollowMouse followMouse = decoration.AddComponent<DecorationFollowMouse>();
                followMouse.Initialize(this);
                
                // 设置为当前正在放置的装饰品
                currentPlacingDecoration = decoration;
                currentPlacingDecorationId = decorationId;
                
                // 设置初始位置为鼠标位置
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0;
                decoration.transform.position = mouseWorldPos;
            }
            else
            {
                Debug.LogWarning($"Decoration {decorationId} 的 icon 和 sceneSprite 都为空！");
            }
        }

        public void PlaceDecoration()
        {
            if (currentPlacingDecoration != null)
            {
                // 移除跟随鼠标组件
                DecorationFollowMouse followMouse = currentPlacingDecoration.GetComponent<DecorationFollowMouse>();
                if (followMouse != null)
                {
                    UnityEngine.Object.DestroyImmediate(followMouse);
                }
                
                // 添加拖拽组件
                currentPlacingDecoration.AddComponent<DecorationDrag>();
                
                // 添加到已购买列表
                this.GetModel<IGameModel>().PurchasedDecorations.Add(currentPlacingDecoration);
                
                // 更新已购买的装饰品数量
                var quantities = this.GetModel<IGameModel>().PurchasedDecorationQuantities;
                if (quantities.ContainsKey(currentPlacingDecorationId))
                {
                    quantities[currentPlacingDecorationId]++;
                }
                else
                {
                    quantities[currentPlacingDecorationId] = 1;
                }
                
                // 清空当前放置的装饰品
                currentPlacingDecoration = null;
                currentPlacingDecorationId = -1;
            }
        }

        private Vector3 GetDefaultDecorationPosition()
        {
            // 设置默认位置，可以根据需要调整
            return new Vector3(0, 0, 0);
        }

    }
}