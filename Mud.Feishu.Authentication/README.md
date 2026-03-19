# Mud.Feishu.Authentication

飞书用户认证中间件，提供用户上下文管理功能。

## 功能特性

- 基于 `AsyncLocal` 实现线程安全的用户信息存储
- 从 JWT Claims 中自动提取飞书用户信息
- 支持分布式追踪 (ActivitySource)
- 请求结束后自动清理用户上下文

## 安装

```bash
dotnet add package Mud.Feishu.Authentication
```

## 快速开始

### 1. 注册服务

```csharp
// Program.cs
services.AddFeishuUserContext();
```

### 2. 配置中间件

```csharp
app.UseAuthentication();
app.UseFeishuUserAuthentication();  // 添加此中间件
app.UseAuthorization();
```

### 3. 使用用户上下文

```csharp
public class MyService
{
    private readonly ICurrentUserContext _userContext;
    
    public MyService(ICurrentUserContext userContext)
    {
        _userContext = userContext;
    }
    
    public void DoSomething()
    {
        var openId = _userContext.OpenId;      // 飞书用户 OpenId
        var unionId = _userContext.UnionId;    // 飞书用户 UnionId
        var userId = _userContext.UserId;      // 业务系统用户ID
        var name = _userContext.Name;          // 用户名称
        
        if (_userContext.IsAuthenticated)
        {
            // 用户已认证
        }
    }
}
```

## 支持的 Claims

| Claim 类型 | 用途 |
|-----------|------|
| `open_id` | 飞书用户 OpenId |
| `union_id` | 飞书用户 UnionId |
| `user_id` | 业务系统用户ID |
| `ClaimTypes.Name` | 用户名称 |
| `ClaimTypes.NameIdentifier` | 备用 OpenId 来源 |

## 许可证

MIT License
