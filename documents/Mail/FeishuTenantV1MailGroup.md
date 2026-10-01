---
title: 邮件组接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份管理企业内所有邮件组，包括邮件组管理、邮件组管理员、成员、别名及权限成员管理等。
---

# IFeishuTenantV1MailGroup - 租户邮件组API

## 功能描述
飞书邮件组API接口实现了邮件组管理、邮件组管理员管理、邮件组成员管理、邮件组别名管理、邮件组权限成员管理等管理功能。支持租户管理员通过租户访问令牌管理企业内所有邮件组。

## 参考文档
- [创建邮件组](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/create)
- [删除邮件组](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/delete)
- [修改邮件组部分信息](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/patch)
- [修改邮件组全部信息](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/update)
- [查询指定邮件组](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/get)
- [分页批量获取邮件组](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/list)
- [批量创建邮件组管理员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/batch_create)
- [批量删除邮件组管理员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/batch_delete)
- [批量获取邮件组管理员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/list)
- [创建邮件组成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/create)
- [删除邮件组成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/delete)
- [查询指定邮件组成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/get)
- [分页获取所有邮件组成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/list)
- [批量创建邮件组成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/batch_create)
- [批量删除邮件组成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/batch_delete)
- [创建邮件组别名](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-alias/create)
- [删除邮件组别名](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-alias/delete)
- [获取邮件组所有别名](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-alias/list)
- [创建邮件组权限成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/create)
- [删除邮件组权限成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/delete)
- [获取邮件组权限成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/get)
- [分页批量获取邮件组权限成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/list)
- [批量创建邮件组权限成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/batch_create)
- [批量删除邮件组权限成员](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/batch_delete)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 | 接口文档 |
| :--- | :--- | :--- | :--- |----------|
| CreateMailGroupAsync | 创建邮件组 | TenantAccessToken | POST | [CreateMailGroupAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/create) |
| DeleteMailGroupAsync | 删除邮件组 | TenantAccessToken | DELETE | [DeleteMailGroupAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/delete) |
| UpdateMailGroupPartialAsync | 修改邮件组部分信息 | TenantAccessToken | PATCH | [UpdateMailGroupPartialAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/patch) |
| UpdateMailGroupAsync | 修改邮件组全部信息 | TenantAccessToken | PUT | [UpdateMailGroupAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/update) |
| GetMailGroupAsync | 查询指定邮件组 | TenantAccessToken | GET | [GetMailGroupAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/get) |
| GetMailGroupPageListAsync | 分页批量获取邮件组 | TenantAccessToken | GET | [GetMailGroupPageListAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup/list) |
| BatchCreateMailgroupManagerAsync | 批量创建邮件组管理员 | TenantAccessToken | POST | [BatchCreateMailgroupManagerAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/batch_create) |
| BatchDeleteMailGroupManagerAsync | 批量删除邮件组管理员 | TenantAccessToken | POST | [BatchDeleteMailGroupManagerAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/batch_delete) |
| GetMailgroupManagerPageListAsync | 批量获取邮件组管理员 | TenantAccessToken | GET | [GetMailgroupManagerPageListAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-manager/list) |
| CreateMailGroupMemberAsync | 创建邮件组成员 | TenantAccessToken | POST | [CreateMailGroupMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/create) |
| DeleteMailGroupMemberAsync | 删除邮件组成员 | TenantAccessToken | DELETE | [DeleteMailGroupMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/delete) |
| GetMailGroupMemberAsync | 查询指定邮件组成员 | TenantAccessToken | GET | [GetMailGroupMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/get) |
| GetMailGroupMemberPageListAsync | 分页获取所有邮件组成员 | TenantAccessToken | GET | [GetMailGroupMemberPageListAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/list) |
| BatchCreateMailGroupMemberAsync | 批量创建邮件组成员 | TenantAccessToken | POST | [BatchCreateMailGroupMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/batch_create) |
| BatchDeleteMailGroupMemberAsync | 批量删除邮件组成员 | TenantAccessToken | DELETE | [BatchDeleteMailGroupMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-member/batch_delete) |
| CreateMailGroupAliasAsync | 创建邮件组别名 | TenantAccessToken | POST | [CreateMailGroupAliasAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-alias/create) |
| DeleteMailGroupAliasAsync | 删除邮件组别名 | TenantAccessToken | DELETE | [DeleteMailGroupAliasAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-alias/delete) |
| GetMailGroupAliasListAsync | 获取邮件组所有别名 | TenantAccessToken | GET | [GetMailGroupAliasListAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-alias/list) |
| CreateMailGroupPermissionMemberAsync | 创建邮件组权限成员 | TenantAccessToken | POST | [CreateMailGroupPermissionMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/create) |
| DeleteMailGroupPermissionMemberAsync | 删除邮件组权限成员 | TenantAccessToken | DELETE | [DeleteMailGroupPermissionMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/delete) |
| GetailGroupPermissionMemberAsync | 获取邮件组权限成员 | TenantAccessToken | GET | [GetailGroupPermissionMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/get) |
| GetMailgroupPermissionMemberPageListAsync | 分页批量获取邮件组权限成员 | TenantAccessToken | GET | [GetMailgroupPermissionMemberPageListAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/list) |
| BatchCreateMailGroupPermissionMembersAsync | 批量创建邮件组权限成员 | TenantAccessToken | POST | [BatchCreateMailGroupPermissionMembersAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/batch_create) |
| BatchDeleteMailGroupPermissionMemberAsync | 批量删除邮件组权限成员 | TenantAccessToken | DELETE | [BatchDeleteMailGroupPermissionMemberAsync](https://open.feishu.cn/document/server-docs/mail-v1/mail-group/mailgroup-permission_member/batch_delete) |

## 函数详细内容

### CreateMailGroupAsync
创建邮件组

**函数签名**
```csharp
Task<FeishuApiResult<CreateMailGroupResult>?> CreateMailGroupAsync(
    [Body] CreateMailGroupRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreateMailGroupRequest | ✅ | 创建邮件组请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "mailgroup_id": "xxxxxxxxxxxxxxx",
    "email": "test_mail_group@xxx.xx",
    "name": "邮件组名称"
  }
}
```

**说明**
- 需要申请邮件组管理权限
- 创建成功后可通过邮件组ID或邮件组地址进行后续操作

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new CreateMailGroupRequest
{
    Email = "test_mail_group@xxx.xx",
    Name = "测试邮件组"
};
var result = await mailGroupApi.CreateMailGroupAsync(request);
Console.WriteLine($"邮件组创建成功: {result?.Data?.MailgroupId}");
```

---

### DeleteMailGroupAsync
删除邮件组

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteMailGroupAsync(
    [Path] string mailgroup_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或者邮件组地址 | xxxxxxxxxxxxxx 或 test_mail_group@xxx.xx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 删除操作不可恢复
- 删除后该邮件组将无法接收邮件

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.DeleteMailGroupAsync("test_mail_group@xxx.xx");
Console.WriteLine($"邮件组删除结果: {result.Code == 0}");
```

---

### UpdateMailGroupPartialAsync
修改邮件组部分信息

**函数签名**
```csharp
Task<FeishuApiResult<UpdateMailGroupResult>?> UpdateMailGroupPartialAsync(
    [Path] string mailgroup_id,
    [Body] UpdateMailGroupRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或者邮件组地址 | xxxxxxxxxxxxxx 或 test_mail_group@xxx.xx |
| request | UpdateMailGroupRequest | ✅ | 更新邮件组请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "mailgroup_id": "xxxxxxxxxxxxxxx",
    "email": "test_mail_group@xxx.xx",
    "name": "更新后的邮件组名称"
  }
}
```

**说明**
- 更新邮件组部分字段，没有填写的字段不会被更新
- 支持局部更新，无需传递所有字段

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new UpdateMailGroupRequest
{
    Name = "更新后的邮件组名称"
};
var result = await mailGroupApi.UpdateMailGroupPartialAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"邮件组更新结果: {result?.Data?.Name}");
```

---

### UpdateMailGroupAsync
修改邮件组全部信息

**函数签名**
```csharp
Task<FeishuApiResult<UpdateMailGroupResult>?> UpdateMailGroupAsync(
    [Path] string mailgroup_id,
    [Body] UpdateMailGroupRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或者邮件组地址 | xxxxxxxxxxxxxx 或 test_mail_group@xxx.xx |
| request | UpdateMailGroupRequest | ✅ | 更新邮件组请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "mailgroup_id": "xxxxxxxxxxxxxxx",
    "email": "test_mail_group@xxx.xx",
    "name": "更新后的邮件组名称"
  }
}
```

**说明**
- 更新邮件组所有字段
- 需要提供完整的邮件组信息

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new UpdateMailGroupRequest
{
    Name = "更新后的邮件组名称",
    Description = "更新后的描述"
};
var result = await mailGroupApi.UpdateMailGroupAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"邮件组更新结果: {result?.Data?.Name}");
```

---

### GetMailGroupAsync
查询指定邮件组

**函数签名**
```csharp
Task<FeishuApiResult<MailGroupInfo>?> GetMailGroupAsync(
    [Path] string mailgroup_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或者邮件组地址 | xxxxxxxxxxxxxx 或 test_mail_group@xxx.xx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "mailgroup_id": "xxxxxxxxxxxxxxx",
    "email": "test_mail_group@xxx.xx",
    "name": "邮件组名称",
    "description": "邮件组描述"
  }
}
```

**说明**
- 获取特定邮件组信息
- 可获取邮件组的详细配置信息

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.GetMailGroupAsync("test_mail_group@xxx.xx");
Console.WriteLine($"邮件组名称: {result?.Data?.Name}");
```

---

### GetMailGroupPageListAsync
分页批量获取邮件组

**函数签名**
```csharp
Task<FeishuApiPageListResult<MailGroupInfo>?> GetMailGroupPageListAsync(
    [Query] string? manager_user_id = null,
    [Query] int page_size = Consts.PageSize_20,
    [Query] string? page_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| manager_user_id | string? | ⚪ | 邮件组管理员用户ID | ou_xxxxxx |
| page_size | int | ⚪ | 分页大小，默认值：20 | 20 |
| page_token | string? | ⚪ | 分页标记 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "mailgroup_id": "xxxxxxxxxxxxxxx",
        "email": "test_mail_group@xxx.xx",
        "name": "邮件组名称"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 分页批量获取邮件组
- 可通过manager_user_id筛选特定管理员管理的邮件组

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.GetMailGroupPageListAsync();
if (result?.Data?.Items != null)
{
    foreach (var group in result.Data.Items)
    {
        Console.WriteLine($"邮件组: {group.Name} ({group.Email})");
    }
}
```

---

### BatchCreateMailgroupManagerAsync
批量创建邮件组管理员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchCreateMailgroupManagerAsync(
    [Path] string mailgroup_id,
    [Body] BatchOopsMailgroupManagerRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | BatchOopsMailgroupManagerRequest | ✅ | 批量添加邮件组管理员请求对象 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 批量创建邮件组管理员
- 添加后这些用户可管理该邮件组

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new BatchOopsMailgroupManagerRequest
{
    MailgroupManagerLists = new[]
    {
        new MailgroupManager { UserId = "ou_xxxxxx" },
        new MailgroupManager { UserId = "ou_yyyyyy" }
    }
};
var result = await mailGroupApi.BatchCreateMailgroupManagerAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"管理员添加结果: {result.Code == 0}");
```

---

### BatchDeleteMailGroupManagerAsync
批量删除邮件组管理员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchDeleteMailGroupManagerAsync(
    [Path] string mailgroup_id,
    [Body] BatchOopsMailgroupManagerRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | BatchOopsMailgroupManagerRequest | ✅ | 批量删除邮件组管理员请求对象 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 批量删除邮件组管理员
- 删除后这些用户将无法管理该邮件组

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new BatchOopsMailgroupManagerRequest
{
    MailgroupManagerLists = new[] { new MailgroupManager { UserId = "ou_xxxxxx" } }
};
var result = await mailGroupApi.BatchDeleteMailGroupManagerAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"管理员删除结果: {result.Code == 0}");
```

---

### GetMailgroupManagerPageListAsync
批量获取邮件组管理员

**函数签名**
```csharp
Task<FeishuApiPageListResult<MailgroupManager>?> GetMailgroupManagerPageListAsync(
    [Path] string mailgroup_id,
    [Query] int page_size = Consts.PageSize_20,
    [Query] string? page_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| page_size | int | ⚪ | 分页大小，默认值：20 | 20 |
| page_token | string? | ⚪ | 分页标记 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "user_id": "ou_xxxxxx"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": false
  }
}
```

**说明**
- 批量获取邮件组管理员
- 可查看所有有权管理该邮件组的用户

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.GetMailgroupManagerPageListAsync("test_mail_group@xxx.xx");
if (result?.Data?.Items != null)
{
    foreach (var manager in result.Data.Items)
    {
        Console.WriteLine($"管理员: {manager.UserId}");
    }
}
```

---

### CreateMailGroupMemberAsync
创建邮件组成员

**函数签名**
```csharp
Task<FeishuApiResult<MailGroupMemberOopsResult>?> CreateMailGroupMemberAsync(
    [Path] string mailgroup_id,
    [Body] CreateMailGroupMemberRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    [Query] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | CreateMailGroupMemberRequest | ✅ | 添加邮件组成员请求对象 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| department_id_type | string? | ⚪ | 部门ID类型，默认值：open_department_id | open_department_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "member_id": "xxxxxxxxxxxxxxx",
    "user_id": "ou_xxxxxx",
    "email": "member@xxx.xx"
  }
}
```

**说明**
- 向邮件组添加单个成员
- 成员可以是用户、部门或公共邮箱

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new CreateMailGroupMemberRequest
{
    UserId = "ou_xxxxxx"
};
var result = await mailGroupApi.CreateMailGroupMemberAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"成员添加成功: {result?.Data?.MemberId}");
```

---

### DeleteMailGroupMemberAsync
删除邮件组成员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteMailGroupMemberAsync(
    [Path] string mailgroup_id,
    [Path] string member_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| member_id | string | ✅ | 邮件组成员唯一标识 | xxxxxxxxxxxxxxx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 删除邮件组单个成员
- 删除后该成员将不再接收邮件组邮件

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.DeleteMailGroupMemberAsync("test_mail_group@xxx.xx", "member_id_123");
Console.WriteLine($"成员删除结果: {result.Code == 0}");
```

---

### GetMailGroupMemberAsync
查询指定邮件组成员

**函数签名**
```csharp
Task<FeishuApiResult<MailGroupMemberOopsResult>?> GetMailGroupMemberAsync(
    [Path] string mailgroup_id,
    [Path] string member_id,
    [Query] string? user_id_type = Consts.User_Id_Type,
    [Query] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| member_id | string | ✅ | 邮件组成员唯一标识 | xxxxxxxxxxxxxxx |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| department_id_type | string? | ⚪ | 部门ID类型，默认值：open_department_id | open_department_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "member_id": "xxxxxxxxxxxxxxx",
    "user_id": "ou_xxxxxx",
    "email": "member@xxx.xx"
  }
}
```

**说明**
- 获取邮件组单个成员信息
- 可查看成员的详细信息和配置

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.GetMailGroupMemberAsync("test_mail_group@xxx.xx", "member_id_123");
Console.WriteLine($"成员邮箱: {result?.Data?.Email}");
```

---

### GetMailGroupMemberPageListAsync
分页获取所有邮件组成员

**函数签名**
```csharp
Task<FeishuApiPageListResult<MailGroupMemberInfo>?> GetMailGroupMemberPageListAsync(
    [Path] string mailgroup_id,
    [Query] int page_size = Consts.PageSize_20,
    [Query] string? page_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    [Query] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| page_size | int | ⚪ | 分页大小，默认值：20 | 20 |
| page_token | string? | ⚪ | 分页标记 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| department_id_type | string? | ⚪ | 部门ID类型，默认值：open_department_id | open_department_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "member_id": "xxxxxxxxxxxxxxx",
        "user_id": "ou_xxxxxx",
        "email": "member@xxx.xx"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 分页批量获取邮件组成员列表
- 可查看邮件组所有成员信息

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.GetMailGroupMemberPageListAsync("test_mail_group@xxx.xx");
if (result?.Data?.Items != null)
{
    foreach (var member in result.Data.Items)
    {
        Console.WriteLine($"成员: {member.Email} ({member.UserId})");
    }
}
```

---

### BatchCreateMailGroupMemberAsync
批量创建邮件组成员

**函数签名**
```csharp
Task<FeishuApiResult<BatchCreateMailGroupMemberResult>?> BatchCreateMailGroupMemberAsync(
    [Path] string mailgroup_id,
    [Body] BatchCreateMailGroupMemberRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    [Query] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | BatchCreateMailGroupMemberRequest | ✅ | 批量创建邮件组成员请求体 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| department_id_type | string? | ⚪ | 部门ID类型，默认值：open_department_id | open_department_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "member_id": "xxxxxxxxxxxxxxx",
        "user_id": "ou_xxxxxx",
        "email": "member_a@xxx.xx"
      },
      {
        "member_id": "yyyyyyyyyyyyyyy",
        "user_id": "ou_yyyyyy",
        "email": "member_b@xxx.xx"
      }
    ]
  }
}
```

**说明**
- 一次请求可以给一个邮件组添加多个成员
- 支持批量操作，提高效率

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new BatchCreateMailGroupMemberRequest
{
    Items = new[]
    {
        new MailGroupMemberInfo { UserId = "ou_xxxxxx" },
        new MailGroupMemberInfo { UserId = "ou_yyyyyy" }
    }
};
var result = await mailGroupApi.BatchCreateMailGroupMemberAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"成功添加: {result?.Data?.Items?.Length} 个成员");
```

---

### BatchDeleteMailGroupMemberAsync
批量删除邮件组成员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchDeleteMailGroupMemberAsync(
    [Path] string mailgroup_id,
    [Body] BatchDeleteMailGroupMemberRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | BatchDeleteMailGroupMemberRequest | ✅ | 批量删除邮件组成员请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 一次请求可以删除一个邮件组中的多个成员
- 支持批量操作，提高效率

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new BatchDeleteMailGroupMemberRequest
{
    MemberIdList = new[] { "member_id_123", "member_id_456" }
};
var result = await mailGroupApi.BatchDeleteMailGroupMemberAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"成员批量删除结果: {result.Code == 0}");
```

---

### CreateMailGroupAliasAsync
创建邮件组别名

**函数签名**
```csharp
Task<FeishuApiResult<CreateMailGroupAliasResult>?> CreateMailGroupAliasAsync(
    [Path] string mailgroup_id,
    [Body] CreateMailGroupAliasRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | CreateMailGroupAliasRequest | ✅ | 创建邮件组别名请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "mailgroup_alias": {
      "primary_email": "test_mail_group@xxx.xx",
      "email_alias": "test_mail_group.alias@xxx.xx"
    }
  }
}
```

**说明**
- 创建邮件组别名
- 别名可用于发送和接收邮件

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new CreateMailGroupAliasRequest
{
    EmailAlias = "test_mail_group.alias@xxx.xx"
};
var result = await mailGroupApi.CreateMailGroupAliasAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"别名创建成功: {result?.Data?.MailgroupAlias?.EmailAliasSuffix}");
```

---

### DeleteMailGroupAliasAsync
删除邮件组别名

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteMailGroupAliasAsync(
    [Path] string mailgroup_id,
    [Path] string alias_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| alias_id | string | ✅ | 邮件组别名邮箱地址 | xxx@xx.xxx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 删除邮件组别名
- 删除后该别名将无法使用

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.DeleteMailGroupAliasAsync("test_mail_group@xxx.xx", "test_mail_group.alias@xxx.xx");
Console.WriteLine($"别名删除结果: {result.Code == 0}");
```

---

### GetMailGroupAliasListAsync
获取邮件组所有别名

**函数签名**
```csharp
Task<FeishuApiResult<GetMailGroupAliasResult>?> GetMailGroupAliasListAsync(
    [Path] string mailgroup_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "primary_email": "test_mail_group@xxx.xx",
        "email_alias": "test_mail_group.alias@xxx.xx"
      }
    ]
  }
}
```

**说明**
- 该接口一次性返回所有数据，分页参数无效
- 可查看邮件组所有别名信息

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.GetMailGroupAliasListAsync("test_mail_group@xxx.xx");
if (result?.Data?.Items != null)
{
    foreach (var alias in result.Data.Items)
    {
        Console.WriteLine($"别名: {alias.EmailAliasSuffix}");
    }
}
```

---

### CreateMailGroupPermissionMemberAsync
创建邮件组权限成员

**函数签名**
```csharp
Task<FeishuApiResult<CreateMailGroupPermissionMemberResult>?> CreateMailGroupPermissionMemberAsync(
    [Path] string mailgroup_id,
    [Body] CreateMailGroupPermissionMemberRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    [Query] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | CreateMailGroupPermissionMemberRequest | ✅ | 创建邮件组权限成员请求对象 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| department_id_type | string? | ⚪ | 部门ID类型，默认值：open_department_id | open_department_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "permission_member_id": "xxxxxxxxxxxxxxx",
    "user_id": "ou_xxxxxx",
    "email": "member@xxx.xx"
  }
}
```

**说明**
- 向邮件组添加单个自定义权限成员
- 添加后该成员可发送邮件到该邮件组

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new CreateMailGroupPermissionMemberRequest
{
    UserId = "ou_xxxxxx"
};
var result = await mailGroupApi.CreateMailGroupPermissionMemberAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"权限成员添加成功: {result?.Data?.PermissionMemberId}");
```

---

### DeleteMailGroupPermissionMemberAsync
删除邮件组权限成员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeleteMailGroupPermissionMemberAsync(
    [Path] string mailgroup_id,
    [Path] string permission_member_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| permission_member_id | string | ✅ | 权限成员唯一标识 | xxxxxxxxxxxxxxx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 从自定义成员中删除单个成员
- 删除后该成员无法发送邮件到该邮件组

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.DeleteMailGroupPermissionMemberAsync("test_mail_group@xxx.xx", "permission_member_id_123");
Console.WriteLine($"权限成员删除结果: {result.Code == 0}");
```

---

### GetailGroupPermissionMemberAsync
获取邮件组权限成员

**函数签名**
```csharp
Task<FeishuApiResult<GetMailGroupPermissionMemberResult>?> GetailGroupPermissionMemberAsync(
    [Path] string mailgroup_id,
    [Path] string permission_member_id,
    [Query] string? user_id_type = Consts.User_Id_Type,
    [Query] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| permission_member_id | string | ✅ | 权限成员唯一标识 | xxxxxxxxxxxxxxx |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| department_id_type | string? | ⚪ | 部门ID类型，默认值：open_department_id | open_department_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "permission_member_id": "xxxxxxxxxxxxxxx",
    "user_id": "ou_xxxxxx",
    "department_id": "0",
    "email": "member@xxx.xx"
  }
}
```

**说明**
- 获取邮件组单个权限成员信息
- 可查看权限成员的详细信息和配置

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.GetailGroupPermissionMemberAsync("test_mail_group@xxx.xx", "permission_member_id_123");
Console.WriteLine($"权限成员邮箱: {result?.Data?.Email}");
```

---

### GetMailgroupPermissionMemberPageListAsync
分页批量获取邮件组权限成员

**函数签名**
```csharp
Task<FeishuApiPageListResult<MailGroupPermissionMember>?> GetMailgroupPermissionMemberPageListAsync(
    [Path] string mailgroup_id,
    [Query] int page_size = Consts.PageSize_20,
    [Query] string? page_token = null,
    [Query] string? user_id_type = Consts.User_Id_Type,
    [Query] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| page_size | int | ⚪ | 分页大小，默认值：20 | 20 |
| page_token | string? | ⚪ | 分页标记 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| department_id_type | string? | ⚪ | 部门ID类型，默认值：open_department_id | open_department_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "permission_member_id": "xxxxxxxxxxxxxxx",
        "user_id": "ou_xxxxxx",
        "email": "member@xxx.xx"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 分页批量获取邮件组权限成员列表
- 可查看所有有权限发送到该邮件组的成员

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var result = await mailGroupApi.GetMailgroupPermissionMemberPageListAsync("test_mail_group@xxx.xx");
if (result?.Data?.Items != null)
{
    foreach (var member in result.Data.Items)
    {
        Console.WriteLine($"权限成员: {member.Email} ({member.UserId})");
    }
}
```

---

### BatchCreateMailGroupPermissionMembersAsync
批量创建邮件组权限成员

**函数签名**
```csharp
Task<FeishuApiResult<BatchCreateMailGroupPermissionMembersResult>?> BatchCreateMailGroupPermissionMembersAsync(
    [Path] string mailgroup_id,
    [Body] BatchCreateMailGroupPermissionMembersRequest request,
    [Query] string? user_id_type = Consts.User_Id_Type,
    [Query] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | BatchCreateMailGroupPermissionMembersRequest | ✅ | 批量创建邮件组权限成员请求体 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| department_id_type | string? | ⚪ | 部门ID类型，默认值：open_department_id | open_department_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "permission_member_id": "xxxxxxxxxxxxxxx",
        "user_id": "ou_xxxxxx",
        "email": "member_a@xxx.xx"
      },
      {
        "permission_member_id": "yyyyyyyyyyyyyyy",
        "user_id": "ou_yyyyyy",
        "email": "member_b@xxx.xx"
      }
    ]
  }
}
```

**说明**
- 一次请求可以给一个邮件组添加多个权限成员
- 支持批量操作，提高效率

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new BatchCreateMailGroupPermissionMembersRequest
{
    Items = new[]
    {
        new MailGroupPermissionMember { UserId = "ou_xxxxxx" },
        new MailGroupPermissionMember { UserId = "ou_yyyyyy" }
    }
};
var result = await mailGroupApi.BatchCreateMailGroupPermissionMembersAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"成功添加: {result?.Data?.Items?.Length} 个权限成员");
```

---

### BatchDeleteMailGroupPermissionMemberAsync
批量删除邮件组权限成员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchDeleteMailGroupPermissionMemberAsync(
    [Path] string mailgroup_id,
    [Body] BatchDeleteMailGroupPermissionMembersRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| mailgroup_id | string | ✅ | 邮件组ID或邮箱地址 | xxxxxxxx 或 test_mail_group@xx.xx |
| request | BatchDeleteMailGroupPermissionMembersRequest | ✅ | 批量删除邮件组权限成员请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": null
}
```

**说明**
- 一次请求可以删除一个邮件组中的多个权限成员
- 支持批量操作，提高效率

**代码示例**
```csharp
var mailGroupApi = feishuApp.GetApi<IFeishuTenantV1MailGroup>();
var request = new BatchDeleteMailGroupPermissionMembersRequest
{
    PermissionMemberIdList = new[] { "permission_member_id_123", "permission_member_id_456" }
};
var result = await mailGroupApi.BatchDeleteMailGroupPermissionMemberAsync("test_mail_group@xxx.xx", request);
Console.WriteLine($"权限成员批量删除结果: {result.Code == 0}");
```
