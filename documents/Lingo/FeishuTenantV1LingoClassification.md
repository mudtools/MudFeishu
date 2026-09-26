# 词典分类 - 租户令牌（FeishuTenantV1LingoClassification）

## 接口名称

**词典分类（租户令牌）** -（`IFeishuTenantV1LingoClassification`）

## 功能描述

提供以租户身份获取飞书词典分类的能力。飞书词典（Lingo）分类入口域租户态 SDK 是一组服务端 OpenAPI 的封装，用于分页获取飞书词典的词典分类（一级/二级分类与国际化分类名）。本接口全部端点支持 tenant_access_token 调用。支持获取词典分类操作。

## 参考文档

- [飞书词典概述 - 飞书开放平台](https://open.feishu.cn/document/lingo-v1/overview)

## 函数列表

| 函数名称                   | 功能描述     | 认证方式 | HTTP 方法 |
| -------------------------- | ------------ | -------- | --------- |
| GetClassificationListAsync | 获取词典分类 | 租户令牌 | GET       |

## 函数详细内容

### 获取词典分类

分页获取飞书词典的分类列表（含二级分类名称、对应一级分类 ID 与国际化分类名）。不传 repo_id 时默认返回全员词库的分类。限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。以应用身份获取非全员词库的分类需在「词库设置」页面添加应用。

**函数签名**：

```csharp
Task<FeishuApiResult<GetClassificationListResult>?> GetClassificationListAsync(
    [Query("page_size")] int? page_size = null,
    [Query("page_token")] string? page_token = null,
    [Query("repo_id")] string? repo_id = null,
    CancellationToken cancellationToken = default);
```

**认证**：租户令牌

**参数**：

| 参数名       | 类型      | 必填 | 说明                                                           |
| ------------ | --------- | ---- | -------------------------------------------------------------- |
| `page_size`  | `int?`    | ⚪   | 分页大小，默认 20，取值范围 1 ～ 500                           |
| `page_token` | `string?` | ⚪   | 分页标记，首次请求不填，翻页时取上一次返回的 page_token         |
| `repo_id`    | `string?` | ⚪   | 词库 ID，不传默认范围为全员词库，示例值：`7202510112396640276` |

**响应**：

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "items": [
      {
        "id": "7049606926****37761",
        "name": "行业术语",
        "father_id": "7049606926****37777",
        "i18n_names": [
          {
            "language": 1,
            "name": "行业术语"
          }
        ]
      }
    ],
    "page_token": "408ecac018b2e3518db37275e812aad7bb8ad3e755fc886f322ac6c430ba",
    "has_more": true
  }
}
```

**说明**：词条只能属于二级分类，且每个一级分类下只能选择一个二级分类。当 `has_more` 为 true 时，可使用返回的 `page_token` 继续拉取下一页数据；以应用身份获取非全员词库的分类前，需先在「词库设置」页面添加该应用。
