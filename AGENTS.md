# FeatherLandNew — AI 代理项目指南

> 说明：此前项目根目录没有 `AGENTS.md`，本文件是根据当前仓库实际内容新建的代理可读文档。项目内的注释、文档和提交日志主要使用中文，因此本文件以中文撰写，技术术语保留英文。

---

## 1. 项目概述

- **项目名称 / 产品名**：`FeatherLandNew`（运行时产品名 `featherlandunit`，见 `ProjectSettings/ProjectSettings.asset`）。
- **项目类型**：Unity 2D 休闲桌面宠物/养成类游戏，核心玩法围绕鸟类饲养、场景装饰、天气切换、番茄钟、音乐电台等。
- **目标平台**：当前主要构建为 **Standalone Windows x64**，同时包含 **macOS** 壁纸模式所需的 native 插件与构建后处理。
- **主要功能特性**：
  - 多只鸟的状态机 AI（Idle / Eat / Fly / Run 等）。
  - 多语言本地化（11 种语言）。
  - Steamworks.NET 集成（成就、统计、用户语言）。
  - 窗口 / 全屏 / 壁纸三种屏幕模式（Win32 + macOS bridge）。
  - Addressables 资源分包与按需加载。

---

## 2. 技术栈与关键配置

### 2.1 Unity 与核心渲染

- **Unity 版本**：`6000.0.58f2`（见 `ProjectSettings/ProjectVersion.txt`）。
- **渲染管线**：Universal Render Pipeline（URP）`17.0.4`，Linear 颜色空间，2D 模板。
- **默认分辨率**：1920×1080，可调整窗口，打包时默认全屏窗口模式。

### 2.2 关键 Unity 包（`Packages/manifest.json`）

| 包 | 版本 / 来源 | 用途 |
|---|---|---|
| `com.unity.addressables` | `2.7.2` | 资源分包、按需加载 |
| `com.unity.render-pipelines.universal` | `17.0.4` | URP |
| `com.unity.inputsystem` | `1.14.2` | 输入系统 |
| `com.unity.ai.navigation` | `2.0.9` | NavMesh（2D 配合 NavMeshPlus） |
| `com.unity.test-framework` | `1.5.1` | 测试框架（项目中暂无自定义测试） |
| `com.unity.memoryprofiler` | `1.1.9` | 内存分析 |
| `com.unity.nuget.newtonsoft-json` | `3.2.1` | JSON |
| `com.unity.timeline` / `ugui` / `visualscripting` | 内置 | 动画、UI、可视化脚本 |
| `com.h8man.2d.navmeshplus` | Git | 2D NavMesh 扩展 |
| `com.coffee.ui-effect` | 本地 `UIEffect-5.10.7/Packages/src` | UI 特效 |
| `com.gamelovers.mcp-unity` | Git | 编辑器 MCP 服务（端口 8090，默认自动启动） |
| `com.unity.ide.cursor` | Git | Cursor IDE 支持 |

### 2.3 第三方库 / 本地资源

- **QFramework**：`Assets/QFramework/Framework/Scripts/QFramework.asmdef`，项目主架构（MVC/命令/事件）。
- **UniTask**：`Assets/Plugins/UniTask`，版本 `2.5.10`，用于异步加载与协程替代。
- **DOTween**：`Assets/Plugins/Demigiant/DOTween`，UI 与鸟动画补间。
- **Odin Inspector**：`Assets/Plugins/Sirenix/Odin Inspector`，编辑器窗口与配置可视化。
- **Steamworks.NET**：`Assets/com.rlabrecque.steamworks.net`，版本 `2025.161.0`。
- **OutlineFx**：`Assets/Outline/`，自定义 outline shader。
- **HeathenEngineering.UX**：`Assets/_Heathen Engineering/Assets/UX/`。
- **macOS Wallpaper Bridge**：`Assets/Plugins/macOS/FLWallpaperBridge.bundle`，由构建后处理编译或复制。

### 2.4 关键项目文件

- `Packages/manifest.json` / `packages-lock.json`：包依赖。
- `ProjectSettings/ProjectVersion.txt`：Unity 版本。
- `ProjectSettings/ProjectSettings.asset`：玩家设置（产品名、版本、颜色空间等）。
- `ProjectSettings/EditorBuildSettings.asset`：构建场景列表。
- `ProjectSettings/GraphicsSettings.asset`：URP 全局设置。
- `FeatherLandNew.sln` / 多个 `.csproj`：由 Unity 生成的 C# 解决方案。
- `steam_appid.txt`：Steam AppID（当前为 `3975050`）。

---

## 3. 代码组织与模块划分

### 3.1 顶层目录

```
Assets/
├── Scripts/              # C# 脚本
│   ├── New/              # 基于 QFramework 的新架构（推荐在此开发）
│   ├── BirdStates/       # 鸟状态脚本（Idle、Eat、Fly 等）
│   ├── FSM/              # 通用有限状态机
│   ├── Managers/         # 旧版单例 Manager（保留但已较少使用）
│   ├── Notebook/         # 记事本相关旧脚本
│   ├── Radio/            # 电台相关旧脚本
│   ├── Steamworks.NET/   # SteamManager（来自 Steamworks.NET）
│   └── UI/               # 旧版 UI 脚本
├── Prefabs/              # 预制体（鸟、场景、UI、配置、装饰）
├── Arts/                 # 美术资源（按鸟类、场景、天气、UI 分目录）
├── Audios/               # 音效与背景音乐
├── Fonts/                # 多语言 TMP 字体
├── Animations/           # 动画资源
├── Scenes/               # 场景文件
├── Editor/               # 编辑器扩展工具
├── AddressableAssetsData/# Addressables 分组与 Schema
└── Plugins/              # 第三方插件与 native 库
```

### 3.2 新架构 `Assets/Scripts/New/`

采用 **QFramework** 的 `Architecture<T>` 模式，命名空间统一为 `BirdGame`；编辑器工具为 `BirdGame.Editor` / `BirdGame.EditorTools`。

| 目录 | 职责 |
|---|---|
| `GameApp.cs` | 全局架构入口，注册所有 Model / System / Utility |
| `Models/` | 数据层接口与实现（`IConfigModel`、`ISaveModel`、`IBirdModel` 等） |
| `Systems/` | 业务系统层（`IAssetSystem`、`IUISystem`、`IBirdSystem`、`IAudioSystem`、`ISaveSystem` 等） |
| `Commands/` | 一次性命令（`LoadGameCommand`、`EnterDesktopCommand`、`SpawnBirdCommand` 等） |
| `Events/` | 轻量级事件结构体，用于模块间解耦 |
| `ViewController/` | MonoBehaviour 视图层 |
| `ViewController/UI/Panels/` | 主界面面板 |
| `ViewController/UI/Popups/` | 弹窗 |
| `ViewController/UI/Radio/` | 电台 UI |
| `ViewController/Game/` | 场景内游戏对象（鸟、蛋、食物、装饰、天气等） |
| `ViewController/Debug/` | 编辑器调试面板 |
| `Config/` | 配置 ScriptableObject 类（`BirdConfig`、`ShopConfig`、`MapConfig`、`RadioConfig`、`LocalizationConfig`） |
| `Editor/` | 项目特化编辑器工具（Excel 导入、本地化、纹理工具等） |
| `Utility/` | 工具类（Atlas 管理、全屏/壁纸模式、预制体加载等） |

### 3.3 鸟的行为模块

- 核心脚本：`Assets/Scripts/New/ViewController/Game/Brid.cs`
- 状态机：`Assets/Scripts/FSM/StateMachine.cs` + `Assets/Scripts/BirdStates/Bird*State.cs`
- 状态包括：Idle、Eat、Fly、FlyDown、FlyHorizontal、FlyWait、Run、HatchingEgg 等。

### 3.4 资源与 UI 模块

- **Addressables 分组**：Core、Configs、UI_Essential、UI_Popups、Scenes、Audio_Music、Audio_Effects、Prefabs_Common、Prefabs_Special、Atlas、Shared_Dependencies、Birds。
- **常用标签**：`preload`、`popup`、`config`、`scene`、`music`、`effect`、`Atlas`。
- **UI 基类**：`UIBase : ViewControllerBase`，使用 DOTween 做缩放/淡入淡出，弹窗关闭时 `Destroy(gameObject)`。

---

## 4. 运行架构

### 4.1 启动流程

1. **入口场景**：`Assets/Scenes/B8_Crow.unity`（Build Settings 中索引 0）。
2. 场景中的 `GameEntry`（要求场景中有一个名为 `GameEntry` 的 GameObject）在 `Start()` 中调用：
   - `Architecture<GameApp>.Interface` 自动初始化（`GameApp.Init()` 注册所有 Model / System / Utility）。
   - `SendCommand<LoadGameCommand>()` 开始加载。
3. **LoadGameCommand 流程**：
   - `IAssetSystem.PreloadEssentialAssets()`（标签 `preload`）。
   - 串行加载配置：`RadioConfig` → `ShopConfig` → `BirdConfig` → `LocalizationConfig` → `MapConfig`。
   - `ISaveSystem.InitData()` 与 `IGameSystem.InitAccount()`。
   - `ISceneSystem.LoadScene(currentMap)` 加载当前地图场景预制体。
   - 生成鸟、装饰，启动成就、音频、预加载常用资源。

### 4.2 核心系统职责

| 系统 | 说明 |
|---|---|
| `IAssetSystem` | Addressables 加载、引用计数、Atlas 管理、批量加载接口 |
| `ISceneSystem` | 按索引加载/卸载场景预制体，切图时释放旧资源 |
| `IUISystem` | 面板/弹窗的显示、隐藏、缓存、枚举 `UIPanel` / `UIPopup` |
| `IBirdSystem` | 鸟的生成、数据同步、按地图保存、异步加载世代号防串图 |
| `IGameSystem` | 食物、装饰、点击检测、金币、退出逻辑 |
| `IAudioSystem` | 电台音乐、音效、环境音（按 7 场景 × 5 天气查表） |
| `ISaveSystem` | JSON + MD5 校验 + 备份，写入 `Application.persistentDataPath/GameData/` |
| `IObjectPoolSystem` | 对象池（Food、Num 等） |
| `IMemoryOptimizationSystem` / `IPeriodicCleanupSystem` | 内存优化与 60 秒周期清理 |
| `ILocalizationSystem` | 多语言文本与字体 |
| `ISteamSystem` | Steam API 初始化、回调、成就统计 |
| `IDesktopSystem` / `IFullScreenUtility` | 壁纸模式、窗口模式、全屏模式、点击穿透 |
| `IMonoSystem` | 统一协程与 Update 注册（依赖 `GameEntry`） |

### 4.3 屏幕模式

- `0`：窗口模式
- `1`：壁纸模式（Windows 通过 Win32 API 将窗口设为桌面子窗口；macOS 通过 `FLWallpaperBridge.bundle`）
- `2`：全屏模式
- 快捷键：`1/2/3` 切换，可在 `GameEntry.HandleKeyboardShortcuts()` 中查看完整快捷键。

---

## 5. 构建与发布

### 5.1 构建前准备

1. **必须手动构建 Addressables**：
   - `ProjectSettings/AddressableAssetSettings.asset` 中 `m_BuildAddressablesWithPlayerBuild: 0`，表示打包玩家时不会自动构建 Addressables。
   - 菜单：`Window → Asset Management → Addressables → Build → New Build → Default Build Script`。
   - 常用打包模式：`Packed Mode`（发布）或 `Fast Mode`（编辑器快速迭代）。
2. 检查 `steam_appid.txt` 是否为正确的 AppID；本地测试需要该文件与可执行文件同级。
3. macOS 壁纸模式需要 `Assets/Plugins/macOS/FLWallpaperBridge.bundle`；在 Mac 上构建时会由 `FLWallpaperBuildPostprocessor` 自动用 `clang++` 重新编译 `FLWallpaperBridge.mm`。

### 5.2 构建步骤

1. 打开 `File → Build Settings`。当前 Build Settings 中启用的场景：
   - `Assets/Scenes/B8_Crow.unity`
   - `Assets/Scenes/DebugMode.unity`
2. 选择目标平台（当前以 Standalone Windows x64 为主）。
3. 点击 `Build`，输出目录建议使用 `Build/`（项目已有 `Build/featherlandunit.exe`）。
4. 对于 Steam 发布，将构建产物与 `steam_appid.txt` 一起配置到 Steam depot。

### 5.3 现有构建产物

- `Build/` 目录下已有 Windows 构建：
  - `featherlandunit.exe`
  - `featherlandunit_Data/`
  - `GameAssembly.dll`、`UnityPlayer.dll`、`baselib.dll`
  - D3D12 与 Burst debug 目录

### 5.4 编辑器辅助工具

- `Tools/Excel/导入游戏配置（Excel）`：从 `Assets/Scripts/New/Editor/Excels/*.xlsx` 导入到 ScriptableObject 配置。
- `Tools/优化分组配置 (Optimize Group Settings)`：一键设置 Addressables 分组打包/压缩策略。
- `Tools/Addressables/分析重复依赖`：打开 Addressables Analyze 窗口。
- `Tools/Large Texture Finder`：查找超大纹理。
- `Tools/纹理优化建议 (Texture Optimization Advisor)`：给出纹理压缩与尺寸建议。
- `Tools/Texture Report`：生成纹理分析报告。

---

## 6. 开发规范与代码风格

### 6.1 命名与结构

- 命名空间：业务代码统一使用 `BirdGame`；编辑器工具使用 `BirdGame.Editor` 或 `BirdGame.EditorTools`。
- 接口命名：前缀 `I`，如 `IAssetSystem`。
- 类/方法/属性：PascalCase；私有字段常用 camelCase 或 `_camelCase`。
- 注释以中文为主。

### 6.2 架构约定

- 新增业务逻辑优先放到 `Assets/Scripts/New/Systems/` 并继承 `AbstractSystem`，在 `GameApp.Init()` 中注册。
- 新增数据优先放到 `Assets/Scripts/New/Models/` 并继承 `AbstractModel`，在 `GameApp.Init()` 中注册。
- 新增一次性操作放到 `Assets/Scripts/New/Commands/` 并继承 `AbstractCommand`。
- 视图脚本继承 `ViewControllerBase`（实现 `IController`），通过 `this.GetSystem<T>()` / `this.GetModel<T>()` 访问架构。

### 6.3 性能与内存约定

项目中大量出现以下优化模式，新增代码应遵循：

- 缓存 `Camera.main` 与 `GetComponent<T>` 结果，避免每帧调用。
- 使用 `sqrMagnitude` 替代 `Distance` 做距离比较。
- 复用 `WaitForSeconds`、`PointerEventData`、`RaycastResult` 列表等对象。
- 使用 `Physics2D.OverlapCircleNonAlloc` 等 NonAlloc 物理接口。
- 对象池替代频繁 `Instantiate/Destroy`（Food、Num 等）。
- 资源加载优先走 `IAssetSystem.LoadAssetAsync<T>`，并在合适的生命周期调用 `ReleaseAsset`。
- Atlas 加载走 `IAssetSystem.LoadSpriteFromAtlasAsync` 并成对调用 `ReleaseSpriteFromAtlas`。

### 6.4 Addressables 使用约定

- `preload`：启动时预加载的核心资源，不要滥用。
- `popup`：按需加载的弹窗预制体。
- `config`：配置文件。
- `scene`：场景预制体。
- `music` / `effect`：音频资源。
- `Atlas`：图集资源。

---

## 7. 测试策略

- **单元测试**：Unity Test Framework 已安装，但在 `Assets/` 下未找到自定义 `[Test]` / `[UnityTest]` 用例。如需添加测试，可创建 `Assets/Tests` 目录并使用 `UnityEngine.TestTools`。
- **集成/手工测试**：以编辑器 Play Mode 和 Windows 构建为主。
- **性能测试**：使用 `Window → Analysis → Profiler` 与 `Window → Analysis → Memory Profiler`。
- **Addressables 验证**：使用 `Window → Asset Management → Addressables → Event Viewer` 观察 bundle 加载与内存占用。
- **发布前检查清单**（参考 `CODE_OPTIMIZATION_SUMMARY.md`）：
  - 启动加载时间、内存峰值、场景切换后内存、音乐/音效加载、长时间运行是否泄漏、Console 无错误。

---

## 8. 本地化

- 配置文件：`Assets/Prefabs/Config/LocalizationConfig.asset`（Odin 序列化的 YAML 格式）。
- 支持语言：English、ChineseSimplified、ChineseTraditional、Italian、German、Portuguese、French、Spanish、Russian、Japanese、Korean。
- 导出工具：`export_localization.py`（无额外依赖，直接运行）会解析配置文件并输出 `localization_export.csv`。
- 运行时通过 `ILocalizationSystem.GetString(key)` 获取文本，`ChangeLanguage` 会触发 `ChangeLanguageEvent` 通知 UI 刷新。

---

## 9. 安全与注意事项

- **Steam AppID**：`steam_appid.txt` 包含真实 AppID `3975050`。调试需要它，发布到 Steam 时请按 Steamworks 文档处理。
- **Native 代码**：项目使用 P/Invoke 调用 `user32.dll`、`kernel32.dll` 实现窗口管理与壁纸模式；Windows 构建中 `GameSystem.QuitGame()` 会调用 `ExitProcess(0)`。macOS 壁纸模式依赖 Objective-C++ bridge，需要 Xcode Command Line Tools 或预编译 bundle。
- **存档安全**：存档位于 `Application.persistentDataPath/GameData/`，包含 `.save` 主文件、`.tmp` 临时文件和 `.bak` 备份文件，带有 MD5 校验。修改存档结构时请考虑旧存档向后兼容。
- **内存与 GC**：不要高频调用 `GC.Collect()`；周期性清理已改为 60 秒一次的轻量清理。如需强制清理，使用 `IMemoryOptimizationSystem.PerformFullOptimization()`，但建议在场景切换或暂停时调用。
- **MCP 服务**：`com.gamelovers.mcp-unity` 默认自动在编辑器启动 MCP 服务（端口 8090）。若与本地其他服务冲突，可在 `ProjectSettings/McpUnitySettings.json` 中调整。

---

## 10. 常用入口文件速查

| 文件 | 用途 |
|---|---|
| `Assets/Scripts/New/GameApp.cs` | 全局架构初始化 |
| `Assets/Scripts/New/ViewController/Game/GameEntry.cs` | 运行时入口、屏幕模式、快捷键、点击逻辑 |
| `Assets/Scripts/New/Commands/LoadGameCommand.cs` | 游戏启动加载流程 |
| `Assets/Scripts/New/Systems/IAssetSystem.cs` | 资源加载与引用计数 |
| `Assets/Scripts/New/Systems/ISceneSystem.cs` | 场景切换与资源释放 |
| `Assets/Scripts/New/Systems/IBirdSystem.cs` | 鸟的生成与存档同步 |
| `Assets/Scripts/New/Systems/IAudioSystem.cs` | 音乐、音效、环境音 |
| `Assets/Scripts/New/Systems/ISaveSystem.cs` | 存档读写与校验 |
| `Assets/Scripts/New/Utility/FullScreenUtility.cs` | 窗口/全屏/壁纸模式实现 |
| `Assets/Editor/AddressableGroupConfigurator.cs` | Addressables 分组一键优化 |
| `export_localization.py` | 本地化 CSV 导出 |
| `README.md` | 简要更新日志 |
| `CODE_OPTIMIZATION_SUMMARY.md` | 代码优化总结与最佳实践 |
| `Assets/MEMORY_OPTIMIZATION.md` | 内存优化指南 |

---

**最后更新时间**：2026-07-11（基于当前仓库内容生成）。
