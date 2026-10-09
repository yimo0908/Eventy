# EventyCN

再也不忘记任何季节活动。简单实用的日历插件，在服务器栏中显示当前活动，支持浏览当前、即将到来和过往的活动，重要日期高亮显示。

![日历](EventyCN/images/EventyCalender.gif)

## 功能

- 月历视图浏览游戏内活动，支持按月翻页和「今天」快速跳转
- 服务器栏（DTR）入口显示当前进行中的活动数量，鼠标悬停查看详情，点击打开日历
- 活动按颜色区分，跨天活动在日历上以色块条展示
- 点击日历中的活动色块可直接跳转到活动详情页面
- 支持标记活动为「已完成」，已完成活动可在日历和服务器栏中隐藏
- 服务器栏支持完整文字和简短图标两种显示模式
- 无活动时可选择自动隐藏服务器栏入口
- 活动数据从国服官方 API 实时获取，按月按需加载，自动去重跨月活动

## 截图

### 日历

![日历](EventyCN/images/EventyCalender.gif)

### 服务器栏

![服务器栏](EventyCN/images/EventyServerBar.gif)

## 使用方法

| 命令 | 说明 |
|------|------|
| `/eventycn` | 打开活动日历 |
| `/eventycnconf` | 打开设置界面 |

也可通过卫月插件列表中的「打开主界面」/「打开设置」按钮访问，或点击服务器栏中的活动入口直接打开日历。

## How to get

1. 卫月设置 → 测试版
2. 添加仓库 `https://raw.githubusercontent.com/yimo0908/DalamudPlugin/main/repo.json` 并启用
3. 从插件列表中安装

## 项目结构

```
Eventy/
├── EventyCN/                       # 插件主项目
│   ├── EventyCN.csproj             # 项目文件，SDK 为 Dalamud.CN.NET.Sdk
│   ├── EventyCN.json               # 插件元数据（名称、描述、图标等）
│   ├── Plugin.cs                   # 插件入口，注册命令、窗口与活动数据加载
│   ├── Configuration.cs            # 插件配置（服务器栏显示、已完成活动等）
│   ├── Globals.cs                  # 全局 using 引用
│   ├── ServerBar.cs                # 服务器栏 DTR 条目管理与刷新
│   ├── Updater.cs                  # 活动 API 请求（国服官方接口）
│   ├── Utils.cs                    # 工具方法（日期遍历、打开链接）
│   ├── Attributes/                 # 命令管理特性与命令管理器
│   │   ├── CommandAttribute.cs     # 命令标记特性
│   │   ├── AliasesAttribute.cs     # 命令别名特性
│   │   ├── HelpMessageAttribute.cs # 帮助消息特性
│   │   ├── DoNotShowInHelpAttribute.cs
│   │   └── PluginCommandManager.cs # 反射注册命令管理器
│   ├── Windows/
│   │   ├── Helper.cs               # UI 工具方法（文本换行、颜色转换）
│   │   ├── Main/
│   │   │   └── MainWindow.cs       # 主窗口：月历布局、活动色块、导航
│   │   └── Config/
│   │       ├── ConfigWindow.cs            # 设置窗口入口（Tab 布局）
│   │       ├── ConfigWindow.Settings.cs   # 设置标签页（服务器栏选项）
│   │       └── ConfigWindow.Completed.cs  # 已完成标签页（活动完成标记）
│   └── images/                     # 截图与图标
│       ├── EventyCalender.gif
│       ├── EventyServerBar.gif
│       └── icon.png
├── EventyCN.sln                    # 解决方案文件
├── LICENSE                         # MIT 许可证
└── README.md
```

### 核心文件说明

| 文件 | 职责 |
|------|------|
| `Plugin.cs` | 插件入口点，注册 `/eventycn` 与 `/eventycnconf` 命令，初始化 `MainWindow` 与 `ConfigWindow`，启动活动数据加载（当前月 ± 2 月），管理 `Events` 数据字典 |
| `Configuration.cs` | 持久化配置：服务器栏显示开关、简短模式、无活动时隐藏、已完成活动显示开关、已完成活动 ID 集合 |
| `ServerBar.cs` | 服务器栏 DTR 条目管理：每 5 秒刷新当前活动，构建 Tooltip 文本，处理点击打开日历，根据配置控制可见性 |
| `Updater.cs` | 通过 HTTP 请求国服官方 API `apiff14risingstones.web.sdo.com` 获取指定月份的活动日历数据 |
| `Utils.cs` | 工具方法：`EachDay` 遍历日期范围内的每一天（用于跨天活动展开），`OpenUrl` 安全打开外部链接 |
| `MainWindow.cs` | 月历主窗口：绘制月历网格、活动色块、日期导航（月/年翻页 + 今天按钮）、活动悬停提示与点击跳转 |
| `ConfigWindow.*.cs` | 设置窗口使用 partial class 拆分为三个文件，分别负责窗口入口/Tab 布局、设置选项和已完成活动管理 |
| `Attributes/` | 命令管理框架：通过 `[Command]`、`[Aliases]`、`[HelpMessage]` 特性标注方法，`PluginCommandManager<T>` 反射注册和注销命令 |

## 维护说明

### 本地构建

前提：[.NET 10 SDK](https://dotnet.microsoft.com/download)，XIVLauncher / Dalamud 已安装（用于运行插件）

构建：

```powershell
dotnet build EventyCN\EventyCN.csproj -c Release
```

安装（开发者模式）：

1. 构建后获取 `EventyCN.dll`（位于 `bin\x64\Release\`）
2. 在 Dalamud 设置 → Experimental → Dev Plugin Locations 中添加 DLL 所在文件夹路径
3. 在 Dalamud 插件管理器的 Dev Tools 中启用已加载的开发插件

### 活动数据来源

活动数据来自国服官方 API：

- 接口地址：`https://apiff14risingstones.web.sdo.com/api/home/active/calendar/getActiveCalendarMonth`
- 请求参数：`?month=YYYY-MM`（如 `?month=2026-01`）
- 返回格式：JSON，包含 `code`（状态码，`10000` 为成功）和 `data`（活动数组）

每个活动包含以下字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 活动唯一 ID |
| `name` | string | 活动名称 |
| `url` | string | 活动详情页面链接 |
| `begin_time` | long | 活动开始时间（Unix 时间戳，秒） |
| `end_time` | long | 活动结束时间（Unix 时间戳，秒） |
| `color` | string | 活动颜色（十六进制，如 `#D98481`） |
| `type` | int | 活动类型（`1` 为版本类，加载时过滤） |
| `weight` | int | 活动权重 |
| `banner_url` | string? | 活动 Banner 图片 URL |

### 数据加载机制

1. 插件启动时加载当前月 ± 2 月，共 5 个月的数据
2. 用户翻页到新月份时按需加载该月数据
3. 跨月活动会在其持续期间的每一天都显示，通过 ID 去重避免重复添加
4. 已加载月份记录在 `_loadedMonths` 集合中，不会重复请求
5. 数据加载完成后自动刷新服务器栏

### 开发约定

- **Dalamud 服务注入**：使用 `[PluginService]` 特性静态注入 `IDalamudPluginInterface`、`ICommandManager`、`IPluginLog` 等 Dalamud 服务
- **命令注册**：通过 `[Command]` / `[HelpMessage]` 特性标注方法，由 `PluginCommandManager<Plugin>` 反射注册，避免手动 `AddHandler`
- **配置修改**：直接修改 `Configuration` 字段后调用 `Save()`，设置窗口中统一使用 `changed` 标志位批量保存
- **颜色处理**：API 返回的十六进制颜色通过 `Helper.HexToUint` 转换为 ImGui 使用的 ABGR uint 格式，同时生成半透明版本用于非当月日期
- **可见性**：`ParsedEvent` 为 `struct`，`CnEvent` / `CnA[piResponCredits

- 原版插件 [Eventy](https://githu.com/Infiziert90/Eventy) 由 [Infi](https://github.com/Infiziert90) 开发
- 活动数据来自国服官方 API（[FFXIV Rising Stones](https://ff14risingstones.web.sdo.com/)）

### Icon Credit (Modified)

<a href="https://www.flaticon.com/free-icons/calendar" title="calendar icons">Calendar icons created by DinosoftLabs - Flaticon</a>

## License

[MIT](./LICENSE)
