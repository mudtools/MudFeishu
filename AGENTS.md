# AGENTS.md

This document provides guidance for AI coding agents working on the MudFeishu codebase.

## Project Overview

MudFeishu is an enterprise-grade .NET SDK for Feishu (Lark) API integration. It provides HTTP API clients, WebSocket real-time event subscription, and Webhook event handling capabilities.

## Build Commands

```bash
# Build the entire solution
dotnet build Mud.Feishu.slnx

# Build in Release mode
dotnet build Mud.Feishu.slnx -c Release

# Build a specific project
dotnet build Mud.Feishu/Mud.Feishu.csproj
dotnet build Mud.Feishu.Abstractions/Mud.Feishu.Abstractions.csproj
```

## Test Commands

```bash
# Run all tests
dotnet test Mud.Feishu.slnx

# Run all tests with detailed output
dotnet test Mud.Feishu.slnx --logger "console;verbosity=detailed"

# Run a specific test project
dotnet test Tests/Mud.Feishu.Abstractions.Tests/Mud.Feishu.Abstractions.Tests.csproj

# Run a single test class
dotnet test Tests/Mud.Feishu.Abstractions.Tests --filter "FullyQualifiedName~DefaultFeishuEventHandlerTests"

# Run a single test method
dotnet test Tests/Mud.Feishu.Abstractions.Tests --filter "FullyQualifiedName~DefaultFeishuEventHandlerTests.HandleAsync_ShouldCallProcessBusinessLogic_WhenEventDataIsValid"

# Run tests with code coverage
dotnet test Mud.Feishu.slnx --collect:"XPlat Code Coverage"

# Run tests for specific framework
dotnet test Tests/Mud.Feishu.Abstractions.Tests --framework net8.0

# Run tests using the batch file (Windows)
run-tests.bat
```

## Lint and Format

```bash
# No explicit lint command - use IDE/Editor built-in analyzers
# The project uses .editorconfig for code style rules

# Format code (if dotnet format is available)
dotnet format Mud.Feishu.slnx

# Build with warnings as errors is disabled by default (TreatWarningsAsErrors=false)
```

## Project Structure

```
MudFeishu/
├── Mud.Feishu/              # Core HTTP API client library
├── Mud.Feishu.Abstractions/ # Event handling abstractions
├── Mud.Feishu.Authentication/ # User authentication middleware
├── Mud.Feishu.Redis/        # Redis distributed deduplication
├── Mud.Feishu.Webhook/      # Webhook event handling
├── Mud.Feishu.WebSocket/    # WebSocket real-time events
├── Tests/                   # Test projects (mirror source structure)
│   ├── Mud.Feishu.Tests/
│   ├── Mud.Feishu.Abstractions.Tests/
│   └── ...
├── Demos/                   # Example applications
├── Directory.Build.props    # Global MSBuild properties
└── Mud.Feishu.slnx          # Solution file (new format)
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

### Namespaces

- Use **file-scoped namespaces** (C# 10+)
- Namespace should match folder structure

```csharp
namespace Mud.Feishu.Abstractions.EventHandlers;

public class MyHandler { }
```

### Imports and Global Usings

- Use `GlobalUsings.cs` for common imports
- Place in project root folder
- Order: System → Microsoft → Third-party → Project namespaces

```csharp
global using System;
global using System.Collections.Generic;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Logging;
global using Mud.Feishu.Abstractions;
```

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes, Records, Structs | PascalCase | `FeishuEventHandler` |
| Interfaces | PascalCase with `I` prefix | `IFeishuEventHandler` |
| Methods | PascalCase | `HandleAsync` |
| Properties | PascalCase | `SupportedEventType` |
| Fields (private) | `_camelCase` | `_logger` |
| Constants | PascalCase | `DefaultTimeout` |
| Parameters | camelCase | `eventData` |
| Type parameters | `T` or `TName` | `T`, `TResult` |

### Types and Nullability

- **Nullable reference types are enabled** - use `?` for nullable types
- Use `required` keyword for required properties (when targeting .NET 7+)
- Use pattern matching for null checks

```csharp
public class FeishuAppConfig
{
    public required string AppKey { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Null checks
if (eventData is null)
    throw new ArgumentNullException(nameof(eventData));

// Pattern matching
if (eventData.Event is JsonDocument jsonDocument)
{
    // use jsonDocument
}
```

### Async/Await

- All async methods must end with `Async` suffix
- Use `CancellationToken` parameter with default value

```csharp
public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default);
```

### Error Handling

- Use `ArgumentNullException` for null parameter validation
- Use `InvalidOperationException` for invalid state
- Use custom `FeishuException` for API errors
- Use `ExceptionUtils.ThrowIfNull()` helper method

```csharp
public void Process(EventData eventData)
{
    ExceptionUtils.ThrowIfNull(eventData, nameof(eventData));
    
    // or
    if (eventData is null)
        throw new ArgumentNullException(nameof(eventData));
}
```

### Dependency Injection

- Use constructor injection
- Mark injected dependencies as `readonly`

```csharp
public class DefaultFeishuEventHandler<T> : IFeishuEventHandler
{
    protected readonly ILogger _logger;

    public DefaultFeishuEventHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### Documentation

- Use XML documentation comments for all public APIs
- Include `<summary>`, `<param>`, `<returns>`, `<exception>` tags

```csharp
/// <summary>
/// Handles the specified event.
/// </summary>
/// <param name="eventData">The event data.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>A task representing the async operation.</returns>
/// <exception cref="ArgumentNullException">When eventData is null.</exception>
public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default);
```

### Extension Methods

- Place in `Extensions` folder
- Class name should end with `Extensions`
- Use `this` keyword for the extended type parameter

```csharp
public static class FeishuServiceCollectionExtensions
{
    public static IServiceCollection AddFeishuApp(this IServiceCollection services, ...)
    {
        // implementation
    }
}
```

## Test Guidelines

### Test Framework

- xUnit with FluentAssertions and Moq
- Tests mirror the source folder structure
- Test class naming: `{ClassName}Tests`
- Test method naming: `{MethodName}_Should{ExpectedBehavior}_When{Condition}`

### Test Structure

```csharp
public class TokenUtilsTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly TokenUtils _sut; // System Under Test

    public TokenUtilsTests()
    {
        _loggerMock = new Mock<ILogger>();
        _sut = new TokenUtils(_loggerMock.Object);
    }

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

    [Theory]
    [InlineData("input1", "expected1")]
    [InlineData("input2", "expected2")]
    public void Method_ShouldReturnExpected_WhenGivenInput(string input, string expected)
    {
        // Arrange, Act, Assert
    }
}
```

### Test Organization

- Use `#region` to group related tests
- Use constants for test data values
- Create helper classes/interfaces for mocking complex dependencies

## Security Guidelines

- Never log or expose `AppSecret` or tokens
- Use `MaskSensitiveData()` when logging configuration
- Validate URLs with `UrlValidator` to prevent SSRF
- Use HTTPS for all external communications

## MSBuild Configuration

The `Directory.Build.props` file defines global settings:
- `LangVersion`: 13.0
- `Nullable`: enable
- `ImplicitUsings`: enable
- `GenerateDocumentationFile`: true (for source projects)
- `TreatWarningsAsErrors`: false

## Target Frameworks

Projects target multiple frameworks:
- `netstandard2.0` - For .NET Framework 4.6.1+ compatibility
- `net6.0` - LTS support
- `net8.0` - LTS support (recommended)
- `net10.0` - Latest LTS

Use conditional compilation for framework-specific code:

```csharp
#if NET7_0_OR_GREATER
    required
#endif
```