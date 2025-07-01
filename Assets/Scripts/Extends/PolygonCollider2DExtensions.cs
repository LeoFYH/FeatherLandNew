using UnityEngine;
using System.Collections.Generic;

public static class PolygonCollider2DExtensions
{
    // 主方法：获取随机点
    public static Vector2 GetRandomPoint(this PolygonCollider2D polygon)
    {
        Vector2[] worldPoints = GetWorldPoints(polygon);
        
        if (worldPoints.Length < 3)
            return polygon.bounds.center; // 非多边形时返回中心
        
        // 计算中心点（凸多边形内部点）
        Vector2 center = CalculateCentroid(worldPoints);
        
        // 将多边形分解为三角形
        List<Vector2[]> triangles = CreateTriangles(worldPoints, center);
        
        // 按面积权重随机选择三角形
        Vector2[] randomTriangle = SelectRandomTriangle(triangles);
        
        // 在选中的三角形内生成随机点
        return RandomPointInTriangle(randomTriangle[0], randomTriangle[1], randomTriangle[2]);
    }

    // 获取世界坐标下的顶点
    private static Vector2[] GetWorldPoints(PolygonCollider2D polygon)
    {
        Vector2[] localPoints = polygon.points;
        Vector2[] worldPoints = new Vector2[localPoints.Length];
        for (int i = 0; i < localPoints.Length; i++)
        {
            worldPoints[i] = polygon.transform.TransformPoint(localPoints[i]);
        }
        return worldPoints;
    }

    // 计算多边形质心
    private static Vector2 CalculateCentroid(Vector2[] points)
    {
        Vector2 centroid = Vector2.zero;
        foreach (Vector2 point in points)
        {
            centroid += point;
        }
        return centroid / points.Length;
    }

    // 创建三角形列表（质心+每对顶点）
    private static List<Vector2[]> CreateTriangles(Vector2[] points, Vector2 center)
    {
        List<Vector2[]> triangles = new List<Vector2[]>();
        for (int i = 0; i < points.Length; i++)
        {
            int nextIndex = (i + 1) % points.Length;
            triangles.Add(new Vector2[] { center, points[i], points[nextIndex] });
        }
        return triangles;
    }

    // 按面积权重随机选择三角形
    private static Vector2[] SelectRandomTriangle(List<Vector2[]> triangles)
    {
        List<float> areas = new List<float>();
        float totalArea = 0f;

        // 计算每个三角形的面积
        foreach (Vector2[] triangle in triangles)
        {
            float area = CalculateTriangleArea(triangle);
            areas.Add(area);
            totalArea += area;
        }

        // 加权随机选择
        float randomValue = Random.Range(0f, totalArea);
        float cumulative = 0f;
        
        for (int i = 0; i < triangles.Count; i++)
        {
            cumulative += areas[i];
            if (randomValue <= cumulative)
            {
                return triangles[i];
            }
        }

        return triangles[triangles.Count - 1]; // 备用
    }

    // 计算三角形面积
    private static float CalculateTriangleArea(Vector2[] triangle)
    {
        return Mathf.Abs(
            triangle[0].x * (triangle[1].y - triangle[2].y) +
            triangle[1].x * (triangle[2].y - triangle[0].y) +
            triangle[2].x * (triangle[0].y - triangle[1].y)
        ) / 2f;
    }

    // 在三角形内生成随机点
    private static Vector2 RandomPointInTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        float r1 = Random.Range(0f, 1f);
        float r2 = Random.Range(0f, 1f);
        
        if (r1 + r2 > 1f)
        {
            r1 = 1f - r1;
            r2 = 1f - r2;
        }
        
        return a + r1 * (b - a) + r2 * (c - a);
    }
}