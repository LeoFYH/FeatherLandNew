using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    private static NavigationManager _instance;

    public static NavigationManager Instance
    {
        get
        {
            return _instance;
        }
    }

    public PolygonCollider2D[] areas;

    private Dictionary<int, WalkableArea> colliderDic = new Dictionary<int, WalkableArea>();

    private void Awake()
    {
        _instance = this;
        
        for (int i = 0; i < areas.Length; i++)
        {
            var modifier = areas[i].GetComponent<NavMeshModifier>();
            if (!colliderDic.ContainsKey(modifier.area))
            {
                colliderDic.Add(modifier.area, areas[i].GetComponent<WalkableArea>());
            }
            else
            {
                Debug.Log("存在相同区域！");
            }
        }
    }

    public WalkableArea GetWalkableArea(int area)
    {
        if (!colliderDic.ContainsKey(area))
        {
            return null;
        }

        return colliderDic[area];
    }

    /// <summary>
    /// 随机获取目标点
    /// </summary>
    /// <param name="area">目标区域  3:Ground 4:LeftArea 5:RightArea 6:RockArea</param>
    /// <returns></returns>
    public Vector3 GetRandomTarget(int area)
    {
        if (!colliderDic.ContainsKey(area))
        {
            Debug.Log($"不存在为{area}的区域!");
            return Vector3.zero;
        }

        var walkableArea = colliderDic[area];
        // 尝试最多10次获取有效点
        for (int i = 0; i < 10; i++)
        {
            // 获取WalkableArea的边界
            Bounds bounds = walkableArea.GetComponent<PolygonCollider2D>().bounds;
            
            // 在边界范围内随机取点
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            Vector2 point = new Vector2(x, y);

            // 检查点是否在WalkableArea内
            if (walkableArea)
            {
                return point;
            }
        }
        // 如果10次都没找到合适的点，返回WalkableArea的中心点
        return (Vector2)walkableArea.transform.position;
    }
}
