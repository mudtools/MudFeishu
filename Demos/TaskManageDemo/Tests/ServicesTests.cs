// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.EventHandlers;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services.Feishu;
using TaskManageDemo.Backend.Services.Search;
using TaskManageDemo.Backend.Services.Sync;
using TaskManageDemo.Backend.Services.Templates;

namespace TaskManageDemo.Backend.Tests;

public class TaskSearchServiceTests : IDisposable
{
    private readonly TaskManageDbContext _dbContext;
    private readonly TaskSearchService _sut;

    public TaskSearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<TaskManageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TaskManageDbContext(options);
        _sut = new TaskSearchService(_dbContext, Mock.Of<ILogger<TaskSearchService>>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenNoTasks()
    {
        var result = await _sut.SearchAsync(new TaskSearchParameters());

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnMatchingTasks_WhenKeywordProvided()
    {
        _dbContext.Tasks.AddRange(
            new TaskSync { TaskGuid = "1", Summary = "Test Task", CreatedAt = DateTime.UtcNow },
            new TaskSync { TaskGuid = "2", Summary = "Another Task", CreatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _sut.SearchAsync(new TaskSearchParameters { Keyword = "Test" });

        result.Items.Should().HaveCount(1);
        result.Items[0].Summary.Should().Be("Test Task");
    }

    [Fact]
    public async Task SearchAsync_ShouldExcludeCompleted_WhenIncludeCompletedIsFalse()
    {
        _dbContext.Tasks.AddRange(
            new TaskSync { TaskGuid = "1", Summary = "Active Task", IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new TaskSync { TaskGuid = "2", Summary = "Completed Task", IsCompleted = true, CreatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _sut.SearchAsync(new TaskSearchParameters { IncludeCompleted = false });

        result.Items.Should().HaveCount(1);
        result.Items[0].Summary.Should().Be("Active Task");
    }
}

public class TaskTemplateServiceTests : IDisposable
{
    private readonly TaskManageDbContext _dbContext;
    private readonly Mock<IFeishuTaskService> _taskServiceMock;
    private readonly TaskTemplateService _sut;

    public TaskTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<TaskManageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TaskManageDbContext(options);
        _taskServiceMock = new Mock<IFeishuTaskService>();
        _sut = new TaskTemplateService(_dbContext, _taskServiceMock.Object, Mock.Of<ILogger<TaskTemplateService>>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAllTemplatesAsync_ShouldReturnOnlyPublicTemplates()
    {
        _dbContext.TaskTemplates.AddRange(
            new TaskTemplate { Name = "Public Template", IsPublic = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TaskTemplate { Name = "Private Template", IsPublic = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllTemplatesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Public Template");
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldCreateTemplate()
    {
        var request = new CreateTaskTemplateRequest
        {
            Name = "New Template",
            DefaultSummary = "Default Summary",
            DefaultPriority = 2,
            IsPublic = true
        };

        var result = await _sut.CreateTemplateAsync(request);

        result.Name.Should().Be("New Template");
        result.DefaultPriority.Should().Be(2);
        (await _dbContext.TaskTemplates.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteTemplateAsync_ShouldReturnFalse_WhenTemplateNotFound()
    {
        var result = await _sut.DeleteTemplateAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTaskFromTemplateAsync_ShouldReturnNull_WhenTemplateNotFound()
    {
        var result = await _sut.CreateTaskFromTemplateAsync(999, new CreateTaskFromTemplateRequest());

        result.Should().BeNull();
    }
}

public class EventProcessServiceTests : IDisposable
{
    private readonly TaskManageDbContext _dbContext;
    private readonly EventProcessService _sut;

    public EventProcessServiceTests()
    {
        var options = new DbContextOptionsBuilder<TaskManageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TaskManageDbContext(options);
        _sut = new EventProcessService(_dbContext, Mock.Of<ILogger<EventProcessService>>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task IsProcessedAsync_ShouldReturnFalse_WhenEventNotProcessed()
    {
        var result = await _sut.IsProcessedAsync("new-event-id");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProcessedAsync_ShouldReturnTrue_WhenEventSuccessfullyProcessed()
    {
        var record = await _sut.StartProcessAsync("test-event-id", "test.event");
        await _sut.MarkSuccessAsync(record.Id);

        var result = await _sut.IsProcessedAsync("test-event-id");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task StartProcessAsync_ShouldCreateNewRecord()
    {
        var result = await _sut.StartProcessAsync("new-event-id", "test.event");

        result.EventId.Should().Be("new-event-id");
        result.EventType.Should().Be("test.event");
        result.Status.Should().Be(EventProcessStatus.Processing);
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldIncrementRetryCount()
    {
        var record = await _sut.StartProcessAsync("fail-event-id", "test.event");

        await _sut.MarkFailedAsync(record.Id, "Test error");

        var updated = await _dbContext.Set<EventProcessRecord>().FindAsync(record.Id);
        updated!.RetryCount.Should().Be(1);
        updated.Status.Should().Be(EventProcessStatus.Failed);
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldSetMaxRetryExceeded_WhenMaxRetriesReached()
    {
        var record = await _sut.StartProcessAsync("max-retry-event", "test.event");
        record.MaxRetryCount = 1;
        await _dbContext.SaveChangesAsync();

        await _sut.MarkFailedAsync(record.Id, "Test error");

        var updated = await _dbContext.Set<EventProcessRecord>().FindAsync(record.Id);
        updated!.Status.Should().Be(EventProcessStatus.MaxRetryExceeded);
    }
}
