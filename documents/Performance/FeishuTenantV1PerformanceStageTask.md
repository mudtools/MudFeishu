---
title: 绩效周期任务接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份获取飞书绩效周期任务，支持按指定用户或全量分页方式获取周期内各用户的环节任务信息（任务分类、截止时间、环节状态等）。
---

# 周期任务 - 租户令牌（FeishuTenantV1PerformanceStageTask）

## 接口名称

**周期任务（租户令牌）** -（`IFeishuTenantV1PerformanceStageTask`）

## 功能描述

提供以租户身份获取飞书绩效周期任务的能力。飞书绩效（Performance）「周期任务」SDK 是一组服务端 OpenAPI 的封装，用于按指定用户或全量分页方式获取周期内各用户的环节任务信息（任务分类、截止时间、环节状态等）。本接口全部端点为 performance/v1，仅支持 tenant_access_token 调用。支持获取周期任务（指定用户）、获取周期任务（全部用户）等操作。

## 参考文档

- [获取周期任务（指定用户）- 飞书开放平台](https://open.feishu.cn/document/performance-v1/stage_task/find_by_user_list)

## 函数列表

| 函数名称                     | 功能描述               | 认证方式 | HTTP 方法 |
| ---------------------------- | ---------------------- | -------- | --------- |
| FindStageTaskByUserListAsync | 获取周期任务（指定用户） | 租户令牌 | POST      |
| FindStageTaskByPageAsync     | 获取周期任务（全部用户） | 租户令牌 | POST      |

## 函数详细内容

### 获取周期任务（指定用户）

根据用户 ID 批量获取指定周期的任务信息，支持传入任务分类、任务截止时间参数筛选周期内任务数据（该接口无分页）。限频：20 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<FindStageTaskByUserListResult>?> FindStageTaskByUserListAsync(
    [Body] FindStageTaskByUserListRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                             | 必填 | 说明                                                                                                                                                                                    |
| -------------- | -------------------------------- | ---- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `request`      | `FindStageTaskByUserListRequest` | ✅   | 请求体（semester_id 周期 ID 必填；user_id_lists 用户 ID 列表必填，最大 50 个；task_option_lists 任务分类 1 待完成/2 已完成/3 已逾期，最大 3 个；after_time/before_time 截止时间范围，毫秒时间戳） |
| `user_id_type` | `string?`                        | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                                                                                                                 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "base": {
      "semester_id": "6992035450862224940",
      "semester_name": "2024 上半年绩效",
      "start_time": "1704038400000",
      "end_time": "1719763200000"
    },
    "items": [
      {
        "user_id": "bega29ca",
        "stage_num_lists": [],
        "stage_task_info_lists": []
      }
    ]
  }
}
```

**说明**：该接口无分页字段，一次返回全部指定用户的周期任务；用户 ID 列表最多 50 个，超出请分批调用。

---

### 获取周期任务（全部用户）

批量获取周期下所有用户的任务信息，支持传入任务分类、任务截止时间参数筛选周期内任务数据。限频：20 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<FindStageTaskByPageResult>?> FindStageTaskByPageAsync(
    [Body] FindStageTaskByPageRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                         | 必填 | 说明                                                                                                                                                                       |
| -------------- | ---------------------------- | ---- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `request`      | `FindStageTaskByPageRequest` | ✅   | 请求体（semester_id 周期 ID 必填；task_option_lists 任务分类 1 待完成/2 已完成/3 已逾期，最大 3 个；after_time/before_time 截止时间范围，毫秒时间戳；page_token 分页标记；page_size 分页大小，默认 20，最大 50） |
| `user_id_type` | `string?`                    | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                                                                                                      |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "base": {
      "semester_id": "6992035450862224940",
      "semester_name": "2024 上半年绩效",
      "start_time": "1704038400000",
      "end_time": "1719763200000"
    },
    "items": [
      {
        "user_id": "bega29ca",
        "stage_num_lists": [],
        "stage_task_info_lists": []
      }
    ],
    "has_more": true,
    "page_token": "next_page_token"
  }
}
```

**说明**：分页大小默认 20、最大 50。当 `has_more` 为 true 时，需将返回的 `page_token` 放入请求体继续拉取下一页数据。
