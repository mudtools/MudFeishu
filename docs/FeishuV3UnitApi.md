# 单位管理

## 接口名称：IFeishuV3UnitApi

## 功能描述
通讯录的单位用于代表企业中的"子公司"、"分支机构"这类组织实体。例如，你的企业下存在负责不同业务的两家子公司，那么你可以在同一个租户内，为两家子公司分别创建对应的单位资源。目前单位资源的主要作用是在部分用户权限上实现"子公司"级别的权限隔离。

接口详细文档请参见：[飞书开放平台文档](https://open.feishu.cn/document/server-docs/contact-v3/unit/overview)

## 函数列表

| 序号 | 函数名称 | HTTP方法 | 功能描述 |
|------|----------|----------|----------|
| 1 | CreateUnitAsync | POST | 创建一个单位 |
| 2 | UpdateUnitAsync | PATCH | 修改指定单位的名字 |
| 3 | BindDepartmentAsync | POST | 建立部门与单位的绑定关系 |
| 4 | UnBindDepartmentAsync | POST | 解除部门与单位的绑定关系 |
| 5 | GetDepartmentListAsync | GET | 获取单位绑定的部门列表 |
| 6 | GetUnitInfoAsync | GET | 获取指定单位的信息 |
| 7 | GetUnitListAsync | GET | 获取当前租户内的单位列表 |
| 8 | DeleteUnitByIdAsync | DELETE | 删除指定单位 |

## 函数详细内容

### 1. 创建单位

- **函数名称**：
```csharp
Task<FeishuApiResult<UnitCreateResult>> CreateUnitAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Body] UnitInfoRequest groupInfoRequest,
    CancellationToken cancellationToken = default)
```

- **认证**：需要tenant_access_token（应用身份凭证）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 以应用身份调用API，可读写的数据范围由应用自身的数据权限范围决定 |
  | groupInfoRequest | UnitInfoRequest | 是 | 单位信息请求体，包含单位名称等基本信息 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌对象 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "unit": {
      "unit_id": "ou_123456789",
      "name": "子公司A",
      "type": "branch"
    }
  }
}
```

- **说明**：
  - 创建成功后返回单位ID、名称和类型信息
  - 单位类型通常包括子公司、分支机构等

- **代码示例**：
```csharp
var unitInfo = new UnitInfoRequest
{
    Name = "子公司A",
    Type = "branch"
};

var result = await _feishuV3UnitApi.CreateUnitAsync(tenantToken, unitInfo);
if (result.Success)
{
    Console.WriteLine($"单位创建成功，ID: {result.Data.Unit.UnitId}");
}
```

### 2. 修改单位名称

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> UpdateUnitAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Path] string unit_id,
    [Body] UnitNameUpdateRequest nameUpdateRequest,
    CancellationToken cancellationToken = default)
```

- **认证**：需要tenant_access_token（应用身份凭证）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 以应用身份调用API，可读写的数据范围由应用自身的数据权限范围决定 |
  | unit_id | string | 是 | 需要修改的单位ID |
  | nameUpdateRequest | UnitNameUpdateRequest | 是 | 单位名称更新请求体 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌对象 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 只能修改单位名称，不能修改单位类型
  - 单位ID在URL路径中传递

- **代码示例**：
```csharp
var updateRequest = new UnitNameUpdateRequest
{
    Name = "更新后的子公司名称"
};

var result = await _feishuV3UnitApi.UpdateUnitAsync(tenantToken, "ou_123456789", updateRequest);
if (result.Success)
{
    Console.WriteLine("单位名称修改成功");
}
```

### 3. 绑定部门到单位

- **函数名称**：
```csharp
Task<FeishuApiResult<UnitCreateResult>> BindDepartmentAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Body] UnitBindDepartmentRequest unitBindDepartment,
    CancellationToken cancellationToken = default)
```

- **认证**：需要tenant_access_token（应用身份凭证）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 以应用身份调用API，可读写的数据范围由应用自身的数据权限范围决定 |
  | unitBindDepartment | UnitBindDepartmentRequest | 是 | 部门与单位的绑定关系请求体 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌对象 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "unit": {
      "unit_id": "ou_123456789",
      "name": "子公司A",
      "type": "branch"
    }
  }
}
```

- **说明**：
  - 单个单位可关联的部门数量上限为1,000
  - 同一个部门只能关联一个单位

- **代码示例**：
```csharp
var bindRequest = new UnitBindDepartmentRequest
{
    UnitId = "ou_123456789",
    DepartmentId = "odn_987654321"
};

var result = await _feishuV3UnitApi.BindDepartmentAsync(tenantToken, bindRequest);
if (result.Success)
{
    Console.WriteLine("部门绑定成功");
}
```

### 4. 解除部门与单位绑定

- **函数名称**：
```csharp
Task<FeishuApiResult<UnitCreateResult>> UnBindDepartmentAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Body] UnitBindDepartmentRequest unitBindDepartment,
    CancellationToken cancellationToken = default)
```

- **认证**：需要tenant_access_token（应用身份凭证）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 以应用身份调用API，可读写的数据范围由应用自身的数据权限范围决定 |
  | unitBindDepartment | UnitBindDepartmentRequest | 是 | 部门与单位的绑定关系请求体 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌对象 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "unit": {
      "unit_id": "ou_123456789",
      "name": "子公司A",
      "type": "branch"
    }
  }
}
```

- **说明**：
  - 解除绑定后，部门将不再归属于该单位
  - 需要提供单位和部门的ID

- **代码示例**：
```csharp
var unbindRequest = new UnitBindDepartmentRequest
{
    UnitId = "ou_123456789",
    DepartmentId = "odn_987654321"
};

var result = await _feishuV3UnitApi.UnBindDepartmentAsync(tenantToken, unbindRequest);
if (result.Success)
{
    Console.WriteLine("部门解绑成功");
}
```

### 5. 获取单位绑定的部门列表

- **函数名称**：
```csharp
Task<FeishuApiResult<UnitDepartmentListResult>> GetDepartmentListAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Query("unit_id")] string unit_id,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
    CancellationToken cancellationToken = default)
```

- **认证**：需要tenant_access_token（应用身份凭证）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 以应用身份调用API，可读写的数据范围由应用自身的数据权限范围决定 |
  | unit_id | string | 是 | 单位ID |
  | page_size | int | 否 | 分页大小，默认10 |
  | page_token | string? | 否 | 分页标记，第一次请求不填 |
  | department_id_type | string? | 否 | 部门ID类型，默认使用系统常量 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌对象 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_more": true,
    "page_token": "next_page_token",
    "department_list": [
      {
        "department_id": "odn_987654321",
        "name": "技术部",
        "parent_department_id": "odn_123456789"
      }
    ]
  }
}
```

- **说明**：
  - 支持分页查询
  - 返回部门的基本信息，包括ID、名称和父部门ID

- **代码示例**：
```csharp
var result = await _feishuV3UnitApi.GetDepartmentListAsync(
    tenantToken, 
    "ou_123456789", 
    page_size: 50
);

if (result.Success)
{
    foreach (var dept in result.Data.DepartmentList)
    {
        Console.WriteLine($"部门: {dept.Name} (ID: {dept.DepartmentId})");
    }
}
```

### 6. 获取单位信息

- **函数名称**：
```csharp
Task<FeishuApiResult<UnitInfo>> GetUnitInfoAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Path] string unit_id,
    CancellationToken cancellationToken = default)
```

- **认证**：需要tenant_access_token（应用身份凭证）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 以应用身份调用API，可读写的数据范围由应用自身的数据权限范围决定 |
  | unit_id | string | 是 | 单位ID |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌对象 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "unit": {
      "unit_id": "ou_123456789",
      "name": "子公司A",
      "type": "branch",
      "created_at": "2023-01-01T00:00:00Z",
      "updated_at": "2023-01-02T00:00:00Z"
    }
  }
}
```

- **说明**：
  - 返回单位的详细信息，包括创建和更新时间
  - 单位ID在URL路径中传递

- **代码示例**：
```csharp
var result = await _feishuV3UnitApi.GetUnitInfoAsync(tenantToken, "ou_123456789");
if (result.Success)
{
    var unit = result.Data.Unit;
    Console.WriteLine($"单位名称: {unit.Name}, 类型: {unit.Type}");
}
```

### 7. 获取单位列表

- **函数名称**：
```csharp
Task<FeishuApiResult<UnitListDataResult>> GetUnitListAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    CancellationToken cancellationToken = default)
```

- **认证**：需要tenant_access_token（应用身份凭证）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 以应用身份调用API，可读写的数据范围由应用自身的数据权限范围决定 |
  | page_size | int | 否 | 分页大小，默认10 |
  | page_token | string? | 否 | 分页标记，第一次请求不填 |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌对象 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success",
  "data": {
    "has_more": true,
    "page_token": "next_page_token",
    "unit_list": [
      {
        "unit_id": "ou_123456789",
        "name": "子公司A",
        "type": "branch"
      },
      {
        "unit_id": "ou_987654321",
        "name": "分支机构B",
        "type": "branch"
      }
    ]
  }
}
```

- **说明**：
  - 返回当前租户内所有单位的列表
  - 支持分页查询，适合大量单位数据

- **代码示例**：
```csharp
var result = await _feishuV3UnitApi.GetUnitListAsync(tenantToken, page_size: 100);
if (result.Success)
{
    foreach (var unit in result.Data.UnitList)
    {
        Console.WriteLine($"单位: {unit.Name} (ID: {unit.UnitId}, 类型: {unit.Type})");
    }
}
```

### 8. 删除单位

- **函数名称**：
```csharp
Task<FeishuNullDataApiResult> DeleteUnitByIdAsync(
    [Token][Header("Authorization")] string tenant_access_token,
    [Path] string unit_id,
    CancellationToken cancellationToken = default)
```

- **认证**：需要tenant_access_token（应用身份凭证）
- **参数**：
  | 参数名 | 类型 | 必填 | 说明 |
  |--------|------|------|------|
  | tenant_access_token | string | 是 | 应用调用API时，通过访问凭证进行身份鉴权 |
  | unit_id | string | 是 | 需删除的单位ID |
  | cancellationToken | CancellationToken | 否 | 取消操作令牌对象 |

- **响应**：
```json
{
  "code": 0,
  "msg": "success"
}
```

- **说明**：
  - 删除操作不可逆，请谨慎操作
  - 删除前请确保该单位下没有重要的绑定关系
  - 单位ID在URL路径中传递

- **代码示例**：
```csharp
var result = await _feishuV3UnitApi.DeleteUnitByIdAsync(tenantToken, "ou_123456789");
if (result.Success)
{
    Console.WriteLine("单位删除成功");
}
```

## 版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| v1.0.0 | 2025-11-20 | 初始版本，包含完整的单位管理API接口文档 |
