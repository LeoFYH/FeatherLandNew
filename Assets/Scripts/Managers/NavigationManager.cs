using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

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
    
    // 获取特定NavMesh区域的随机点
    public Vector3 GetRandomPointInArea(int areaMask)
    {
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();
        
        // 检查NavMesh数据是否有效
        if (navMeshData.vertices.Length == 0 || navMeshData.indices.Length == 0)
        {
            Debug.LogError("No NavMesh data found!");
            return Vector3.zero;
        }
        
        // 收集属于目标区域的所有三角形
        List<int> validTriangles = new List<int>();
        for (int i = 0; i < navMeshData.indices.Length; i += 3)
        {
            // 检查三角形区域是否匹配
            int areaIndex = navMeshData.areas[i / 3];
            if ((areaMask & (1 << areaIndex)) != 0)
            {
                validTriangles.Add(i);
            }
        }
        
        if (validTriangles.Count == 0)
        {
            Debug.LogError($"No triangles found in area!");
            return Vector3.zero;
        }
        
        // 随机选择一个三角形
        int randomIndex = Random.Range(0, validTriangles.Count);
        int triangleStart = validTriangles[randomIndex];
        
        // 获取三角形的三个顶点
        Vector3 pointA = navMeshData.vertices[navMeshData.indices[triangleStart]];
        Vector3 pointB = navMeshData.vertices[navMeshData.indices[triangleStart + 1]];
        Vector3 pointC = navMeshData.vertices[navMeshData.indices[triangleStart + 2]];
        
        // 在三角形内部生成随机点（使用重心坐标）
        float r1 = Random.Range(0f, 1f);
        float r2 = Random.Range(0f, 1f);
        
        // 确保点在三角形内
        if (r1 + r2 > 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }
        
        return pointA + r1 * (pointB - pointA) + r2 * (pointC - pointA);
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

        var point = colliderDic[area].GetComponent<PolygonCollider2D>().GetRandomPoint();
        return new Vector3(point.x, point.y, 0);
        // var walkableArea = colliderDic[area];
        // // 尝试最多10次获取有效点
        // for (int i = 0; i < 10; i++)
        // {
        //     // 获取WalkableArea的边界
        //     Bounds bounds = walkableArea.GetComponent<PolygonCollider2D>().bounds;
        //     
        //     // 在边界范围内随机取点
        //     float x = Random.Range(bounds.min.x, bounds.max.x);
        //     float y = Random.Range(bounds.min.y, bounds.max.y);
        //     Vector2 point = new Vector2(x, y);
        //
        //     // 检查点是否在WalkableArea内
        //     if (walkableArea)
        //     {
        //         return point;
        //     }
        // }
        // // 如果10次都没找到合适的点，返回WalkableArea的中心点
        // return (Vector2)walkableArea.transform.position;
    }
}
