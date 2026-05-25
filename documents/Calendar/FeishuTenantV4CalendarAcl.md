# 日历访问控制 - 租户令牌
**IFeishuTenantV4CalendarAcl**

## 功能描述
访问控制（ACL）用于管理日历的成员权限。一个日历内可以创建多个 ACL，每一个 ACL 内可以为一个成员设置日历的访问权限。访问权限包括：游客（只能看到忙闲信息）、订阅者（可查看所有日程详情）、编辑者（可在日历内创建或修改日程）、管理员（可管理日历及共享设置）。

## 参考文档
- [日历访问控制概述](https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/introduction)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreateCalendarAclAsync | 创建访问控制 | 租户令牌 | POST |
| DeleteCalendarAclAsync | 删除访问控制 | 租户令牌 | DELETE |
| GetCalendarAclsPageListAsync | 分页获取访问控制列表 | 租户令牌 | GET |

## 函数详细内容

### CreateCalendarAclAsync
以应用身份为指定日历添加访问控制，即日历成员权限。

**函数签名**
```csharp
Task<FeishuApiResult<CreateCalendarAclResult>?> CreateCalendarAclAsync(
    string calendar_id,
    CreateCalendarAclRequest createCalendarAclRequest,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| createCalendarAclRequest | CreateCalendarAclRequest | ✅ | 创建访问控制请求体 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 权限角色包括：unknown（未知/游客）、free_busy_reader（只查看忙闲）、reader（订阅者）、writer（编辑者）、owner（管理员）

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "acl_id": "user_xxxxxx",
    "role": "writer"
  }
}
```

**代码示例**
```csharp
var request = new CreateCalendarAclRequest
{
    Role = "writer",
    Scope = new AclScope { Type = "user", UserId = "ou_xxx" }
};
var result = await api.CreateCalendarAclAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn",
    createCalendarAclRequest: request
);
```

---

### DeleteCalendarAclAsync
以应用身份删除指定日历内的某一访问控制。

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteCalendarAclAsync(
    string calendar_id,
    string acl_id,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| acl_id | string | ✅ | 访问控制 ID | user_xxxxxx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success"
}
```

**代码示例**
```csharp
var result = await api.DeleteCalendarAclAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn",
    acl_id: "user_xxxxxx"
);
```

---

### GetCalendarAclsPageListAsync
以应用身份分页获取指定日历的访问控制列表。

**函数签名**
```csharp
Task<FeishuApiResult<GetCalendarEventPageListResult>?> GetCalendarAclsPageListAsync(
    string calendar_id,
    int page_size = 20,
    string? page_token = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| calendar_id | string | ✅ | 日历 ID | feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn |
| page_size | int | ⚪ | 分页大小 | 20 |
| page_token | string | ⚪ | 分页标记，首次查询不填 | - |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [],
    "page_token": "",
    "has_more": false
  }
}
```

**代码示例**
```csharp
var result = await api.GetCalendarAclsPageListAsync(
    calendar_id: "feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn",
    page_size: 20
);
Console.WriteLine($"访问控制列表: {result?.Data?.Items?.Count}");
```