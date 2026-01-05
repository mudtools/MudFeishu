# Mud.Feishu 更新日志

## 1.1.0 (2025-12-31)

**FEATURES**

### 🔧 核心优化与重构

- 📦 **多框架支持**: 支持 .NET Standard 2.0、.NET 6.0、.NET 8.0、.NET 10.0
  - 提供跨平台兼容性，支持从 .NET Framework 4.6+ 到 .NET 10.0
  - 统一 API 接口，不同框架版本使用相同的编程模型
  - 自动编译时条件处理，充分利用各平台特性

- 🏗️ **响应类型统一**: 更新所有 API 响应类型为 `FeishuApiResult<T>` 系列
  - `FeishuApiResult<T>` - 通用响应类型
  - `FeishuApiPageListResult<T>` - 分页列表响应
  - `FeishuApiListResult<T>` - 列表响应
  - `FeishuNullDataApiResult` - 空数据响应

- 🔧 **消息发送接口重构**: 统一消息发送接口设计
  - `SendMessageRequest` 替代 `TextMessageRequest`，支持所有消息类型
  - `MessageTextContent` 替代 `TextContent`，保持类型一致性
  - 改进 Content 字段序列化机制

### 📋 新增审批 API 支持

- ✅ **审批定义管理**
  - `IFeishuV4Approval` - V4 审批基础接口
  - `IFeishuTenantV4Approval` - V4 租户审批接口
  - 支持创建审批定义、查询审批实例等核心功能

### 📝 任务管理增强

- 🎯 **自定义字段管理**
  - 创建、更新、查询自定义字段
  - 自定义字段选项管理
  - 自定义字段资源绑定
  - 分页查询自定义字段列表

- 📊 **任务分组管理**
  - 创建、更新、删除任务分组
  - 查询任务分组列表
  - 任务分组资源绑定

### 🔄 WebSocket 实时事件订阅

- 🌐 **飞书 WebSocket 客户端** (`Mud.Feishu.WebSocket`)
  - 支持飞书 WebSocket 实时事件订阅
  - 自动重连机制，保证连接稳定性
  - 心跳检测，及时发现连接异常
  - 二进制消息解析，支持完整事件类型

### 📡 Webhook 事件处理

- 🎭 **事件处理器抽象层** (`Mud.Feishu.Abstractions`)
  - 策略模式架构，灵活扩展事件处理器
  - 工厂模式管理，自动发现和注册处理器
  - 完整的飞书事件类型覆盖：
    - 用户事件：创建、更新、离职、状态变更
    - 部门事件：创建、更新、删除
    - 员工事件：入职、离职、信息变更
    - 消息事件：接收、发送状态、阅读状态
    - 任务事件：创建、更新、删除、状态变更
    - 审批事件：提交、通过、拒绝、撤销

### 🎨 消息和卡片增强

- 📰 **消息流卡片 API**
  - 应用消息流卡片完整接口支持
  - 卡片实体组件管理
  - 卡片内容更新和删除

- 💬 **群组功能增强**
  - 群公告管理
  - 会话标签页管理
  - 群组自定义菜单设置

### 🛠️ 配置和工具优化

- ⚙️ **配置增强**
  - 添加 `FeishuOptions` 配置类
  - 支持配置文件绑定
  - 日志配置选项

- 🔒 **安全增强**
  - URL 验证功能，防止恶意请求
  - Authorization 头常量统一管理

**REFACTOR**

- 📁 **命名空间统一**
  - 统一接口命名空间结构
  - 全局引用导入，简化代码

- 🧹 **代码清理**
  - 移除废弃的 TaskSectionsResult 类
  - 移除无效的类和接口
  - 统一服务注册 API，移除废弃的 `UseMultiHandler` 方法

**BUG FIX**

- 🔧 **修复 HttpClient 配置问题**
- 🔧 **修复 API 端点 URL 格式问题**
- 🔧 **修复消息和任务附件 API 实现**

**DOCS**

- 📚 **文档更新**
  - 更新 README 文档结构
  - 移除冗余的架构设计和性能特性文档
  - 添加部门事件处理器文档和使用示例
  - 优化项目描述和功能说明

---

## 1.0.2 (2025-11-26)

**FEATURES**

- 🏗️ **重构优化**: 创建 `ChatGroupBase` 基类，整合聊天群组相关通用属性
  - 减少 70+ 个重复属性，提升代码复用性
  - 统一 `GetChatGroupInfoResult`、`CreateUpdateChatResult`、`UpdateChatRequest`、`CreateChatRequest` 类结构
  - 保持完整的 JsonPropertyName 特性，确保 JSON 序列化兼容性

- 📚 **文档完善**: 为所有聊天群组和群组成员相关类添加完整的 XML 文档注释
  - `ChatGroupModeratorPageListResult` - 聊天群组管理员分页列表结果
  - `ChatItemInfo` - 聊天项目基本信息
  - `ShareLinkDataResult` - 分享链接数据结果
  - `AddMemberResult` - 添加成员操作结果
  - `GetMemberIsInChatResult` - 成员群组状态查询结果
  - `GetMemberPageListResult` - 群组成员分页列表结果
  - `RemoveMemberResult` - 移除成员操作结果
  - `GroupManagerResult` - 群管理员操作结果

- 🎯 **代码质量**: 提升代码可读性和维护性
  - 所有新增注释遵循 C# XML 文档规范
  - 包含详细的业务含义和使用场景说明
  - 区分不同参数值的实际效果

## 1.0.1 (2025-11-20)

**REFACTOR**

- 优化依赖注入配置结构
- 改进令牌管理器的并发安全性
- 重构 HTTP 客户端工厂配置

**FEATURES**

- 增强错误处理机制
- 添加详细的日志记录支持
- 支持自定义 HTTP 头配置

**BUG FIX**

- 修复令牌刷新时的并发问题
- 解决分页查询中的数据丢失问题
- 修复批量消息发送的状态追踪错误


### 📱 消息服务
- **多类型消息**: 文本、图片、文件、卡片等丰富消息类型
- **批量发送**: 支持批量消息发送和状态追踪
- **消息互动**: 表情回复、消息撤回、已读回执
- **异步处理**: 完全异步的消息处理机制

---

## 1.0.0 (2025-11-01)

### 🎉 首次发布 - Mud.Feishu 飞书 API SDK

**FEATURES**

### 🔐 认证授权系统
- **多重令牌管理**: 支持应用令牌、租户令牌、用户令牌
- **自动刷新机制**: 智能令牌刷新，提前 5 分钟触发更新
- **高并发安全**: 使用 `ConcurrentDictionary` 和 `Lazy<Task>` 避免缓存击穿
- **OAuth 授权流程**: 完整支持飞书 OAuth 2.0 授权


### 🏢 组织架构管理
#### 用户管理 (V1/V3)
- **用户 CRUD**: 创建、查询、更新、删除用户
- **批量操作**: 批量获取用户信息、批量状态更新
- **部门关联**: 用户与部门的多对多关系管理
- **搜索过滤**: 支持多种搜索条件和分页

#### 部门管理 (V1/V3)
- **树形结构**: 支持无限层级的部门树
- **递归查询**: 递归获取子部门和成员
- **权限继承**: 部门权限自动继承机制

#### 员工管理 (V1)
- **员工信息**: 员工详细信息管理
- **入职离职**: 员工入职和离职流程支持

#### 用户组管理 (V3)
- **用户组 CRUD**: 创建、查询、更新、删除用户组
- **成员管理**: 用户组成员的添加、移除、查询
- **权限分配**: 基于用户组的权限控制

### 🏢 企业管理体系
#### 人员类型管理 (V3)
- **分类体系**: 员工类型分类和标签管理
- **灵活配置**: 支持自定义人员类型属性

#### 职级管理 (V3)
- **职级体系**: 完整的职级晋升和管理
- **职级关联**: 与薪资、权限的关联配置

#### 职位序列管理 (V3)
- **职业路径**: 员工职业发展路径管理
- **序列定义**: 不同序列的职位定义

#### 职务管理 (V3)
- **职务定义**: 具体职务的职责和权限定义
- **职务分配**: 员工职务的分配和变更

#### 角色管理 (V3)
- **权限角色**: 基于角色的访问控制 (RBAC)
- **角色继承**: 角色权限的继承和组合
- **成员管理**: 角色成员的添加、移除操作

#### 单位管理 (V3)
- **组织单位**: 企业组织单位的管理
- **单位层级**: 单位之间的层级关系

#### 工作城市管理 (V3)
- **办公地点**: 工作城市和地点管理
- **地点关联**: 与部门、员工的关联关系

### 🔧 核心技术特性

#### 特性驱动设计
- **[HttpClientApi] 特性**: 自动生成 HTTP 客户端代码
- **强类型支持**: 编译时类型检查，减少运行时错误
- **统一响应**: 基于 `FeishuApiResult<T>` 的统一响应处理

#### 依赖注入友好
- **服务注册**: `AddFeishuApiService()` 扩展方法
- **配置灵活**: 支持配置文件和代码配置
- **生命周期管理**: 自动管理服务生命周期

#### 高性能缓存
- **智能缓存**: 令牌自动缓存和刷新
- **并发控制**: 解决高并发下的缓存问题
- **资源管理**: 实现 `IDisposable` 接口

#### 异常处理
- **统一异常**: `FeishuException` 统一异常处理
- **错误分类**: 不同类型错误的分类处理
- **日志集成**: 与 .NET 日志系统集成

### 🌐 API 覆盖范围

#### 认证授权 API
- `IFeishuV3AuthenticationApi` - V3 认证授权接口

#### 消息服务 API
- `IFeishuV1Message` - V1 消息基础接口
- `IFeishuTenantV1Message` - V1 租户消息接口
- `IFeishuUserV1Message` - V1 用户消息接口
- `IFeishuTenantV1BatchMessage` - V1 批量消息接口

#### 组织架构 API (V1)
- `IFeishuV1ChatGroup` - V1 聊天群组基础接口
- `IFeishuTenantV1ChatGroup` - V1 租户聊天群组接口
- `IFeishuUserV1ChatGroup` - V1 用户聊天群组接口
- `IFeishuV1ChatGroupMember` - V1 聊天群组成员基础接口
- `IFeishuTenantV1ChatGroupMember` - V1 租户聊天群组成员接口
- `IFeishuUserV1ChatGroupMember` - V1 用户聊天群组成员接口
- `IFeishuV1Departments` - V1 部门管理基础接口
- `IFeishuTenantV1Departments` - V1 租户部门管理接口
- `IFeishuUserV1Departments` - V1 用户部门管理接口
- `IFeishuV1Employees` - V1 员工管理基础接口
- `IFeishuTenantV1Employees` - V1 租户员工管理接口
- `IFeishuUserV1Employees` - V1 用户员工管理接口

#### 企业管理 API (V3)
- `IFeishuV3Departments` - V3 部门管理基础接口
- `IFeishuTenantV3Departments` - V3 租户部门管理接口
- `IFeishuUserV3Departments` - V3 用户部门管理接口
- `IFeishuTenantV3EmployeeType` - V3 租户人员类型管理接口
- `IFeishuTenantV3JobFamilies` - V3 租户职位序列管理接口
- `IFeishuTenantV3JobLevel` - V3 租户职级管理接口
- `IFeishuV3JobTitle` - V3 职务管理基础接口
- `IFeishuTenantV3JobTitle` - V3 租户职务管理接口
- `IFeishuUserV3JobTitle` - V3 用户职务管理接口
- `IFeishuTenantV3RoleMember` - V3 租户角色成员管理接口
- `IFeishuTenantV3Role` - V3 租户角色管理接口
- `IFeishuTenantV3Unit` - V3 租户单位管理接口
- `IFeishuV3User` - V3 用户管理基础接口
- `IFeishuTenantV3User` - V3 租户用户管理接口
- `IFeishuUserV3User` - V3 用户管理接口
- `IFeishuTenantV3UserGroupMember` - V3 租户用户组成员管理接口
- `IFeishuTenantV3UserGroup` - V3 租户用户组管理接口
- `IFeishuTenantV3WorkCity` - V3 租户工作城市管理接口
- `IFeishuV3WorkCity` - V3 工作城市基础接口

### 📦 技术栈

#### 框架支持
- **.NET Standard 2.0** - 兼容 .NET Framework 4.6.1+
- **.NET 6.0** - LTS 长期支持版本
- **.NET 8.0** - LTS 长期支持版本 
- **.NET 10.0** - LTS 长期支持版本

#### 核心依赖
- **Mud.ServiceCodeGenerator v1.4.5.3** - HTTP 客户端代码生成器
- **System.Text.Json v10.0.1** - 高性能 JSON 序列化 (.NET Standard 2.0)
- **Microsoft.Extensions.Http** - HTTP 客户端工厂
  - .NET 6.0 / .NET Standard 2.0: v8.0.1
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Http.Polly** - 弹性和瞬态故障处理
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.DependencyInjection** - 依赖注入
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Logging** - 日志记录
  - .NET 6.0 / .NET Standard 2.0: v8.0.3
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Configuration.Binder** - 配置绑定
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1

## 🔗 相关链接

- [项目Gitee主页](https://gitee.com/mudtools/MudFeishu)
- [项目Github主页](https://github.com/mudtools/MudFeishu)
- [NuGet 包](https://www.nuget.org/packages/Mud.Feishu/)
- [文档网站](https://www.mudtools.cn/documents/guides/feishu/)
- [飞书开放平台](https://open.feishu.cn/document/)
- [问题反馈](https://gitee.com/mudtools/MudFeishu/issues)

---

*注意：本 CHANGELOG 遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/) 规范。*
