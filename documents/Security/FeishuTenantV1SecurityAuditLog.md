---
title: 行为审计日志接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份查询飞书行为审计日志，获取成员的操作行为日志（时间、地点、操作对象等），用于发现违规操作以保护企业数据安全。
---

# 行为审计日志 - 租户令牌（FeishuTenantV1SecurityAuditLog）

## 接口名称

**行为审计日志（租户令牌）** -（`IFeishuTenantV1SecurityAuditLog`）

## 功能描述

提供以租户身份查询飞书行为审计日志的能力。飞书安全与合规（Security）「行为审计日志」SDK 用于查询成员的操作行为日志（时间、地点、操作对象等），管理员可借此发现违规操作以保护企业数据和信息安全。实际端点路径为 admin/v1/audit_infos，仅支持 tenant_access_token 调用。查询时请适当缩短查询时间范围并控制查询频次。支持获取行为审计日志数据操作。

## 参考文档

- [获取行为审计日志数据 - 飞书开放平台](https://open.feishu.cn/document/server-docs/security_and_compliance-v1/audit_log/audit_data_get)

## 函数列表

| 函数名称             | 功能描述               | 认证方式 | HTTP 方法 | 接口文档 |
| -------------------- | ---------------------- | -------- | --------- |----------|
| GetAuditLogDataAsync | 获取行为审计日志数据   | 租户令牌 | GET       | [GetAuditLogDataAsync](https://open.feishu.cn/document/server-docs/security_and_compliance-v1/audit_log/audit_data_get) |

## 函数详细内容

### 获取行为审计日志数据

查询成员的操作行为日志，支持按事件名称、事件模块、操作者、操作对象、用户类型、时间范围（起止相差不超过 30 天）等筛选；查询参数采用查询对象模式 `GetAuditLogDataQuery`，见 AGENTS.md API-2。限频：100 次/分钟。所需权限：admin:audit_info:readonly（获取行为审计日志）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<GetAuditLogDataResult>?> GetAuditLogDataAsync(
    [Query] GetAuditLogDataQuery? query = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名  | 类型                      | 必填 | 说明                                                                                          |
| ------- | ------------------------- | ---- | --------------------------------------------------------------------------------------------- |
| `query` | `GetAuditLogDataQuery?`   | ⚪   | 时间范围（oldest/latest，秒级时间戳）、事件与操作者/对象筛选、分页参数等可选查询参数，该对象会整体展开为查询参数 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_more": true,
    "page_token": "next_page_token",
    "items": [
      {
        "event_id": "7001",
        "event_name": "下载文件",
        "event_module": "drive",
        "operator": "ou_30b07b63089ea46518789914dac63d36",
        "object": "docx_xxxxxxxx",
        "receiver": "ou_30b07b63089ea46518789914dac63d36",
        "env_info": {
          "ip": "10.0.0.1",
          "device": "Windows"
        },
        "event_time": "1704038400"
      }
    ]
  }
}
```

**说明**：查询时间范围的起止相差不超过 30 天，请适当缩短时间范围并控制查询频次。当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据。
