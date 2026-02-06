using UnityEngine;
using QFramework;
using BirdGame;

/// <summary>
/// 鸟方向测试脚本
/// 用于验证鸟在不同状态下方向判断的正确性
/// </summary>
public class BirdDirectionTest : ViewControllerBase
{
    [Header("测试设置")]
    public bool enableDebugLog = true;
    public float logInterval = 1.0f;
    
    private float lastLogTime = 0f;
    private Brid targetBird;
    
    void Start()
    {
        // 查找场景中的鸟
        FindTargetBird();
    }
    
    void Update()
    {
        if (!enableDebugLog) return;
        
        // 定期输出日志
        if (Time.time - lastLogTime >= logInterval)
        {
            LogBirdDirectionInfo();
            lastLogTime = Time.time;
        }
    }
    
    private void FindTargetBird()
    {
        // 尝试找到第一个鸟
        var birds = FindObjectsOfType<Brid>();
        if (birds.Length > 0)
        {
            targetBird = birds[0];
            Debug.Log($"找到测试目标鸟: {targetBird.name}, 索引: {targetBird.birdIndex}");
        }
        else
        {
            Debug.LogWarning("场景中未找到鸟对象");
        }
    }
    
    private void LogBirdDirectionInfo()
    {
        if (targetBird == null) return;
        
        var stateMachine = targetBird.GetType().GetField("_stateMachine", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(targetBird) as StateMachine;
            
        if (stateMachine == null) return;
        
        string currentStateName = stateMachine.CurrentState?.Name ?? "Unknown";
        string foodInfo = targetBird.currFood != null ? 
            $"食物位置: {targetBird.currFood.transform.position.x:F2}" : "无食物";
            
        Debug.Log($"=== 鸟方向测试 ===\n" +
                  $"状态: {currentStateName}\n" +
                  $"位置: {targetBird.transform.position.x:F2}\n" +
                  $"{foodInfo}\n" +
                  $"Sprite翻转: {targetBird.sr.flipX}\n" +
                  $"速度: {targetBird.agent.velocity.x:F3}\n" +
                  $"剩余距离: {targetBird.agent.remainingDistance:F3}\n" +
                  $"===================");
    }
    
    [ContextMenu("立即测试方向")]
    public void TestDirectionNow()
    {
        LogBirdDirectionInfo();
    }
    
    [ContextMenu("查找所有鸟")]
    public void FindAllBirds()
    {
        var birds = FindObjectsOfType<Brid>();
        Debug.Log($"场景中共找到 {birds.Length} 只鸟:");
        for (int i = 0; i < birds.Length; i++)
        {
            Debug.Log($"  {i+1}. {birds[i].name} (索引: {birds[i].birdIndex})");
        }
    }
}