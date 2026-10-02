# 会议纪要 API（用户级）

## 接口名称
**飞书会议纪要 API -（IFeishuUserV1VideoConferencingNotes）**

## 功能描述
飞书会议纪要资源，用户可以查看会议生成的纪要文档、逐字稿等产物，并获取相关上下文（例如会中共享文档等），可以用于复盘、检索增强、对齐校验与可追溯引用。
当前接口使用用户令牌访问，适应于用户应用场景。

## 参考文档
- [飞书官方文档 - 会议纪要概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/notes_overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
|---------|---------|---------|----------|----------|
| GetNoteAsync | 获取纪要详情 | 用户令牌 | GET | [GetNoteAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/get) |
| SubscriptionNoteAsync | 订阅纪要变更事件 | 用户令牌 | POST | [SubscriptionNoteAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/subscription) |
| UnSubscriptionNoteAsync | 取消订阅纪要变更事件 | 用户令牌 | POST | [UnSubscriptionNoteAsync](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/unsubscription) |

---

## 函数详细内容

### 获取纪要详情

**函数签名**：
```csharp
Task<FeishuApiResult<GetNoteResult>?> GetNoteAsync(
   [Path] string note_id,
   [Query] string? user_id_type = "open_id",
   CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| note_id | string | ✅ | 纪要 ID，示例值：`6943848821689040898` |
| user_id_type | string? | ⚪ | 用户 ID 类型，可选值：`open_id`、`union_id`、`user_id`，默认值：`open_id` |

**接口文档**：[获取纪要详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/get)

**说明**：获取一篇纪要的详细数据。只能获取自己可见纪要文档，以及相关联的产物、关联引用信息。

---

### 订阅纪要变更事件

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> SubscriptionNoteAsync(
    [Body] SubscriptionNoteRequest request,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| request | SubscriptionNoteRequest | ✅ | 订阅纪要变更事件请求体 |

**接口文档**：[订阅纪要变更事件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/subscription)

**说明**：订阅当前用户身份相关的纪要资源变更事件。通过指定事件类型，来订阅纪要资源不同的事件变更。

---

### 取消订阅纪要变更事件

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> UnSubscriptionNoteAsync(
  [Body] UnSubscriptionNoteRequest request,
  CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| request | UnSubscriptionNoteRequest | ✅ | 取消订阅纪要变更事件请求体 |

**接口文档**：[取消订阅纪要变更事件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/unsubscription)

**说明**：取消订阅当前用户身份相关的纪要资源变更事件。通过指定事件类型，来取消订阅纪要资源对应的事件变更。
