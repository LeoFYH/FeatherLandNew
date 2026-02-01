# Prefab 图集管理与URP内存优化使用指南

## 概述

此功能允许您为项目中的 Prefab 添加一个脚本，该脚本能够自动管理关联图集的加载和卸载，并通过 AssetSystem 进行引用计数管理。这确保了资源的有效使用，避免了不必要的内存占用。同时包含URP渲染管线的内存优化功能。

## 主要组件

### 1. PrefabAtlasManager.cs

这个脚本可以直接添加到任何 Prefab 上，具有以下功能：
- 在 Prefab 启动时自动加载关联的图集
- 在 Prefab 销毁时自动释放图集
- 通过 AssetSystem 管理图集的引用计数

### 2. SpriteAtlasApplier.cs

这是一个专门用于将精灵应用到UI组件的辅助脚本：
- 可以将加载的精灵直接应用到Image或SpriteRenderer组件
- 更精确的精灵控制
- 适合单一精灵的显示需求

### 3. DelayedPrefabLoader.cs

延迟加载方案，先加载图集再加载预制体：
- 无需逐个绑定精灵
- 确保图集就绪后再实例化预制体
- 适合包含大量精灵的预制体

### 4. AtlasBasedPrefabLoader.cs

批量图集加载方案，支持多个图集：
- 可以同时加载多个必需的图集
- 确保所有图集都就绪后再加载预制体
- 适合复杂的UI界面或场景

### 5. URPMemoryOptimizer.cs

URP内存优化管理器：
- 优化阴影设置以减少内存使用
- 管理渲染纹理池
- 优化后期处理效果
- 适用于URP渲染管线的内存优化

### 6. MemoryOptimizationManager.cs

综合内存优化管理器：
- 结合URP和图集资源的内存管理
- 自动监控内存使用
- 定期清理未使用的资源
- 提供全面的内存优化功能

### 7. AssetSystem 扩展

我们在 AssetSystem 中添加了两个新方法：
- `AddAtlasReference(string atlasGuid)` - 增加图集引用计数
- `RemoveAtlasReference(string atlasGuid)` - 减少图集引用计数，当计数为0时自动释放图集

## 如何使用

### 方案 1: 使用 PrefabAtlasManager (适用于多个精灵的Prefab)

1. 在 Unity 编辑器中选择您的 Prefab
2. 在 Inspector 面板中点击 "Add Component"
3. 搜索并添加 "Prefab Atlas Manager" 组件
4. 配置 "Atlas Reference" 字段，拖入您的图集 AssetReference
5. 在 "Sprite Names" 数组中输入 Prefab 使用的所有精灵名称
6. 确保 Prefab 上有 Image 或 SpriteRenderer 组件用于显示精灵

### 方案 2: 使用 SpriteAtlasApplier (适用于单一精灵的显示)

1. 在需要显示精灵的游戏对象上添加 "Sprite Atlas Applier" 组件
2. 设置 "Sprite Name" - 精灵的名称
3. 设置 "Atlas Reference" - 图集的 AssetReference
4. 设置 "Target Image" 或 "Target Sprite Renderer" - 接收精灵的目标组件

### 方案 3: 使用 DelayedPrefabLoader (推荐 - 无需逐个绑定)

1. 创建一个空的游戏对象，添加 "Delayed Prefab Loader" 组件
2. 设置 "Atlas Reference" - 预制体所需的图集
3. 设置 "Prefab Reference" - 要加载的预制体
4. 设置 "Parent Transform" (可选) - 预制体实例化的父级
5. 运行时此组件会先加载图集，图集加载完成后再加载预制体

### 方案 4: 使用 AtlasBasedPrefabLoader (适用于多图集场景)

1. 创建一个空的游戏对象，添加 "Atlas Based Prefab Loader" 组件
2. 在 "Required Atlases" 列表中添加所有必需的图集
3. 设置 "Prefab Reference" - 要加载的预制体
4. 设置 "Parent Transform" (可选) - 预制体实例化的父级
5. 运行时此组件会先加载所有必需的图集，然后再加载预制体

### URP内存优化设置

1. 在主场景中添加 "URP Memory Optimizer" 或 "Memory Optimization Manager" 组件
2. 根据项目需求调整阴影质量、渲染纹理池大小等设置
3. 启用自动资源清理功能

## URP内存优化策略

### 1. 阴影优化
- 降低阴影质量设置
- 减少阴影距离
- 使用较低分辨率的阴影贴图

### 2. 渲染纹理优化
- 启用渲染目标池
- 控制渲染纹理池大小
- 定期清理未使用的渲染纹理

### 3. 后期处理优化
- 禁用不必要的后期处理效果
- 降低后期处理质量
- 避免过多的后期处理叠加

### 4. 资源管理优化
- 使用图集减少Draw Call
- 实现资源预加载和及时释放
- 定期清理未使用的资源

## 运行时行为

- 当使用延迟加载方案时，会先确保图集完全加载
- 然后才会加载和实例化预制体
- 预制体中的精灵会自动从已加载的图集中获取
- 当预制体被销毁时，组件会自动减少图集的引用计数
- 当图集的引用计数为 0 时，AssetSystem 会自动释放图集资源
- 内存优化管理器会定期监控和清理内存

## 解决图片不显示的常见问题

1. **确保精灵名称正确** - 精灵名称必须与图集中的实际精灵名称完全一致
2. **检查图集是否在Addressables中** - 图集必须被正确标记为Addressable资源
3. **验证组件引用** - 确保Image或SpriteRenderer组件被正确引用
4. **检查精灵路径** - 如果使用路径查找，请确保路径格式正确
5. **使用延迟加载方案** - 避免图集未就绪就尝试显示精灵

## 最佳实践

1. **对于复杂预制体，使用延迟加载方案** - 无需逐个绑定精灵
2. **只在需要图集的 Prefab 上添加相应组件**
3. **准确填写精灵名称**，以确保资源管理的有效性
4. **合理组织图集**，将经常一起使用的精灵放在同一个图集中
5. **对于复杂的UI，使用 AtlasBasedPrefabLoader 来确保所有必需图集都加载完成**
6. **在URP项目中启用内存优化管理器**
7. **测试内存使用情况**，确保图集管理按预期工作

## 注意事项

- 确保您的图集已在 Addressables 系统中正确配置
- 此系统依赖于 AssetSystem 的引用计数机制
- 避免在运行时频繁创建和销毁带有图集管理的 Prefab，以免造成性能问题
- 精灵名称必须与图集中的实际名称完全匹配
- 延迟加载方案可以有效避免图集未就绪导致的显示问题
- URP内存优化需要根据目标平台性能进行调整