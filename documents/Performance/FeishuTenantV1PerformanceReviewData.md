---
title: 绩效结果接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份获取飞书绩效结果，返回被评估人在指定周期、项目中各环节的评估结果信息（周期、项目、评估项、评估模板及评估数据）。
---

# 绩效结果 - 租户令牌（FeishuTenantV1PerformanceReviewData）

## 接口名称

**绩效结果（租户令牌）** -（`IFeishuTenantV1PerformanceReviewData`）

## 功能描述

提供以租户身份获取飞书绩效结果的能力。飞书绩效（Performance）「绩效结果」SDK（v1）用于获取被评估人在指定周期、指定项目中各个环节的评估结果信息，包含绩效所在的周期、项目、评估项、评估模版以及各环节评估数据等信息。仅支持 tenant_access_token 或 user_access_token 调用；tenant_access_token 鉴权模式下推荐使用 v2 的获取绩效详情数据接口获取更丰富的返回数据。支持获取绩效结果操作。

## 参考文档

- [获取绩效结果 - 飞书开放平台](https://open.feishu.cn/document/server-docs/performance-v1/query)

## 函数列表

| 函数名称             | 功能描述     | 认证方式 | HTTP 方法 |
| -------------------- | ------------ | -------- | --------- |
| QueryReviewDataAsync | 获取绩效结果 | 租户令牌 | POST      |

## 函数详细内容

### 获取绩效结果

获取被评估人在指定周期、指定项目中各个环节的评估结果信息，包含绩效所在的周期、项目、评估项、评估模版以及各环节评估数据等信息。限频：20 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryReviewDataResult>?> QueryReviewDataAsync(
    [Body] QueryReviewDataRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                     | 必填 | 说明                                                                                                                                                                                                                                                                                                     |
| -------------- | ------------------------ | ---- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `request`      | `QueryReviewDataRequest` | ✅   | 请求体（start_time/end_time 周期时间范围必填，填写 semester_id_list 时无效；stage_types 环节类型必填，仅支持 leader_review/communication_and_open_result/view_result；stage_progress 环节状态 0~4；semester_id_list 周期 ID 列表最大 50 个；reviewee_user_id_list 被评估人 ID 列表必填最大 50 个；updated_later_than 更新时间下限，毫秒时间戳） |
| `user_id_type` | `string?`                | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                                                                                                                                                                                                                                   |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "semesters": [
      {
        "id": "6992035450862224940",
        "name": "2024 上半年绩效",
        "start_time": "1704038400000",
        "end_time": "1719763200000"
      }
    ],
    "activities": [
      {
        "id": "7343513161666707459",
        "name": "上半年绩效评估"
      }
    ],
    "indicators": [
      {
        "id": "7343513161666707459",
        "name": "价值观"
      }
    ],
    "templates": [],
    "units": [],
    "fields": [],
    "datas": []
  }
}
```

**说明**：返回结果为「字典 + 数据」结构：`semesters`/`activities`/`indicators`/`templates`/`units`/`fields` 为维度字典，`datas` 为各环节评估数据并引用上述字典 ID。填写 `semester_id_list` 时 `start_time`/`end_time` 无效；需要更丰富的环节评估数据时，建议改用 v2 的获取绩效详情数据接口。
