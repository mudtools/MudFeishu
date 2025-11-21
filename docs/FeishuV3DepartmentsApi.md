# 部门管理

## 功能描述
飞书组织机构部门是指企业组织架构树上的某一个节点。在部门内部，可添加用户作为部门成员，也可添加新的部门作为子部门。该API提供完整的部门管理功能，包括部门的创建、更新、查询、删除以及部门群管理等功能，支持层级化的组织架构管理。

## 函数列表

| 函数名称 | 功能描述 | 认证方式 | HTTP方法 |
|---------|---------|---------|---------|
| CreateDepartmentAsync | 在通讯录内创建一个部门 | tenant_access_token | POST |
| UpdatePartDepartmentAsync | 更新指定部门的部分信息 | tenant_access_token | PATCH |
| UpdateDepartmentAsync | 更新指定部门的信息 | tenant_access_token | PUT |
| UpdateDepartmentIdAsync | 更新部门的自定义ID | tenant_access_token | PATCH |
| UnbindDepartmentChatAsync | 将指定部门的部门群转为普通群 | tenant_access_token | POST |
| GetDepartmentInfoByIdAsync | 获取单个部门信息 | user_access_token | GET |
| GetDepartmentsByIdsAsync | 批量获取部门信息 | tenant_access_token | GET |
| GetDepartmentsByParentIdAsync | 查询指定部门下的子部门列表 | tenant_access_token | GET |
| GetParentDepartmentsByIdAsync | 递归获取指定部门的父部门信息 | tenant_access_token | GET |
| SearchDepartmentsAsync | 通过部门名称关键词搜索部门 | user_access_token | POST |
| DeleteDepartmentByIdAsync | 从通讯录中删除指定的部门 | tenant_access_token | DELETE |


## CreateDepartmentAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- departmentCreateRequest (必填) - 创建部门的请求体
- user_id_type (可选) - 用户ID类型，默认为"user_id"
- department_id_type (可选) - 部门ID类型，默认为"department_id"
- client_token (可选) - 幂等性判断token

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "department": {
      "name": "技术部",
      "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "open_department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "parent_department_id": "0",
      "leader_user_id": "ou_123456789",
      "member_count": 10,
      "status": { "is_deleted": false }
    }
  }
}

错误响应：
{
  "code": 400,
  "msg": "部门名称已存在"
}
```

**说明**：
- 部门名称不可包含斜杠（/）
- 根部门的父部门ID为"0"
- 支持创建部门群聊
- HRBP列表和负责人信息可同时设置

**代码示例**：
```csharp
// 创建新部门示例
var createRequest = new DepartmentCreateRequest
{
    Name = "技术研发部",
    ParentDepartmentId = "0", // 根部门
    LeaderUserId = "ou_123456789",
    Order = "1",
    CreateGroupChat = true,
    DepartmentId = "tech_dept_001",
    Leaders = new List<DepartmentLeader>
    {
        new DepartmentLeader { UserId = "ou_123456789", Type = 1 }
    },
    DepartmentHrbps = new List<string> { "ou_hrbp_001" }
};

var result = await _feishuApi.CreateDepartmentAsync(
    tenantAccessToken,
    createRequest,
    user_id_type: "user_id",
    department_id_type: "department_id",
    client_token: Guid.NewGuid().ToString());

if (result.Success)
{
    var department = result.Data.Department;
    Console.WriteLine($"部门创建成功：{department.Name} (ID: {department.DepartmentId})");
}
else
{
    Console.WriteLine($"创建失败：{result.Message}");
}
```

---

## UpdatePartDepartmentAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- department_id (必填) - 部门ID
- departmentCreateRequest (必填) - 部分更新请求体
- user_id_type (可选) - 用户ID类型
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "department": {
      "name": "更新后的技术部",
      "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "leader_user_id": "ou_new_leader"
    }
  }
}
```

**说明**：
- 只更新传入的字段，其他字段保持不变
- 支持更新名称、主管、排序等信息

**代码示例**：
```csharp
// 部分更新部门信息
var updateRequest = new DepartmentPartUpdateRequest
{
    Name = "技术研发部（更新）",
    LeaderUserId = "ou_new_leader_123",
    Order = "2"
};

var result = await _feishuApi.UpdatePartDepartmentAsync(
    tenantAccessToken,
    "od-4e6789c92a3c8e02dbe89d3f9b87c",
    updateRequest);

if (result.Success)
{
    Console.WriteLine("部门信息更新成功");
}
```

---

## UpdateDepartmentAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- department_id (必填) - 部门ID
- departmentCreateRequest (必填) - 完整更新请求体
- user_id_type (可选) - 用户ID类型
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "department": {
      "name": "完整更新的技术部",
      "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c"
    }
  }
}
```

**说明**：
- 需要提供完整的部门信息
- 未提供的字段会被清空

**代码示例**：
```csharp
// 完整更新部门信息
var fullUpdateRequest = new DepartmentUpdateRequest
{
    Name = "完整技术研发部",
    ParentDepartmentId = "od_parent_001",
    LeaderUserId = "ou_full_leader",
    Order = "1",
    CreateGroupChat = false,
    Leaders = new List<DepartmentLeader>
    {
        new DepartmentLeader { UserId = "ou_full_leader", Type = 1 }
    }
};

var result = await _feishuApi.UpdateDepartmentAsync(
    tenantAccessToken,
    "od-4e6789c92a3c8e02dbe89d3f9b87c",
    fullUpdateRequest);
```

---

## UpdateDepartmentIdAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- department_id (必填) - 原部门ID
- departMentUpdateIdRequest (必填) - 更新ID请求体
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 用于更新部门的自定义ID
- 新ID必须唯一

**代码示例**：
```csharp
// 更新部门自定义ID
var updateIdRequest = new DepartMentUpdateIdRequest
{
    NewDepartmentId = "new_tech_dept_2024"
};

var result = await _feishuApi.UpdateDepartmentIdAsync(
    tenantAccessToken,
    "od-4e6789c92a3c8e02dbe89d3f9b87c",
    updateIdRequest);

if (result.Success)
{
    Console.WriteLine("部门ID更新成功");
}
```

---

## UnbindDepartmentChatAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- departmentRequest (必填) - 部门请求体
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "msg": "success"
}
```

**说明**：
- 将部门群转换为普通群
- 转换后群聊不再与部门关联

**代码示例**：
```csharp
// 解绑部门群聊
var deptRequest = new DepartmentRequest
{
    DepartmentId = "od-4e6789c92a3c8e02dbe89d3f9b87c"
};

var result = await _feishuApi.UnbindDepartmentChatAsync(
    tenantAccessToken,
    deptRequest);

if (result.Success)
{
    Console.WriteLine("部门群已解绑");
}
```

---

## GetDepartmentInfoByIdAsync
**认证**：user_access_token  
**参数**：
- user_access_token (必填) - 用户访问令牌
- department_id (必填) - 部门ID
- department_id_type (可选) - 部门ID类型
- user_id_type (可选) - 用户ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "department": {
      "name": "技术部",
      "department_id": "od-4e6789c92a3c8e02dbe89d3f9b87c",
      "parent_department_id": "0",
      "leader_user_id": "ou_123456789",
      "member_count": 10,
      "primary_member_count": 8,
      "department_hrbps": ["ou_hrbp_001"],
      "group_chat_employee_types": [1, 2]
    }
  }
}
```

**说明**：
- 获取部门的完整信息
- 包含成员数量、HRBP等详细信息

**代码示例**：
```csharp
// 获取部门详细信息
var result = await _feishuApi.GetDepartmentInfoByIdAsync(
    userAccessToken,
    "od-4e6789c92a3c8e02dbe89d3f9b87c");

if (result.Success)
{
    var dept = result.Data.Department;
    Console.WriteLine($"部门：{dept.Name}");
    Console.WriteLine($"成员数：{dept.MemberCount}");
    Console.WriteLine($"主要负责人：{dept.LeaderUserId}");
    
    foreach (var hrbp in dept.DepartmentHrbps)
    {
        Console.WriteLine($"HRBP：{hrbp}");
    }
}
```

---

## GetDepartmentsByIdsAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- department_ids (必填) - 部门ID数组
- user_id_type (可选) - 用户ID类型
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "items": [
      {
        "name": "技术部",
        "department_id": "od-tech001",
        "member_count": 15
      },
      {
        "name": "产品部", 
        "department_id": "od-prod001",
        "member_count": 8
      }
    ]
  }
}
```

**说明**：
- 一次获取多个部门的信息
- 最多支持50个部门ID

**代码示例**：
```csharp
// 批量获取部门信息
var departmentIds = new[] { "od-tech001", "od-prod001", "od-sales001" };

var result = await _feishuApi.GetDepartmentsByIdsAsync(
    tenantAccessToken,
    departmentIds);

if (result.Success)
{
    foreach (var dept in result.Data.Items)
    {
        Console.WriteLine($"{dept.Name} - {dept.MemberCount}人");
    }
}
```

---

## GetDepartmentsByParentIdAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- department_id (必填) - 父部门ID
- fetch_child (可选) - 是否递归获取子部门，默认false
- page_size (可选) - 分页大小，默认10
- page_token (可选) - 分页标记
- user_id_type (可选) - 用户ID类型
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "items": [
      {
        "name": "前端组",
        "department_id": "od-frontend",
        "parent_department_id": "od-tech001"
      },
      {
        "name": "后端组",
        "department_id": "od-backend", 
        "parent_department_id": "od-tech001"
      }
    ],
    "page_token": "next_page_token"
  }
}
```

**说明**：
- 获取指定部门下的子部门列表
- 支持递归获取所有层级的子部门

**代码示例**：
```csharp
// 获取子部门列表（支持分页）
var pageSize = 20;
var pageToken = "";

do
{
    var result = await _feishuApi.GetDepartmentsByParentIdAsync(
        tenantAccessToken,
        "od-tech001",
        fetch_child: false,
        page_size: pageSize,
        page_token: string.IsNullOrEmpty(pageToken) ? null : pageToken);

    if (result.Success)
    {
        foreach (var dept in result.Data.Items)
        {
            Console.WriteLine($"{dept.Name} ({dept.DepartmentId})");
        }

        pageToken = result.Data.PageToken;
    }
} while (!string.IsNullOrEmpty(pageToken));
```

---

## GetParentDepartmentsByIdAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- department_id (必填) - 部门ID
- page_size (可选) - 分页大小，默认10
- page_token (可选) - 分页标记
- user_id_type (可选) - 用户ID类型
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "items": [
      {
        "name": "技术部",
        "department_id": "od-tech001",
        "parent_department_id": "0"
      },
      {
        "name": "公司总部",
        "department_id": "0",
        "parent_department_id": ""
      }
    ]
  }
}
```

**说明**：
- 递归获取所有父级部门信息
- 从直接父部门到根部门的完整路径

**代码示例**：
```csharp
// 获取部门层级路径
var result = await _feishuApi.GetParentDepartmentsByIdAsync(
    tenantAccessToken,
    "od-frontend-team");

if (result.Success)
{
    Console.WriteLine("部门层级路径：");
    var path = new List<string>();
    
    foreach (var dept in result.Data.Items)
    {
        path.Add(dept.Name);
    }
    
    path.Reverse(); // 从根到子
    Console.WriteLine(string.Join(" > ", path));
}
```

---

## SearchDepartmentsAsync
**认证**：user_access_token  
**参数**：
- user_access_token (必填) - 用户访问令牌
- searchRequest (必填) - 搜索请求体
- page_size (可选) - 分页大小，默认10
- page_token (可选) - 分页标记
- user_id_type (可选) - 用户ID类型
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "data": {
    "items": [
      {
        "name": "技术研发部",
        "department_id": "od-tech001",
        "parent_department_id": "0"
      }
    ],
    "page_token": "next_page_token"
  }
}
```

**说明**：
- 通过部门名称关键词搜索
- 只支持搜索可见的部门
- 不支持搜索国际化名称

**代码示例**：
```csharp
// 搜索部门
var searchRequest = new SearchRequest
{
    Query = "技术"
};

var result = await _feishuApi.SearchDepartmentsAsync(
    userAccessToken,
    searchRequest,
    page_size: 20);

if (result.Success)
{
    Console.WriteLine($"找到 {result.Data.Items.Count} 个相关部门：");
    foreach (var dept in result.Data.Items)
    {
        Console.WriteLine($"- {dept.Name} ({dept.DepartmentId})");
    }
}
```

---

## DeleteDepartmentByIdAsync
**认证**：tenant_access_token  
**参数**：
- tenant_access_token (必填) - 应用访问令牌
- department_id (必填) - 部门ID
- department_id_type (可选) - 部门ID类型

**响应**：
```json
成功响应：
{
  "code": 0,
  "msg": "success"
}

错误响应：
{
  "code": 400,
  "msg": "部门下还有成员，无法删除"
}
```

**说明**：
- 删除部门前需确保无子部门和无成员
- 删除操作不可恢复

**代码示例**：
```csharp
// 删除部门（先检查是否可以删除）
var checkResult = await _feishuApi.GetDepartmentsByParentIdAsync(
    tenantAccessToken,
    "od-dept-to-delete");

if (checkResult.Success && checkResult.Data.Items.Any())
{
    Console.WriteLine("部门下还有子部门，无法删除");
    return;
}

var deleteResult = await _feishuApi.DeleteDepartmentByIdAsync(
    tenantAccessToken,
    "od-dept-to-delete");

if (deleteResult.Success)
{
    Console.WriteLine("部门删除成功");
}
else
{
    Console.WriteLine($"删除失败：{deleteResult.Message}");
}
```

---

## 版本记录

| 版本 | 日期 | 说明 | 作者 |
|-----|-----|-----|-----|
| v1.0.0 | 2025-11-20 | 初始版本，包含所有部门管理API | Mud Studio |
