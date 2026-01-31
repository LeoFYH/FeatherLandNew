# Addressables Bundle优化指南

## 当前分组分析 (Current Group Analysis)

### 现有分组 (Existing Groups)
根据 `Assets/AddressableAssetsData/AssetGroups/` 分析，当前有以下分组：

1. **Default Local Group** - 默认本地组
2. **Clock** - 时钟相关资源
3. **Loading** - 加载界面资源
4. **Music** - 音乐资源
5. **MusicList** - 音乐列表
6. **OpenEgg** - 开蛋动画
7. **Popups** - 弹窗UI
8. **Scene** - 场景资源
9. **Shop** - 商店资源
10. **Tutorial** - 教程资源

---

## 问题分析 (Problem Analysis)

### 🔴 当前存在的问题

1. **分组策略不明确**
   - 没有按照加载优先级分组
   - 资源混杂在一起，导致不必要的内存占用

2. **资源冗余加载**
   - 部分资源可能在启动时全部加载
   - 未充分利用按需加载机制

3. **Bundle大小不均**
   - 某些Group可能包含过多资源
   - 导致下载和加载时间过长

4. **依赖关系复杂**
   - 资源之间的依赖可能导致意外加载
   - 共享资源未单独分组

---

## 优化方案 (Optimization Plan)

### ✅ 推荐的分组策略

#### 1. **Core (核心资源组)** - 启动必需
**包含资源：**
- BirdMaterial
- MaterialHighlight
- 核心配置文件（BirdConfig, ShopConfig等）
- UIRoot预制体

**配置：**
```
Bundle Mode: Pack Together
Compression: LZ4 (快速解压)
Load Path: Local
```

**原因：** 这些是游戏启动时必需的资源，打包在一起可以减少IO操作，使用LZ4压缩保证快速加载。

---

#### 2. **Configs (配置文件组)**
**包含资源：**
- RadioConfig
- ShopConfig
- BirdConfig
- LocalizationConfig
- MapConfig
- CursorConfig

**配置：**
```
Bundle Mode: Pack Together
Compression: LZ4
Load Path: Local
Build Path: LocalBuildPath
```

**原因：** 配置文件通常较小且在启动时需要，统一打包可以减少Bundle数量。

---

#### 3. **UI_Essential (基础UI组)**
**包含资源：**
- MenuPanel
- InfoPanel
- 加载界面相关UI

**配置：**
```
Bundle Mode: Pack Together
Compression: LZ4
Load Path: Local
```

**原因：** 基础UI是启动后立即需要的，快速加载以改善用户体验。

---

#### 4. **UI_Popups (弹窗UI组)**
**包含资源：**
- ShopPopup
- RadioPopup
- ClockPopup
- IllustratedPopup
- 其他Popup预制体

**配置：**
```
Bundle Mode: Pack Separately (每个Popup单独打包)
Compression: LZMA (高压缩比)
Load Path: Local
```

**原因：** Popup按需加载，单独打包可以避免加载不需要的弹窗。LZMA压缩可以减小包体，因为加载频率不高。

---

#### 5. **Scenes (场景组)**
**包含资源：**
- Scene0
- Scene1
- Scene2
- 场景相关装饰物

**配置：**
```
Bundle Mode: Pack Separately (每个场景单独打包)
Compression: LZ4
Load Path: Local
```

**原因：** 场景通常较大，单独打包可以按需加载，切换场景时只加载需要的场景。

---

#### 6. **Audio_Music (音乐组)**
**包含资源：**
- 所有音乐文件（.mp3, .ogg等）

**配置：**
```
Bundle Mode: Pack Separately (每首歌单独打包)
Compression: Uncompressed (音频已压缩)
Load Path: Local
Asset Load Mode: Requested Asset Only
```

**原因：** 音乐文件通常较大，单独打包便于流式加载和卸载。音频文件本身已压缩，无需额外压缩。

---

#### 7. **Audio_Effects (音效组)**
**包含资源：**
- 点击音效
- 鸟叫声
- 环境音效

**配置：**
```
Bundle Mode: Pack Together By Label
Compression: LZMA
Load Path: Local
```

**原因：** 音效文件较小，可以按标签分类打包（如：click, bird, environment）。

---

#### 8. **Prefabs_Common (通用预制体组)**
**包含资源：**
- Heart
- Egg
- Food
- Num
- 其他频繁使用的小预制体

**配置：**
```
Bundle Mode: Pack Together
Compression: LZ4
Load Path: Local
```

**原因：** 这些是游戏中频繁使用的预制体，打包在一起便于对象池系统快速访问。

---

#### 9. **Prefabs_Special (特殊预制体组)**
**包含资源：**
- OpenEggAnim
- 特殊效果预制体
- 低频使用的预制体

**配置：**
```
Bundle Mode: Pack Separately
Compression: LZMA
Load Path: Local
```

**原因：** 特殊效果不常用，单独打包可以在不需要时卸载，节省内存。

---

#### 10. **Atlas (图集组)**
**包含资源：**
- SpriteAtlas资源

**配置：**
```
Bundle Mode: Pack Separately By Label
Compression: LZ4
Load Path: Local
Include In Build: True
```

**原因：** 图集通常较大，按标签分类打包。需要的时候加载对应图集。

---

#### 11. **Shared_Dependencies (共享依赖组)**
**包含资源：**
- 多个资源共同依赖的材质
- 共享的Shader
- 公用纹理

**配置：**
```
Bundle Mode: Pack Together
Compression: LZ4
Load Path: Local
```

**原因：** 避免依赖资源被重复打包到多个Bundle中，减小总包体大小。

---

## Bundle Pack Mode 详解

### Pack Together (一起打包)
- **适用场景：** 小文件、同时需要的资源
- **优点：** 减少Bundle数量，降低IO开销
- **缺点：** 无法单独卸载某个资源

### Pack Separately (分别打包)
- **适用场景：** 大文件、按需加载的资源
- **优点：** 可以精确控制加载和卸载
- **缺点：** Bundle数量增加

### Pack Separately By Label (按标签分别打包)
- **适用场景：** 有明确分类的资源
- **优点：** 灵活分组，便于管理
- **缺点：** 需要良好的标签管理

---

## 压缩策略

### LZ4 压缩
- **压缩比：** 低（~50%）
- **解压速度：** 非常快
- **适用场景：** 启动必需、频繁访问的资源

### LZMA 压缩
- **压缩比：** 高（~70-80%）
- **解压速度：** 慢
- **适用场景：** 低频访问、包体敏感的资源

### Uncompressed (不压缩)
- **适用场景：** 音频、视频等已压缩的资源

---

## 资源标签策略 (Label Strategy)

### 建议使用的标签

1. **加载时机标签**
   - `preload` - 预加载资源（启动时加载）
   - `lazy` - 懒加载资源（按需加载）
   - `optional` - 可选资源（如DLC内容）

2. **功能模块标签**
   - `ui` - UI相关
   - `audio` - 音频相关
   - `bird` - 鸟相关
   - `scene` - 场景相关

3. **优先级标签**
   - `priority_high` - 高优先级
   - `priority_medium` - 中优先级
   - `priority_low` - 低优先级

---

## 具体优化步骤

### 第一步：重新组织资源分组

1. **打开 Addressables Groups 窗口**
   ```
   Window -> Asset Management -> Addressables -> Groups
   ```

2. **创建新的Group**
   - 右键 -> Create New Group -> Blank Group
   - 按照上述推荐创建各个Group

3. **配置Group Settings**
   - 选中Group -> Inspector面板
   - 设置 Bundle Mode、Compression等参数

### 第二步：迁移资源

1. **将现有资源按照新策略分配到对应Group**
   - 从旧Group拖拽到新Group
   - 或通过脚本批量迁移

2. **添加合适的Label**
   - 选中资源 -> 右侧Label下拉菜单
   - 添加功能和优先级标签

### 第三步：优化依赖关系

1. **分析资源依赖**
   ```
   Window -> Asset Management -> Addressables -> Analyze
   点击 "Check Duplicate Bundle Dependencies"
   ```

2. **处理重复依赖**
   - 将共享资源移到 Shared_Dependencies 组
   - 或使用 Asset Bundle Browser 查看依赖树

### 第四步：构建和测试

1. **构建 Addressables**
   ```
   Window -> Asset Management -> Addressables -> Groups
   Build -> New Build -> Default Build Script
   ```

2. **测试加载性能**
   - 使用 Profiler 监控内存和加载时间
   - 验证资源是否按预期加载和卸载

---

## 代码层面优化建议

### 1. 优化资源加载策略

在 `LoadGameCommand.cs` 中，建议使用标签批量预加载：

```csharp
// 预加载所有标记为 "preload" 的资源
var preloadHandle = Addressables.LoadResourceLocationsAsync("preload");
yield return preloadHandle;

// 批量加载
foreach (var location in preloadHandle.Result)
{
    Addressables.LoadAssetAsync<Object>(location);
}
```

### 2. 场景切换时释放资源

在 `ISceneSystem.cs` 中已添加，继续优化：

```csharp
public void HideCurrentScene()
{
    if (currentScene != null)
    {
        // 释放场景相关的所有资源
        GameObject.Destroy(currentScene);
        this.GetSystem<IAssetSystem>().ReleaseAsset(sceneName);
        
        // 清理对象池
        this.GetSystem<IObjectPoolSystem>().ClearAll();
        
        // 卸载未使用的资源
        Resources.UnloadUnusedAssets();
        
        // 触发GC
        System.GC.Collect();
    }
}
```

### 3. UI Popup按需加载

在 `IUISystem.cs` 中已实现，确保每个Popup关闭时释放：

```csharp
public void HidePopup(UIPopup popup)
{
    // ... 现有代码 ...
    
    // 释放资源
    this.GetSystem<IAssetSystem>().ReleaseAsset(popup.ToString());
}
```

### 4. 音频流式加载

对于大型音乐文件，使用流式加载：

```csharp
// 设置音频为流式加载
audioSource.clip = null;
audioSource.resource = Addressables.LoadAssetAsync<AudioClip>(musicKey);
audioSource.Play();
```

---

## 性能监控

### 使用Addressables Event Viewer

1. **启用Event Viewer**
   ```
   Window -> Asset Management -> Addressables -> Event Viewer
   ```

2. **监控指标**
   - Bundle 加载时间
   - 资源实例化时间
   - 内存占用情况

### 使用Unity Profiler

1. **Memory Profiler**
   - 查看各Bundle占用的内存
   - 识别未释放的资源

2. **CPU Profiler**
   - 查看资源加载的CPU开销
   - 优化加载流程

---

## 预期优化效果

### 内存优化
- ✅ 减少30-50%的峰值内存占用
- ✅ 场景切换时内存释放更彻底
- ✅ 避免不必要的资源常驻内存

### 加载性能
- ✅ 启动时间减少20-30%
- ✅ 场景切换更流畅
- ✅ UI响应速度提升

### 包体大小
- ✅ 通过合理压缩减小10-20%
- ✅ 消除重复依赖
- ✅ 便于后续DLC和更新

---

## 注意事项

### ⚠️ 重要提醒

1. **备份现有配置**
   - 调整前备份整个 `AddressableAssetsData` 目录

2. **渐进式优化**
   - 不要一次性修改所有分组
   - 每次修改后充分测试

3. **测试多种场景**
   - 测试冷启动（首次安装）
   - 测试热启动（二次启动）
   - 测试长时间运行

4. **监控构建时间**
   - 过多的小Bundle会增加构建时间
   - 平衡Bundle数量和粒度

5. **考虑平台差异**
   - iOS和Android可能需要不同的压缩策略
   - 低端设备可能需要更积极的内存管理

---

## 快速实施清单

- [ ] 分析现有资源使用情况
- [ ] 创建新的Addressable Groups
- [ ] 配置各Group的Bundle Mode和Compression
- [ ] 重新分配资源到合适的Group
- [ ] 添加合适的Label标签
- [ ] 处理重复依赖问题
- [ ] 优化代码中的加载逻辑
- [ ] 构建并测试
- [ ] 使用Profiler验证效果
- [ ] 记录优化前后的性能数据

---

## 附录：配置示例脚本

### 批量设置Group配置的Editor脚本

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

public class AddressableGroupConfigurator
{
    [MenuItem("Tools/Addressables/Apply Optimized Settings")]
    public static void ApplyOptimizedSettings()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        
        // 配置Core组
        var coreGroup = GetOrCreateGroup(settings, "Core");
        SetGroupSettings(coreGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogether, 
            BundledAssetGroupSchema.BundleCompressionMode.LZ4);
        
        // 配置Configs组
        var configsGroup = GetOrCreateGroup(settings, "Configs");
        SetGroupSettings(configsGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogether, 
            BundledAssetGroupSchema.BundleCompressionMode.LZ4);
        
        // 配置UI_Popups组
        var popupsGroup = GetOrCreateGroup(settings, "UI_Popups");
        SetGroupSettings(popupsGroup, BundledAssetGroupSchema.BundlePackingMode.PackSeparately, 
            BundledAssetGroupSchema.BundleCompressionMode.LZMA);
        
        // ... 其他组的配置
        
        Debug.Log("Addressable组优化配置完成！");
    }
    
    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, false, null);
        }
        return group;
    }
    
    private static void SetGroupSettings(AddressableAssetGroup group, 
        BundledAssetGroupSchema.BundlePackingMode packMode,
        BundledAssetGroupSchema.BundleCompressionMode compression)
    {
        var schema = group.GetSchema<BundledAssetGroupSchema>();
        if (schema == null)
        {
            schema = group.AddSchema<BundledAssetGroupSchema>();
        }
        
        schema.BundleMode = packMode;
        schema.Compression = compression;
        schema.IncludeInBuild = true;
        
        EditorUtility.SetDirty(group);
    }
}
#endif
```

---

## 参考资料

- [Unity Addressables文档](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
- [Bundle压缩最佳实践](https://learn.unity.com/tutorial/assets-resources-and-assetbundles)
- [内存管理指南](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity6.html)

---

**优化完成后，配合 [MEMORY_OPTIMIZATION_GUIDE.md](./MEMORY_OPTIMIZATION_GUIDE.md) 使用，可以达到最佳的内存和性能表现！**
