// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Tests.Integration;

namespace TaskManageDemo.Backend.Tests.Controllers;

/// <summary>
/// TasksController 集成测试
/// </summary>
public class TasksControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;

    public TasksControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTasks_ShouldReturnEmptyList_WhenNoTasksExist()
    {
        // Arrange
        var token = await GetTestTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<TaskDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTask_ShouldReturnCreatedTask_WhenValidRequest()
    {
        // Arrange
        var token = await GetTestTokenAsync();
        var request = new CreateTaskRequest
        {
            Summary = "Test Task",
            Description = "Test Description",
            Priority = 2,
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TaskDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Summary.Should().Be(request.Summary);
        result.Data.Priority.Should().Be(request.Priority);
    }

    [Fact]
    public async Task CreateTask_ShouldReturnBadRequest_WhenSummaryIsEmpty()
    {
        // Arrange
        var token = await GetTestTokenAsync();
        var request = new CreateTaskRequest
        {
            Summary = "", // 无效
            Priority = 1,
        };

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTaskById_ShouldReturnTask_WhenTaskExists()
    {
        // Arrange
        var token = await GetTestTokenAsync();
        
        // 先创建任务
        var createRequest = new CreateTaskRequest
        {
            Summary = "Test Task for Get",
            Priority = 1,
        };
        var createHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(createRequest)
        };
        createHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var createResponse = await _client.SendAsync(createHttpRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<TaskDto>>();
        var taskId = createResult!.Data.Id;

        // Act
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{taskId}");
        httpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var response = await _client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TaskDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Id.Should().Be(taskId);
    }

    [Fact]
    public async Task UpdateTask_ShouldReturnUpdatedTask_WhenValidRequest()
    {
        // Arrange
        var token = await GetTestTokenAsync();
        
        // 先创建任务
        var createRequest = new CreateTaskRequest
        {
            Summary = "Test Task for Update",
            Priority = 1,
        };
        var createHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(createRequest)
        };
        createHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var createResponse = await _client.SendAsync(createHttpRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<TaskDto>>();
        var taskId = createResult!.Data.Id;

        // Act
        var updateRequest = new UpdateTaskRequest
        {
            Summary = "Updated Task",
            Priority = 3,
        };
        var updateHttpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/tasks/{taskId}")
        {
            Content = JsonContent.Create(updateRequest)
        };
        updateHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var response = await _client.SendAsync(updateHttpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TaskDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Summary.Should().Be(updateRequest.Summary);
        result.Data.Priority.Should().Be(updateRequest.Priority);
    }

    [Fact]
    public async Task DeleteTask_ShouldReturnNoContent_WhenTaskExists()
    {
        // Arrange
        var token = await GetTestTokenAsync();
        
        // 先创建任务
        var createRequest = new CreateTaskRequest
        {
            Summary = "Test Task for Delete",
            Priority = 1,
        };
        var createHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(createRequest)
        };
        createHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var createResponse = await _client.SendAsync(createHttpRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<TaskDto>>();
        var taskId = createResult!.Data.Id;

        // Act
        var deleteHttpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/tasks/{taskId}");
        deleteHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var response = await _client.SendAsync(deleteHttpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 验证已删除
        var getHttpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{taskId}");
        getHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var getResponse = await _client.SendAsync(getHttpRequest);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<string> GetTestTokenAsync()
    {
        // 返回测试令牌
        // 在实际项目中，应该调用登录 API 或使用模拟令牌
        return "test-token";
    }
}

// 辅助类型
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T Data { get; init; } = default!;
}

public record PagedResponse<T>
{
    public List<T> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record TaskDto
{
    public int Id { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Priority { get; init; }
    public bool IsCompleted { get; init; }
}

public record CreateTaskRequest
{
    public string Summary { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Priority { get; init; }
}

public record UpdateTaskRequest
{
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public int? Priority { get; init; }
}
