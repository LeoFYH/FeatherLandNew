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

    public float radius = 10f;
    public int maxTries = 30;
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

    public bool IsPointInNavMeshArea(int area, Vector3 position)
    {
        int areaIndex = area;
        if (areaIndex < 0)
        {
            return false;
        }

        int areaMask = 1 << areaIndex;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 0.1f, areaMask))
        {
            // 命中并且区域匹配
            return (hit.mask & areaMask) != 0;
        }

        // 没有命中任何区域或不在指定区域内
        return false;
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

        //var point = colliderDic[area].GetComponent<PolygonCollider2D>().GetRandomPoint();
        //return new Vector3(point.x, point.y, 0);
        int areaIndex = area;
        if (areaIndex < 0)
        {
            return Vector3.zero;
        }

        var triangulation = NavMesh.CalculateTriangulation();

        List<(Vector3, Vector3, Vector3)> matchingTriangles = new List<(Vector3, Vector3, Vector3)>();

        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            // Get triangle vertex indices
            int i0 = triangulation.indices[i];
            int i1 = triangulation.indices[i + 1];
            int i2 = triangulation.indices[i + 2];

            // Check area
            int triangleArea = triangulation.areas[i / 3];
            if (triangleArea == areaIndex)
            {
                Vector3 v0 = triangulation.vertices[i0];
                Vector3 v1 = triangulation.vertices[i1];
                Vector3 v2 = triangulation.vertices[i2];
                matchingTriangles.Add((v0, v1, v2));
            }
        }

        if (matchingTriangles.Count == 0)
        {
            return Vector3.zero;
        }

        // Pick a random triangle and sample inside it
        var triangle = matchingTriangles[Random.Range(0, matchingTriangles.Count)];
        Vector3 point = RandomPointInTriangle(triangle.Item1, triangle.Item2, triangle.Item3);
        point.z = 0f; // 2D 强制处理

        return point;
    }
    
    private Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Random.value;
        float r2 = Random.value;

        if (r1 + r2 >= 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }

        return a + r1 * (b - a) + r2 * (c - a);
    }
}
