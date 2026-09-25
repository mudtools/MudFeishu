# 职位发布记录（FeishuTenantV1HireJobPublishRecord）

## 接口名称

**职位发布记录** -（`IFeishuTenantV1HireJobPublishRecord`）

## 功能描述

以租户身份按招聘渠道搜索职位发布记录（职位广告），返回官网/三方/内推等渠道下的职位广告列表及职位状态、薪资、地址、自定义字段等聚合信息。

## 参考文档

- [搜索职位发布记录](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/search)

## 函数列表

| 函数名称                    | 功能描述         | 限频                    | 所需权限                                       | HTTP 方法 |
| --------------------------- | ---------------- | ----------------------- | ---------------------------------------------- | --------- |
| SearchJobPublishRecordAsync | 搜索职位发布记录 | 1000 次/分、50 次/秒    | hire:job:readonly 或 hire:job（字段权限 contact:user.employee_id:readonly） | POST |

## 函数详细内容

### SearchJobPublishRecordAsync — 搜索职位发布记录（查询对象模式）

`POST /open-apis/hire/v1/job_publish_records/search`

```csharp
Task<FeishuApiResult<SearchJobPublishRecordResult>?> SearchJobPublishRecordAsync(
    [Body] SearchJobPublishRecordRequest request,
    [Query] JobPublishRecordSearchQuery? query = null,
    CancellationToken cancellationToken = default);
```

请求体 `SearchJobPublishRecordRequest`：`job_channel_id`（招聘渠道 ID；官网 ID 可通过获取招聘官网列表获取，三方渠道 ID 见枚举常量，猎头渠道 "2"，内推渠道 "3"）。

`JobPublishRecordSearchQuery`（实现 `IQueryParameter`）：

| 字段             | 查询参数名        | 类型   | 必填 | 说明                 |
| ---------------- | ----------------- | ------ | ---- | -------------------- |
| PageToken        | page_token        | string | 否   | 分页标记             |
| PageSize         | page_size         | int    | 否   | 分页大小             |
| UserIdType       | user_id_type      | string | 否   | 用户 ID 类型         |
| DepartmentIdType | department_id_type| string | 否   | 部门 ID 类型         |
| JobLevelIdType   | job_level_id_type | string | 否   | 职级 ID 类型         |
| JobFamilyIdType  | job_family_id_type| string | 否   | 序列 ID 类型         |

响应 `data.items` 为 `WebsiteJobPost[]`（职位广告 ID、职位 ID/编码、过期时间、状态、部门/职级/序列、薪资、亮点、地址列表、自定义字段、目标专业等）。
