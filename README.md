# Dragon

基于 Unity 2022.3 LTS 的 2D 游戏客户端框架，核心入口为 `GameCenter`。启动时由 `GameCenterScript` 创建系统、以 **UniTask** 异步初始化各模块，并在每帧驱动状态机、终端与地图场景。定时调度统一使用 `UniTaskHelper`（已替代旧版 `SchedulerManager` / `CoroutinePool`）。

**Unity 工程路径：** `Client/Main/`  
**核心代码路径：** `Client/Main/Assets/GameMain/`

---

## 仓库结构

```
Dragon/
├── Client/Main/          ← Unity 工程（场景、资源、GameMain 代码）
├── Config/               ← Luban 配置表源数据与生成脚本
│   ├── Datas/            ← Excel 源表（item、map、localization、entity、skill 等）
│   ├── luban.conf        ← 生成配置（client / server / all）
│   └── gen.bat / gen.sh  ← 生成脚本
└── README.md
```

配置生成输出：
- **C# 代码** → `Client/Main/Assets/GameMain/GameConfig/`
- **JSON 数据** → `Client/Main/Assets/AddressableAssets/Configs/`

---

## GameMain 目录

代码按领域分层，统一通过 `GameCenter` 总线访问：

| 目录 | 说明 |
|------|------|
| `Center/` | `GameCenter`、`GameCenterScript` 入口 |
| `Library/` | 基础框架：资源、事件、对象池、Time（`UniTaskHelper`）、UI 工具、Transform、HTTP、DebugLog、配置 Editor 工具等 |
| `Logic/` | 游戏逻辑：状态机、输入、音频、道具、**红点系统** |
| `Logic/RedDot/` | 红点树、Provider 模板、Registry 注册入口、UI 绑定组件 |
| `GameBattle/` | 战斗与实体：地图场景、实体 FSM、移动/攻击控制、伤害结算、Y 轴排序渲染 |
| `GameUI/` | UI 管理：主界面、弹窗、Toast、道具界面 |
| `GameData/` | 运行时用户数据与存档 |
| `GameConfig/` | Luban 自动生成，勿手改 |
| `Global/` | 全局常量、战斗数值、伤害类型枚举 |

---

## 启动与生命周期

```
Boot 场景
  └─ GameCenterScript.Create()
       ├─ Awake / Start
       │    ├─ GameCenter.CreateSystem()     // 创建单例，不加载资源
       │    └─ GameCenter.Initialize()       // UniTask 异步初始化（Forget）
       │         ├─ CoreInitializeAsync      // Addressables 准备、配置、资源预加载、存档等
       │         └─ LogicInitialize          // 本地化、音频、道具、地图等
       └─ 每帧 Update / LateUpdate / FixedUpdate
```

| 阶段 | 方法 | 说明 |
|------|------|------|
| 创建 | `GameCenter.CreateSystem()` | 实例化所有 Manager 单例 |
| 初始化 | `GameCenter.Initialize()` | Addressables 初始化/Catalog/下载 → 加载配置表 → 预加载启动标签 → 读存档；完成后切到 `GameStateCode.Main` |
| 每帧 | `GameCenter.Update / LateUpdate` | 驱动状态机、终端、地图 |
| 卸载 | `GameCenter.Uninitialize()` | 释放音频、实体、事件、DebugLog 等 |
| 重置逻辑 | `GameCenter.ResetLogicSystem()` | 仅重建逻辑层（不重建 Core） |

访问已注册模块统一通过 `GameCenter.XXX`，例如 `GameCenter.EventManager`、`GameCenter.ConfigManager`。

---

## 模块一览

### 图例

- **GC** = 已在 `GameCenter` 中创建并持有静态引用
- **GC Init** = 在 `CoreInitializeAsync` 或 `LogicInitialize` 中显式初始化
- **独立** = 项目内存在，需自行获取单例或调用，未纳入 `GameCenter` 生命周期

---

## 一、入口与全局

### GameCenter / GameCenterScript

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| GC | 是 |
| 概要 | 游戏总线：集中持有各 Manager、编排初始化顺序与每帧更新 |

```csharp
// 一般由 GameCenterScript 自动调用，业务侧通过静态字段访问
GameCenter.ConfigManager.Tables.Tbitem.Get(1001);
GameCenter.StateManager.ChangeState(GameStateCode.Fight, param);
```

### GlobalDefine

| | |
|---|---|
| 位置 | `Assets/GameMain/Global/GlobalDefine.cs` |
| 概要 | 主菜单场景名、战斗常量（重力、速度倍率）、`DamageType` 枚举 |

### AddressablesLabelNames

| | |
|---|---|
| 命名空间 | `Dragon.Resource` |
| 位置 | `Assets/GameMain/Library/Resource/AddressablesLabelNames.cs` |
| 概要 | Addressables 标签常量（Sprite / Prefab / UI / Audio）及 `BootstrapEntries` 启动预加载配置 |

```csharp
// 追加启动预加载标签时，在此扩展 BootstrapEntries
AddressablesLabelNames.BootstrapEntries
```

---

## 二、内核模块（GameCenter Core）

### ResourceManager

| | |
|---|---|
| 命名空间 | `Dragon.Resource` |
| GC / Init | 是 / `Initialize()` |
| 概要 | 基于 Addressables 的统一资源加载、引用计数、缓存与释放 |

```csharp
// 同步加载
var prefab = GameCenter.ResourceManager.Load<GameObject>("ui_lobby");

// 异步加载
GameCenter.ResourceManager.LoadAsync<Sprite>("icon_gold", sprite => { /* ... */ });

// 释放（引用计数归零后真正卸载）
GameCenter.ResourceManager.Release("icon_gold");
```

### AddressablesRemoteService

| | |
|---|---|
| 命名空间 | `Dragon.Resource` |
| GC / Init | 是 / `CoreInitializeAsync` 中 `PrepareStartupResourcesAsync` |
| 概要 | Addressables 远程资源生命周期：运行时初始化、Catalog 更新、按 key/标签下载依赖；未配置 Remote 时自动跳过 |

```csharp
// 启动流程（GameCenter 已调用）
await GameCenter.AddressablesRemoteService.PrepareStartupResourcesAsync(
    ResourcePreloadService.GetStartupDownloadKeys(), onProgress: p => { /* ... */ });

// 单独使用
await GameCenter.AddressablesRemoteService.InitializeAsync();
bool updated = await GameCenter.AddressablesRemoteService.UpdateCatalogsAsync();
long size = await GameCenter.AddressablesRemoteService.GetDownloadSizeAsync("UI");
```

### ResourcePreloadService

| | |
|---|---|
| 命名空间 | `Dragon.Resource` |
| GC / Init | 是 / 启动时在 `CoreInitializeAsync` 中预加载 |
| 概要 | 按 Addressables 标签批量预加载；`GetStartupDownloadKeys()` 汇总 Config 标签 + `BootstrapEntries` 供下载 |

```csharp
// 启动预加载（读取 AddressablesLabelNames.BootstrapEntries）
await GameCenter.ResourcePreloadService.PreloadBootstrapResourcesAsync(onProgress: p => Debug.Log(p));

// 按标签预加载
await GameCenter.ResourcePreloadService.PreloadByLabelAsync<GameObject>(
    AddressablesLabelNames.UI, onProgress: p => Debug.Log(p));
```

### ConfigManager

| | |
|---|---|
| 命名空间 | `Dragon.Config` |
| GC / Init | 是 / `Init()`（异步，Luban 配置表） |
| 概要 | 加载 JSON 配置并构建 `Tables`，供全局查表 |

```csharp
// 初始化完成后
var item = GameCenter.ConfigManager.Tables.Tbitem.Get(1001);
var map  = GameCenter.ConfigManager.Tables.Tbmap.Get(mapId);
```

### GameStateManager

| | |
|---|---|
| 命名空间 | `Dragon.Logic` |
| GC / Init | 是 / `Initialize()` |
| 概要 | 游戏顶层状态机（Init → Update → PreLoad → Login → Main → Loading → Fight） |

```csharp
GameCenter.StateManager.ChangeState(GameStateCode.Loading, new GameLoadingParam { MapId = 1 });
bool inFight = GameCenter.StateManager.IsCurState(GameStateCode.Fight);
```

状态实现目录：`Logic/GameState/State/`（`10_Init` … `60_Fight`）。

### EventManager

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| GC / Init | 是 / 懒创建（`Instance` 时挂 `EventRootScript`） |
| 概要 | 整型事件 ID 的注册/派发，支持同步与延迟触发 |

```csharp
// 注册
GameCenter.EventManager.Register(MainEventDefine.EID_ITEM_INVENTORY_CHANGED, OnItemChanged);

// 派发
GameCenter.EventManager.TriggerEvent(MainEventDefine.EID_ITEM_INVENTORY_CHANGED, param);

// 反注册
GameCenter.EventManager.UnRegister(MainEventDefine.EID_ITEM_INVENTORY_CHANGED, OnItemChanged);
```

事件 ID 定义见 `EventConstDefine`、`MainEventDefine`、`UIEventDefine`、`BattleEventDefine`。

### InputManager

| | |
|---|---|
| 命名空间 | `Dragon.GameInput` |
| GC / Init | 是 / `Init()` |
| 概要 | Unity Input System 封装，按上下文切换菜单/战斗/禁用输入 |

```csharp
GameCenter.InputManager.SetInputContext(InputManager.InputContextState.Battle);
var move = GameCenter.InputManager.BattleInput.Move.ReadValue<Vector2>();
```

### GameDataManager

| | |
|---|---|
| 命名空间 | `Dragon.GameData` |
| GC / Init | 是 / `Init()` |
| 概要 | 管理 `GameMetaData`（存档索引）与 `GameUserData`（当前角色数据） |

```csharp
var user = GameCenter.DataManager.UserData;
GameCenter.DataManager.CreateNewSaveDataInfo(uniqueId);
GameCenter.DataManager.LoadUserData(uniqueId);
```

### GameSaveManager

| | |
|---|---|
| 命名空间 | `Dragon.GameData` |
| GC / Init | 是 / `Init()` |
| 概要 | 存档读写：临时档、手动档（3 槽）、自动档（3 槽），底层使用 `FileIO` + JSON |

```csharp
GameCenter.SaveManager.SaveManualUserData(meta, uniqueId, userData, slotIndex: 0);
var loaded = GameCenter.SaveManager.LoadManualUserData(meta, uniqueId, slotIndex: 0);
```

### GameUIManager

| | |
|---|---|
| 命名空间 | `Dragon.GameUI` |
| GC / Init | 是 / 懒创建 |
| 概要 | 主界面与单层 UI 的加载、缓存、显示/隐藏（预制体需挂 `UIBase`） |

```csharp
await GameCenter.GameUIManager.OpenMainUI<UILobby, UILobbyData>("ui_lobby", data);
await GameCenter.GameUIManager.OpenSingleUI<UIFight, UIFightData>("ui_fight", sortingOrder: 10, data);
GameCenter.GameUIManager.CloseUI("ui_fight");
```

### PopupManager

| | |
|---|---|
| 命名空间 | `Dragon.GameUI` |
| GC / Init | 是 / 懒创建 |
| 概要 | 弹窗栈管理，支持缓存复用、置顶；关闭时若挂有 `UIDisplayEffect` 退场动效，会等动效播完再销毁/回收 |

```csharp
GameCenter.PopupManager.OpenPopup("popup_confirm", new PopupConfirmData { /* ... */ });
GameCenter.PopupManager.ClosePopup("popup_confirm");
```

### ToastManager

| | |
|---|---|
| 命名空间 | `Dragon.GameUI` |
| GC / Init | 是 / 懒创建 |
| 概要 | 浮动提示，每次 `Show` 实例化一条，支持多条并存；渐隐动画基于 DOTween |

```csharp
GameCenter.ToastManager.Show("获得金币 x100");
GameCenter.ToastManager.Show("保存成功", displayDuration: 2f);
```

### UIQueueManager

| | |
|---|---|
| 命名空间 | `Dragon.GameUI` |
| GC / Init | 是 / 懒创建 |
| 概要 | UI/弹窗顺序队列：上一项关闭后再打开下一项 |

```csharp
GameCenter.UIQueueManager.EnqueuePopup("popup_story", storyData);
GameCenter.UIQueueManager.EnqueueOpenMainUI<UILobby, UILobbyData>("ui_lobby", lobbyData);
GameCenter.UIQueueManager.ClearPending(); // 清空未执行的队列
```

### PoolManager

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| GC / Init | 是 / 懒创建（卸载时 `ClearAll`） |
| 概要 | GameObject 预制体对象池，支持同步注册与 Addressables 异步注册 |

```csharp
GameCenter.PoolManager.Register("fx_hit", hitPrefab, prewarm: 5, maxInactive: 20);
var go = GameCenter.PoolManager.Get("fx_hit");
GameCenter.PoolManager.Release("fx_hit", go);

// 异步注册
await GameCenter.PoolManager.RegisterAsync("fx_hit", prewarm: 5);
```

实现 `IPoolReset` 可在取出/归还时重置状态。

### DebugLogManager

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| 位置 | `Assets/GameMain/Library/DebugLog/` |
| GC / Init | 是 / `Init()`（`NewCoreSystem` 中调用），卸载时 `Shutdown()` |
| 概要 | 收集 Unity Console 日志到内存缓冲，配合 `popup_debug_log` 弹窗查看 |

```csharp
// 启动后自动收集，也可手动打开弹窗
GameCenter.DebugLogManager.OpenPopup();
string all = GameCenter.DebugLogManager.GetFullText();
GameCenter.DebugLogManager.Clear();
```

---

## 三、逻辑模块（GameCenter Logic）

### LocalManager

| | |
|---|---|
| 命名空间 | `Dragon.Localization` |
| GC / Init | 是 / `Init()` |
| 概要 | 多语言切换，从 Luban `Tblocalization` 按 ID 取文案 |

```csharp
GameCenter.LocalManager.SwitchLanguage(LanguageType.English);
string text = GameCenter.LocalManager.GetLocalString("10001"); // key 为配置表 ID

// UI 组件：LocalizationUIHandler 自动刷新（挂到 TMP 或代码绑定）
LocalizationHelper.SetText(tmpText, "10001");

// 编辑器：LocalStringEditorPreview 可在非 Play 模式预览文案
```

### TerminalManager

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| GC / Init | 是 / `Init()`，每帧 `Update` |
| 概要 | 游戏内调试终端（GM 命令行），可注册自定义命令 |

```csharp
GameCenter.TerminalManager.RegisterCommand("give", 2, "给予道具 {0} x{1}");
// 默认快捷键打开终端弹窗，在 Update 中检测输入
```

### AudioManager

| | |
|---|---|
| 命名空间 | `Dragon.Audio` |
| GC / Init | 是 / `Init()` |
| 概要 | BGM / 环境音双通道交叉淡化，音效多通道池，资源为 Addressables 地址 |

```csharp
GameCenter.AudioManager.PlayBgm("bgm_main", loop: true);
GameCenter.AudioManager.PlayAmbient("amb_forest");
GameCenter.AudioManager.PlaySfx("sfx_click");
GameCenter.AudioManager.BgmVolume = 0.8f;
```

### ItemManager

| | |
|---|---|
| 命名空间 | `Dragon.Logic` |
| GC / Init | 是 / `Init()` |
| 概要 | 道具表校验、背包读写、数量变更后派发 `EID_ITEM_INVENTORY_CHANGED` |

```csharp
GameCenter.ItemManager.TryGain(itemId: 1001, count: 5);
GameCenter.ItemManager.TryConsume(itemId: 1001, count: 2);
int n = GameCenter.ItemManager.GetItemCount(1001);
```

### MapSceneManager

| | |
|---|---|
| 命名空间 | `Dragon.Logic` |
| GC / Init | 是 / `Init()`，每帧 `Update` / `LateUpdate` |
| 概要 | 地图场景加载、实体生成、相机跟随；持有 `EntityManager`、`CameraManager`、`EntitySpawnner` |

```csharp
GameCenter.MapSceneManager.LoadSceneAndSpawnAsync(mapId: 1);
// 场景加载完成后自动 InitEntitySpawnner → SpawnAllEntities
var player = GameCenter.MapSceneManager.EntityManager.Entities; // 遍历实体
```

### RedDotManager（红点系统）

| | |
|---|---|
| 命名空间 | `Dragon.Logic` |
| 位置 | `Assets/GameMain/Logic/RedDot/` |
| GC / Init | **否**（独立单例；接入时在 `LogicInitialize` 调用 `Init()`） |
| 概要 | 树形红点中枢：叶子 Provider 计算数值 → 非叶子聚合 → 派发 `EID_REDDOT_CHANGED`；UI 只负责展示 |

**目录结构：**

| 子目录/文件 | 说明 |
|-------------|------|
| `RedDotManager.cs` | 树注册、脏标记、向上聚合、事件派发 |
| `RedDotRegistry.cs` | **集中注册入口**：非叶子 `RegisterNode`、叶子 `RegisterLeaf` |
| `RedDotKey.cs` | key 常量（`.` 分段表示层级，如 `lobby.mail.system`） |
| `Providers/RedDotExampleProvider.cs` | **Provider 模板**，复制后实现 `Evaluate()` |
| `RedDotUI/RedDotView.cs` | UI 挂载：监听变化、显隐与计数展示 |
| `RedDotUI/RedDotEnterClear.cs` | 入口点击消红点（`SeenUntilEnter` / `OncePerSession`） |

**接入步骤：**

```csharp
// 1. RedDotKey.cs 增加 key 常量
public const string LobbyShop = "lobby.shop";

// 2. 复制 RedDotExampleProvider → Providers/RedDotShopProvider.cs，实现 Evaluate
public RedDotValue Evaluate()
{
    int count = GameCenter.ItemManager.GetNewShopItemCount();
    return count > 0 ? new RedDotValue(count) : RedDotValue.None;
}

// 3. RedDotRegistry.RegisterAll 中注册树
manager.RegisterLeaf(RedDotKey.LobbyShop, RedDotKey.Lobby,
    new RedDotShopProvider(RedDotKey.LobbyShop, RedDotRefreshMode.Dynamic,
        MainEventDefine.EID_ITEM_INVENTORY_CHANGED));

// 4. GameCenter.LogicInitialize 中初始化（接入后）
RedDotManager.Instance.Init();

// 5. UI 预制体挂 RedDotView，Inspector 填 key；按钮挂 RedDotEnterClear.OnClickClear
```

**核心 API：**

```csharp
RedDotManager.Instance.GetValue("lobby.mail");       // 查询聚合值
RedDotManager.Instance.IsVisible("lobby");             // 是否显示
RedDotManager.Instance.MarkDirty("lobby.mail.system"); // 业务数据变化后标记脏
RedDotManager.Instance.MarkSeen("seen.tutorial");      // 点进即消
RedDotManager.Instance.RefreshAll();                   // 登录/全量拉取后重算
```

**枚举说明：**

| 类型 | 值 | 说明 |
|------|-----|------|
| `RedDotRefreshMode` | `Dynamic` | 数据变化即重算 |
| | `SeenUntilEnter` | 进入界面或 `MarkSeen` 后消隐，PlayerPrefs 持久化 |
| | `OncePerSession` | 本次进程内消隐，不持久化 |
| `RedDotAggregateMode` | `Or` / `Sum` / `Max` | 非叶子对子节点的聚合方式 |
| `RedDotDisplayStyle` | `DotOnly` / `CountOnly` / `DotAndCount` / `DotOrCount` | `RedDotView` 展示样式 |

---

## 四、战斗子模块（随 MapSceneManager 创建，未单独挂 GameCenter）

### EntityManager

| | |
|---|---|
| 命名空间 | 全局（无 namespace） |
| 概要 | 战斗实体列表、Skin 同步、占位检测、生命周期 `Update` / `LateUpdate` |

通过 `GameCenter.MapSceneManager.EntityManager` 访问。

实体分层结构：

| 子目录 | 说明 |
|--------|------|
| `EntityData/` | 纯数据：Character（Player/Enemy/Npc）、Interactable（Block） |
| `EntityInherit/` | 运行时实体类：Character、Player、Enemy、Npc、Block |
| `EntityState/` | 实体 FSM 状态（Idle/Move/Run/Jump/Birth/Death 等） |
| `EntityControl/` | 输入提供者、移动控制器、攻击控制器 |
| `EntityCombat/` | 伤害结算（`CombatDamageSettlement`、`IAttackable`） |
| `EntityAction/` | 预留目录，尚无实现 |

### CameraManager

| | |
|---|---|
| 命名空间 | `Dragon.Logic` |
| 概要 | Cinemachine 虚拟相机，绑定玩家并在 X 轴受限内跟随 |

通过 `GameCenter.MapSceneManager.CameraManager` 访问。

### EntitySpawnner

| | |
|---|---|
| 命名空间 | 全局 |
| 概要 | 读取场景 `SpawnRoot` 下 `SpawnPoint`，按配置生成 NPC / 敌人 / 玩家 |

地图场景需包含名为 `SpawnRoot` 的 GameObject。

### CombatDamageSettlement

| | |
|---|---|
| 命名空间 | 全局 |
| 概要 | 统一伤害结算入口，基于双方 `Entity.Data` 属性计算最终伤害 |

```csharp
var result = CombatDamageSettlement.Settle(attacker, defender, hitContext);
float damage = result.FinalDamage;
```

相关类型：`AttackHitContext`（命中上下文）、`DamageSettlementResult`（结算结果）、`IAttackable`（可受击接口）。

### YSortRenderer

| | |
|---|---|
| 命名空间 | 全局 |
| 位置 | `Assets/GameMain/GameBattle/Render/` |
| 概要 | 根据世界坐标 Y 轴动态调整 `Renderer.sortingOrder`，实现 2D 前后遮挡 |

挂在角色或场景物件上；渲染器在子节点时，指定 `_sortTransform` 为逻辑位置节点。

---

## 五、基础库（Library，多数未纳入 GameCenter）

### UniTaskHelper

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| 位置 | `Assets/GameMain/Library/Time/UniTaskHelper.cs` |
| 概要 | **项目统一定时调度**：Delay / Interval / NextFrame / EveryFrame；返回 `UniTaskHelper.Handle`，支持 `Cancel` / `CancelByOwner`；Owner 为 Unity 对象时销毁自动取消 |

> 已替代并删除 `SchedulerManager`、`CoroutinePool`。业务侧请勿再引入自研帧调度或全局协程池。

```csharp
var handle = UniTaskHelper.Delay(1.5f, () => Debug.Log("延迟执行"), owner: this, useUnscaledTime: true);
UniTaskHelper.Interval(0.5f, OnTick, owner: this);
UniTaskHelper.NextFrame(() => RefreshUI(), owner: this);
UniTaskHelper.EveryFrame(dt => UpdateLogic(dt), owner: this);

handle.Cancel();
UniTaskHelper.CancelByOwner(this);
```

### TimeUtils

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| 概要 | 受游戏速度影响的 `DeltaTime`；`GameCenterScript` 每帧调用 `Update` |

```csharp
GameCenter.SetGameSpeedRate(2f);  // 2 倍速
float dt = TimeUtils.DeltaTime;   // 已乘速度系数
```

### CountdownHelper / StopwatchHelper / TimeFormatUtils

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| 概要 | 倒计时 / 秒表（内部基于 `UniTaskHelper.Interval`，固定 1 秒 tick）、时间格式化工具 |

```csharp
CountdownHelper.StartCountdown(60f, onTick: sec => UpdateLabel(sec), onCompleted: OnTimeUp, owner: this);
CountdownHelper.CancelByOwner(this);

StopwatchHelper.Start(onTick: elapsed => UpdateLabel(elapsed), owner: this);
StopwatchHelper.CancelByOwner(this);
```

### FiniteStateMachine（FSM）

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| 概要 | 通用有限状态机；`GameStateManager` 与实体状态（如 `CharacterIdleState`）均基于此 |

```csharp
var fsm = new FiniteStateMachine("MyFsm", host);
fsm.Register(new MyState());
fsm.TransToState(stateId, arg);
fsm.Update(dt);
```

### FileIO

| | |
|---|---|
| 命名空间 | 全局 |
| 概要 | 用户数据读写；Editor 下路径为 `Assets/AddressableAssets/UserData`，真机为 `persistentDataPath` |

```csharp
FileIO.WriteData(subFolder, "save.json", json);
string json = FileIO.ReadData(subFolder, "save.json");
bool exists = FileIO.DataExists(subFolder, "save.json");
```

### PathUtils

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| 概要 | 跨平台资源路径 Provider（Editor / Android / iOS / WebGL / Win），支持研发路径切换 |

```csharp
string path = PathUtils.GetFullPath(relativePath);
PathUtils.IsDevEnv = true; // 仅 Editor 有效
```

### HttpHelper

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| 概要 | `UnityWebRequest` + UniTask 的 GET/POST 封装，支持超时与取消 |

```csharp
var resp = await HttpHelper.GetAsync("https://api.example.com/data");
if (resp.IsSuccess) Debug.Log(resp.Text);
```

### IDUtils

| | |
|---|---|
| 命名空间 | `Dragon.Library` |
| 概要 | 线程安全的唯一 long ID 生成（时间戳 + 计数器） |

```csharp
long saveId = IDUtils.NewId();
```

### ObjTransformHelper

| | |
|---|---|
| 命名空间 | 全局 |
| 位置 | `Assets/GameMain/Library/Transform/ObjTransformHelper.cs` |
| 概要 | 场景物件本地 Transform 持续动效：上下/左右晃动、顺/逆时针旋转、呼吸缩放；可组合开关 |

适用于指示箭头、提示物件等装饰物。挂在目标节点上，在 Inspector 勾选 `DisplacementMode`，或运行时调用 `SetModes` / `ConfigureXxx`。

### ResourceManagerDebugger

| | |
|---|---|
| 命名空间 | `Dragon.Resource` |
| 概要 | 资源管理器调试辅助（统计缓存、引用计数等），开发期使用 |

### SpriteLoader

| | |
|---|---|
| 命名空间 | `Dragon.Resource` |
| 位置 | `Assets/GameMain/Library/Resource/AssetsLoader/SpriteLoader.cs` |
| 概要 | Image / SpriteRenderer 异步换图：先占位图再 Addressables 加载，避免闪烁 |

```csharp
SpriteLoader.Instance.Init(); // 加载占位 Sprite
SpriteLoader.Instance.SetImage(image, "icon_gold");
SpriteLoader.Instance.SetSpriteRenderer(spriteRenderer, "char_skin_01");
```

### ConfigDatasAutoGenWatcher / AutoConfigProcessor（Editor）

| | |
|---|---|
| 命名空间 | `Dragon.Editor` |
| 位置 | `Assets/GameMain/Library/Config/Editor/` |
| 概要 | 配置表 Editor 工具：`ConfigDatasAutoGenWatcher` 监听 `Config/Datas/` xlsx 变更并防抖触发 Luban Gen；`AutoConfigProcessor` 窗口扫描配置并维护 `ConfigFileList` |

```
Tools/Config/Auto Gen On Datas Change   ← 开关 xlsx 保存后自动 gen
Tools/Config/Auto Config Processor      ← 配置清单维护窗口
```

---

## 六、配置与数据（GameConfig / GameData）

| 模块 | 说明 |
|------|------|
| `cfg.Tables` / `GameConfig/` | Luban 生成的配置表代码 |
| `dragon/` | item、map、localization |
| `entity/dragon/` | player、enemy、npc、block、CharacterAttr |
| `skill/dragon/` | actionSkill、launchSkill、buffSkill、buff |
| `GameUserData` / `GameMetaData` | 运行时用户数据与存档元信息 |
| `ConfigFileList` | 配置清单 ScriptableObject，驱动 `ConfigManager` 批量加载 |

查表统一入口：`GameCenter.ConfigManager.Tables`（须等待 `ConfigManager.Init()` 完成）。

配置表修改流程：编辑 `Config/Datas/` 下 Excel → 运行 `Config/gen.bat`（或开启 `Tools/Config/Auto Gen On Datas Change`）→ 重新导入 Unity。

---

## 七、UI 基类与工具组件

### 基类约定

| 类型 | 基类 | 管理器 |
|------|------|--------|
| 主界面 / 单层 UI | `UIBase<TData>` | `GameUIManager` |
| 弹窗 | `PopupBase` / `PopupBase<TData>` | `PopupManager` / `UIQueueManager` |
| Toast | `UIToast` | `ToastManager` |
| 本地化文本 | `LocalizationUIHandler` + `LocalizationHelper` | 挂到 TMP 或代码一行绑定，随 `LocalManager.LanguageChanged` 刷新 |
| 红点展示 | `RedDotView` | 绑定 `RedDotKey`，监听 `EID_REDDOT_CHANGED` 自动刷新 |
| Debug 日志弹窗 | `PopupDebugLog` | 通过 `DebugLogManager.OpenPopup()` 打开 |

预制体通过 Addressables 地址加载，地址字符串通常与资源名一致（如 `"ui_lobby"`、`"popup_confirm"`）。

### UI 工具组件

挂载在 UI 预制体节点上，无需纳入 `GameCenter`（位于 `Library/UI/`）：

| 组件 | 说明 |
|------|------|
| `UIDisplayEffect` | Enable 时播放入场动效（ScaleIn / FadeIn）；可选退场（ScaleOut / FadeOut）；`PopupBase.Close` 会等待退场完成；可配开关窗 SFX |
| `UIButtonEffect` | 按钮按下/释放动效（如 ScalePressBounce）与点击音效 |
| `UITabGroup` / `UITabItem` | 通用页签组：Entries 配置页签与内容面板，支持默认选中与变更回调 |
| `UIObjectStateMachine` | 两组对象列表在 Collapsed / Expanded 状态间切换 |
| `UISpriteAnimator` | Image 帧动画播放，支持循环与 OnEnable 自动播放 |
| `UITMPOutlineEffect` | TMP 描边/面/底纹效果，编辑器即时预览 |
| `UIHoleMaskGraphic` | 全屏半透明遮罩 + 圆角矩形镂空（改 Mesh，无自定义 Shader） |
| `UIHoleUiTargetFollower` | 按 UI 目标同步镂空代理 Rect，供 `UIHoleMaskGraphic` 跟随 |
| `UIHoleWorldTargetFollower` | 世界坐标目标转屏幕镂空，供引导/教程高亮 |
| `RedDotView` | 红点显隐与计数（见 Logic/RedDot） |
| `RedDotEnterClear` | 按钮点击标记已读，配合 `SeenUntilEnter` / `OncePerSession` |

**引导/教程镂空示例：**

```
PopupTutorialSlide
  └─ UIHoleMaskGraphic（全屏遮罩）
       └─ UIHoleUiTargetFollower（ProxyRect → 跟随目标按钮）
```

---

## 八、推荐依赖关系（简图）

```mermaid
flowchart TB
    Boot[Boot 场景] --> GCS[GameCenterScript]
    GCS --> GC[GameCenter]
    GC --> Core[Core 内核]
    GC --> Logic[Logic 逻辑]
    Core --> RM[ResourceManager]
    Core --> ARS[AddressablesRemoteService]
    Core --> CM[ConfigManager]
    Core --> EM[EventManager]
    Core --> DM[GameDataManager]
    Core --> DLM[DebugLogManager]
    Core --> UI[GameUIManager / Popup / Toast]
    Logic --> MSM[MapSceneManager]
    Logic -.-> RDM[RedDotManager]
    MSM --> EntM[EntityManager]
    MSM --> CamM[CameraManager]
    MSM --> Combat[CombatDamageSettlement]
    CM --> Luban[cfg.Tables]
    DM --> Save[GameSaveManager]
    Save --> FIO[FileIO]
    RM --> Addr[Addressables]
    Config[Config/ Excel] -->|Luban gen| Luban
    TimeLib[UniTaskHelper] -.->|Interval| CountDown[Countdown / Stopwatch]
```

---

## 九、扩展新模块的建议

1. 实现单例或 `Instance` 访问器，必要时实现 `Init` / `Shutdown` / `Clear`。
2. 若需全局访问且参与启动流程，在 `GameCenter` 增加静态字段，并在 `NewCoreSystem` / `NewLogicSystem` 与对应 `Initialize` / `Uninitialize` 中注册。
3. 若需每帧更新，在 `GameCenter.Update` 或 `LateUpdate` 中调用。
4. 定时任务统一使用 `UniTaskHelper`（或基于它的 `CountdownHelper` / `StopwatchHelper`），按 owner 取消，避免散落 `Invoke` / 裸协程。
5. 跨模块通信优先使用 `EventManager` + `EventConstDefine` 分段 ID，避免硬引用。
6. 资源加载统一走 `ResourceManager`，配置读取走 `ConfigManager.Tables`。
7. 红点接入：在 `RedDotKey` 定义 key → 复制 `RedDotExampleProvider` 实现 → `RedDotRegistry` 注册 → `RedDotManager.Init()` → UI 挂 `RedDotView`。

---

## 技术栈

| 类别 | 技术 | 版本 |
|------|------|------|
| 引擎 | Unity | 2022.3.62f3c1 LTS |
| 渲染 | URP (2D) | 14.0.12 |
| 资源 | Addressables | 1.22.3 |
| 异步 / 定时 | UniTask + `UniTaskHelper` | Plugins 内置 |
| 配置 | Luban | 1.1.1 |
| 输入 | Unity Input System | 1.14.2 |
| 相机 | Cinemachine | 2.10.7 |
| 动画/UI 动效 | DOTween | Demigiant 插件 |
| 文本 | TextMesh Pro + MiSans 字体 | 3.0.9 |
| 序列化 | Newtonsoft.Json | 3.2.2 |
| IDE | Cursor IDE 包 | 2.0.27 |

---

## 近期架构变更摘要

| 变更 | 说明 |
|------|------|
| 删除 `SchedulerManager` | 定时调度改由 `UniTaskHelper` 承担，不再挂入 `GameCenter` 每帧 Update |
| 删除 `CoroutinePool` | 初始化与异步流程改用 UniTask（`.Forget()` / `await`） |
| `CountdownHelper` / `StopwatchHelper` | 内部改为 `UniTaskHelper.Interval` |
| 删除 `RoundManager` | 移除 `Logic/Round/` 回合制调度模块及相关类型 |
| UI 动效增强 | `UIDisplayEffect` 支持退场；新增 `UIButtonEffect`、`UITabGroup` |
| 场景装饰 | 新增 `ObjTransformHelper`（晃动 / 旋转 / 呼吸缩放） |
| **红点系统模板** | 新增 `Logic/RedDot/`：`RedDotManager` 树聚合 + `RedDotExampleProvider` 模板 + `RedDotRegistry` 注册 + `RedDotView` / `RedDotEnterClear` UI 绑定 |
| `AddressablesRemoteService` | 纳入 `GameCenter` Core；启动时 Catalog 更新与依赖下载，本地资源自动跳过 |
| `AddressablesLabelNames` | 从 `GlobalDefine` 迁至 `Library/Resource/`，`BootstrapEntries` 统一配置启动预加载 |
| 引导镂空 | 新增 `UIHoleMaskGraphic` + `UIHoleUiTargetFollower` / `UIHoleWorldTargetFollower` |
| 本地化 | `LocalizationUIHandler` + `LocalizationHelper` 替代旧 `LocalStringUIHandler`；Editor 预览 `LocalStringEditorPreview` |
| 配置 Editor | `ConfigDatasAutoGenWatcher`（xlsx 变更自动 Gen）、`AutoConfigProcessor`（配置清单窗口） |
| `SpriteLoader` | Image / SpriteRenderer 异步换图占位加载 |
