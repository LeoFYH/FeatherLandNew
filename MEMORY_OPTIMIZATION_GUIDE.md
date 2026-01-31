# 内存优化指南 (Memory Optimization Guide)

## 概述 (Overview)

本项目已实施全面的内存优化措施，在不破坏原有QFramework架构的前提下，显著降低运行时内存占用。

This project has implemented comprehensive memory optimization measures that significantly reduce runtime memory usage without breaking the existing QFramework architecture.

---

## 已实施的优化 (Implemented Optimizations)

### 1. 资源管理系统优化 (AssetSystem Optimization)

**位置 (Location):** `Assets/Scripts/New/Systems/IAssetSystem.cs`

**优化内容 (Optimizations):**
- ✅ **引用计数系统**: 为所有加载的资源添加引用计数，防止资源被过早释放或一直占用内存
- ✅ **自动资源清理**: 每分钟检查一次，自动释放5分钟未使用的资源
- ✅ **缓存大小限制**: 最多缓存100个资源，超过限制时自动清理最旧的资源
- ✅ **最后访问时间追踪**: 记录每个资源的最后访问时间，用于智能清理

**效果 (Impact):**
- 减少长期运行时的内存泄漏
- 自动管理资源生命周期
- 防止资源无限制累积

---

### 2. 对象池系统优化 (ObjectPoolSystem Optimization)

**位置 (Location):** `Assets/Scripts/New/Systems/IObjectPoolSystem.cs`

**优化内容 (Optimizations):**
- ✅ **池大小限制**: 每个对象池最多保留20个非活跃对象
- ✅ **定期清理**: 每2分钟自动清理对象池，保留一半对象，销毁另一半
- ✅ **超出限制销毁**: 当对象回收时，如果池已满，直接销毁对象而不是保留
- ✅ **垃圾回收触发**: 清理后主动触发GC，及时释放内存

**效果 (Impact):**
- 防止对象池无限增长
- 减少非活跃对象占用的内存
- 定期回收内存

---

### 3. DOTween动画清理 (DOTween Cleanup)

**位置 (Location):** `Assets/Scripts/New/ViewController/Game/Brid.cs`

**优化内容 (Optimizations):**
- ✅ **OnDestroy清理**: 在鸟对象销毁时，自动清理所有DOTween动画
- ✅ **Transform和SpriteRenderer清理**: 清理所有相关的Tween动画
- ✅ **状态机清理**: 释放状态机引用
- ✅ **对象池回收**: 确保心形特效等对象被正确回收

**效果 (Impact):**
- 防止动画内存泄漏
- 避免已销毁对象的动画继续运行
- 及时释放Tween占用的内存

---

### 4. 内存优化系统 (MemoryOptimizationSystem)

**位置 (Location):** `Assets/Scripts/New/Systems/IMemoryOptimizationSystem.cs`

**新增功能 (New Features):**
- ✅ **手动内存清理**: `TriggerMemoryCleanup()` 可随时手动触发清理
- ✅ **内存使用监控**: `GetCurrentMemoryUsage()` 实时监控内存使用情况（MB）
- ✅ **自动优化**: 每5分钟自动检查内存，超过500MB时自动清理
- ✅ **纹理内存优化**: 启用Mipmap流式加载，限制纹理内存为256MB
- ✅ **强制垃圾回收**: 深度GC清理（三次连续GC）

**使用方法 (Usage):**
```csharp
// 手动触发内存清理
this.GetSystem<IMemoryOptimizationSystem>().TriggerMemoryCleanup();

// 获取当前内存使用
float memoryMB = this.GetSystem<IMemoryOptimizationSystem>().GetCurrentMemoryUsage();
Debug.Log($"当前内存: {memoryMB:F2} MB");

// 启用/禁用自动优化
this.GetSystem<IMemoryOptimizationSystem>().SetAutoOptimization(true);
```

---

### 5. 场景切换优化 (Scene Transition Optimization)

**位置 (Location):** `Assets/Scripts/New/Systems/ISceneSystem.cs`

**优化内容 (Optimizations):**
- ✅ **场景切换前清理**: 在加载新场景前触发内存清理
- ✅ **清理所有对象池**: 切换场景时清空所有对象池
- ✅ **释放旧场景资源**: 正确释放旧场景的Addressable资源

**效果 (Impact):**
- 防止场景切换时内存累积
- 确保旧场景资源被完全释放
- 为新场景腾出足够内存

---

### 6. 游戏入口优化 (GameEntry Optimization)

**位置 (Location):** `Assets/Scripts/New/ViewController/Game/GameEntry.cs`

**优化内容 (Optimizations):**
- ✅ **启动时应用纹理优化**: 游戏启动时自动应用纹理内存优化设置
- ✅ **记录初始内存**: 在Console中输出游戏启动时的内存使用
- ✅ **退出时清理**: 游戏退出前触发一次完整的内存清理

---

## 性能优化参数 (Performance Parameters)

| 参数 | 默认值 | 说明 |
|------|--------|------|
| 资源自动释放时间 | 300秒 (5分钟) | 资源超过此时间未使用会被自动释放 |
| 最大缓存资源数 | 100个 | 超过此数量会强制清理最旧资源 |
| 对象池最大大小 | 20个/池 | 每个对象池最多保留的非活跃对象数 |
| 对象池清理间隔 | 120秒 (2分钟) | 定期清理对象池的时间间隔 |
| 内存自动清理间隔 | 300秒 (5分钟) | 自动检查内存并清理的时间间隔 |
| 内存清理阈值 | 500 MB | 超过此阈值触发自动清理 |
| 纹理内存预算 | 256 MB | 纹理Mipmap流式加载的内存限制 |

---

## 使用建议 (Usage Recommendations)

### 1. 长时间运行的游戏
如果游戏需要长时间运行（超过1小时），建议：
```csharp
// 每30分钟手动触发一次清理
InvokeRepeating("ManualCleanup", 1800f, 1800f);

void ManualCleanup()
{
    this.GetSystem<IMemoryOptimizationSystem>().TriggerMemoryCleanup();
}
```

### 2. 内存敏感的场景
在切换到内存密集型场景前：
```csharp
// 提前清理内存
this.GetSystem<IMemoryOptimizationSystem>().TriggerMemoryCleanup();
yield return new WaitForSeconds(0.5f); // 等待清理完成
// 然后加载新场景
this.GetSystem<ISceneSystem>().LoadScene(sceneIndex);
```

### 3. 监控内存使用
在开发阶段，可以定期监控内存：
```csharp
#if UNITY_EDITOR
void Update()
{
    if (Input.GetKeyDown(KeyCode.M))
    {
        float memory = this.GetSystem<IMemoryOptimizationSystem>().GetCurrentMemoryUsage();
        Debug.Log($"[内存监控] 当前使用: {memory:F2} MB");
    }
}
#endif
```

---

## 进一步优化建议 (Further Optimization Suggestions)

### 1. 纹理压缩设置
建议检查并优化所有纹理资源的导入设置：
- 启用纹理压缩（ASTC、ETC2等）
- 降低不必要的纹理分辨率
- 使用Sprite Atlas合并小图标

### 2. 动画剪辑优化
对于大量动画帧：
- 考虑使用Sprite Atlas
- 减少不必要的动画帧
- 使用压缩的动画格式

### 3. Addressables优化
- 合理设置资源的加载和卸载策略
- 使用Asset Bundle分组管理资源
- 避免将所有资源标记为preload

### 4. 音频优化
- 使用压缩的音频格式
- 大型音频文件使用流式加载
- 不要同时加载过多音频资源

---

## 监控和调试 (Monitoring and Debugging)

### Unity Profiler
使用Unity Profiler监控内存使用：
1. Window -> Analysis -> Profiler
2. 选择Memory模块
3. 查看详细的内存分配

### Console日志
优化系统会自动输出日志：
```
[INFO] 游戏启动时内存使用: 245.67 MB
[INFO] 自动清理未使用资源: OpenEggAnim
[INFO] 清理对象池 Heart，销毁了 15 个非活跃对象
[INFO] === 开始手动内存清理 ===
[INFO] 清理前内存使用: 523.45 MB
[INFO] 清理后内存使用: 412.89 MB
[INFO] 释放内存: 110.56 MB
```

---

## 注意事项 (Notes)

1. **不要频繁手动清理**: 过于频繁的清理会影响性能，建议使用自动清理机制
2. **资源引用计数**: 确保在不需要资源时调用`ReleaseAsset()`
3. **对象池使用**: 尽量使用对象池系统，避免频繁Instantiate和Destroy
4. **DOTween清理**: 所有使用DOTween的脚本在OnDestroy时都应该调用Kill()
5. **测试验证**: 优化后请进行充分测试，确保不影响游戏正常运行

---

## 版本历史 (Version History)

### v1.0 (2026-01-30)
- ✅ 实施资源管理系统优化
- ✅ 实施对象池系统优化
- ✅ 添加DOTween清理机制
- ✅ 创建内存优化系统
- ✅ 优化场景切换流程
- ✅ 优化游戏入口点

---

## 联系和支持 (Contact and Support)

如有问题或建议，请查看项目文档或联系开发团队。

For questions or suggestions, please refer to the project documentation or contact the development team.
