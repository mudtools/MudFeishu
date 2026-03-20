# AGENTS.md

Guidance for AI coding agents working on the MudFeishu codebase.

## Project Overview

MudFeishu is an enterprise-grade .NET SDK for Feishu (Lark) API integration, providing HTTP API clients, WebSocket real-time events, and Webhook handling.

## Build Commands

```bash
dotnet build Mud.Feishu.slnx                      # Build solution
dotnet build Mud.Feishu.slnx -c Release           # Release build
dotnet build Mud.Feishu/Mud.Feishu.csproj         # Build specific project
```

## Test Commands

```bash
dotnet test Mud.Feishu.slnx                       # Run all tests
dotnet test Mud.Feishu.slnx --logger "console;verbosity=detailed"
dotnet test Tests/Mud.Feishu.Abstractions.Tests   # Run specific test project
dotnet test Tests/Mud.Feishu.Abstractions.Tests --filter "FullyQualifiedName~DefaultFeishuEventHandlerTests"  # Single test class
dotnet test Tests/Mud.Feishu.Abstractions.Tests --filter "FullyQualifiedName~DefaultFeishuEventHandlerTests.HandleAsync_ShouldCallProcessBusinessLogic_WhenEventDataIsValid"  # Single test
dotnet test Mud.Feishu.slnx --collect:"XPlat Code Coverage"
```

## Lint and Format

```bash
dotnet format Mud.Feishu.slnx
```

Project uses `.editorconfig` for code style. `TreatWarningsAsErrors` is disabled.

## Project Structure

```
MudFeishu/
├── Mud.Feishu/               # Core HTTP API client
├── Mud.Feishu.Abstractions/  # Event handling abstractions
├── Mud.Feishu.Authentication/# User authentication middleware
├── Mud.Feishu.Redis/         # Redis distributed deduplication
├── Mud.Feishu.Webhook/       # Webhook event handling
├── Mud.Feishu.WebSocket/     # WebSocket real-time events
├── Tests/                    # Test projects (mirror source structure)
├── Demos/                    # Example applications
├── Directory.Build.props     # Global MSBuild properties
└── Mud.Feishu.slnx           # Solution file
```

## Code Style Guidelines

### File Header

All source files must start with the copyright header:

```csharp
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------
```

### Namespaces and Imports

- Use **file-scoped namespaces** (C# 10+)
- Use `GlobalUsings.cs` for common imports
- Import order: System → Microsoft → Third-party → Project namespaces

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes, Records, Structs | PascalCase | `FeishuEventHandler` |
| Interfaces | `I` + PascalCase | `IFeishuEventHandler` |
| Methods | PascalCase | `HandleAsync` |
| Properties | PascalCase | `SupportedEventType` |
| Private fields | `_camelCase` | `_logger` |
| Parameters | camelCase | `eventData` |

### Types and Nullability

- **Nullable reference types enabled** - use `?` for nullable types
- Use `required` keyword for required properties (NET7+)

```csharp
public class FeishuAppConfig
{
    public required string AppKey { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

### Async/Await

- All async methods must end with `Async` suffix
- Include `CancellationToken` parameter with default value

```csharp
public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default);
```

### Error Handling

- Use `ArgumentNullException` for null parameters, `InvalidOperationException` for invalid state
- Use `ExceptionUtils.ThrowIfNull()` helper for validation

### Dependency Injection

- Use constructor injection, mark dependencies as `readonly`

```csharp
public class DefaultFeishuEventHandler : IFeishuEventHandler
{
    protected readonly ILogger _logger;
    public DefaultFeishuEventHandler(ILogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### Documentation

- Use XML documentation for public APIs with `<summary>`, `<param>`, `<returns>`, `<exception>` tags

## Test Guidelines

- xUnit with FluentAssertions and Moq
- Tests mirror source folder structure
- Test class: `{ClassName}Tests`
- Test method: `{MethodName}_Should{Behavior}_When{Condition}`

```csharp
public class TokenUtilsTests
{
    private readonly TokenUtils _sut;
    public TokenUtilsTests() => _sut = new TokenUtils(new Mock<ILogger>().Object);

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenValidInput()
    {
        // Arrange
        var eventData = new EventData { /* ... */ };
        // Act
        var result = await _sut.HandleAsync(eventData);
        // Assert
        result.Should().NotBeNull();
    }
}
```

## Security Guidelines

- Never log or expose `AppSecret` or tokens
- Use `MaskSensitiveData()` when logging configuration
- Validate URLs to prevent SSRF

## MSBuild Configuration

`Directory.Build.props`: `LangVersion`: 13.0, `Nullable`: enable, `ImplicitUsings`: enable

## Target Frameworks

`netstandard2.0`, `net6.0`, `net8.0` (recommended), `net10.0`

Use conditional compilation: `#if NET7_0_OR_GREATER` for framework-specific code.