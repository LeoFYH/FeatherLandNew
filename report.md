# Feather Land 游戏常量提取与 Bug 诊断报告

> 生成日期:2026-07-07 | 分支:Mac6.3New | 只读分析,未修改任何游戏代码/配置
> 说明:配置以 .asset 序列化值为准(Inspector 里改过的值会覆盖 C# 字段默认值)。

---

## 一、六组常量

### 1. 食物系统

**食物种类与价格**(`Assets/Prefabs/Config/ShopConfig.asset` 行 1945-1989,"Food" 工具组)

| 食物 | 价格(金币) | 喂食经验加成 addValue | foodScale |
|------|-----------|---------------------|-----------|
| Seed(种子) | 100 | +0.2 | 0.04 |
| Grain(谷物) | 1000 | +1 | 0.06 |
| Bean(豆子) | 10000 | +3 | 0.06 |

**单次喂食经验公式**(`Assets/Scripts/BirdStates/BirdEatState.cs` 行 201-206):

```csharp
_brid.currentExp.Value += conf.eatExp;            // 鸟自身"吃一次经验"(每只鸟配置不同,多数为 1)
if (_brid.currFood != null)
    _brid.currentExp.Value += _brid.currFood.addValue;  // 食物加成
```

即 **每次进食经验 = 鸟的 eatExp + 食物 addValue**,两者叠加。

**场上食物数量上限:8 个**(`Assets/Scripts/New/Systems/IGameSystem.cs` 行 190-197):超过 8 个时自动回收最早投放的那个。每次点击投放 1 颗。

**投放冷却:无**。未找到任何投放间隔/冷却常量,只有位置合法性检查(`IsCoverGround`)。

**食物随时间消失:会,未被吃约 17 秒后消失**(`Assets/Scripts/New/ViewController/Game/Food.cs`):
- 行 12:`_wait8s = new WaitForSeconds(8f)` — 投放 8 秒后检查是否被鸟锁定;
- 行 79-122 `DestroyDelay`:再等 5 秒(行 83 `while (timer < 5)`),然后 4 秒淡出(行 21 `fadeDuration = 4f`);
- 合计 8+5+4 ≈ **17 秒**;期间被鸟锁定(isTargeted)则中断消失流程。

### 2. 场景容量

**基础容量:每张地图 20 只**
- 运行时实际值:`Assets/Prefabs/Config/BirdConfig.asset` 行 180 `maxBirdCount: 20`
- ⚠️ 代码默认值是 35(`Assets/Scripts/New/Config/BirdConfig.cs` 行 19 `public int maxBirdCount = 35;`),被 asset 序列化值 20 覆盖。**以 20 为准**。

**可升级,按地图独立,共 3 档**(商店 "Capacity" 工具,`Assets/Prefabs/Config/ShopConfig.asset` 行 1990-2034):

| 档位 | 价格 | 配置 addCount | 实际生效增量 | 购买后容量(实际) | UI 标签 selectionName |
|------|------|--------------|------------|----------------|---------------------|
| 1 | 500 | 10 | **+20** | 40 | "30" |
| 2 | 5000 | 20 | **+20** | 60 | "40" |
| 3 | 15000 | 30 | **+20** | 80 | "80" |

⚠️ 购买代码写死每档 `+= 20`,**不读取配置的 addCount**(`Assets/Scripts/New/ViewController/UI/Popups/Shop/ShopToolItem.cs` 行 651;行 653 被注释掉的 `selectedTool.addCount` 是原始意图)。详见"不一致清单"第 1 条。

### 3. 图鉴判定逻辑

**结论:开出瞬间永久点亮,与当前是否持有无关。**

- 开蛋瞬间注册:`Assets/Scripts/New/Commands/SpawnBirdCommand.cs` 行 74 调用 `CheckIllustratedUpdate(val)`,行 165-175:

```csharp
if (!saveModel.IllustratedData.birds.Contains(birdIndex))
{
    saveModel.IllustratedData.birds.Add(birdIndex);   // 全局鸟 ID 永久写入存档列表
    ...
}
```

- 存档结构:`Assets/Scripts/New/Models/ISaveModel.cs`,`IllustratedData` 内仅一个 `List<int> birds`(已解锁鸟 ID 集合),只增不减——卖掉鸟不会移除。
- UI 判定:图鉴格子按 `IllustratedData.birds.Contains(id)` 点亮(`Assets/Scripts/New/ViewController/UI/Popups/Illustrated/IllustratedItem.cs` 行 39;详情面板 `Assets/Scripts/New/ViewController/UI/Popups/IllustratedPopup.cs` 行 115、204),**不遍历当前持有的鸟**。
- 兜底回填:每次加载/同步时把当前拥有但不在图鉴里的鸟补进列表(`Assets/Scripts/New/Systems/IBirdSystem.cs` 行 491-518 `SyncIllustratedDataFromBirds`)。

### 4. 自动经验

**结算单位:每 60 秒一次 tick,字段含义为"每分钟成长值"。**

- 字段定义:`Assets/Scripts/New/Config/Items/BirdItem.cs` 行 78,标签为 **"每分钟增加的成长值"**(`public float autoExp;`)。
- 结算代码:`Assets/Scripts/New/ViewController/Game/Brid.cs` 行 439-443,`Update` 里 `if (Time.time - startTimer >= 60)` 触发一次 `AutoExp()`;行 517 `currentExp.Value += conf.autoExp`(每 tick 加一次完整 autoExp,不乘 deltaTime、不按帧)。
- 所以 0.00222 的含义是 **0.00222 经验/分钟**。

**不喂食会自然长大吗?会,但仅限"游戏运行中 + 鸟在当前地图被实例化"时累计:**
- `AutoExp()` 只对 `isSmall == true` 的幼鸟生效(Brid.cs 行 512),达到 `totalExp` 即成年(行 518-521)。
- 计时基于 `Time.time`,进程关闭不结算;切走地图后该图的鸟对象销毁,也不结算(未确认是否有其他补偿路径,未找到)。
- 估算:totalExp=3、autoExp=0.00222 的鸟 → 3 ÷ 0.00222 ≈ 1351 分钟 ≈ **22.5 小时挂机**;高山 Bearded Vulture(autoExp=0.00694, totalExp=6.6)→ 951 分钟 ≈ **15.9 小时**。

**成长阶段阈值:只有两阶段**(幼鸟 isSmall → 成鸟),阈值即每只鸟自己的 `totalExp`(BirdItem asset,抽样:2 / 2.5 / 3 / 3.9 / 6.6 不等),无中间阶段常量。

### 5. 新玩家初始状态

| 项目 | 数值 | 出处 |
|------|------|------|
| 初始金币 | **200** | `Assets/Prefabs/Config/ShopConfig.asset` 行 16 `startCoins: 200`;新档写入于 `Assets/Scripts/New/Systems/ISaveSystem.cs` 行 104-107 |
| 老档保底补发 | 金币 < 200 的老玩家一次性补到 200 | `Assets/Scripts/New/Systems/IGameSystem.cs` 行 908-917(`hasReceivedStartCoins` 标记) |
| 赠送鸟蛋/鸟 | **未找到赠送逻辑**(需用初始金币购蛋,森林最便宜的蛋 50 金币,ShopConfig.asset 行 21) | — |

⚠️ `AccountData.coins` 字段默认值是 600(`Assets/Scripts/New/Models/ISaveModel.cs` 行 48),但新档创建时立即被 startCoins=200 覆盖,600 实际不生效。见"不一致清单"第 4 条。

### 6. 收益结算

**只在游戏进程运行时结算,每 60 秒一次;未找到离线收益。**

- 结算协程:`Assets/Scripts/New/Systems/IBirdSystem.cs` 行 45-53,`WaitForSeconds(60f)` 循环调用 `AddAllMapsIncome()`。
- 结算范围:行 55-70,遍历**所有地图**存档里的鸟(不只当前地图),幼鸟计 `individualEarningSmall`、成鸟计 `individualEarningBig`,总和直接加到金币。
- 个体收益 = 配置收益 × 个体随机倍率 0.7~1.3(Box-Muller 正态,`Assets/Scripts/New/Models/IBirdModel.cs` 行 50-63)。
- **离线收益:未找到**任何比较上次退出时间补发的代码(无 lastQuitTime/DateTime 差值逻辑)。
- 注意不对称:金币收益按全地图结算,而自动经验(第 4 组)只对当前地图实例化的鸟结算。

---

## 二、Bug 诊断

### Bug 1:金币到 50 万左右不再增加 —— 已定位,历史版本的上限截断

**结论:是旧版本 `coinsLimit = 500000` 的硬截断造成的,当前代码已移除。不是数据类型溢出。**

证据链:
1. 金币类型为 `float`(`Assets/Scripts/New/Models/ISaveModel.cs` 行 48;`Assets/Scripts/New/Models/IAccountModel.cs` 行 10)——float 最大值 3.4e38,50 万远不构成溢出。
2. 上限常量真实值:`Assets/Prefabs/Config/ShopConfig.asset` 行 17 **`coinsLimit: 500000`**(代码默认 10000 被覆盖,`Assets/Scripts/New/Config/ShopConfig.cs` 行 78)。与玩家反馈"50 万左右"精确吻合。
3. git 历史(commit `33635a86`,2026-04-28)显示删除前的代码,在 `Coins.Register` 回调里:

```csharp
int limit = ShopConfig.coinsLimit;      // 500000
if (v > limit) { Coins.Value = limit; return; }   // 超过即打回 500000
```

   删除后原地留有注释(`Assets/Scripts/New/Systems/IGameSystem.cs` 行 922):"不再对 coinsLimit 做截断;之前会让 >500000 的金币切地图时被吞,玩家无感知丢失"。
4. 当前代码全库搜索无任何对金币的 Clamp/Min 截断。

**判定:反馈来自 2026-04-28 之前的构建。发新版本后该问题即消失。**

次要提示(非本次反馈原因):float 在 50 万量级的精度约 0.03,不足以吞掉每分钟 ≥0.2 的收益;但金币涨到约 420 万以上(ulp≥0.5)时,小额小数收益将开始丢失。长期建议改用 double 或整数分值存储。

### Bug 2:高山(场景1)图鉴点不亮 —— 注册逻辑无 bug,定位为已修复的 UI 显示 bug;鸟 59 配置异常属实但与图鉴无关

**结论 A:图鉴注册/存档路径对场景 1 没有任何逻辑错误。**
- 注册用的是**全局唯一鸟 ID**,不涉及场景索引换算(`SpawnBirdCommand.cs` 行 74、165-175),不存在 off-by-one 或漏 case。
- 已逐一核对:高山蛋配置的 `birdType`(ShopConfig.asset 行 108-202,含 18/40-46/55-66/73-75/82-88/94-96/99-102/231-233/237-239)与 `Assets/Prefabs/Config/BirdItems/高山/` 全部 43 个 BirdItem 的 `id` 完全一致;图鉴 UI 也用同一套 ID 判定(IllustratedItem.cs 行 39)。
- 加载时还有 `SyncIllustratedDataFromBirds` 回填兜底(IBirdSystem.cs 行 491-518),即使开蛋当刻漏存,只要鸟还在就会补录。

**结论 B:玩家看到的"点不亮"最可能是皮肤格子的黑色残留显示 bug,存在于 2026-04-06 ~ 04-11 的构建,已修复。**
- 2026-04-06(commit `4ae1776e3`)为图鉴皮肤格子引入了对象复用池(IllustratedPopup.cs 现行 96-124 的 `reuseIndex` 逻辑);
- 而当时 `BirdSkin.Init` 只在未解锁时置 `icon.color = Color.black`,**没有解锁时重置回白色的分支**——复用一个曾显示为黑色的格子去展示已解锁的鸟,仍然是黑的;
- 图鉴自 2026-01-20(commit `1000c498e`)改为全局图鉴、森林排最前,打开图鉴默认先选中森林第一项,之后翻到高山时格子全部走复用路径,高山成为最先、最集中出现黑色残留的场景——与"多名玩家反馈高山点不亮"吻合;
- 2026-04-11(commit `396e4e8fc`"修复图鉴解锁的显示问题")补上了 `else { icon.color = Color.white; }`,现行代码已包含(`Assets/Scripts/New/ViewController/UI/Popups/Illustrated/BirdSkin.cs` 行 28-31)。
- 若有玩家在 4-11 修复之后的构建上仍复现,则以上不能解释,需要拿到该玩家的 `IllustratedData` 存档文件核对(标注:**未确认**是否存在此类案例)。

**结论 C:鸟 ID 59 的 `eatExp: 0.01` 是真实的配置异常,但它不可能导致图鉴点不亮。**
- 配置原值(`Assets/Prefabs/Config/BirdItems/高山/59_Secretarybird 2.asset` 行 25/44/45/46):`id: 59, totalExp: 6.6, eatExp: 0.01, autoExp: 0.00694`;同类另外三只(43/58/60)`eatExp` 均为 **1**,相差 100 倍,疑似录入错误(0.01 vs 1.0,**未确认**设计意图)。
- 图鉴在**孵出瞬间**注册,与成长/成年完全无关(SpawnBirdCommand.cs 行 74 在生成鸟之前就已写入图鉴)——玩家只要开出 59 就点亮,喂养速度不影响。
- 另外"喂 660 次才成年"的算法不成立:实际每次喂食经验 = `eatExp + 食物 addValue`(BirdEatState.cs 行 202-206),用最便宜的种子也是 0.01+0.2=0.21/次 → 约 **32 次**;用豆子 3.01/次 → **3 次**;纯挂机靠 autoExp 也只需约 15.9 小时。59 只是比同类慢,并非不可达成。

---

## 三、配置值与代码读取方式不一致清单

1. **容量升级增量:配置被忽略**——`ShopConfig.asset` 三档 `addCount: 10/20/30`,但购买代码写死 `addedBirdCountList[mapIndex] += 20`(`ShopToolItem.cs` 行 651),配置字段形同虚设;UI 档位标签 "30/40/80" 与两者都对不上(实际容量序列是 20→40→60→80)。策划改 addCount 不会生效。
2. **基础鸟容量:代码默认 35 vs asset 实值 20**——`BirdConfig.cs:19` 写 35,运行时被 `BirdConfig.asset:180` 的 20 覆盖。看代码会得出错误结论。
3. **coinsLimit:代码默认 10000 vs asset 实值 500000,且现已是死配置**——截断逻辑删除后(IGameSystem.cs:922)该字段无任何运行时消费者(仅调试面板 ShopEditor.cs 可改),建议删除或注明废弃。
4. **初始金币:字段默认 600 vs 实际 200**——`AccountData.coins = 600`(ISaveModel.cs:48)在新档创建时立即被 `startCoins = 200` 覆盖(ISaveSystem.cs:104-107),600 永不生效,易误导。
5. **autoExp 单位陷阱**——数值形如 0.00222,极易被当成"每秒";实际字段标签与代码均为**每分钟**(60 秒 tick 加一次完整值,Brid.cs:439-443、517)。
6. **鸟 59 eatExp = 0.01**——同类三只均为 1,量级差 100 倍,疑似漏了小数点(0.01 ↔ 1.0),**未确认**是否有意为之,建议策划复核。
7. **收益与自动经验的结算范围不对称**——金币收益遍历全部地图存档鸟(IBirdSystem.cs:55-70),自动经验只对当前地图实例化的鸟生效(Brid.cs 的 Update)。若设计上应一致,属隐性偏差。
