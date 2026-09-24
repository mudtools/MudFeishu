# 飞书 Aily 技能 - 租户令牌（FeishuTenantV1AilySkills）

## 接口名称

**飞书 Aily 技能（租户令牌）** -（`IFeishuTenantV1AilySkills`）

## 功能描述

提供以租户身份调用飞书 Aily 技能的能力。飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的能力，把它集成到自己的业务系统里。支持调用技能、获取技能信息和查询技能列表等操作。

## 参考文档

- [飞书 Aily 使用指南 - 飞书开放平台](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent/agentuseguide)

## 函数列表

| 函数名称                | 功能描述       | 认证方式 | HTTP 方法 |
| ----------------------- | -------------- | -------- | --------- |
| StartSkillAsync         | 调用技能       | 租户令牌 | POST      |
| GetSkillAsync           | 获取技能信息   | 租户令牌 | GET       |
| GetSkillPageListAsync   | 查询技能列表   | 租户令牌 | GET       |

## 函数详细内容

### 调用技能

用于调用某个 Aily 应用的特定技能，支持指定技能入参；并同步返回技能执行的结果。

**函数签名**：

```csharp
Task<FeishuApiResult<StartSkillResult>?> StartSkillAsync(
    [Path] string app_id,
    [Path] string skill_id,
    [Body] StartSkillRequest request,
    [Header("X-Aily-BizUserID")] string? x_aily_biz_user_id = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名               | 类型                | 必填 | 说明                                                                                 |
| -------------------- | ------------------- | ---- | ------------------------------------------------------------------------------------ |
| `app_id`             | `string`            | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_e7004f87f1__c`                         |
| `skill_id`           | `string`            | ✅   | 技能 ID，示例值：`skill_6cc6166178ca`                                                 |
| `request`            | `StartSkillRequest` | ✅   | 调用技能请求体                                                                        |
| `x_aily_biz_user_id` | `string?`           | ⚪   | 可选请求头（`X-Aily-BizUserID`），标识创建会话的唯一用户 ID（建议使用内部唯一 ID 或飞书账号的 user_id），最大长度 255 |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "output": "技能执行结果",
    "status": "success"
  }
}
```

**说明**：同步返回技能执行输出与状态；请求体通过 `global_variable`（含 `query`、`files`、`channel`）与 `input` 传入技能入参。以应用身份调用时可通过 `X-Aily-BizUserID` 请求头标识业务侧的用户，便于在 Aily 侧串联同一用户的会话。

---

### 获取技能信息

用于查询某个 Aily 应用的特定技能详情。

**函数签名**：

```csharp
Task<FeishuApiResult<SkillOopsResult>?> GetSkillAsync(
    [Path] string app_id,
    [Path] string skill_id,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名      | 类型     | 必填 | 说明                                                 |
| ----------- | -------- | ---- | ---------------------------------------------------- |
| `app_id`    | `string` | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_e7004f87f1__c` |
| `skill_id`  | `string` | ✅   | 技能 ID，示例值：`skill_6cc6166178ca`               |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "skill": {
      "id": "skill_6cc6166178ca",
      "label": "数据分析",
      "description": "对指定数据进行分析",
      "samples": [
        "分析上月的销售数据"
      ],
      "input_schema": "{}",
      "output_schema": "{}"
    }
  }
}
```

**说明**：返回的 `input_schema` / `output_schema` 描述技能的输入输出结构，可用于构造调用技能的请求体与解析结果。

---

### 查询技能列表

用于查询某个 Aily 应用的技能列表；包括内置的数据分析与问答技能、以及未在对话开启的技能。

**函数签名**：

```csharp
Task<FeishuApiResult<SkillPageListResult>?> GetSkillPageListAsync(
    [Path] string app_id,
    [Query("page_size")] int page_size = Consts.PageSize_20,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名        | 类型      | 必填 | 说明                                                                 |
| ------------- | --------- | ---- | -------------------------------------------------------------------- |
| `app_id`      | `string`  | ✅   | Aily 应用 ID（spring_xxx__c），示例值：`spring_e7004f87f1__c`         |
| `page_size`   | `int`     | ⚪   | 分页大小，默认取 `Consts.PageSize_20`                                |
| `page_token`  | `string?` | ⚪   | 分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "skills": [
      {
        "id": "skill_6cc6166178ca",
        "label": "数据分析",
        "description": "对指定数据进行分析",
        "samples": [
          "分析上月的销售数据"
        ],
        "input_schema": "{}",
        "output_schema": "{}"
      }
    ],
    "has_more": false,
    "page_token": ""
  }
}
```

**说明**：返回技能分页列表；`has_more` 为 true 时需携带返回的 `page_token` 继续拉取。列表包含未在对话中开启的技能，可直接调用。
