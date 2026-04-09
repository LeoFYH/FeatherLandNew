using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

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
        bool IsCoverDecoration();
        bool IsCoverUI();
        bool IsOnGround();
        void CreateDecoration(int decorationId, int index);
        void CreateFixedDecoration(int decorationId, int index);
        void DestroyDecoration(int decorationId, int index, GameObject decorationObject);
        void PlaceDecoration();
        bool IsPlacingDecoration();
        void CreateDecorations();
        void OpenUrl(string url);
        void InitAccount();
    }

    public class GameSystem : AbstractSystem, IGameSystem
    {
        //private GameObject foodPrefab;
        private GameObject numPrefab;
        private IBirdModel birdModel;
        private GameObject currentPlacingDecoration; // 当前正在放置的装饰品
        private int currentPlacingDecorationId; // 当前正在放置的装饰品ID
        private int currentIndex;

        // Performance optimization: Cache Camera.main to avoid FindObjectOfType calls
        private Camera cachedMainCamera;

        // Memory optimization: pre-allocate physics buffers to avoid GC allocations
        private static readonly Collider2D[] _overlapBuffer = new Collider2D[16];
        
        [Header("食物位置偏移")]
        [Tooltip("食物落下位置相对于鼠标的偏移量")]
        private Vector3 foodDropOffset = new Vector3(0f, 0, 0f); // 基础偏移量（X轴改为0，让食物在左右两侧均匀随机）
        
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
            // Performance optimization: Cache Camera.main at initialization
            cachedMainCamera = Camera.main;
            
            // this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Food", obj =>
            // {
            //     foodPrefab = obj;
            // });
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
            this.GetSystem<IObjectPoolSystem>().Get("Num", null, go =>
            {
                go.transform.position = pos;
                go.GetComponent<NumPanel>().Init(s);
            });
        }

        public void CreateFood()
        {
            if (IsCoverGround())
            {
                this.GetSystem<ICursorSystem>().Feed();
                this.GetSystem<IAudioSystem>().PlayEffect(EffectType.DropFood);

                this.GetSystem<IObjectPoolSystem>().Get("Food", null, obj =>
                {
                    Food food = obj.GetComponent<Food>();
                    food.isTargeted = false;

                    // 根据当前选择的食物类型更换sprite
                    //var gameModel = this.GetModel<IGameModel>();
                    var saveModel = this.GetModel<ISaveModel>();
                    var configModel = this.GetModel<IConfigModel>();

                    if (saveModel.AccountData.sceneTools == null)
                        saveModel.AccountData.sceneTools = new List<SceneToolInfo>();
                    if (saveModel.AccountData.sceneTools.Count == 0)
                        saveModel.AccountData.sceneTools.Add(new SceneToolInfo());
                    // 查找食物工具配置
                    for (int i = 0; i < configModel.ShopConfig.tools.Length; i++)
                    {
                        var toolItem = configModel.ShopConfig.tools[i];
                        if (saveModel.AccountData.sceneTools[0].tools.Count <= i)
                        {
                            saveModel.AccountData.sceneTools[0].tools.Add(new ToolInfo());
                        }

                        if (saveModel.AccountData.sceneTools[0].tools[i].unlockedList == null)
                        {
                            saveModel.AccountData.sceneTools[0].tools[i].unlockedList = new List<int>() { 0 };
                        }

                        if (toolItem.name.ToLower() == "food")
                        {
                            // 查找匹配的食物类型
                            for (int j = 0; j < toolItem.selections.Length; j++)
                            {
                                var selection = toolItem.selections[j];
                                if (j == saveModel.AccountData.sceneTools[0].tools[i].equipedId)
                                {
                                    // 更换食物的sprite
                                    SpriteRenderer spriteRenderer = food.GetComponent<SpriteRenderer>();
                                    if (spriteRenderer != null && selection.icon != null)
                                    {
                                        spriteRenderer.sprite = selection.icon;
                                    }

                                    food.addValue = selection.addValue;
                                    // 设置食物大小
                                    food.transform.localScale = Vector3.one * selection.foodScale;
                                    break;
                                }
                            }

                            break;
                        }
                    }

                    // Performance optimization: Use cached camera
                    if (cachedMainCamera == null)
                    {
                        cachedMainCamera = Camera.main;
                    }
                    Vector3 mouseWorldPos = cachedMainCamera.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorldPos.z = 0; // 确保Z轴位置正确

                    // 生成有间距的随机位置
                    Vector3 finalPosition = GetValidFoodPosition(mouseWorldPos + foodDropOffset);

                    // 设置位置
                    food.transform.position = finalPosition;

                    // 添加随机旋转
                    float randomRotation = Random.Range(0f, 360f);
                    food.transform.rotation = Quaternion.Euler(0f, 0f, randomRotation);

                    // 如果有Rigidbody2D，确保它不会移动
                    Rigidbody2D rb = food.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.bodyType = RigidbodyType2D.Static;
                        // 或者
                        // rb.constraints = RigidbodyConstraints2D.FreezeAll;
                    }
                    
                    food.Init();

                    birdModel.Foods.Add(food);

                    if (birdModel.Foods.Count > 8)
                    {
                        // 删除最早的食物
                        var foodToRemove = birdModel.Foods[0];
                        birdModel.Foods.RemoveAt(0);
                        this.GetSystem<IObjectPoolSystem>().Recycle("Food", foodToRemove.gameObject);
                        //GameObject.Destroy(foodToRemove.gameObject);
                    }
                }); //GameObject.Instantiate(foodPrefab).GetComponent<Food>();
            }
        }

        public void ReduceFood(Food food)
        {
            food.hp--;
            if (food.hp <= 0)
            {
                CreateNum("+1", food.transform.position);
                birdModel.Foods.Remove(food);
                this.GetSystem<IObjectPoolSystem>().Recycle("Food", food.gameObject);
                //GameObject.Destroy(food.gameObject);
            }
        }

        public void RecycleFood(Food food)
        {
            birdModel.Foods.Remove(food);
            //GameObject.Destroy(food.gameObject);
            this.GetSystem<IObjectPoolSystem>().Recycle("Food", food.gameObject);
        }

        public bool TryGetUntargetedFood(Vector3 position, out Food food)
        {
            food = null;
            float closestSqrDistance = float.MaxValue;

            foreach (var temp in birdModel.Foods)
            {
                if (!temp.isTargeted && !temp.isDisabling && temp.gameObject.activeSelf)
                {
                    Vector3 diff = position - temp.transform.position;
                    float sqrDistance = diff.sqrMagnitude;
                    if (sqrDistance < closestSqrDistance)
                    {
                        closestSqrDistance = sqrDistance;
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
            
            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;
            
            // Performance optimization: Use cached camera instead of Camera.main
            if (cachedMainCamera == null)
            {
                cachedMainCamera = Camera.main;
            }
            if (cachedMainCamera == null)
            {
                return false;
            }
            
            // 将鼠标位置转换为世界坐标
            Vector3 worldPosition = cachedMainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -cachedMainCamera.transform.position.z));
            Vector2 worldPosition2D = new Vector2(worldPosition.x, worldPosition.y);
            
            // CPU优化：使用Brid.birdCollider（已缓存），避免每帧GetComponent
            foreach (var birdData in birdModel.BirdList)
            {
                if (birdData?.bird == null || birdData.bird.gameObject == null) continue;

                Collider2D collider2D = birdData.bird.birdCollider;

                if (collider2D != null)
                {
                    if (collider2D.OverlapPoint(worldPosition2D))
                    {
                        return false;
                    }
                }
                else
                {
                    Vector2 diff = worldPosition2D - (Vector2)birdData.bird.transform.position;
                    if (diff.sqrMagnitude < 0.25f)
                    {
                        return false;
                    }
                }
            }

            if (NavigationManager.Instance == null)
                return false;

            // 检查基础偏移位置是否在可导航区域
            Vector2 worldPos2D = new Vector2(worldPosition.x, worldPosition.y);
            if (NavigationManager.Instance.IsPointInNavMeshArea(3, worldPos2D + (Vector2)foodDropOffset))
                return true;

            return false;
        }

        public bool IsCoverBird()
        {
            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;
            
            // Performance optimization: Use cached camera instead of Camera.main
            if (cachedMainCamera == null)
            {
                cachedMainCamera = Camera.main;
            }
            if (cachedMainCamera == null)
            {
                return false;
            }
            
            // 将鼠标位置转换为世界坐标
            Vector3 worldPosition = cachedMainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -cachedMainCamera.transform.position.z));
            Vector2 worldPosition2D = new Vector2(worldPosition.x, worldPosition.y);
            
            // CPU优化：使用Brid.birdCollider（已缓存），避免每帧GetComponent
            foreach (var birdData in birdModel.BirdList)
            {
                if (birdData?.bird == null || birdData.bird.gameObject == null) continue;

                Collider2D collider2D = birdData.bird.birdCollider;

                if (collider2D != null)
                {
                    if (collider2D.OverlapPoint(worldPosition2D))
                    {
                        return true;
                    }
                }
                else
                {
                    Vector2 diff = worldPosition2D - (Vector2)birdData.bird.transform.position;
                    if (diff.sqrMagnitude < 0.25f)
                    {
                        return true;
                    }
                }
            }

            // CPU优化：使用IBirdModel.EggList代替FindGameObjectsWithTag("Egg")
            foreach (var egg in birdModel.EggList)
            {
                if (egg == null || egg.gameObject == null) continue;

                Collider2D collider2D = egg.GetComponent<Collider2D>();

                if (collider2D != null)
                {
                    if (collider2D.OverlapPoint(worldPosition2D))
                    {
                        return true;
                    }
                }
                else
                {
                    Vector2 diff = worldPosition2D - (Vector2)egg.transform.position;
                    if (diff.sqrMagnitude < 0.25f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsCoverDecoration()
        {
            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;

            // Performance optimization: Use cached camera instead of Camera.main
            if (cachedMainCamera == null)
            {
                cachedMainCamera = Camera.main;
            }
            if (cachedMainCamera == null)
            {
                return false;
            }

            // 将鼠标位置转换为世界坐标
            Vector3 worldPosition = cachedMainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -cachedMainCamera.transform.position.z));
            Vector2 worldPosition2D = new Vector2(worldPosition.x, worldPosition.y);

            // Memory optimization: Use NonAlloc to avoid array allocation every call
            float checkRadius = 0.5f;
            int hitCount = Physics2D.OverlapCircleNonAlloc(worldPosition2D, checkRadius, _overlapBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                if (_overlapBuffer[i].CompareTag("Decoration"))
                {
                    return true;
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

            if (cachedMainCamera == null) cachedMainCamera = Camera.main;
            if (cachedMainCamera == null) return false;
            Vector2 mousePosition = cachedMainCamera.ScreenToWorldPoint(Input.mousePosition);

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
                // Performance optimization: Use sqrMagnitude instead of Distance
                bool isValidPosition = true;
                float sqrMinFoodDistance = minFoodDistance * minFoodDistance;
                foreach (var existingFood in birdModel.Foods)
                {
                    if (existingFood != null && existingFood.gameObject != null)
                    {
                        Vector3 diff = finalPosition - existingFood.transform.position;
                        float sqrDistance = diff.sqrMagnitude;
                        // Performance optimization: Compare squared distance directly
                        if (sqrDistance < sqrMinFoodDistance)
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

        public void CreateDecoration(int decorationId, int index)
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var decorationItem = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[decorationId];
            
            // 优先使用场景Sprite，如果没有则使用icon
            Sprite spriteToUse = decorationItem.sceneSprite != null ? decorationItem.sceneSprite : decorationItem.icon;
            
            if (spriteToUse != null)
            {
                // 创建一个 GameObject 来承载 Sprite
                GameObject decoration = new GameObject("Decoration");
                
                // 添加 SpriteRenderer 组件
                SpriteRenderer spriteRenderer = decoration.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = spriteToUse;  // 设置 Sprite
                spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

                if (!decorationItem.isGround)
                    decoration.AddComponent<DepthMask>();
                
                // 设置大小
                decoration.transform.localScale = Vector3.one * decorationItem.scale;
                
                // 添加碰撞器用于点击检测
                BoxCollider2D collider = decoration.AddComponent<BoxCollider2D>();
                collider.size = spriteRenderer.sprite.bounds.size;
                
                // 添加跟随鼠标组件
                DecorationFollowMouse followMouse = decoration.AddComponent<DecorationFollowMouse>();
                followMouse.Initialize(this);
                
                // 设置为当前正在放置的装饰品
                currentPlacingDecoration = decoration;
                currentPlacingDecorationId = decorationId;
                currentIndex = index;
                Debug.Log("CurrentIndex: " + currentIndex);
                
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

        /// <summary>
        /// 获取可用的 fixedPositions 索引（优先使用被释放的索引）
        /// </summary>
        private int GetAvailableFixedPositionIndex(int decorationId, int mapIndex)
        {
            var accountData = this.GetModel<ISaveModel>().AccountData;
            var decorationInfo = accountData.sceneDecorationInfos[mapIndex].decorations[decorationId];
            var decorationItem = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[decorationId];
            
            // 初始化 usedFixedPositionIndices（向后兼容）
            if (decorationInfo.usedFixedPositionIndices == null)
            {
                decorationInfo.usedFixedPositionIndices = new List<int>();
            }
            
            // 如果没有 fixedPositions，返回 -1
            if (decorationItem.fixedPositions == null || decorationItem.fixedPositions.Length == 0)
            {
                return -1;
            }
            
            // 优先查找未使用的索引（被释放的索引）
            for (int i = 0; i < decorationItem.fixedPositions.Length; i++)
            {
                if (!decorationInfo.usedFixedPositionIndices.Contains(i))
                {
                    return i;
                }
            }
            
            // 如果所有索引都被使用，返回 -1（表示没有可用位置）
            return -1;
        }

        public void CreateFixedDecoration(int decorationId, int index)
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var decorationItem = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[decorationId];
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>(decorationItem.prefab.AssetGUID, obj =>
            {
                var decoration = GameObject.Instantiate(obj);
                if (index < decorationItem.fixedPositions.Length)
                {
                    Debug.LogWarning("设置位置");
                    decoration.transform.localPosition = decorationItem.fixedPositions[index];
                    decoration.GetComponentsInChildren<Transform>()[1].localPosition = Vector3.zero;
                }

                currentIndex = index;
                DecorationClickHandler clickHandler = decoration.GetComponentInChildren<DecorationClickHandler>();
                clickHandler.Initialize(decorationId, currentIndex);
            });
            // // 优先使用场景Sprite，如果没有则使用icon
            // Sprite spriteToUse = decorationItem.sceneSprite != null ? decorationItem.sceneSprite : decorationItem.icon;
            //
            // if (spriteToUse != null)
            // {
            //     // 创建一个 GameObject 来承载 Sprite
            //     GameObject decoration = new GameObject("FixedDecoration");
            //     
            //     // 添加 SpriteRenderer 组件
            //     SpriteRenderer spriteRenderer = decoration.AddComponent<SpriteRenderer>();
            //     spriteRenderer.sprite = spriteToUse;  // 设置 Sprite
            //     spriteRenderer.sortingOrder = 3;
            //     spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            //     
            //     if (!decorationItem.isGround)
            //         decoration.AddComponent<DepthMask>();
            //     
            //     // 设置大小
            //     decoration.transform.localScale = Vector3.one * decorationItem.scale;
            //     
            //     // 添加碰撞器用于点击检测
            //     BoxCollider2D collider = decoration.AddComponent<BoxCollider2D>();
            //     collider.size = spriteRenderer.sprite.bounds.size;
            //     
            //     // 设置固定位置
            //     if (index < decorationItem.fixedPositions.Length)
            //     {
            //         Debug.LogWarning("设置位置");
            //         decoration.transform.position = decorationItem.fixedPositions[index];
            //     }
            //
            //     currentIndex = index;
            //     
            //     // 添加点击检测组件
            //     DecorationClickHandler clickHandler = decoration.AddComponent<DecorationClickHandler>();
            //     
            //     clickHandler.Initialize(decorationId, currentIndex);
            // }
            // else
            // {
            //     Debug.LogWarning($"Decoration {decorationId} 的 icon 和 sceneSprite 都为空！");
            // }
        }

        public void DestroyDecoration(int decorationId, int index, GameObject decorationObject)
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var accountData = this.GetModel<ISaveModel>().AccountData;
            var decorationInfo = accountData.sceneDecorationInfos[mapIndex].decorations[decorationId];
            var positionList = decorationInfo.position;
            
            // 初始化 usedFixedPositionIndices（向后兼容）
            if (decorationInfo.usedFixedPositionIndices == null)
            {
                decorationInfo.usedFixedPositionIndices = new List<int>();
            }
            
            // 通过装饰对象的实际位置在 position 列表中查找对应的索引，而不是使用传入的 index
            // 因为删除第一个装饰后，后续装饰的索引会前移，但 decorationIndex 不会自动更新
            Vector3 decorationPos = decorationObject.transform.position;
            int actualIndex = -1;
            float minDistance = float.MaxValue;
            const float positionTolerance = 0.1f; // 位置匹配的容差
            
            // 查找最接近的位置索引
            // Performance optimization: Use sqrMagnitude instead of Distance
            float sqrTolerance = positionTolerance * positionTolerance;
            for (int i = 0; i < positionList.Count; i++)
            {
                Vector3 diff = decorationPos - positionList[i];
                float sqrDistance = diff.sqrMagnitude;
                if (sqrDistance < sqrTolerance && sqrDistance < minDistance)
                {
                    minDistance = sqrDistance;
                    actualIndex = i;
                }
            }
            
            // 如果找不到匹配的位置，使用传入的 index（但需要检查边界）
            if (actualIndex == -1)
            {
                if (index >= 0 && index < positionList.Count)
                {
                    actualIndex = index;
                }
                else
                {
                    Debug.LogWarning($"无法找到装饰品 {decorationId} 对应的位置索引，使用最后一个索引");
                    if (positionList.Count > 0)
                    {
                        actualIndex = positionList.Count - 1;
                    }
                    else
                    {
                        Debug.LogError($"装饰品 {decorationId} 的 position 列表为空，无法删除");
                        return;
                    }
                }
            }
            
            // 找到被删除位置对应的 fixedPositions 索引
            Vector3 deletedPosition = positionList[actualIndex];
            var decorationItem = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[decorationId];
            int fixedPositionIndex = -1;
            float minFixedDistance = float.MaxValue;
            const float fixedPositionTolerance = 0.1f;
            
            // 在 fixedPositions 中查找匹配的位置索引
            // Performance optimization: Use sqrMagnitude instead of Distance
            if (decorationItem.fixedPositions != null && decorationItem.fixedPositions.Length > 0)
            {
                float sqrFixedTolerance = fixedPositionTolerance * fixedPositionTolerance;
                for (int i = 0; i < decorationItem.fixedPositions.Length; i++)
                {
                    Vector3 diff = deletedPosition - decorationItem.fixedPositions[i];
                    float sqrDistance = diff.sqrMagnitude;
                    if (sqrDistance < sqrFixedTolerance && sqrDistance < minFixedDistance)
                    {
                        minFixedDistance = sqrDistance;
                        fixedPositionIndex = i;
                    }
                }
            }
            
            // 销毁装饰品对象
            GameObject.Destroy(decorationObject);
            
            // 从列表中删除对应的位置（使用实际找到的索引）
            if (actualIndex >= 0 && actualIndex < positionList.Count)
            {
                positionList.RemoveAt(actualIndex);
                decorationInfo.count--;
                
                // 确保 count 不为负数
                if (decorationInfo.count < 0)
                {
                    decorationInfo.count = 0;
                }
                
                // 如果找到了对应的 fixedPositions 索引，从已使用列表中移除
                if (fixedPositionIndex >= 0)
                {
                    if (decorationInfo.usedFixedPositionIndices.Contains(fixedPositionIndex))
                    {
                        decorationInfo.usedFixedPositionIndices.Remove(fixedPositionIndex);
                        Debug.Log($"销毁装饰品 {decorationId}，释放 fixedPositions 索引 {fixedPositionIndex}，剩余数量: {decorationInfo.count}");
                    }
                }
                
                Debug.Log($"销毁装饰品 {decorationId}，使用索引 {actualIndex}（传入索引: {index}），剩余数量: {decorationInfo.count}");
            }
            else
            {
                Debug.LogError($"装饰品 {decorationId} 删除失败：索引 {actualIndex} 超出范围（列表大小: {positionList.Count}）");
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
                
                // 添加点击检测组件
                DecorationClickHandler clickHandler = currentPlacingDecoration.AddComponent<DecorationClickHandler>();
                clickHandler.Initialize(currentPlacingDecorationId, currentIndex);
                int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
                // 更新已购买的装饰品数量
                var accountData = this.GetModel<ISaveModel>().AccountData;
                Debug.Log("index:" + currentIndex);
                accountData.sceneDecorationInfos[mapIndex].decorations[currentPlacingDecorationId].position[currentIndex] =
                    currentPlacingDecoration.transform.position;
                // 清空当前放置的装饰品
                currentPlacingDecoration = null;
                currentPlacingDecorationId = -1;
                currentIndex = -1;
            }
        }

        public bool IsPlacingDecoration()
        {
            return currentPlacingDecoration != null;
        }

        public void CreateDecorations()
        {
            var accountData = this.GetModel<ISaveModel>().AccountData;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            while (accountData.sceneDecorationInfos.Count <= mapIndex)
            {
                accountData.sceneDecorationInfos.Add(new SceneDecorationInfo());
            }
            int count = accountData.sceneDecorationInfos[mapIndex].decorations.Count;
            //Dictionary<int, int> decount = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                var decorationInfo = accountData.sceneDecorationInfos[mapIndex].decorations[i];
                
                // 初始化 position 列表
                if (decorationInfo.position == null)
                {
                    decorationInfo.position = new List<Vector3>();
                }
                
                // 初始化 usedFixedPositionIndices（向后兼容）
                if (decorationInfo.usedFixedPositionIndices == null)
                {
                    decorationInfo.usedFixedPositionIndices = new List<int>();
                }
                
                // 重建 usedFixedPositionIndices：根据现有的 position 列表匹配 fixedPositions
                var decorationItem = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[i];
                decorationInfo.usedFixedPositionIndices.Clear();
                
                if (decorationItem.fixedPositions != null && decorationItem.fixedPositions.Length > 0)
                {
                    foreach (var pos in decorationInfo.position)
                    {
                        // 查找这个位置对应的 fixedPositions 索引
                        int matchedIndex = -1;
                        float minDistance = float.MaxValue;
                        const float positionTolerance = 0.1f;
                        
                        // Performance optimization: Use sqrMagnitude instead of Distance
                        float sqrPositionTolerance = positionTolerance * positionTolerance;
                        for (int k = 0; k < decorationItem.fixedPositions.Length; k++)
                        {
                            Vector3 diff = pos - decorationItem.fixedPositions[k];
                            float sqrDistance = diff.sqrMagnitude;
                            if (sqrDistance < sqrPositionTolerance && sqrDistance < minDistance)
                            {
                                minDistance = sqrDistance;
                                matchedIndex = k;
                            }
                        }
                        
                        // 如果找到匹配的索引且未在列表中，添加到已使用列表
                        if (matchedIndex >= 0 && !decorationInfo.usedFixedPositionIndices.Contains(matchedIndex))
                        {
                            decorationInfo.usedFixedPositionIndices.Add(matchedIndex);
                        }
                    }
                }
                
                // 先确保 position 列表有足够的容量
                while (decorationInfo.position.Count < decorationInfo.count)
                {
                    decorationInfo.position.Add(Vector3.zero);
                }
                
                // 只加载一次 Prefab，然后多次实例化，避免异步问题
                int finalId = i;
                this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>(decorationItem.prefab.AssetGUID,
                    obj =>
                    {
                        // 实例化所有该类型的装饰品
                        for (int j = 0; j < decorationInfo.count; j++)
                        {
                            var decoration = GameObject.Instantiate(obj, decorationInfo.position[j], Quaternion.identity);
                            DecorationClickHandler clickHandler = decoration.GetComponentInChildren<DecorationClickHandler>();
                            if (clickHandler != null)
                            {
                                clickHandler.Initialize(finalId, j);
                            }
                            else
                            {
                                Debug.LogError($"Decoration {finalId} at index {j} 缺少 DecorationClickHandler 组件");
                            }
                        }
                    });
            }
        }

        public void OpenUrl(string url)
        {
            try
            {
                Debug.Log($"正在打开外部链接: {url}");
                
                // 使用系统默认浏览器打开链接
                Application.OpenURL(url);
            }
            catch (Exception ex)
            {
                Debug.LogError($"打开外部链接失败: {ex.Message}");
            }
        }

        public void InitAccount()
        {
            this.GetModel<IAccountModel>().Coins.Value = this.GetModel<ISaveModel>().AccountData.coins;
            this.GetModel<IAccountModel>().Coins.Register(v =>
            {
                int limit = this.GetModel<IConfigModel>().ShopConfig.coinsLimit;
                if (v > limit)
                {
                    this.GetModel<IAccountModel>().Coins.Value = limit;
                    return;
                }
                this.GetModel<ISaveModel>().AccountData.coins = v;
            });
        }

        private Vector3 GetDefaultDecorationPosition()
        {
            // 设置默认位置，可以根据需要调整
            return new Vector3(0, 0, 0);
        }

    }
}