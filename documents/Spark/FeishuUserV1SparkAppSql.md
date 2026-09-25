# 飞书妙搭 SQL - 用户令牌（FeishuUserV1SparkAppSql）

## 接口名称

**飞书妙搭 SQL（用户令牌）** -（`IFeishuUserV1SparkAppSql`）

## 功能描述

提供以用户身份在飞书妙搭应用下执行 SQL 语句的能力。飞书妙搭（Spark）SQL SDK 是一组服务端 OpenAPI 的封装，用于在妙搭应用下执行 SQL 语句。支持执行 SQL 操作。

## 参考文档

- [飞书妙搭概述 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview)

## 函数列表

| 函数名称                 | 功能描述   | 认证方式 | HTTP 方法 |
| ------------------------ | ---------- | -------- | --------- |
| ExecuteSqlCommandAsync   | 执行 SQL   | 用户令牌 | POST      |

## 函数详细内容

### 执行 SQL

在应用下执行 SQL。接口频率限制 10 次/秒。

**函数签名**：

```csharp
Task<FeishuApiResult<ExecuteSqlCommandResult>?> ExecuteSqlCommandAsync(
    [Path] string app_id,
    [Body] ExecuteSqlCommandRequest request,
    [Query("env")] string? env = "online",
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名     | 类型                       | 必填 | 说明                                                                 |
| ---------- | -------------------------- | ---- | -------------------------------------------------------------------- |
| `app_id`   | `string`                   | ✅   | 妙搭应用 id，可从妙搭应用 URL 中获取，如 `https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v` 中的 `app_4jcn5n11bpf5v` 即为 app_id |
| `request`  | `ExecuteSqlCommandRequest` | ✅   | 执行 SQL 请求体（`sql` 为要执行的 SQL 语句）                         |
| `env`      | `string?`                  | ⚪   | 访问的 database 环境，默认为 online（线上环境），示例值：`online`、`dev` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "result": "[{\"id\":1,\"name\":\"张三\"}]"
  }
}
```

**说明**：`SELECT` 命令返回查询结果的 JSON 序列化字符串，调用方需自行反序列化；`DELETE` 等无返回命令的 `result` 为空。接口调用频率限制为 10 次/秒，超出会被限流。
