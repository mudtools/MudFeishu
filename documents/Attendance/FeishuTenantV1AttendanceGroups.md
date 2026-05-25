# 考勤组管理接口（租户） - FeishuTenantV1AttendanceGroups

## 接口名称
**考勤组管理（租户令牌）** - (FeishuTenantV1AttendanceGroups)

## 功能描述
考勤组是对部门或者员工在某个特定场所及特定时间段内的出勤情况的一种规则设定，包括上下班、迟到、早退、病假、婚假、丧假、公休、工作时间、加班情况等。通过设置考勤组，可以从部门、员工两个维度，来设定考勤方式、考勤时间、考勤地点等考勤规则。

## 参考文档
- [飞书开放平台 - 考勤组管理文档](https://open.feishu.cn/document/server-docs/attendance-v1/group/create)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| CreateGroupAsync | 创建或修改考勤组 | 租户令牌 | POST |
| DeleteGroupByIdAsync | 删除考勤组 | 租户令牌 | DELETE |
| GetGroupByIdAsync | 获取考勤组详情 | 租户令牌 | GET |
| GetGroupByNameAsync | 按名称查询考勤组 | 租户令牌 | POST |
| GetGroupPageListAsync | 分页获取考勤组列表 | 租户令牌 | GET |

---

## 函数详细内容

### CreateGroupAsync - 创建或修改考勤组

创建或修改考勤组，设置考勤方式、考勤时间、考勤地点等规则。

#### 函数签名

```csharp
Task<FeishuApiResult<CreateAttendanceGroupResult>?> CreateGroupAsync(
    [Body] CreateAttendanceGroupRequest createAttendanceShiftsRequest,
    [Query("employee_type")] string employee_type = "employee_id",
    [Query("dept_type")] string dept_type = "department_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| createAttendanceShiftsRequest | CreateAttendanceGroupRequest | ✅ | 创建或修改考勤组请求体 | - |
| employee_type | string | ⚪ | 员工ID类型：employee_id/employee_no/open_id | "employee_id" |
| dept_type | string | ⚪ | 部门ID类型：department_id/open_department_id | "department_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "group_id": "6919358128597097404",
    "group_name": "研发部考勤组"
  }
}
```

#### 代码示例

```csharp
// 创建一个固定班制的考勤组
var request = new CreateAttendanceGroupRequest
{
    GroupName = "研发部考勤组",
    ShiftId = "6919358778597097404",
    Members = new[]
    {
        new GroupMember { UserId = "ou_abc123" },
        new GroupMember { UserId = "ou_def456" }
    }
};

var result = await feishuClient.CreateGroupAsync(request);
if (result?.Code == 0)
{
    Console.WriteLine($"考勤组创建成功，ID: {result.Data?.GroupId}");
}
```

---

### DeleteGroupByIdAsync - 删除考勤组

通过考勤组ID删除考勤组，对应设置-假勤设置-考勤组操作列的删除功能。

#### 函数签名

```csharp
Task<FeishuNullDataApiResult?> DeleteGroupByIdAsync(
    [Path] string group_id,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| group_id | string | ✅ | 考勤组ID | "6919358128597097404" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success"
}
```

#### 说明
- 删除后将影响该考勤组内所有员工的考勤规则
- 请谨慎操作

#### 代码示例

```csharp
// 删除指定ID的考勤组
var result = await feishuClient.DeleteGroupByIdAsync("6919358128597097404");
if (result?.Code == 0)
{
    Console.WriteLine("考勤组删除成功");
}
```

---

### GetGroupByIdAsync - 获取考勤组详情

通过考勤组ID获取考勤组详情，包含基本信息、考勤班次、考勤方式、考勤设置信息。

#### 函数签名

```csharp
Task<FeishuApiResult<AttendanceGroupsInfo>?> GetGroupByIdAsync(
    [Path] string group_id,
    [Query("employee_type")] string employee_type = "employee_id",
    [Query("dept_type")] string dept_type = "department_id",
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| group_id | string | ✅ | 考勤组ID | "6919358128597097404" |
| employee_type | string | ⚪ | 员工ID类型 | "employee_id" |
| dept_type | string | ⚪ | 部门ID类型 | "department_id" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "group_id": "6919358128597097404",
    "group_name": "研发部考勤组",
    "shift_id": "6919358778597097404",
    "members": [
      { "user_id": "ou_abc123", "name": "张三" }
    ]
  }
}
```

#### 代码示例

```csharp
// 获取考勤组详情
var result = await feishuClient.GetGroupByIdAsync("6919358128597097404");
if (result?.Code == 0)
{
    Console.WriteLine($"考勤组名称: {result.Data?.GroupName}");
    Console.WriteLine($"成员数量: {result.Data?.Members?.Count ?? 0}");
}
```

---

### GetGroupByNameAsync - 按名称查询考勤组

按考勤组名称查询考勤组摘要信息，支持名称精确匹配和模糊匹配两种方式。查询结果按考勤组修改时间desc排序，且最大记录数为10条。

#### 函数签名

```csharp
Task<FeishuApiResult<GroupsSearchResult>?> GetGroupByNameAsync(
    [Body] GroupsSearchRequest groupsSearchRequest,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| groupsSearchRequest | GroupsSearchRequest | ✅ | 按名称查询考勤组请求体 | - |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "groups": [
      {
        "group_id": "6919358128597097404",
        "group_name": "研发部考勤组"
      }
    ]
  }
}
```

#### 代码示例

```csharp
// 搜索包含"研发"的考勤组
var request = new GroupsSearchRequest
{
    GroupName = "研发",
    FuzzyMatch = true
};

var result = await feishuClient.GetGroupByNameAsync(request);
if (result?.Code == 0)
{
    foreach (var group in result.Data?.Groups ?? [])
    {
        Console.WriteLine($"考勤组: {group.GroupName}");
    }
}
```

---

### GetGroupPageListAsync - 分页获取考勤组列表

分页获取所有考勤组列表，列表中的数据为考勤组信息，字段包含考勤组名称和考勤组id。

#### 函数签名

```csharp
Task<FeishuApiResult<AttendanceGroupsPageResult>?> GetGroupPageListAsync(
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default);
```

#### 认证
**租户令牌** (TenantAccessToken)

#### 参数

| 参数名 | 类型 | 必填 | 说明 | 示例值 |
|-------|------|-----|------|--------|
| page_size | int | ⚪ | 分页大小，默认10 | 10 |
| page_token | string | ⚪ | 分页标记，第一次请求不填 | "xxx" |

#### 响应

**成功响应示例：**

```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "groups": [
      { "group_id": "6919358128597097404", "group_name": "研发部考勤组" }
    ],
    "page_token": "next_page_token",
    "has_more": true
  }
}
```

#### 代码示例

```csharp
// 分页获取所有考勤组
string? pageToken = null;
do
{
    var result = await feishuClient.GetGroupPageListAsync(10, pageToken);
    if (result?.Code == 0)
    {
        foreach (var group in result.Data?.Groups ?? [])
        {
            Console.WriteLine($"考勤组: {group.GroupName}");
        }
        pageToken = result.Data?.PageToken;
    }
} while (!string.IsNullOrEmpty(pageToken));
```
