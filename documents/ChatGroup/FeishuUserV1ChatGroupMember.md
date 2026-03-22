# 飞书群成员 API（用户级）

## 接口名称
**飞书群成员 API -（IFeishuUserV1ChatGroupMember）**

## 功能描述
飞书群成员包括用户和机器人。在飞书群组内，支持添加用户或者机器人作为群成员，同时支持将用户或者机器人设置为群管理员。
当前接口使用用户令牌访问，适应于用户应用场景。

## 参考文档
- [飞书官方文档 - 群成员](https://open.feishu.cn/document/server-docs/group/chat-member/intro)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| AddManagersAsync | 添加群管理员 | 用户令牌 | POST |
| DeleteManagersAsync | 删除群管理员 | 用户令牌 | POST |
| AddMemberAsync | 添加群成员 | 用户令牌 | POST |
| MeJoinChatGroupAsync | 主动加入群聊 | 用户令牌 | PATCH |
| RemoveMemberAsync | 移除群成员 | 用户令牌 | DELETE |
| GetMemberPageListByIdAsync | 分页获取群成员列表 | 用户令牌 | GET |
| GetMemberInChatByIdAsync | 判断是否在群中 | 用户令牌 | GET |

---

## 函数详细内容

### 添加群管理员

**函数名称**：添加群管理员

**函数签名**：
```csharp
Task<FeishuApiResult<GroupManagerResult>?> AddManagersAsync(
    [Path] string chat_id,
    [Body] GroupManagerRequest addGroupManagerRequest,
    [Query("member_id_type")] string member_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| addGroupManagerRequest | GroupManagerRequest | ✅ | 指定群管理员请求体 |
| member_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "manager_ids": ["ou_xxx"]
  }
}
```

**说明**：当前用户必须是群主才能添加管理员。

**代码示例**：
```csharp
public class UserGroupMemberService
{
    private readonly IFeishuUserV1ChatGroupMember _memberClient;

    public UserGroupMemberService(IFeishuUserV1ChatGroupMember memberClient)
    {
        _memberClient = memberClient;
    }

    public async Task AssignManagerAsync(string chatId, string userId)
    {
        var request = new GroupManagerRequest
        {
            manager_ids = new List<string> { userId }
        };

        await _memberClient.AddManagersAsync(chatId, request);
    }
}
```

---

### 删除群管理员

**函数名称**：删除群管理员

**函数签名**：
```csharp
Task<FeishuApiResult<GroupManagerResult>?> DeleteManagersAsync(
    [Path] string chat_id,
    [Body] GroupManagerRequest deleteGroupManagerRequest,
    [Query("member_id_type")] string member_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| deleteGroupManagerRequest | GroupManagerRequest | ✅ | 删除群管理员请求体 |
| member_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

---

### 添加群成员

**函数名称**：添加群成员

**函数签名**：
```csharp
Task<FeishuApiResult<AddMemberResult>?> AddMemberAsync(
    [Path] string chat_id,
    [Body] MembersRequest addMemberRequest,
    [Query("member_id_type")] string member_id_type = "open_id",
    [Query("succeed_type")] int succeed_type = 0,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| addMemberRequest | MembersRequest | ✅ | 将用户或机器人拉入群聊请求体 |
| member_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |
| succeed_type | int | ⚪ | 出现不可用ID后的处理方式，0/1/2，默认值：0 |

---

### 主动加入群聊

**函数名称**：主动加入群聊

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> MeJoinChatGroupAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |

**响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

**说明**：将当前调用接口的用户加入指定群聊。群组需要设置为允许成员主动加入。

---

### 移除群成员

**函数名称**：移除群成员

**函数签名**：
```csharp
Task<FeishuApiResult<RemoveMemberResult>?> RemoveMemberAsync(
    [Path] string chat_id,
    [Body] MembersRequest membersRequest,
    [Query("member_id_type")] string member_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| membersRequest | MembersRequest | ✅ | 移除成员 ID 列表请求体 |
| member_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

---

### 分页获取群成员列表

**函数名称**：分页获取群成员列表

**函数签名**：
```csharp
Task<FeishuApiResult<GetMemberPageListResult>?> GetMemberPageListByIdAsync(
    [Path] string chat_id,
    [Query("user_id_type")] string user_id_type = "open_id",
    [Query("page_size")] int? page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| user_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |
| page_size | int? | ⚪ | 分页大小，默认值：10 |
| page_token | string? | ⚪ | 分页标记 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "member_id": "ou_xxx",
        "member_type": "user",
        "name": "张三"
      }
    ],
    "has_more": true
  }
}
```

---

### 判断是否在群中

**函数名称**：判断是否在群中

**函数签名**：
```csharp
Task<FeishuApiResult<GetMemberIsInChatResult>?> GetMemberInChatByIdAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "is_in_chat": true
  }
}
```
