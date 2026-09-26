# 飞书招聘（Hire）SDK 接口文档

## 概述

飞书招聘（Hire）SDK 提供了招聘服务端 OpenAPI（hire-v1 招聘配置类端点与 candidate-management 候选人管理端点）的完整封装，覆盖职位与招聘需求、面试/Offer 配置、设置与字典、招聘官网与内推、人才管理、投递流程管理、外部系统信息导入、生态对接、附件与猎头供应商等招聘全链路能力。

> 说明：本 SDK 共 165 个端点，其中 164 个为 **tenant-only**（仅支持 tenant_access_token），1 个为 **user-only**（仅支持 user_access_token，见 `IFeishuUserV1HireCandidate`）；令牌类型由接口上的 `[Token(FeishuTokenTypes.TenantAccessToken | FeishuTokenTypes.UserAccessToken, ...)]` 声明，调用方无需额外指定。接口已按子域方案（B）组织为 12 个（11 个租户态 + 1 个用户态），见下方「API 接口导航」。

**主要功能：**

- 职位组合创建/更新（一步完成职位、管理人员、登记表配置）
- 职位设置（JobConfig）的读取与更新（面试轮次、登记表、自助约面等）
- 职位管理人员（招聘负责人/用人经理/招聘助理）批量更新与查询
- 职位基础信息、聚合详情（含设置、门店、标签、阶段统计）与列表查询
- 职位上架（open）、职位发布记录搜索、招聘官网广告发布
- 地址码查询（按国家/省/市/区）与地址列表查询（按用途）
- 角色详情/列表、用户角色列表查询（含业务管理范围与权限配置）
- 职位模板、职能分类、职位类别列表查询
- 招聘需求 CRUD（创建/更新/查询/删除）与需求模板查询
- 面试设置：面试轮次类型、面试反馈表、面试登记表模板、面试官认证信息
- Offer 设置：申请表列表/模板详情、自定义字段更新、审批模板
- 招聘配置字典：招聘流程、科目、信息登记表模板、人才标签
- 候选人全链路：内推与内推官网职位、招聘官网（推广渠道/官网用户/官网职位/官网投递与投递任务/申请表模板）
- 人才管理：人才组合创建/更新、人才池与人才文件夹、标签与黑名单、在职状态、人才字段与列表
- 投递流程管理：面试信息与评价记录（v1/v2）、面试附件与速记、满意度问卷、Offer 全生命周期、背调订单、三方协议、入职/转正/离职与员工维护
- 外部系统（ATS/RMS）信息导入：外部投递、外部面试与面评、外部 Offer、外部背调、外部内推奖励
- 生态对接：背调/笔试服务商的账号自定义字段、背调订单进度与结果回传、背调套餐、笔试安排与结果回传、试卷列表
- 内推奖励账户：注册、启用/停用、余额查询、全额提现与提现对账
- 猎头供应商：供应商查询、猎头账号禁用/启用、人才猎头保护期
- 招聘附件：附件上传、附件元信息、人才简历附件 PDF 下载链接

**适用场景：**

- 与三方招聘系统（ATS/RMS）同步职位与组织枚举主数据
- 按规则批量创建/更新职位并自动发布至招聘官网
- 权限审计：拉取角色与用户角色的权限配置及业务管理范围
- 招聘数据仓库：增量拉取职位列表与发布记录

**文档使用指引：**

本索引文档提供了所有飞书招聘相关 API 的导航入口。每个 API 文档包含接口名称、功能描述、函数签名、参数说明及参考文档链接。

## 快速开始

### 安装

```bash
dotnet add package Mud.Feishu
```

### 配置文件

在 `appsettings.json` 中添加飞书应用配置：

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "cli_xxx",
      "AppSecret": "your_app_secret",
      "BaseUrl": "https://open.feishu.cn",
      "IsDefault": true
    }
  ]
}
```

### 注册服务

在 `Program.cs` 中注册飞书服务：

```csharp
// 添加飞书服务
builder.Services.AddFeishuApp(builder.Configuration, "FeishuApps");

// 注册 API 服务（FeishuModule.Hire 或 FeishuModule.All 均可）
builder.Services.CreateFeishuServicesBuilder()
    .AddHireApi()
    .Build();
```

### 依赖注入使用

在 Controller 或服务中通过构造函数注入接口：

```csharp
using Mud.Feishu;

public class HireController : ControllerBase
{
    private readonly IFeishuTenantV1HireJob _jobApi;

    public HireController(IFeishuTenantV1HireJob jobApi)
    {
        _jobApi = jobApi;
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetJob(string jobId)
    {
        var result = await _jobApi.GetJobAsync(jobId, user_id_type: "open_id");
        return Ok(result);
    }
}
```

## API 接口导航

接口已按「模块 × 令牌 × 功能子域」组织为 12 个（合并规则见 AGENTS.md「接口子域合并」）：

| 子域接口 | 方法数 | 覆盖范围 | 文档 |
| -------- | ----- | -------- | ---- |
| `IFeishuTenantV1HireJob` | 15 | 职位组合创建/更新、职位设置、管理人员批量更新、职位信息/聚合详情/列表、职位开放、发布人；职位类别、职能分类、职位模板、发布记录搜索、广告发布 | [职位（租户）](./FeishuTenantV1HireJob.md) |
| `IFeishuTenantV1HireJobRequirement` | 6 | 招聘需求创建/更新/按 ID 批量查询/列表/删除、需求模板 | [招聘需求（租户）](./FeishuTenantV1HireJobRequirement.md) |
| `IFeishuTenantV1HireInterview` | 5 | 面试轮次类型、面试反馈表、面试登记表模板、面试官查询与更新 | [面试设置（租户）](./FeishuTenantV1HireInterview.md) |
| `IFeishuTenantV1HireOffer` | 4 | Offer 申请表列表/详情、申请表自定义字段更新、Offer 审批模板 | [Offer 设置（租户）](./FeishuTenantV1HireOffer.md) |
| `IFeishuTenantV1HireSetting` | 9 | 招聘流程、科目、信息登记表模板、人才标签、地点、角色、用户角色 | [招聘设置与字典（租户）](./FeishuTenantV1HireSetting.md) |
| `IFeishuTenantV1HireCandidate` | 70 | 内推信息与内推官网职位、招聘官网/推广渠道/官网用户/官网职位、官网投递与投递任务、官网申请表模板；人才管理（人才池/文件夹/标签/黑名单/组合创建更新/入职状态）；投递流程管理（面试信息与评价记录、Offer、背调订单、三方协议、入职与员工）；人才备注、评估/阅卷/面试任务、简历来源 | [候选人（租户）](./FeishuTenantV1HireCandidate.md) |
| `IFeishuUserV1HireCandidate` | 1 | 以用户身份批量获取招聘待办事项（评估/Offer/笔试/面试待办） | [候选人（用户）](./FeishuUserV1HireCandidate.md) |
| `IFeishuTenantV1HireExternal` | 22 | 外部系统信息导入：人才外部信息、外部投递、外部面试与面评、外部 Offer、外部背调、外部内推奖励 | [外部系统信息导入（租户）](./FeishuTenantV1HireExternal.md) |
| `IFeishuTenantV1HireEco` | 17 | 生态对接：账号自定义字段、背调订单进度/结果回传、背调自定义字段与套餐、笔试安排/结果回传、试卷列表 | [生态对接（租户）](./FeishuTenantV1HireEco.md) |
| `IFeishuTenantV1HireReferralAccount` | 6 | 内推奖励账户注册、启用/停用、余额查询、全额提现、提现对账 | [内推账户（租户）](./FeishuTenantV1HireReferralAccount.md) |
| `IFeishuTenantV1HireAgency` | 7 | 猎头供应商查询、猎头账号查询与禁用/取消禁用、猎头保护期设置与查询 | [猎头供应商（租户）](./FeishuTenantV1HireAgency.md) |
| `IFeishuTenantV1HireAttachment` | 3 | 招聘附件上传、附件元信息查询、人才简历附件 PDF 下载链接 | [招聘附件（租户）](./FeishuTenantV1HireAttachment.md) |

历史单资源文档（接口名以合并后为准，仅供检索）：[发布记录](./FeishuTenantV1HireJobPublishRecord.md)、[广告](./FeishuTenantV1HireAdvertisement.md)、[职位模板](./FeishuTenantV1HireJobSchema.md)、[职能分类](./FeishuTenantV1HireJobFunction.md)、[职位类别](./FeishuTenantV1HireJobType.md)、[地址](./FeishuTenantV1HireLocation.md)、[角色](./FeishuTenantV1HireRole.md)、[用户角色](./FeishuTenantV1HireUserRole.md)

## 查询对象模式（API-2）

查询参数 ≥ 6 个的接口采用查询对象模式（`Mud.HttpUtils.IQueryParameter`），实现类位于 `Mud.Feishu.DataModels.Hire`：

| 查询对象                 | 对应接口                | 查询参数                                                                   |
| ------------------------ | ----------------------- | -------------------------------------------------------------------------- |
| `JobListQuery`           | `IFeishuTenantV1HireJob.GetJobListAsync`           | update_start_time、update_end_time、page_size、page_token、4 个 id_type |
| `JobPublishRecordSearchQuery` | `IFeishuTenantV1HireJob.SearchJobPublishRecordAsync` | page_token、page_size、4 个 id_type |
| `UserRoleListQuery`      | `IFeishuTenantV1HireSetting.GetUserRoleListAsync` | page_token、page_size、user_id、role_id、update_start_time、update_end_time、user_id_type |
| `InterviewerListQuery`   | `IFeishuTenantV1HireInterview.GetInterviewerListAsync` | 分页、面试官 user_id 列表、认证状态、更新时间范围与用户 ID 类型 |
| `JobRequirementListQuery` | `IFeishuTenantV1HireJobRequirement.GetJobRequirementListAsync` | 分页、职位 ID、创建/更新时间范围与各类 ID 类型 |
| `TalentTagListQuery`     | `IFeishuTenantV1HireSetting.GetTalentTagListAsync` | 关键词、ID 列表、标签类型、启停状态与分页 |
| `ReferralWebsiteJobPostListQuery` | `IFeishuTenantV1HireCandidate.GetReferralWebsiteJobPostListAsync` | 流程类型、分页与各类 ID 类型 |
| `WebsiteJobPostListQuery` | `IFeishuTenantV1HireCandidate.GetWebsiteJobPostListAsync` | 分页、创建/更新时间范围与各类 ID 类型 |
| `TalentListQuery`        | `IFeishuTenantV1HireCandidate.GetTalentListAsync` | 关键词、更新时间范围、分页、排序与 ID 类型 |
| `BackgroundCheckOrderListQuery` | `IFeishuTenantV1HireCandidate.GetBackgroundCheckOrderListAsync` | 分页、投递 ID、更新时间范围与用户 ID 类型 |
| `GetEmployeeByApplicationQuery` | `IFeishuTenantV1HireCandidate.GetEmployeeByApplicationAsync` | 投递 ID（必填）与各类 ID 类型 |
| `InterviewListQuery`     | `IFeishuTenantV1HireCandidate.GetInterviewListAsync` | 分页、投递/面试 ID、面试开始时间范围与各类 ID 类型 |
| `InterviewQuestionnaireListQuery` | `IFeishuTenantV1HireCandidate.GetInterviewQuestionnaireListAsync` | 分页、投递/面试 ID 与更新时间范围 |

## 命名空间与版本信息

- **根命名空间**：`Mud.Feishu`
- **DTO 命名空间**：`Mud.Feishu.DataModels.Hire`
- **目标框架**：.NET Standard 2.0 / .NET 6+ / .NET 8+
