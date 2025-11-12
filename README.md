# MudFeishu

MudFeishu 是一个 .NET 库，用于简化与飞书（Feishu）API 的集成。它提供了一组类型安全的客户端和服务，使开发人员能够轻松地在 .NET 应用程序中调用飞书 API。

## 功能特性

- **类型安全的 API 客户端**：为飞书 API 提供强类型化的客户端接口。
- **自动令牌管理**：自动处理访问令牌的获取和刷新。
- **请求构建器**：使用特性（Attribute）简化 HTTP 请求的构建。
- **响应处理**：提供统一的响应处理机制，简化错误处理和数据解析。

## 安装

你可以通过 NuGet 安装 MudFeishu：

```bash
dotnet add package Mud.Feishu
```

## 使用示例

### 初始化客户端

```csharp
using Mud.Feishu;
using Mud.Feishu.DataModels;

var credentials = new AppCredentials
{
    AppId = "your_app_id",
    AppSecret = "your_app_secret"
};

var client = new FeishuClient(credentials);
```

### 获取访问令牌

```csharp
var tokenResult = await client.Authentication.GetTenantAccessTokenAsync();
if (tokenResult.Code == 0)
{
    Console.WriteLine($"Tenant Access Token: {tokenResult.TenantAccessToken}");
}
else
{
    Console.WriteLine($"Error: {tokenResult.Msg}");
}
```

## 贡献

欢迎贡献代码和建议！请参阅 [CONTRIBUTING.md](CONTRIBUTING.md) 获取更多信息。

## 许可证

MudFeishu 遵循 [MIT 许可证](LICENSE)。