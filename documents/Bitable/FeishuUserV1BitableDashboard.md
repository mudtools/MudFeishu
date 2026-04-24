# 多维表格仪表盘 - 用户权限（FeishuUserV1BitableDashboard）

## 接口名称

**多维表格仪表盘（用户权限）** -（`IFeishuUserV1BitableDashboard`）

## 功能描述

提供以用户身份管理飞书多维表格仪表盘的能力。仪表盘 block，仪表盘与数据看板类似，可以从不同的维度统计对多维表格中的数据进行统计。仪表盘的唯一标识为 block_id，以 blk 开头，可通过多维表格 URL 获取 block_id。支持复制仪表盘和列出仪表盘等操作。

## 参考文档

- [仪表盘 - 飞书开放平台](https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-dashboard/copy)

## 函数列表

| 函数名称                    | 功能描述     | 认证方式 | HTTP 方法 |
| --------------------------- | ------------ | -------- | --------- |
| CopyDashboardAsync          | 复制仪表盘   | 用户令牌 | POST      |
| GetDashboardPageListAsync   | 列出仪表盘   | 用户令牌 | GET       |

## 函数详细内容

### 复制仪表盘

基于现有仪表盘复制出新的仪表盘。

**函数签名**：

```csharp
Task<FeishuApiResult<CopyDashboardResult>?> CopyDashboardAsync(
    [Path] string app_token,
    [Path] string block_id,
    [Body] CopyDashboardRequest copyDashboardRequest,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名                  | 类型                      | 必填 | 说明                                                               |
| ----------------------- | ------------------------- | ---- | ------------------------------------------------------------------ |
| `app_token`             | `string`                  | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`     |
| `block_id`              | `string`                  | ✅   | 多维表格仪表盘的唯一标识，以 blk 开头，示例值：`blkEsvEEaNllY2UV` |
| `copyDashboardRequest`  | `CopyDashboardRequest`   | ✅   | 复制仪表盘请求体                                                   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "block_id": "blkNewDashboardId",
    "name": "复制的仪表盘"
  }
}
```

**说明**：复制仪表盘时将创建一个与原仪表盘结构相同的新仪表盘。以用户身份操作时，操作记录将关联到当前用户。

---

### 列出仪表盘

分页列出多维表格中的所有仪表盘。

**函数签名**：

```csharp
Task<FeishuApiPageListResult<AppDashboard>?> GetDashboardPageListAsync(
    [Path] string app_token,
    [Query("page_size")] int page_size = 20,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                                                     |
| ------------- | --------- | ---- | ---------------------------------------------------------------------------------------- |
| `app_token`   | `string`  | ✅   | 多维表格 App 的唯一标识，示例值：`AW3Qbtr2cakCnesXzXVbbsrIcVT`                           |
| `page_size`   | `int`     | ⚪   | 分页大小，默认值：20                                                                     |
| `page_token`  | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "block_id": "blkEsvEEaNllY2UV",
        "name": "仪表盘1"
      }
    ],
    "page_token": "next_page_token",
    "has_more": false
  }
}
```

**说明**：返回多维表格中所有仪表盘的基本信息，支持分页获取。
