# 用户V3部门管理 - FeishuUserV3Departments

## 接口名称
**用户V3部门管理 - (IFeishuUserV3Departments)**

## 功能描述
本接口提供飞书通讯录V3版本部门的管理功能，适用于用户应用场景。支持部门的查询、创建、更新、删除等操作。使用用户令牌访问，适合代表用户执行部门相关操作的场景。

飞书组织机构部门是指企业组织架构树上的某一个节点。在部门内部，可添加用户作为部门成员，也可添加新的部门作为子部门。

## 参考文档
- [飞书官方文档 - 部门字段概述](https://open.feishu.cn/document/server-docs/contact-v3/department/field-overview)

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP 方法 |
|---------|---------|---------|----------|
| GetDepartmentInfoByIdAsync | 获取部门信息 | 用户令牌 | GET |
| GetDepartmentsByIdsAsync | 批量获取部门 | 用户令牌 | GET |
| GetDepartmentsByParentIdAsync | 获取子部门列表 | 用户令牌 | GET |
| GetParentDepartmentsByIdAsync | 获取父部门列表 | 用户令牌 | GET |

## 函数详细内容

### 获取部门信息

**函数名称**：获取部门信息

**函数签名**：
```csharp
Task<FeishuApiResult<GetDepartmentInfoResult>?> GetDepartmentInfoByIdAsync(
    [Path] string department_id,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| user_id_type | string | ⚪ | 用户ID类型，默认 `open_id` |
| department_id_type | string | ⚪ | 部门ID类型，默认 `open_department_id` |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "department": {
      "name": "产品研发部",
      "department_id": "od-xxx",
      "parent_department_id": "0",
      "member_count": 50
    }
  }
}
```

**代码示例**：
```csharp
public class UserDepartmentV3Service
{
    private readonly IFeishuUserV3Departments _departmentClient;

    public UserDepartmentV3Service(IFeishuUserV3Departments departmentClient)
    {
        _departmentClient = departmentClient;
    }

    public async Task GetDepartmentAsync(string departmentId)
    {
        var result = await _departmentClient.GetDepartmentInfoByIdAsync(departmentId);
        if (result?.Code == 0)
        {
            Console.WriteLine($"部门名称: {result.Data?.Department?.Name}");
        }
    }
}
```

---

### 批量获取部门

**函数名称**：批量获取部门

**函数签名**：
```csharp
Task<FeishuApiResult<BatchGetDepartmentRequest>?> GetDepartmentsByIdsAsync(
    [Query("department_ids")] string[] department_ids,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_ids | string[] | ✅ | 部门ID数组 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

---

### 获取子部门列表

**函数名称**：获取子部门列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<GetDepartmentInfo>?> GetDepartmentsByParentIdAsync(
    [Path] string department_id,
    [Query("fetch_child")] bool fetch_child = false,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| fetch_child | bool | ⚪ | 是否递归获取所有子部门，默认 `false` |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：查询指定部门下的子部门列表，列表内包含部门的名称、ID、父部门、负责人以及状态等信息。

---

### 获取父部门列表

**函数名称**：获取父部门列表

**函数签名**：
```csharp
Task<FeishuApiPageListResult<GetDepartmentInfo>?> GetParentDepartmentsByIdAsync(
    [Query("department_id")] string department_id,
    [Query("page_size")] int? page_size = Consts.PageSize,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default);
```

**认证**：用户令牌

**参数**：

| 参数名 | 类型 | 必填 | 说明 |
|-------|------|------|------|
| department_id | string | ✅ | 部门ID |
| page_size | int | ⚪ | 分页大小，默认10 |
| page_token | string | ⚪ | 分页标记 |
| user_id_type | string | ⚪ | 用户ID类型 |
| department_id_type | string | ⚪ | 部门ID类型 |
| cancellationToken | CancellationToken | ⚪ | 取消操作令牌 |

**说明**：递归获取指定部门的父部门信息，包括部门名称、ID、负责人以及状态等。
