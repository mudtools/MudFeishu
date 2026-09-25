# 招聘广告（FeishuTenantV1HireAdvertisement）

## 接口名称

**招聘广告** -（`IFeishuTenantV1HireAdvertisement`）

## 功能描述

以租户身份发布职位广告至指定招聘官网渠道（如官网、内推平台），完成职位在招聘门户的上架展示。

## 参考文档

- [发布招聘官网广告](https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/publish)

## 函数列表

| 函数名称                  | 功能描述         | 限频     | 所需权限          | HTTP 方法 |
| ------------------------- | ---------------- | -------- | ----------------- | --------- |
| PublishAdvertisementAsync | 发布招聘官网广告 | 10 次/秒 | hire:advertisement | POST      |

## 函数详细内容

### PublishAdvertisementAsync — 发布招聘官网广告

`POST /open-apis/hire/v1/advertisements/{advertisement_id}/publish`

```csharp
Task<FeishuNullDataApiResult?> PublishAdvertisementAsync(
    [Path] string advertisement_id,
    [Body] PublishAdvertisementRequest request,
    CancellationToken cancellationToken = default);
```

请求体 `PublishAdvertisementRequest`：`job_channel_id`（招聘渠道 ID；发布内推平台可传 "3"，官网渠道 ID 可通过获取招聘官网列表获取），单次仅可发布 1 个渠道。响应体为空，返回 `FeishuNullDataApiResult`。
