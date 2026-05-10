// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Tests.Integration;

namespace TaskManageDemo.Backend.Tests.Controllers;

/// <summary>
/// TasksController 集成测试
/// </summary>
public class TasksControllerTests : IClassFixture<IntegrationTestFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;

    public TasksControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public Task InitializeAsync()
    {
        return _factory.InitializeTestDataAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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
        result.Data.Should().NotBeNull();
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
    }

    [Fact]
    public async Task CreateTask_ShouldReturnBadRequest_WhenSummaryIsEmpty()
    {
        // Arrange
        var token = await GetTestTokenAsync();
        var request = new CreateTaskRequest
        {
            Summary = "",
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
    }

    [Fact]
    public async Task DeleteTask_ShouldReturnNoContent_WhenTaskExists()
    {
        // Arrange
        var token = await GetTestTokenAsync();
        var uniqueSummary = $"Test Task for Delete {Guid.NewGuid()}";

        var createRequest = new CreateTaskRequest
        {
            Summary = uniqueSummary,
            Priority = 1,
        };
        var createHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(createRequest)
        };
        createHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var createResponse = await _client.SendAsync(createHttpRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<TaskDto>>();
        var taskId = createResult!.Data.Id;

        // Act
        var deleteHttpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/tasks/{taskId}");
        deleteHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var response = await _client.SendAsync(deleteHttpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getHttpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{taskId}");
        getHttpRequest.Headers.Add("Authorization", $"Bearer {token}");
        var getResponse = await _client.SendAsync(getHttpRequest);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, $"because task {taskId} should be deleted. Response: {getContent}");
    }

    private async Task<string> GetTestTokenAsync()
    {
        var secret = "T@skM@n@geDem0$Tr0ngP@ssw0rd!2025#Key";
        var issuer = "TaskManageDemo";
        var audience = "TaskManageDemo.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, IntegrationTestFactory.TestUserFeishuId),
            new Claim(ClaimTypes.Name, IntegrationTestFactory.TestUserName),
            new Claim("feishu_id", IntegrationTestFactory.TestUserFeishuId),
            new Claim(ClaimTypes.Role, "user"),
            new Claim("permission", "task:read"),
            new Claim("permission", "task:create"),
            new Claim("permission", "task:update"),
            new Claim("permission", "task:delete"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }
}
