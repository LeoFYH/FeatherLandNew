using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int coin = 100;
    public int eggPackage = 100;
    public GameObject eggPrefab;
    public GameObject foodPrefab;
    public List<Food> foods;
    public List<Nest> nests;
    public List<Transform> flyPositions;
    public int noOpenEggs;
    public float createFoodTime = 0.5f;
    float foodTimer;
    public GameObject numPrefab;
    private float lastClickTime = 0f;  // 记录上次点击时间
    private float clickInterval = 1f;  // 点击间隔时间（1秒）

    private void Awake()
    {
        Instance = this;
    }

    public void CreateNum(string s, Vector3 pos)
    {
        GameObject go = Instantiate(numPrefab, UIManager.Instance.transform);
        go.transform.position = pos;
        go.GetComponent<NumPanel>().Init(s);
    }

    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        
        // 取消撒食物冷却，每次点击都能撒
        if (Input.GetMouseButtonDown(0))
        {
            CreateFood();
        }
    }

    void CreateFood()
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
            Food food = Instantiate(foodPrefab).GetComponent<Food>();
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
            
            foods.Add(food);

            if (foods.Count > 8)
            {
                // 删除最早的食物
                var foodToRemove = foods[0];
                foods.RemoveAt(0);
                Destroy(foodToRemove.gameObject);
            }
        }
    }

    public void CreateBrid()
    {
        noOpenEggs = 3;
        for (int i = 0; i < 3; i++)
        {
            GameObject go = Instantiate(eggPrefab);
            go.transform.position = new Vector3((i-1)*2, 0, 0);
        }
    }

    public void ReduceFood()
    {
        foods[0].hp--;
        if (foods[0].hp <= 0)
        {
            //CreateNum("+1");
            Destroy(foods[0].gameObject);
            foods.Remove(foods[0]);
        }
    }

    public void ReduceFood(Food food)
    {
        food.hp--;
        if (food.hp <= 0)
        {
            CreateNum("+1", food.transform.position);
            foods.Remove(food);
            Destroy(food.gameObject);
        }
    }

    public void RecycleFood(Food food)
    {
        foods.Remove(food);
        Destroy(food.gameObject);
    }

    public bool TryGetUntargetedFood(Vector3 position, out Food food)
    {
        food = null;
        float closestDistance = float.MaxValue;

        foreach (var temp in foods)
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
