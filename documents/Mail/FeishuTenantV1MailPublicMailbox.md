# IFeishuTenantV1MailPublicMailbox - 租户公共邮箱API

## 功能描述
飞书公共邮箱API接口实现公共邮箱管理、公共邮箱成员管理以及公共邮箱别名管理等管理功能。支持租户管理员通过租户访问令牌管理企业内所有公共邮箱。

## 参考文档
- [创建公共邮箱](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/create)
- [修改公共邮箱部分信息](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/patch)
- [修改公共邮箱全部信息](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/update)
- [查询指定公共邮箱](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/get)
- [将公共邮箱移至回收站](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/remove_to_recycle_bin)
- [永久删除公共邮箱](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/delete)
- [分页查询所有公共邮箱](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/list)
- [添加公共邮箱成员](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/create)
- [删除公共邮箱单个成员](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/delete)
- [删除公共邮箱所有成员](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/clear)
- [查询指定公共邮箱成员信息](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/get)
- [查询所有公共邮箱成员信息](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/list)
- [批量添加公共邮箱成员](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/batch_create)
- [批量删除公共邮箱成员](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-member/batch_delete)
- [创建公共邮箱别名](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-alias/create)
- [删除公共邮箱别名](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-alias/delete)
- [查询公共邮箱的所有别名](https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox-alias/list)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| CreatePublicMailboxAsync | 创建公共邮箱 | TenantAccessToken | POST |
| UpdatePublicMailboxPartialAsync | 修改公共邮箱部分信息 | TenantAccessToken | PATCH |
| UpdatePublicMailboxAsync | 修改公共邮箱全部信息 | TenantAccessToken | PUT |
| GetPublicMailboxAsync | 查询指定公共邮箱 | TenantAccessToken | GET |
| RemoveToRecycleBinPublicMailboxAsync | 将公共邮箱移至回收站 | TenantAccessToken | DELETE |
| DeletePublicMailboxAsync | 永久删除公共邮箱 | TenantAccessToken | DELETE |
| GetPublicMailboxPageListAsync | 分页查询所有公共邮箱 | TenantAccessToken | GET |
| CreatePublicMailboxMemberAsync | 添加公共邮箱成员 | TenantAccessToken | POST |
| DeletePublicMailboxMemberAsync | 删除公共邮箱单个成员 | TenantAccessToken | DELETE |
| DeletePublicMailboxAllMemberAsync | 删除公共邮箱所有成员 | TenantAccessToken | POST |
| GetPublicMailboxMemberAsync | 查询指定公共邮箱成员信息 | TenantAccessToken | GET |
| GetPublicMailboxMemberPageListAsync | 查询所有公共邮箱成员信息 | TenantAccessToken | GET |
| BatchCreatePublicMailboxMemberAsync | 批量添加公共邮箱成员 | TenantAccessToken | POST |
| BatchDeletePublicMailboxMemberAsync | 批量删除公共邮箱成员 | TenantAccessToken | DELETE |
| CreatePublicMailboxAliasAsync | 创建公共邮箱别名 | TenantAccessToken | POST |
| DeletePublicMailboxAliasAsync | 删除公共邮箱别名 | TenantAccessToken | DELETE |
| GetPublicMailboxAliasListAsync | 查询公共邮箱的所有别名 | TenantAccessToken | GET |

## 函数详细内容

### CreatePublicMailboxAsync
创建公共邮箱

**函数签名**
```csharp
Task<FeishuApiResult<PublicMailboxOopsResult>?> CreatePublicMailboxAsync(
    [Body] CreatePublicMailboxRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| request | CreatePublicMailboxRequest | ✅ | 创建公共邮箱请求对象 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "public_mailbox_id": "xxxxxxxxxxxxxxx",
    "email": "test_public_mailbox@xxx.xx",
    "name": "公共邮箱名称",
    "create_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 需要申请公共邮箱管理权限
- 创建成功后可通过公共邮箱ID或邮箱地址进行后续操作

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var request = new CreatePublicMailboxRequest
{
    Email = "test_public_mailbox@xxx.xx",
    Name = "测试公共邮箱"
};
var result = await publicMailboxApi.CreatePublicMailboxAsync(request);
Console.WriteLine($"公共邮箱创建成功: {result?.Data?.PublicMailboxId}");
```

---

### UpdatePublicMailboxPartialAsync
修改公共邮箱部分信息

**函数签名**
```csharp
Task<FeishuApiResult<UpdatePublicMailboxResult>?> UpdatePublicMailboxPartialAsync(
    [Path] string public_mailbox_id,
    [Body] UpdatePublicMailboxRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| request | UpdatePublicMailboxRequest | ✅ | 修改公共邮箱部分信息请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "public_mailbox_id": "xxxxxxxxxxxxxxx",
    "email": "test_public_mailbox@xxx.xx",
    "name": "更新后的公共邮箱名称",
    "update_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 更新公共邮箱部分字段，没有填写的字段不会被更新
- 支持局部更新，无需传递所有字段

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var request = new UpdatePublicMailboxRequest
{
    Name = "更新后的公共邮箱名称"
};
var result = await publicMailboxApi.UpdatePublicMailboxPartialAsync("test_public_mailbox@xxx.xx", request);
Console.WriteLine($"公共邮箱更新结果: {result?.Data?.Name}");
```

---

### UpdatePublicMailboxAsync
修改公共邮箱全部信息

**函数签名**
```csharp
Task<FeishuApiResult<UpdatePublicMailboxResult>?> UpdatePublicMailboxAsync(
    [Path] string public_mailbox_id,
    [Body] UpdatePublicMailboxRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| request | UpdatePublicMailboxRequest | ✅ | 修改公共邮箱部分信息请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "public_mailbox_id": "xxxxxxxxxxxxxxx",
    "email": "test_public_mailbox@xxx.xx",
    "name": "更新后的公共邮箱名称",
    "update_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 更新公共邮箱全部字段
- 需要提供完整的公共邮箱信息

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var request = new UpdatePublicMailboxRequest
{
    Name = "更新后的公共邮箱名称",
    Description = "更新后的描述"
};
var result = await publicMailboxApi.UpdatePublicMailboxAsync("test_public_mailbox@xxx.xx", request);
Console.WriteLine($"公共邮箱更新结果: {result?.Data?.Name}");
```

---

### GetPublicMailboxAsync
查询指定公共邮箱

**函数签名**
```csharp
Task<FeishuApiResult<PublicMailboxOopsResult>?> GetPublicMailboxAsync(
    [Path] string public_mailbox_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "public_mailbox_id": "xxxxxxxxxxxxxxx",
    "email": "test_public_mailbox@xxx.xx",
    "name": "公共邮箱名称",
    "description": "公共邮箱描述",
    "create_time": "2026-06-03T11:41:00+08:00",
    "update_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 获取公共邮箱信息
- 可获取公共邮箱的详细配置信息

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.GetPublicMailboxAsync("test_public_mailbox@xxx.xx");
Console.WriteLine($"公共邮箱名称: {result?.Data?.Name}");
```

---

### RemoveToRecycleBinPublicMailboxAsync
将公共邮箱移至回收站

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> RemoveToRecycleBinPublicMailboxAsync(
    [Path] string public_mailbox_id,
    [Body] RemoveToRecycleBinPublicMailboxRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| request | RemoveToRecycleBinPublicMailboxRequest | ✅ | 将公共邮箱移至回收站请求体 | - |
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
- 将公共邮箱移至回收站
- 移至回收站后可通过永久删除接口彻底删除

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var request = new RemoveToRecycleBinPublicMailboxRequest();
var result = await publicMailboxApi.RemoveToRecycleBinPublicMailboxAsync("test_public_mailbox@xxx.xx", request);
Console.WriteLine($"移至回收站结果: {result.Code == 0}");
```

---

### DeletePublicMailboxAsync
永久删除公共邮箱

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeletePublicMailboxAsync(
    [Path] string public_mailbox_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
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
- 该接口会永久删除公共邮箱地址
- 可用于释放邮箱回收站的公共邮箱地址，一旦删除，该邮箱地址将无法恢复

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.DeletePublicMailboxAsync("test_public_mailbox@xxx.xx");
Console.WriteLine($"永久删除结果: {result.Code == 0}");
```

---

### GetPublicMailboxPageListAsync
分页查询所有公共邮箱

**函数签名**
```csharp
Task<FeishuApiPageListResult<PublicMailboxInfo>?> GetPublicMailboxPageListAsync(
    [Query] int page_size = 20,
    [Query] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| page_size | int | ⚪ | 分页大小，默认值：20 | 20 |
| page_token | string? | ⚪ | 分页标记 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "public_mailbox_id": "xxxxxxxxxxxxxxx",
        "email": "test_public_mailbox@xxx.xx",
        "name": "公共邮箱名称"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 分页批量获取公共邮箱列表
- 可查看企业内所有公共邮箱信息

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.GetPublicMailboxPageListAsync();
if (result?.Data?.Items != null)
{
    foreach (var mailbox in result.Data.Items)
    {
        Console.WriteLine($"公共邮箱: {mailbox.Name} ({mailbox.Email})");
    }
}
```

---

### CreatePublicMailboxMemberAsync
添加公共邮箱成员

**函数签名**
```csharp
Task<FeishuApiResult<PublicMailboxMemberOopsResult>?> CreatePublicMailboxMemberAsync(
    [Path] string public_mailbox_id,
    [Body] CreatePublicMailboxMemberRequest request,
    [Query] string? user_id_type = "user",
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| request | CreatePublicMailboxMemberRequest | ✅ | 添加公共邮箱成员请求体 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "member_id": "xxxxxxxxxxxxxxx",
    "create_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 向公共邮箱添加单个成员
- 成员可以是用户或部门

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var request = new CreatePublicMailboxMemberRequest
{
    UserId = "ou_xxxxxx"
};
var result = await publicMailboxApi.CreatePublicMailboxMemberAsync("test_public_mailbox@xxx.xx", request);
Console.WriteLine($"成员添加成功: {result?.Data?.MemberId}");
```

---

### DeletePublicMailboxMemberAsync
删除公共邮箱单个成员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeletePublicMailboxMemberAsync(
    [Path] string public_mailbox_id,
    [Path] string member_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| member_id | string | ✅ | 公共邮箱内成员唯一标识 | xxxxxxxxxxxxxxx |
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
- 删除公共邮箱单个成员
- 删除后该成员将无法访问该公共邮箱

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.DeletePublicMailboxMemberAsync("test_public_mailbox@xxx.xx", "member_id_123");
Console.WriteLine($"成员删除结果: {result.Code == 0}");
```

---

### DeletePublicMailboxAllMemberAsync
删除公共邮箱所有成员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeletePublicMailboxAllMemberAsync(
    [Path] string public_mailbox_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
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
- 删除公共邮箱所有成员
- 删除后所有成员将无法访问该公共邮箱

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.DeletePublicMailboxAllMemberAsync("test_public_mailbox@xxx.xx");
Console.WriteLine($"所有成员删除结果: {result.Code == 0}");
```

---

### GetPublicMailboxMemberAsync
查询指定公共邮箱成员信息

**函数签名**
```csharp
Task<FeishuApiResult<PublicMailboxMemberOopsResult>?> GetPublicMailboxMemberAsync(
    [Path] string public_mailbox_id,
    [Path] string member_id,
    [Query] string? user_id_type = "user",
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| member_id | string | ✅ | 公共邮箱内成员唯一标识 | xxxxxxxxxxxxxxx |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "member_id": "xxxxxxxxxxxxxxx",
    "user_id": "ou_xxxxxx",
    "name": "成员名称",
    "create_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 获取公共邮箱单个成员信息
- 可查看成员的详细信息和配置

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.GetPublicMailboxMemberAsync("test_public_mailbox@xxx.xx", "member_id_123");
Console.WriteLine($"成员名称: {result?.Data?.Name}");
```

---

### GetPublicMailboxMemberPageListAsync
查询所有公共邮箱成员信息

**函数签名**
```csharp
Task<FeishuApiPageListResult<PublicMailboxMemberInfo>?> GetPublicMailboxMemberPageListAsync(
    [Path] string public_mailbox_id,
    [Query] int page_size = 20,
    [Query] string? page_token = null,
    [Query] string? user_id_type = "user",
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
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
        "member_id": "xxxxxxxxxxxxxxx",
        "user_id": "ou_xxxxxx",
        "name": "成员名称"
      }
    ],
    "page_token": "evt_xxx",
    "has_more": true
  }
}
```

**说明**
- 查询所有公共邮箱成员信息
- 可查看公共邮箱所有成员信息

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.GetPublicMailboxMemberPageListAsync("test_public_mailbox@xxx.xx");
if (result?.Data?.Items != null)
{
    foreach (var member in result.Data.Items)
    {
        Console.WriteLine($"成员: {member.Name} ({member.UserId})");
    }
}
```

---

### BatchCreatePublicMailboxMemberAsync
批量添加公共邮箱成员

**函数签名**
```csharp
Task<FeishuApiResult<BatchCreatePublicMailboxMemberResult>?> BatchCreatePublicMailboxMemberAsync(
    [Path] string public_mailbox_id,
    [Body] BatchCreatePublicMailboxMemberRequest request,
    [Query] string? user_id_type = "user",
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| request | BatchCreatePublicMailboxMemberRequest | ✅ | 批量添加公共邮箱成员请求体 | - |
| user_id_type | string? | ⚪ | 用户ID类型，默认值：open_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "success_count": 5,
    "failed_count": 0
  }
}
```

**说明**
- 一次请求可以给一个公共邮箱添加多个成员
- 支持批量操作，提高效率

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var request = new BatchCreatePublicMailboxMemberRequest
{
    Members = new List<PublicMailboxMemberItem>
    {
        new PublicMailboxMemberItem { UserId = "ou_xxxxxx" },
        new PublicMailboxMemberItem { UserId = "ou_yyyyyy" }
    }
};
var result = await publicMailboxApi.BatchCreatePublicMailboxMemberAsync("test_public_mailbox@xxx.xx", request);
Console.WriteLine($"成功添加: {result?.Data?.SuccessCount} 个成员");
```

---

### BatchDeletePublicMailboxMemberAsync
批量删除公共邮箱成员

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> BatchDeletePublicMailboxMemberAsync(
    [Path] string public_mailbox_id,
    [Body] BatchDeletePublicMailboxMemberRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| request | BatchDeletePublicMailboxMemberRequest | ✅ | 批量删除公共邮箱成员请求体 | - |
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
- 一次请求可以删除一个公共邮箱中的多个成员
- 支持批量操作，提高效率

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var request = new BatchDeletePublicMailboxMemberRequest
{
    MemberIds = new List<string> { "member_id_123", "member_id_456" }
};
var result = await publicMailboxApi.BatchDeletePublicMailboxMemberAsync("test_public_mailbox@xxx.xx", request);
Console.WriteLine($"成员批量删除结果: {result.Code == 0}");
```

---

### CreatePublicMailboxAliasAsync
创建公共邮箱别名

**函数签名**
```csharp
Task<FeishuApiResult<CreatePublicMailboxAliasResult>?> CreatePublicMailboxAliasAsync(
    [Path] string public_mailbox_id,
    [Body] CreatePublicMailboxAliasRequest request,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| request | CreatePublicMailboxAliasRequest | ✅ | 创建公共邮箱别名请求体 | - |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "alias_id": "alias_123456",
    "alias_email": "test_public_mailbox.alias@xxx.xx",
    "create_time": "2026-06-03T11:41:00+08:00"
  }
}
```

**说明**
- 创建公共邮箱别名
- 别名可用于发送和接收邮件

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var request = new CreatePublicMailboxAliasRequest
{
    AliasEmail = "test_public_mailbox.alias@xxx.xx"
};
var result = await publicMailboxApi.CreatePublicMailboxAliasAsync("test_public_mailbox@xxx.xx", request);
Console.WriteLine($"别名创建成功: {result?.Data?.AliasEmail}");
```

---

### DeletePublicMailboxAliasAsync
删除公共邮箱别名

**函数签名**
```csharp
Task<FeishuNullDataApiResult?> DeletePublicMailboxAliasAsync(
    [Path] string public_mailbox_id,
    [Path] string alias_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| alias_id | string | ✅ | 公共邮箱别名 | xxx@xx.xxx |
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
- 删除公共邮箱别名
- 删除后该别名将无法使用

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.DeletePublicMailboxAliasAsync("test_public_mailbox@xxx.xx", "test_public_mailbox.alias@xxx.xx");
Console.WriteLine($"别名删除结果: {result.Code == 0}");
```

---

### GetPublicMailboxAliasListAsync
查询公共邮箱的所有别名

**函数签名**
```csharp
Task<FeishuApiResult<GetPublicMailboxAliasListResult>?> GetPublicMailboxAliasListAsync(
    [Path] string public_mailbox_id,
    CancellationToken cancellationToken = default);
```

**认证**
TenantAccessToken（租户访问令牌）

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| public_mailbox_id | string | ✅ | 公共邮箱唯一标识或公共邮箱地址 | xxxxxxxxxxxxxxx 或 test_public_mailbox@xxx.xx |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "alias_id": "alias_123456",
        "alias_email": "test_public_mailbox.alias@xxx.xx",
        "create_time": "2026-06-03T11:41:00+08:00"
      }
    ]
  }
}
```

**说明**
- 获取所有公共邮箱别名
- 可查看公共邮箱所有别名信息

**代码示例**
```csharp
var publicMailboxApi = feishuApp.GetApi<IFeishuTenantV1MailPublicMailbox>();
var result = await publicMailboxApi.GetPublicMailboxAliasListAsync("test_public_mailbox@xxx.xx");
if (result?.Data?.Items != null)
{
    foreach (var alias in result.Data.Items)
    {
        Console.WriteLine($"别名: {alias.AliasEmail}");
    }
}
```
