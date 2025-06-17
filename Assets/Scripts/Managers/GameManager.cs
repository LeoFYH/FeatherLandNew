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
        
        // 检查是否达到点击间隔时间
        if (Time.time - lastClickTime >= clickInterval)
        {
            if (Input.GetMouseButtonDown(0))
            {
                CreateFood();
                lastClickTime = Time.time;  // 更新最后点击时间
            }
        }
    }

    void CreateFood()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // 确保Z轴位置正确
        
        //增加射线检测，如果点击的是鸟，则不生成米粒
        //创建一条从屏幕射出的射线
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //获得射线检测命中到的目标
        RaycastHit2D hit = Physics2D.Raycast(ray.origin,ray.direction,Mathf.Infinity);
        if(hit.collider != null && hit.collider.gameObject.CompareTag("Bird"))
        {
            return;
        }
        
        WalkableArea walkableArea = FindObjectOfType<WalkableArea>();
        if (walkableArea != null && walkableArea.IsPointInside(new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
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
            if (!temp.isTargeted)
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
