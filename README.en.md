# MudFeishu

MudFeishu is a .NET library designed to simplify integration with the Feishu API. It provides a set of type-safe clients and services that enable developers to easily call Feishu APIs within .NET applications.

## Features

- **Type-Safe API Clients**: Offers strongly-typed client interfaces for the Feishu API.
- **Automatic Token Management**: Automatically handles acquisition and refresh of access tokens.
- **Request Builder**: Simplifies HTTP request construction using attributes.
- **Response Handling**: Provides a unified response handling mechanism to streamline error handling and data parsing.

## Installation

You can install MudFeishu via NuGet:

```bash
dotnet add package Mud.Feishu
```

## Usage Example

### Initialize the Client

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

### Obtain an Access Token

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

## Contributing

Contributions and suggestions are welcome! Please refer to [CONTRIBUTING.md](CONTRIBUTING.md) for more information.

## License

MudFeishu is licensed under the [MIT License](LICENSE).