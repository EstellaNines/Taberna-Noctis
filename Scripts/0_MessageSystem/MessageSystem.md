## 消息系统一页速查（订阅 / 发送 / 退订）

本页提供最常用调用范式、示例代码与脚本作用说明，便于快速上手与团队统一约定。

### 核心脚本

- `MessageManager`：运行时消息总线（字符串 Key 通道 + 泛型类型通道），含内置日志与快照接口。
- `MessageSystemWindow`：编辑器监控与调试窗口（菜单：自制工具/消息系统/消息系统），支持表格、排序、列宽拖拽、按 Channel/Type 过滤、导出 JSON、日志控制。

---

## 快速范式：按字符串 Key 通道

### 订阅 / 退订（推荐在 OnEnable/OnDisable 中成对使用）

```csharp
using UnityEngine;
using UnityEngine.Events;

public class KeySubscriber : MonoBehaviour
{
	private void OnEnable()
	{
		MessageManager.Register<string>("User/Login", OnUserLogin);
		MessageManager.Register<int>("Score/Changed", OnScoreChanged);
	}

	private void OnDisable()
	{
		MessageManager.Remove<string>("User/Login", OnUserLogin);
		MessageManager.Remove<int>("Score/Changed", OnScoreChanged);
	}

	private void OnUserLogin(string userId)
	{
		Debug.Log($"User/Login: {userId}");
	}

	private void OnScoreChanged(int newScore)
	{
		Debug.Log($"Score/Changed: {newScore}");
	}
}
```

### 发送

```csharp
MessageManager.Send("User/Login", "player_001");
MessageManager.Send("Score/Changed", 123);
```

### 清空某 Key 全部订阅

```csharp
MessageManager.ClearKey("Score/Changed");
```

---

## 快速范式：按类型通道（以泛型类型为 Key）

### 定义消息类型（建议重写 ToString 便于日志可读）

```csharp
public struct DamageEvent
{
	public string source;
	public int amount;
	public override string ToString() => $"{source}:{amount}";
}
```

### 订阅 / 退订

```csharp
public class TypeSubscriber : MonoBehaviour
{
	private void OnEnable()
	{
		MessageManager.Register<DamageEvent>(OnDamage);
	}

	private void OnDisable()
	{
		MessageManager.Remove<DamageEvent>(OnDamage);
	}

	private void OnDamage(DamageEvent e)
	{
		Debug.Log($"Damage from {e.source}, {e.amount}");
	}
}
```

### 发送

```csharp
MessageManager.Send(new DamageEvent { source = "EnemyA", amount = 15 });
```

### 全量清空（字符串与类型通道都清空）

```csharp
MessageManager.Clear();
```

---

## 调试与监控（编辑器）

- 打开窗口：自制工具/消息系统/消息系统
- 功能包含：
  - 表格监控：Frame、Time(s)、Channel、Type、Payload
  - 表头点击排序、列宽拖拽
  - 文本过滤（按 Channel/Type）
  - 实时滚动查看、清空日志、设置最大条数、启用/禁用日志
  - 导出 JSON（当前日志视图）

### 日志控制（代码）

```csharp
MessageManager.SetLogEnabled(true);
MessageManager.SetMaxLog(1000);
var logs = MessageManager.GetLogSnapshot();
MessageManager.ClearLog();
```

### 运行时订阅快照（概览）

```csharp
var sSnap = MessageManager.GetStringBusSnapshot();   // (key, payloadType, subscribers)
var tSnap = MessageManager.GetTypeBusSnapshot();     // (payloadType, subscribers)
```

---

## 对外公开 API 清单（常用）

### 字符串 Key 通道

- Register<T>(string key, UnityAction<T> action)
- Remove<T>(string key, UnityAction<T> action)
- Send<T>(string key, T data)
- ClearKey(string key)

### 类型通道（以泛型类型为 Key）

- Register<T>(UnityAction<T> action)
- Remove<T>(UnityAction<T> action)
- Send<T>(T data)

### 全局清理

- Clear()

### 日志与快照

- SetLogEnabled(bool), GetLogEnabled()
- SetMaxLog(int), GetMaxLog()
- GetLogSnapshot(), ClearLog()
- GetStringBusSnapshot(), GetTypeBusSnapshot()

---

## 设计与规范建议

- 在 `OnEnable` 中注册、在 `OnDisable` 中退订，以避免对象失活后仍被回调。
- 同一字符串 Key 建议团队约定固定负载类型，避免混用导致覆盖。
- 为自定义消息类型实现 `ToString()`，提升日志可读性。
- 本实现使用 `lock` 保护字典，适合主线程消息；若跨线程，请自行在业务侧切回主线程。

---

## 脚本作用说明

- `Scripts/MessageSystem/MessageManager.cs`

  - 统一的消息中心：
    - 字符串 Key 通道与泛型类型通道
    - 订阅、发送、退订、按 Key 清除、全量清空
    - 内置时间戳/帧号日志缓冲（可配置最大条数、启用/禁用）
    - 运行时快照接口（字符串/类型通道订阅数）

- `Scripts/Editor/MessageSystemWindow.cs`
  - 编辑器监控与操作平台：
    - 实时表格（Frame/Time/Channel/Type/Payload），点击表头排序
    - 列宽拖拽、按 Channel/Type 过滤
    - 日志滚动查看、清空日志、设置最大条数、开关日志
    - 一键导出 JSON
    - 快速测试发送（字符串 Key 与类型通道）

---

## 变更记录（摘）

- 新增：表格列宽拖拽、按通道/类型过滤、导出 JSON。
