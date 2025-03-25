```markdown

专为解析《星际争霸2》回放文件中协议数据的C#库，提供核心二进制数据解码能力。

## 核心功能

✅ **基础协议解析**
- 回放头部信息提取（版本/时长/签名验证）
- 游戏事件流解码（单位操作/技能触发）
- 玩家消息解析（聊天内容/系统提示）
- 游戏统计数据分析（资源消耗/科技树）

✅ **数据结构支持**
- PyList：动态对象集合（支持索引访问）
- PyTuple：固定长度有序集合
- PyDictionary：键值对映射容器

✅ **多线程加速**
- 并行事件流解析（`quick_decode_replay_game_events`方法）

## 代码示例

### 基础解析

```csharp
// 初始化协议解析器
var protocol = S2Protocol.Latest;

// 解析回放头部信息
var headerData = File.ReadAllBytes("replay.header");
var header = protocol.decode_replay_header(headerData) as Replay.Header;

// 输出基础信息
Console.WriteLine($"游戏版本: {header.m_version.m_major}.{header.m_version.m_minor}");
Console.WriteLine($"对战时长: {header.m_elapsedGameLoops / 16}秒");
```

### 事件处理
```csharp
// 解析游戏事件流
var gameEventsData = File.ReadAllBytes("replay.game.events");
var gameEvents = protocol.decode_replay_game_events(gameEventsData);

// 遍历前10个事件
foreach (var evt in gameEvents.Take(10)) 
{
    Console.WriteLine($"[帧数:{evt["_gameloop"]}] 事件类型:{evt["_event"]}");
}
```

### 玩家信息提取
```csharp
// 解析详细数据
var detailsData = File.ReadAllBytes("replay.details");
var details = protocol.decode_replay_details(detailsData) as Replay.Details;

// 显示玩家列表
foreach (var player in details.m_playerList)
{
    Console.WriteLine($"玩家 {player.m_name} ({player.m_race})");
    Console.WriteLine($"种族: {Encoding.UTF8.GetString((byte[])player.m_race)}");
}
```

## 性能说明

```csharp
// 多线程加速模式（实测提升3-4倍）
var quickResults = protocol.quick_decode_replay_game_events(gameEventsData);


## 协议支持

通过派生类实现版本适配：
```csharp
public class Protocol75689 : S2Protocol
{
    // 版本特定类型配置
    public Protocol75689(){
        typeinfos = new Parser("...").Parse() as PyList;
        game_eventid_typeid = 27;
        // 其他版本特定初始化...
    }
}
```

## 技术实现

### 解码流程架构
```
原始字节流 → BitPackedDecoder → PyDictionary → Replay对象模型
```

### 核心组件
- `BitPackedBuffer`：位级数据读取器
- `BitPackedDecoder`：基础协议解码器
- `VersionedDecoder`：版本化协议处理器
- `Parser`：类Python语法解析器