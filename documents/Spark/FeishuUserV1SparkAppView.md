# 飞书妙搭视图 - 用户令牌（FeishuUserV1SparkAppView）

## 接口名称

**飞书妙搭视图（用户令牌）** -（`IFeishuUserV1SparkAppView`）

## 功能描述

提供以用户身份查询飞书妙搭应用视图数据记录的能力。飞书妙搭（Spark）视图 SDK 是一组服务端 OpenAPI 的封装，用于查询妙搭应用下的视图数据记录。支持查询视图数据记录操作。

## 参考文档

- [飞书妙搭概述 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview)

## 函数列表

| 函数名称                  | 功能描述           | 认证方式 | HTTP 方法 |
| ------------------------- | ------------------ | -------- | --------- |
| GetViewRecordListAsync    | 查询视图数据记录   | 用户令牌 | GET       |

## 函数详细内容

### 查询视图数据记录

查询应用下的视图数据记录，包括指定列、字段值及分页信息，适用于需要获取应用下某视图数据的记录、展示等场景。查询参数采用查询对象模式（`GetViewRecordListQuery`）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetViewRecordListResult>?> GetViewRecordListAsync(
    [Path] string app_id,
    [Path] string view_name,
    [Query] GetViewRecordListQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名      | 类型                         | 必填 | 说明                                                                 |
| ----------- | ---------------------------- | ---- | -------------------------------------------------------------------- |
| `app_id`    | `string`                     | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取，如 `https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v` 中的 `app_4jcn5n11bpf5v` 即为 app_id |
| `view_name` | `string`                     | ✅   | 妙搭视图表名，必须属于 app_id 对应的妙搭应用，可从妙搭应用数据库管理视图列表中获取，示例值：`student_table_view` |
| `query`     | `GetViewRecordListQuery?`    | ⚪   | 分页、列筛选、过滤、排序、环境与用户 ID 类型查询参数，该查询对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": "[{\"id\":1,\"name\":\"张三\"}]",
    "total": 100,
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回视图数据记录分页列表，`items` 为记录数组序列化后的 JSONString，需调用方自行反序列化。`GetViewRecordListQuery` 支持 `PageSize`、`PageToken`、`Select`、`Filter`、`Order`、`Env`、`UserIdentifierType` 过滤与排序条件，整体展开为对应的查询参数，新增可选条件时无需修改接口签名。
