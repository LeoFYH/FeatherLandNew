using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class WalkableArea : MonoBehaviour
{
    private PolygonCollider2D areaCollider;
    
    [Header("调试设置")]
    public bool showDebugGizmos = true;  // 是否在Scene视图中显示调试图形
    public Color gizmosColor = new Color(0, 1, 0, 0.2f);  // 调试区域的颜色

    private void Awake()
    {
        areaCollider = GetComponent<PolygonCollider2D>();
    }

    // 检查点是否在可行走区域内
    public bool IsPointInside(Vector2 point)
    {
        return areaCollider.OverlapPoint(point);
    }

    // 获取区域内的随机点
    public Vector2 GetRandomPoint(Vector2 center, float radius)
    {
        Vector2 point;
        int attempts = 0;
        const int MAX_ATTEMPTS = 30;

        do
        {
            // 在圆形范围内随机取点
            float randomRadius = radius * Mathf.Sqrt(Random.value);
            float randomAngle = Random.value * 2 * Mathf.PI;
            
            float x = center.x + randomRadius * Mathf.Cos(randomAngle);
            float y = center.y + randomRadius * Mathf.Sin(randomAngle);
            
            point = new Vector2(x, y);
            attempts++;

            // 如果尝试太多次还找不到合适的点，就返回中心点
            if (attempts >= MAX_ATTEMPTS)
            {
                return center;
            }
        } 
        while (!IsPointInside(point));

        return point;
    }

    // 获取最近的有效点（如果点在区域外，返回区域内最近的点）
    public Vector2 GetClosestValidPoint(Vector2 point)
    {
        if (IsPointInside(point))
        {
            return point;
        }

        // 获取碰撞器的所有点
        Vector2[] points = areaCollider.points;
        Vector2 closestPoint = transform.TransformPoint(points[0]);
        float minDistance = Vector2.Distance(point, closestPoint);

        // 遍历所有边，找到最近的点
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = transform.TransformPoint(points[i]);
            Vector2 b = transform.TransformPoint(points[(i + 1) % points.Length]);
            Vector2 nearestPoint = GetNearestPointOnLine(a, b, point);
            
            float distance = Vector2.Distance(point, nearestPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = nearestPoint;
            }
        }

        return closestPoint;
    }

    // 获取线段上最近的点
    private Vector2 GetNearestPointOnLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.magnitude;
        line.Normalize();

        Vector2 pointToStart = point - lineStart;
        float dot = Vector2.Dot(pointToStart, line);

        if (dot <= 0)
            return lineStart;
        if (dot >= lineLength)
            return lineEnd;

        return lineStart + line * dot;
    }

    // 在Scene视图中显示区域
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || areaCollider == null) return;

        Gizmos.color = gizmosColor;
        Vector2[] points = areaCollider.points;
        
        // 填充多边形区域
        Vector3[] vertices = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = transform.TransformPoint(points[i]);
        }

        for (int i = 1; i < points.Length - 1; i++)
        {
            Gizmos.DrawLine(vertices[0], vertices[i]);
            Gizmos.DrawLine(vertices[i], vertices[i + 1]);
        }
        Gizmos.DrawLine(vertices[points.Length - 1], vertices[0]);
    }

    // 获取区域内的完全随机点
    public Vector2 GetRandomPointInArea()
    {
        // 获取碰撞器的边界
        Bounds bounds = areaCollider.bounds;
        Vector2 point;
        int attempts = 0;
        const int MAX_ATTEMPTS = 30;

        do
        {
            // 在边界盒内取随机点
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            point = new Vector2(x, y);
            attempts++;

            if (attempts >= MAX_ATTEMPTS)
            {
                // 如果找不到合适的点，返回区域中心
                return new Vector2(bounds.center.x, bounds.center.y);
            }
        } 
        while (!IsPointInside(point));

        return point;
    }

    // 获取区域的边界
    public Bounds GetAreaBounds()
    {
        return areaCollider.bounds;
    }
}