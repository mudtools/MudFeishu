# 会议报告
**IFeishuTenantV1VideoConferencingReport**

## 功能描述
会议报告用于记录一段时间内租户会议的使用情况，支持获取每日会议使用报告和 Top 用户列表。支持最近 90 天内的数据查询。

## 参考文档
- [会议报告概述](https://open.feishu.cn/document/server-docs/vc-v1/report/meeting-report-overview)

## 函数列表
| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
| :--- | :--- | :--- | :--- |
| GetDailyReportAsync | 获取会议报告 | 租户令牌 | GET |
| GetTopUserReportAsync | 获取 Top 用户列表 | 租户令牌 | GET |

## 函数详细内容

### GetDailyReportAsync
获取一段时间内组织的每日会议使用报告。支持最近 90 天内的数据查询。

**函数签名**
```csharp
Task<FeishuApiResult<GetDailyReportResult>?> GetDailyReportAsync(
    string start_time,
    string end_time,
    int? unit = null,
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| start_time | string | ✅ | 开始时间（unix时间，单位sec） | 1608888867 |
| end_time | string | ✅ | 结束时间（unix时间，单位sec） | 1608888966 |
| unit | int? | ⚪ | 数据驻留地：0=中国大陆，1=美国，2=新加坡，3=日本 | 0 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 支持最近 90 天内的数据查询
- unit 参数仅在租户存在多个驻留地数据且开通了该查询功能时使用

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "meeting_report": {
      "meeting_count": 100,
      "meeting_duration": 36000,
      "participant_count": 500
    }
  }
}
```

**代码示例**
```csharp
var result = await api.GetDailyReportAsync(
    start_time: "1608888867",
    end_time: "1608888966"
);
Console.WriteLine($"报告数据: {result?.Data}");
```

---

### GetTopUserReportAsync
获取一段时间内组织内会议使用的 Top 用户列表。支持最近 90 天内的数据查询；默认返回前 10 位，最多可查询前 100 位。

**函数签名**
```csharp
Task<FeishuApiResult<GetTopUserReportResult>?> GetTopUserReportAsync(
    string start_time,
    string end_time,
    int limit,
    int order_by,
    int? unit = null,
    string? user_id_type = "open_id",
    CancellationToken cancellationToken = default);
```

**认证**
租户令牌

**参数**
| 参数名 | 类型 | 必填 | 描述 | 示例 |
| :--- | :--- | :--- | :--- | :--- |
| start_time | string | ✅ | 开始时间（unix时间，单位sec） | 1608888867 |
| end_time | string | ✅ | 结束时间（unix时间，单位sec） | 1608888966 |
| limit | int | ✅ | 取前多少位，最多 100 | 10 |
| order_by | int | ✅ | 排序依据（降序）：1=会议数量，2=会议时长 | 1 |
| unit | int? | ⚪ | 数据驻留地：0=中国大陆，1=美国，2=新加坡，3=日本 | 0 |
| user_id_type | string | ⚪ | 用户 ID 类型：open_id / union_id / user_id | open_id |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌对象 | default |

**说明**
- 排序为降序排列
- 默认返回前 10 位，limit 最大值为 100

**响应**
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "top_user_report": [
      {
        "user_id": "ou_xxx",
        "meeting_count": 50,
        "meeting_duration": 18000
      }
    ]
  }
}
```

**代码示例**
```csharp
var result = await api.GetTopUserReportAsync(
    start_time: "1608888867",
    end_time: "1608888966",
    limit: 10,
    order_by: 1
);
Console.WriteLine($"Top 用户: {result?.Data?.TopUserReport?.Count}");
```