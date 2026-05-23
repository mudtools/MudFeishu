## 接口名称

**Wiki 知识库节点（租户令牌）- (IFeishuTenantV2WikiNodes)**

## 功能描述

知识空间中的节点，支持文档、表格等多种文件类型。文件是各种类型的文件的统称，泛指云空间内所有的文件。每个文件都有唯一 token 作为标识。

本接口提供以**租户身份**对知识空间节点进行管理的能力，包括创建节点、移动节点、复制节点、获取节点信息等操作，适用于后台服务、机器人等场景。

## 参考文档

- [飞书 Wiki 概述](https://open.feishu.cn/document/server-docs/docs/wiki-v2/wiki-overview)

## 函数列表

| 函数名称                           | 功能描述           | 认证方式 | HTTP 方法 |
| ------------------------------ | -------------- | ---- | ------- |
| `CreateSpaceNodeAsync`         | 创建知识空间节点       | 租户令牌 | POST    |
| `GetNodeSpaceInfoAsync`        | 获取知识空间节点信息     | 租户令牌 | GET     |
| `GetSpaceNodesPageListAsync`   | 获取知识空间子节点列表    | 租户令牌 | GET     |
| `MoveSpaceNodeAsync`           | 移动知识空间节点       | 租户令牌 | POST    |
| `UpdateTitleSpaceNodeAsync`    | 更新知识空间节点标题     | 租户令牌 | POST    |
| `CopySpaceNodeAsync`           | 创建知识空间节点副本     | 租户令牌 | POST    |
| `MoveDocsToWikiSpaceNodeAsync` | 移动云空间文档至知识空间   | 租户令牌 | POST    |
| `GetTaskByIdAsync`             | 获取 Wiki 异步任务结果 | 租户令牌 | GET     |

## 函数详细内容

### 创建知识空间节点

**函数名称**：创建知识空间节点

**函数签名**：

```csharp
Task<FeishuApiResult<SpaceNodeResult>?> CreateSpaceNodeAsync(
    [Path] string space_id,
    [Body] CreateSpaceNodeRequest createSpaceNodeRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                      | 类型                       | 必填   | 说明      |
| ------------------------ | ------------------------ | ---- | ------- |
| `space_id`               | `string`                 | ✅ 必填 | 知识空间 ID |
| `createSpaceNodeRequest` | `CreateSpaceNodeRequest` | ✅ 必填 | 创建节点请求体 |
| `cancellationToken`      | `CancellationToken`      | ⚪ 可选 | 取消操作令牌  |

**CreateSpaceNodeRequest 参数**：

| 参数名                 | 类型        | 必填   | 说明                                           |
| ------------------- | --------- | ---- | -------------------------------------------- |
| `obj_type`          | `string`  | ✅ 必填 | 文档类型：docx、sheet、mindnote、bitable、file、slides |
| `parent_node_token` | `string?` | ⚪ 可选 | 父节点 token，为空则创建为一级节点                         |
| `node_type`         | `string`  | ✅ 必填 | 节点类型：origin（实体）、shortcut（快捷方式）               |
| `origin_node_token` | `string?` | ⚪ 可选 | 快捷方式对应的实体 node\_token                        |
| `title`             | `string?` | ⚪ 可选 | 文档标题                                         |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "node": {
      "space_id": "6870403571079249922",
      "node_token": "wikcnKQ1k3p******8Vabcef",
      "obj_token": "docx******xxx",
      "obj_type": "docx",
      "parent_node_token": null,
      "node_type": "origin",
      "title": "产品需求文档"
    }
  }
}
```

***

### 获取知识空间节点信息

**函数名称**：获取知识空间节点信息

**函数签名**：

```csharp
Task<FeishuApiResult<SpaceNodeResult>?> GetNodeSpaceInfoAsync(
    [Query("token")] string token,
    [Query("obj_type")] string? obj_type = "wiki",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                 | 类型                  | 必填   | 说明                       |
| ------------------- | ------------------- | ---- | ------------------------ |
| `token`             | `string`            | ✅ 必填 | 知识库节点 token 或云文档实际 token |
| `obj_type`          | `string?`           | ⚪ 可选 | 文档类型，默认 wiki             |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌                   |

**说明**：

- 知识库节点 token：URL 中 token 前为 wiki
- 云文档实际 token：URL 中 token 前为 docx、base、sheets 等
- 使用云文档 token 查询时，需要对 obj\_type 参数传入文档对应的类型

***

### 获取知识空间子节点列表

**函数名称**：获取知识空间子节点列表

**函数签名**：

```csharp
Task<FeishuApiPageListResult<SpaceNodeInfo>?> GetSpaceNodesPageListAsync(
    [Path] string space_id,
    [Query("parent_node_token")] string? parent_node_token = null,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                 | 类型                  | 必填   | 说明                 |
| ------------------- | ------------------- | ---- | ------------------ |
| `space_id`          | `string`            | ✅ 必填 | 知识空间 ID            |
| `parent_node_token` | `string?`           | ⚪ 可选 | 父节点 token，为空获取一级节点 |
| `page_size`         | `int`               | ⚪ 可选 | 分页大小，默认 10         |
| `page_token`        | `string?`           | ⚪ 可选 | 分页标记               |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌             |

**说明**：

- 此接口为分页接口，由于权限过滤，可能返回列表为空，但分页标记（has\_more）为 true 时，可以继续分页请求

***

### 移动知识空间节点

**函数名称**：移动知识空间节点

**函数签名**：

```csharp
Task<FeishuApiResult<SpaceNodeResult>?> MoveSpaceNodeAsync(
    [Path] string space_id,
    [Path] string node_token,
    [Body] MoveSpaceNodeRequest moveSpaceNodeRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型                     | 必填   | 说明            |
| ---------------------- | ---------------------- | ---- | ------------- |
| `space_id`             | `string`               | ✅ 必填 | 知识空间 ID       |
| `node_token`           | `string`               | ✅ 必填 | 需要移动的节点 token |
| `moveSpaceNodeRequest` | `MoveSpaceNodeRequest` | ✅ 必填 | 移动节点请求体       |
| `cancellationToken`    | `CancellationToken`    | ⚪ 可选 | 取消操作令牌        |

**MoveSpaceNodeRequest 参数**：

| 参数名                   | 类型        | 必填   | 说明          |
| --------------------- | --------- | ---- | ----------- |
| `target_parent_token` | `string?` | ⚪ 可选 | 目标父节点 token |
| `target_space_id`     | `string?` | ⚪ 可选 | 目标知识空间 ID   |

**说明**：

- 支持跨知识空间移动
- 如果有子节点，会携带子节点一起移动

***

### 更新知识空间节点标题

**函数名称**：更新知识空间节点标题

**函数签名**：

```csharp
Task<FeishuNullDataApiResult?> UpdateTitleSpaceNodeAsync(
    [Path] string space_id,
    [Path] string node_token,
    [Body] UpdateTitleSpaceNodeRequest updateTitleSpaceNodeRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                           | 类型                            | 必填   | 说明       |
| ----------------------------- | ----------------------------- | ---- | -------- |
| `space_id`                    | `string`                      | ✅ 必填 | 知识空间 ID  |
| `node_token`                  | `string`                      | ✅ 必填 | 节点 token |
| `updateTitleSpaceNodeRequest` | `UpdateTitleSpaceNodeRequest` | ✅ 必填 | 更新标题请求体  |
| `cancellationToken`           | `CancellationToken`           | ⚪ 可选 | 取消操作令牌   |

**UpdateTitleSpaceNodeRequest 参数**：

| 参数名     | 类型       | 必填   | 说明    |
| ------- | -------- | ---- | ----- |
| `title` | `string` | ✅ 必填 | 节点新标题 |

***

### 创建知识空间节点副本

**函数名称**：创建知识空间节点副本

**函数签名**：

```csharp
Task<FeishuApiResult<SpaceNodeResult>?> CopySpaceNodeAsync(
    [Path] string space_id,
    [Path] string node_token,
    [Body] CopySpaceNodeRequest copySpaceNodeRequest,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                    | 类型                     | 必填   | 说明       |
| ---------------------- | ---------------------- | ---- | -------- |
| `space_id`             | `string`               | ✅ 必填 | 知识空间 ID  |
| `node_token`           | `string`               | ✅ 必填 | 节点 token |
| `copySpaceNodeRequest` | `CopySpaceNodeRequest` | ✅ 必填 | 复制节点请求体  |
| `cancellationToken`    | `CancellationToken`    | ⚪ 可选 | 取消操作令牌   |

**CopySpaceNodeRequest 参数**：

| 参数名                   | 类型        | 必填   | 说明          |
| --------------------- | --------- | ---- | ----------- |
| `target_parent_token` | `string?` | ⚪ 可选 | 目标父节点 Token |
| `target_space_id`     | `string?` | ⚪ 可选 | 目标知识空间 ID   |
| `title`               | `string?` | ⚪ 可选 | 复制后的新标题     |

**说明**：

- 目标知识空间 ID 与目标父节点 Token 不可同时为空

***

### 移动云空间文档至知识空间

**函数名称**：移动云空间文档至知识空间

**函数签名**：

```csharp
Task<FeishuApiResult<MoveDocsToWikiSpaceNodeResult>?> MoveDocsToWikiSpaceNodeAsync(
    [Path] string space_id,
    [Body] MoveDocsToWikiSpaceNodeRequest moveDocsToWikiSpaceNode,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                       | 类型                               | 必填   | 说明      |
| ------------------------- | -------------------------------- | ---- | ------- |
| `space_id`                | `string`                         | ✅ 必填 | 知识空间 ID |
| `moveDocsToWikiSpaceNode` | `MoveDocsToWikiSpaceNodeRequest` | ✅ 必填 | 移动文档请求体 |
| `cancellationToken`       | `CancellationToken`              | ⚪ 可选 | 取消操作令牌  |

**MoveDocsToWikiSpaceNodeRequest 参数**：

| 参数名                 | 类型        | 必填   | 说明                            |
| ------------------- | --------- | ---- | ----------------------------- |
| `parent_wiki_token` | `string?` | ⚪ 可选 | 父节点 token，为空则移动为一级节点          |
| `obj_type`          | `string`  | ✅ 必填 | 文档类型：doc、sheet、bitable、docx 等 |
| `obj_token`         | `string`  | ✅ 必填 | 文档 token                      |
| `apply`             | `bool?`   | ⚪ 可选 | 无权限时是否申请移动                    |

**说明**：

- 该接口为异步接口

***

### 获取 Wiki 异步任务结果

**函数名称**：获取 Wiki 异步任务结果

**函数签名**：

```csharp
Task<FeishuApiResult<GetTaskResult>?> GetTaskByIdAsync(
    [Path] string task_id,
    [Query("task_type")] string task_type = "move",
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名                 | 类型                  | 必填   | 说明           |
| ------------------- | ------------------- | ---- | ------------ |
| `task_id`           | `string`            | ✅ 必填 | 任务 ID        |
| `task_type`         | `string`            | ⚪ 可选 | 任务类型，默认 move |
| `cancellationToken` | `CancellationToken` | ⚪ 可选 | 取消操作令牌       |

***

## 代码示例

### 使用租户令牌管理知识空间节点

```csharp
public class WikiNodeTenantService
{
    private readonly IFeishuTenantV2WikiNodes _wikiNodesClient;

    public WikiNodeTenantService(IFeishuTenantV2WikiNodes wikiNodesClient)
    {
        _wikiNodesClient = wikiNodesClient;
    }

    // 创建文档节点
    public async Task<SpaceNodeInfo?> CreateDocumentAsync(
        string spaceId, 
        string title, 
        string? parentNodeToken = null)
    {
        var request = new CreateSpaceNodeRequest
        {
            ObjType = "docx",
            NodeType = "origin",
            Title = title,
            ParentNodeToken = parentNodeToken
        };

        var result = await _wikiNodesClient.CreateSpaceNodeAsync(spaceId, request);
        return result?.Data?.Node;
    }

    // 遍历知识空间节点树
    public async Task<List<SpaceNodeInfo>> GetAllNodesAsync(string spaceId)
    {
        var allNodes = new List<SpaceNodeInfo>();
        await GetNodesRecursiveAsync(spaceId, null, allNodes);
        return allNodes;
    }

    private async Task GetNodesRecursiveAsync(
        string spaceId, 
        string? parentToken, 
        List<SpaceNodeInfo> allNodes)
    {
        string? pageToken = null;

        do
        {
            var result = await _wikiNodesClient.GetSpaceNodesPageListAsync(
                spaceId, 
                parentToken, 
                page_size: 50,
                page_token: pageToken);

            if (result?.Data?.Items == null) break;

            foreach (var node in result.Data.Items)
            {
                allNodes.Add(node);

                if (node.HasChild == true)
                {
                    await GetNodesRecursiveAsync(spaceId, node.NodeToken, allNodes);
                }
            }

            pageToken = result.Data.PageToken;
        } while (!string.IsNullOrEmpty(pageToken));
    }

    // 移动节点到新的父节点
    public async Task<bool> MoveNodeAsync(
        string spaceId, 
        string nodeToken, 
        string? targetParentToken = null,
        string? targetSpaceId = null)
    {
        var request = new MoveSpaceNodeRequest
        {
            TargetParentToken = targetParentToken,
            TargetSpaceId = targetSpaceId
        };

        var result = await _wikiNodesClient.MoveSpaceNodeAsync(spaceId, nodeToken, request);
        return result?.Code == 0;
    }

    // 复制节点
    public async Task<SpaceNodeInfo?> CopyNodeAsync(
        string spaceId, 
        string nodeToken, 
        string? targetParentToken = null,
        string? newTitle = null)
    {
        var request = new CopySpaceNodeRequest
        {
            TargetParentToken = targetParentToken,
            Title = newTitle
        };

        var result = await _wikiNodesClient.CopySpaceNodeAsync(spaceId, nodeToken, request);
        return result?.Data?.Node;
    }

    // 将云文档移动到知识空间
    public async Task<bool> MoveDocToWikiAsync(
        string spaceId,
        string docToken,
        string objType,
        string? parentWikiToken = null)
    {
        var request = new MoveDocsToWikiSpaceNodeRequest
        {
            ObjToken = docToken,
            ObjType = objType,
            ParentWikiToken = parentWikiToken,
            Apply = false
        };

        var result = await _wikiNodesClient.MoveDocsToWikiSpaceNodeAsync(spaceId, request);
        return result?.Code == 0;
    }
}
```

