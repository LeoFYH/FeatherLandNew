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
        private GameObject foodPrefab;
        private GameObject numPrefab;
        private IBirdModel birdModel;
        private GameObject currentPlacingDecoration; // 当前正在放置的装饰品
        private int currentPlacingDecorationId; // 当前正在放置的装饰品ID
        private int currentIndex;
        
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

                // 根据当前选择的食物类型更换sprite
                var gameModel = this.GetModel<IGameModel>();
                var saveModel = this.GetModel<ISaveModel>();
                var configModel = this.GetModel<IConfigModel>();
                
                // 查找食物工具配置
                for (int i = 0; i < configModel.ShopConfig.tools.Length; i++)
                {
                    var toolItem = configModel.ShopConfig.tools[i];
                    if (toolItem.name.ToLower() == "food")
                    {
                        // 查找匹配的食物类型
                        for (int j = 0; j < toolItem.selections.Length; j++)
                        {
                            var selection = toolItem.selections[j];
                            if (j == saveModel.AccountData.tools[i].equipedId)
                            {
                                // 更换食物的sprite
                                SpriteRenderer spriteRenderer = food.GetComponent<SpriteRenderer>();
                                if (spriteRenderer != null && selection.icon != null)
                                {
                                    spriteRenderer.sprite = selection.icon;
                                }
                                
                                // 设置食物大小
                                food.transform.localScale = Vector3.one * selection.foodScale;
                                break;
                            }
                        }
                        break;
                    }
                }

                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
            
            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;
            
            // 获取主摄像机
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }
            
            // 将鼠标位置转换为世界坐标
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -mainCamera.transform.position.z));
            
            // 检查是否点击到鸟，如果点击到鸟，则不生成食物
            GameObject[] birds = GameObject.FindGameObjectsWithTag("Bird");
            
            foreach (var bird in birds)
            {
                if (bird == null) continue;
                
                // 获取鸟的Collider2D
                Collider2D collider2D = bird.GetComponent<Collider2D>();
                
                if (collider2D != null)
                {
                    // 使用OverlapPoint检测鼠标是否在碰撞器内（适用于触发器）
                    if (collider2D.OverlapPoint(worldPosition))
                    {
                        return false;
                    }
                }
                else
                {
                    // 如果没有碰撞器，使用简单的距离检测
                    float distance = Vector2.Distance(worldPosition, bird.transform.position);
                    if (distance < 0.5f) // 使用0.5f作为检测范围
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
            
            // 获取主摄像机
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }
            
            // 将鼠标位置转换为世界坐标
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -mainCamera.transform.position.z));
            
            // 查找所有带有"Bird"或"Egg"标签的GameObject
            GameObject[] birds = GameObject.FindGameObjectsWithTag("Bird");
            GameObject[] eggs = GameObject.FindGameObjectsWithTag("Egg");
            
            // 检查鸟
            foreach (var bird in birds)
            {
                if (bird == null) continue;
                
                // 获取鸟的Collider2D
                Collider2D collider2D = bird.GetComponent<Collider2D>();
                
                if (collider2D != null)
                {
                    // 使用OverlapPoint检测鼠标是否在碰撞器内（适用于触发器）
                    if (collider2D.OverlapPoint(worldPosition))
                    {
                        //Debug.Log($"检测到鸟: {bird.name}");
                        return true;
                    }
                }
                else
                {
                    // 如果没有碰撞器，使用简单的距离检测
                    float distance = Vector2.Distance(worldPosition, bird.transform.position);
                    if (distance < 0.5f) // 使用0.5f作为检测范围
                    {
                        Debug.Log($"通过距离检测到鸟: {bird.name}, 距离: {distance}");
                        return true;
                    }
                }
            }
            
            // 检查蛋
            foreach (var egg in eggs)
            {
                if (egg == null) continue;
                
                // 获取蛋的Collider2D
                Collider2D collider2D = egg.GetComponent<Collider2D>();
                
                if (collider2D != null)
                {
                    // 使用OverlapPoint检测鼠标是否在碰撞器内（适用于触发器）
                    if (collider2D.OverlapPoint(worldPosition))
                    {
                        Debug.Log($"检测到蛋: {egg.name}");
                        return true;
                    }
                }
                else
                {
                    // 如果没有碰撞器，使用简单的距离检测
                    float distance = Vector2.Distance(worldPosition, egg.transform.position);
                    if (distance < 0.5f) // 使用0.5f作为检测范围
                    {
                        Debug.Log($"通过距离检测到蛋: {egg.name}, 距离: {distance}");
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

        public void CreateFixedDecoration(int decorationId, int index)
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var decorationItem = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[decorationId];
            
            // 优先使用场景Sprite，如果没有则使用icon
            Sprite spriteToUse = decorationItem.sceneSprite != null ? decorationItem.sceneSprite : decorationItem.icon;
            
            if (spriteToUse != null)
            {
                // 创建一个 GameObject 来承载 Sprite
                GameObject decoration = new GameObject("FixedDecoration");
                
                // 添加 SpriteRenderer 组件
                SpriteRenderer spriteRenderer = decoration.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = spriteToUse;  // 设置 Sprite
                
                // 设置大小
                decoration.transform.localScale = Vector3.one * decorationItem.scale;
                
                // 添加碰撞器用于点击检测
                BoxCollider2D collider = decoration.AddComponent<BoxCollider2D>();
                collider.size = spriteRenderer.sprite.bounds.size;
                
                // 设置固定位置
                decoration.transform.position = decorationItem.fixedPosition;

                currentIndex = index;
                
                // 添加点击检测组件
                DecorationClickHandler clickHandler = decoration.AddComponent<DecorationClickHandler>();
                
                clickHandler.Initialize(decorationId, currentIndex);
            }
            else
            {
                Debug.LogWarning($"Decoration {decorationId} 的 icon 和 sceneSprite 都为空！");
            }
        }

        public void DestroyDecoration(int decorationId, int index, GameObject decorationObject)
        {
            // 销毁装饰品对象
            GameObject.Destroy(decorationObject);
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var accountData = this.GetModel<ISaveModel>().AccountData;
            if(accountData.sceneDecorationInfos[mapIndex].decorations[decorationId].count > 0)
                accountData.sceneDecorationInfos[mapIndex].decorations[decorationId].position.RemoveAt(index);
            accountData.sceneDecorationInfos[mapIndex].decorations[decorationId].count--;
            if (accountData.sceneDecorationInfos[mapIndex].decorations[decorationId].count <= 0)
            {
                accountData.sceneDecorationInfos[mapIndex].decorations[decorationId].count = 0;
            }
            this.GetSystem<ISaveSystem>().SaveData();
            Debug.Log($"销毁装饰品 {decorationId}，剩余数量: {accountData.sceneDecorationInfos[mapIndex].decorations[decorationId].count}");
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
                this.GetSystem<ISaveSystem>().SaveData();
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
            for (int i = 0; i < count; i++)
            {
                if (accountData.sceneDecorationInfos[mapIndex].decorations[i].position == null)
                {
                    accountData.sceneDecorationInfos[mapIndex].decorations[i].position = new List<Vector3>();
                }
                
                for (int j = 0; j < accountData.sceneDecorationInfos[mapIndex].decorations[i].count; j++)
                {
                    var decorationItem = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[i];
                    // 创建一个 GameObject 来承载 Sprite
                    GameObject decoration = new GameObject("Decoration");
                    Sprite spriteToUse = decorationItem.sceneSprite != null
                        ? decorationItem.sceneSprite
                        : decorationItem.icon;

                    // 添加 SpriteRenderer 组件
                    SpriteRenderer spriteRenderer = decoration.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = spriteToUse; // 设置 Sprite

                    // 设置大小
                    decoration.transform.localScale = Vector3.one * decorationItem.scale;

                    // 添加碰撞器用于点击检测
                    BoxCollider2D collider = decoration.AddComponent<BoxCollider2D>();
                    collider.size = spriteRenderer.sprite.bounds.size;

                    // 添加拖拽组件
                    decoration.AddComponent<DecorationDrag>();

                    // 添加点击检测组件
                    DecorationClickHandler clickHandler = decoration.AddComponent<DecorationClickHandler>();
                    clickHandler.Initialize(i, j);
                    if (accountData.sceneDecorationInfos[mapIndex].decorations[i].position.Count <= j)
                    {
                        accountData.sceneDecorationInfos[mapIndex].decorations[i].position.Add(Vector3.zero);
                    }

                    decoration.transform.position = accountData.sceneDecorationInfos[mapIndex].decorations[i].position[j];
                }
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
                this.GetSystem<ISaveSystem>().SaveData();
            });
        }

        private Vector3 GetDefaultDecorationPosition()
        {
            // 设置默认位置，可以根据需要调整
            return new Vector3(0, 0, 0);
        }

    }
}