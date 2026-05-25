## 接口名称
**Wiki 知识库（租户令牌）- (IFeishuTenantV2Wiki)**

## 功能描述
飞书知识库是一个面向组织的知识管理系统。通过结构化沉淀高价值信息，形成完整的知识体系。此外，明确的内容分类，层级式的页面树，还能够轻松提升知识的流转和传播效率，更好地成就组织和个人。

本接口提供以**租户身份**对知识空间进行管理的能力，适用于后台服务、机器人等场景，无需用户登录即可操作知识空间。

## 参考文档
- [飞书 Wiki 概述](https://open.feishu.cn/document/server-docs/docs/wiki-v2/wiki-overview)
- [如何将应用添加为知识库管理员](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/wiki-v2/wiki-qa#b5da330b)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|-----------|
| `GetSpacesPageListAsync` | 获取有权限访问的知识空间列表 | 租户令牌 | GET |
| `GetSpaceInfoAsync` | 获取知识空间详细信息 | 租户令牌 | GET |
| `GetSpaceMemberPageListAsync` | 获取知识空间成员列表 | 租户令牌 | GET |
| `CreateSpaceMemberAsync` | 添加知识空间成员 | 租户令牌 | POST |
| `DeleteSpaceMemberAsync` | 删除知识空间成员 | 租户令牌 | DELETE |
| `UpdateSpaceSettingAsync` | 更新知识空间设置 | 租户令牌 | PUT |

## 函数详细内容

### 获取知识空间列表

**函数名称**：获取有权限访问的知识空间列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<SpaceInfo>?> GetSpacesPageListAsync(
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `page_size` | `int` | ⚪ 可选 | 分页大小，默认 10 |
| `page_token` | `string?` | ⚪ 可选 | 分页标记，首次请求不传 |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "name": "产品文档库",
        "description": "产品相关文档",
        "space_id": "6870403571079249922",
        "space_type": "team",
        "visibility": "private",
        "open_sharing": "closed"
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

**说明**：
- 请确认应用或机器人拥有部分知识空间的访问权限，否则返回列表为空
- 此接口为分页接口，由于权限过滤，可能返回列表为空，但当分页标记（has_more）为 true 时，可以继续分页请求
- 此接口不会返回**我的文档库**

---

### 获取知识空间信息

**函数名称**：获取知识空间详细信息

**函数签名**：
```csharp
Task<FeishuApiResult<SpaceInfoResult>?> GetSpaceInfoAsync(
    [Path] string space_id,
    [Query("lang")] string? lang = "en",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `space_id` | `string` | ✅ 必填 | 知识空间 ID，示例：6870403571079249922 |
| `lang` | `string?` | ⚪ 可选 | 语言设置，默认 en。可选值：zh、en、ja 等 |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "space": {
      "name": "产品文档库",
      "description": "产品相关文档",
      "space_id": "6870403571079249922",
      "space_type": "team",
      "visibility": "private",
      "open_sharing": "closed"
    }
  }
}
```

---

### 获取知识空间成员列表

**函数名称**：获取知识空间成员列表

**函数签名**：
```csharp
Task<FeishuApiResult<SpaceMemberResult>?> GetSpaceMemberPageListAsync(
    [Path] string space_id,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `space_id` | `string` | ✅ 必填 | 知识空间 ID |
| `page_size` | `int` | ⚪ 可选 | 分页大小，默认 10 |
| `page_token` | `string?` | ⚪ 可选 | 分页标记 |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "members": [
      {
        "member_id": "ou_xxx",
        "member_type": "openid",
        "member_role": "admin",
        "member_name": "张三"
      }
    ],
    "page_token": "xxx",
    "has_more": false
  }
}
```

---

### 添加知识空间成员

**函数名称**：添加知识空间成员

**函数签名**：
```csharp
Task<FeishuApiResult<SpaceInfoResult>?> CreateSpaceMemberAsync(
    [Path] string space_id,
    [Body] CreateSpaceMemberRequest createSpaceMemberRequest,
    [Query("need_notification")] bool? need_notification = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `space_id` | `string` | ✅ 必填 | 知识空间 ID |
| `createSpaceMemberRequest` | `CreateSpaceMemberRequest` | ✅ 必填 | 添加成员请求体 |
| `need_notification` | `bool?` | ⚪ 可选 | 添加权限后是否通知对方 |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌 |

**CreateSpaceMemberRequest 参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `member_type` | `string` | ✅ 必填 | 成员类型：openid、userid、email、unionid（**租户身份无法使用 opendepartmentid**） |
| `member_id` | `string` | ✅ 必填 | 成员 ID，类型由 member_type 决定 |
| `member_role` | `string` | ✅ 必填 | 角色：admin（管理员）、member（成员） |

**说明**：
- 调用此接口前，请确保应用为知识空间的管理员
- 使用租户 access token 身份操作时，**无法使用部门 ID (opendepartmentid) 添加成员**
- 公开知识空间不支持添加成员，但可以添加管理员
- 个人知识空间不支持添加其他管理员，但可以添加成员

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "space": {
      "space_id": "6870403571079249922",
      "name": "产品文档库"
    }
  }
}
```

---

### 删除知识空间成员

**函数名称**：删除知识空间成员

**函数签名**：
```csharp
Task<FeishuApiResult<DeleteSpaceMemberResult>?> DeleteSpaceMemberAsync(
    [Path] string space_id,
    [Path] string member_id,
    [Body] DeleteSpaceMemberRequest deleteSpaceMemberRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `space_id` | `string` | ✅ 必填 | 知识空间 ID |
| `member_id` | `string` | ✅ 必填 | 成员 ID |
| `deleteSpaceMemberRequest` | `DeleteSpaceMemberRequest` | ✅ 必填 | 删除成员请求体 |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌 |

**DeleteSpaceMemberRequest 参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `member_type` | `string` | ✅ 必填 | 成员类型 |
| `member_role` | `string` | ✅ 必填 | 角色：admin、member |
| `type` | `string?` | ⚪ 可选 | 协作者类型：user、chat、department |

---

### 更新知识空间设置

**函数名称**：更新知识空间设置

**函数签名**：
```csharp
Task<FeishuApiResult<UpdateSpaceSettingResult>?> UpdateSpaceSettingAsync(
    [Path] string space_id,
    [Body] UpdateSpaceSettingRequest updateSpaceSettingRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `space_id` | `string` | ✅ 必填 | 知识空间 ID |
| `updateSpaceSettingRequest` | `UpdateSpaceSettingRequest` | ✅ 必填 | 更新设置请求体 |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌 |

**UpdateSpaceSettingRequest 参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| `create_setting` | `string?` | ⚪ 可选 | 谁可以创建一级页面：admin/admin_and_member |
| `security_setting` | `string?` | ⚪ 可选 | 可阅读用户可否创建副本/打印/导出/复制：allow/not_allow |
| `comment_setting` | `string?` | ⚪ 可选 | 可阅读用户可否评论：allow/not_allow |

---

## 代码示例

### 使用租户令牌管理知识空间

```csharp
public class WikiTenantService
{
    private readonly IFeishuTenantV2Wiki _wikiClient;

    public WikiTenantService(IFeishuTenantV2Wiki wikiClient)
    {
        _wikiClient = wikiClient;
    }

    // 获取应用有权限访问的知识空间列表
    public async Task<List<SpaceInfo>> GetAccessibleSpacesAsync()
    {
        var spaces = new List<SpaceInfo>();
        string? pageToken = null;

        do
        {
            var result = await _wikiClient.GetSpacesPageListAsync(
                page_size: 50,
                page_token: pageToken);

            if (result?.Data?.Items != null)
            {
                spaces.AddRange(result.Data.Items);
            }

            pageToken = result?.Data?.PageToken;
        } while (!string.IsNullOrEmpty(pageToken));

        return spaces;
    }

    // 获取空间详情
    public async Task<SpaceInfo?> GetSpaceDetailAsync(string spaceId)
    {
        var result = await _wikiClient.GetSpaceInfoAsync(spaceId);
        return result?.Data?.Space;
    }

    // 为空间添加成员
    public async Task<bool> AddMemberToSpaceAsync(
        string spaceId, 
        string userOpenId, 
        string role = "member")
    {
        var request = new CreateSpaceMemberRequest
        {
            MemberType = "openid",
            MemberId = userOpenId,
            MemberRole = role
        };

        var result = await _wikiClient.CreateSpaceMemberAsync(
            spaceId, 
            request, 
            need_notification: true);

        return result?.Code == 0;
    }

    // 更新空间设置
    public async Task<bool> UpdateSpaceSettingsAsync(
        string spaceId,
        string? createSetting = null,
        string? securitySetting = null)
    {
        var request = new UpdateSpaceSettingRequest
        {
            CreateSetting = createSetting,
            SecuritySetting = securitySetting
        };

        var result = await _wikiClient.UpdateSpaceSettingAsync(spaceId, request);
        return result?.Code == 0;
    }
}
```
