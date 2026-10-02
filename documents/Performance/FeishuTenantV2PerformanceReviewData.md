---
title: 绩效详情数据接口（租户令牌）| MudFeishu
description: 该接口用于以租户身份获取飞书绩效详情数据，返回被评估人各环节的绩效评估详情（不含校准环节），相比 v1 返回数据更丰富。
---

# 绩效详情数据 - 租户令牌（FeishuTenantV2PerformanceReviewData）

## 接口名称

**绩效详情数据（租户令牌）** -（`IFeishuTenantV2PerformanceReviewData`）

## 功能描述

提供以租户身份获取飞书绩效详情数据的能力。飞书绩效（Performance）「绩效详情数据」SDK（v2）用于获取被评估人各环节的绩效评估详情（不包含校准环节），如环节评估数据、环节提交状态等；相比 v1 获取绩效结果接口返回数据更丰富。本接口仅支持 tenant_access_token 调用。支持获取绩效详情数据操作。

## 参考文档

- [获取绩效详情数据 - 飞书开放平台](https://open.feishu.cn/document/performance-v1/review_data/query-2)

## 函数列表

| 函数名称                  | 功能描述         | 认证方式 | HTTP 方法 | 接口文档 |
| ------------------------- | ---------------- | -------- | --------- |----------|
| QueryReviewDataDetailAsync | 获取绩效详情数据 | 租户令牌 | POST      | [QueryReviewDataDetailAsync](https://open.feishu.cn/document/performance-v1/review_data/query-2) |

## 函数详细内容

### 获取绩效详情数据

获取被评估人各环节的绩效评估详情（不包含校准环节），如环节评估数据、环节提交状态等；stage_types 与 stage_ids 至少要传一个，不传默认不返回任何环节评估数据。限频：20 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。

**函数签名**：

```csharp
Task<FeishuApiResult<QueryReviewDataDetailResult>?> QueryReviewDataDetailAsync(
    [Body] QueryReviewDataDetailRequest request,
    [Query("user_id_type")] string? user_id_type = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名         | 类型                           | 必填 | 说明                                                                                                                                                                                                                                                                                                         |
| -------------- | ------------------------------ | ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `request`      | `QueryReviewDataDetailRequest` | ✅   | 请求体（semester_ids 周期 ID 列表必填 0~10 个；reviewee_user_ids 被评估人 ID 列表必填 0~10 个；stage_types 环节类型与 stage_ids 环节 ID 至少传一个；review_stage_roles 评估型环节执行人角色；need_leader_review_data_source 是否返回终评数据来源；updated_later_than 更新时间下限，毫秒时间戳；stage_progresses 环节状态列表 0~50 个） |
| `user_id_type` | `string?`                      | ⚪   | 用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id                                                                                                                                                                                                                                     |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "datas": [
      {
        "user_id": {
          "union_id": "on_8ddcc7c7c0f7c09f8d7c1a3b2c4d5e6f",
          "user_id": "bega29ca"
        },
        "semester_id": "7343513161666707459",
        "activity_id": "7343513161666707459",
        "review_template_id": "7343513161666723843",
        "stages": []
      }
    ]
  }
}
```

**说明**：`stage_types` 与 `stage_ids` 至少传一个，否则不返回任何环节评估数据。返回不包含校准环节；`stages` 内含环节、评估记录、评估内容明细以及 360° 与合作项目上级记录信息。
