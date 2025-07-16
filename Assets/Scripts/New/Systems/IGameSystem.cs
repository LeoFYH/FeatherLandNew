using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BirdGame
{
    public interface IGameSystem : ISystem
    {
        void CreateNum(string s, Vector3 pos);
        void CreateFood();
        void ReduceFood(Food food);
        void RecycleFood(Food food);
        bool TryGetUntargetedFood(Vector3 position, out Food food);
    }

    public class GameSystem : AbstractSystem, IGameSystem
    {
        private GameObject foodPrefab;
        private GameObject numPrefab;
        private IBirdModel birdModel;

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

        public void CreateNum(string s, Vector3 pos)
        {
            GameObject go = GameObject.Instantiate(numPrefab);
            go.transform.position = pos;
            go.GetComponent<NumPanel>().Init(s);
        }

        public void CreateFood()
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
                    if (hit.collider.CompareTag("Bird"))
                    {
                        return;
                    }
                }
            }
        
            if (NavigationManager.Instance.IsPointInNavMeshArea(3, mousePosition))
            {
                this.GetSystem<IAudioSystem>().PlayEffect(EffectType.DropFood);
                Food food = GameObject.Instantiate(foodPrefab).GetComponent<Food>();
                food.isTargeted = false;
            
                // 设置位置
                food.transform.position = mouseWorldPos;
            
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
    }
}