# 飞书群成员 API（租户级）

## 接口名称
**飞书群成员 API -（IFeishuTenantV1ChatGroupMember）**

## 功能描述
飞书群成员包括用户和机器人。在飞书群组内，支持添加用户或者机器人作为群成员，同时支持将用户或者机器人设置为群管理员。
当前接口使用租户令牌访问，适应于租户应用场景。

## 参考文档
- [飞书官方文档 - 群成员](https://open.feishu.cn/document/server-docs/group/chat-member/intro)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| AddManagersAsync | 添加群管理员 | 租户令牌 | POST |
| DeleteManagersAsync | 删除群管理员 | 租户令牌 | POST |
| AddMemberAsync | 添加群成员 | 租户令牌 | POST |
| MeJoinChatGroupAsync | 主动加入群聊 | 租户令牌 | PATCH |
| RemoveMemberAsync | 移除群成员 | 租户令牌 | DELETE |
| GetMemberPageListByIdAsync | 分页获取群成员列表 | 租户令牌 | GET |
| GetMemberInChatByIdAsync | 判断是否在群中 | 租户令牌 | GET |

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

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID，示例："oc_a0553eda9014c201e6969b478895c230" |
| addGroupManagerRequest | GroupManagerRequest | ✅ | 指定群管理员请求体 |
| member_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "manager_ids": ["ou_xxx", "ou_yyy"]
  }
}
```

**代码示例**：
```csharp
public class GroupMemberService
{
    private readonly IFeishuTenantV1ChatGroupMember _memberClient;

    public GroupMemberService(IFeishuTenantV1ChatGroupMember memberClient)
    {
        _memberClient = memberClient;
    }

    public async Task PromoteToManagerAsync(string chatId, string userId)
    {
        var request = new GroupManagerRequest
        {
            manager_ids = new List<string> { userId }
        };

        var result = await _memberClient.AddManagersAsync(chatId, request);
        if (result?.Code == 0)
        {
            Console.WriteLine("管理员添加成功");
        }
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

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| deleteGroupManagerRequest | GroupManagerRequest | ✅ | 删除群管理员请求体 |
| member_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "manager_ids": []
  }
}
```

**说明**：删除群组内指定的管理员，包括用户类型的管理员和机器人类型的管理员。

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

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| addMemberRequest | MembersRequest | ✅ | 将用户或机器人拉入群聊请求体 |
| member_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |
| succeed_type | int | ⚪ | 出现不可用ID后的处理方式，0/1/2，默认值：0 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "invalid_ids": [],
    "invalid_id_failure": {}
  }
}
```

**说明**：succeed_type 可选值：
- 0：不存在/不可见的 ID 会拉群失败，并返回错误响应
- 1：将参数中可用的 ID 全部拉入群聊，返回拉群成功的响应，并展示剩余不可用的 ID 及原因
- 2：参数中只要存在任一不可用的 ID，就会拉群失败，返回错误响应，并展示出不可用的 ID

**代码示例**：
```csharp
public async Task AddMembersToGroupAsync(string chatId, List<string> userIds)
{
    var request = new MembersRequest
    {
        id_list = userIds
    };

    // 使用 succeed_type=1，允许部分成功
    var result = await _memberClient.AddMemberAsync(
        chatId, 
        request, 
        succeed_type: 1);
    
    if (result?.Data?.invalid_ids?.Count > 0)
    {
        Console.WriteLine($"以下用户添加失败: {string.Join(",", result.Data.invalid_ids)}");
    }
}
```

---

### 主动加入群聊

**函数名称**：主动加入群聊

**函数签名**：
```csharp
Task<FeishuNullDataApiResult?> MeJoinChatGroupAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

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

**说明**：将当前调用接口的操作者（用户或机器人）加入指定群聊。群组需要设置为允许成员主动加入。

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

**认证**：租户令牌

**参数**：

| 参数 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| chat_id | string | ✅ | 群 ID |
| membersRequest | MembersRequest | ✅ | 移除成员 ID 列表请求体 |
| member_id_type | string | ⚪ | 用户 ID 类型，默认值："open_id" |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "removed_ids": ["ou_xxx"]
  }
}
```

**说明**：将指定的用户或机器人从群聊中移出。

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

**认证**：租户令牌

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
    "page_token": "next_token",
    "has_more": true
  }
}
```

**代码示例**：
```csharp
public async Task ListAllMembersAsync(string chatId)
{
    string? pageToken = null;
    do
    {
        var result = await _memberClient.GetMemberPageListByIdAsync(
            chatId,
            page_token: pageToken,
            page_size: 100);
        
        if (result?.Data?.items != null)
        {
            foreach (var member in result.Data.items)
            {
                Console.WriteLine($"成员: {member.name} ({member.member_id})");
            }
        }
        pageToken = result?.Data?.page_token;
    } while (!string.IsNullOrEmpty(pageToken));
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

**认证**：租户令牌

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

**说明**：根据使用的 access_token 判断对应的用户或者机器人是否在指定的群里。
