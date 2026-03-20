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
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services;
using TaskManageDemo.Backend.Services.Feishu;
using TaskManageDemo.Backend.Services.History;
using TaskManageDemo.Backend.Services.Sync;
using TaskManageDemo.Backend.Services.Transaction;

namespace TaskManageDemo.Backend.Tests.Services;

/// <summary>
/// 任务服务测试
/// </summary>
public class TaskServiceTests : IDisposable
{
    private readonly TaskManageDbContext _dbContext;
    private readonly Mock<IFeishuTaskService> _feishuTaskServiceMock;
    private readonly Mock<ITaskSyncService> _taskSyncServiceMock;
    private readonly Mock<IFeishuNotificationService> _notificationServiceMock;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly Mock<ITaskHistoryService> _taskHistoryServiceMock;
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<TaskManageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TaskManageDbContext(options);

        _feishuTaskServiceMock = new Mock<IFeishuTaskService>();
        _taskSyncServiceMock = new Mock<ITaskSyncService>();
        _notificationServiceMock = new Mock<IFeishuNotificationService>();
        _transactionServiceMock = new Mock<ITransactionService>();
        _taskHistoryServiceMock = new Mock<ITaskHistoryService>();

        _transactionServiceMock
            .Setup(x => x.ExecuteAsync(It.IsAny<Func<Task<TaskSync>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<TaskSync>> operation, CancellationToken _) => operation());

        _transactionServiceMock
            .Setup(x => x.ExecuteAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task> operation, CancellationToken _) => operation());

        _sut = new TaskService(
            _dbContext,
            _feishuTaskServiceMock.Object,
            _taskSyncServiceMock.Object,
            _notificationServiceMock.Object,
            _transactionServiceMock.Object,
            _taskHistoryServiceMock.Object,
            Mock.Of<ILogger<TaskService>>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetTasksAsync_ShouldReturnEmpty_WhenNoTasks()
    {
        var result = await _sut.GetTasksAsync(new TaskQueryParameters());

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetTasksAsync_ShouldReturnFilteredTasks_WhenFilterApplied()
    {
        var task1 = new TaskSync
        {
            TaskGuid = "task-1",
            Summary = "Completed Task",
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var task2 = new TaskSync
        {
            TaskGuid = "task-2",
            Summary = "Active Task",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.AddRange(task1, task2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTasksAsync(new TaskQueryParameters { IsCompleted = false });

        result.Items.Should().HaveCount(1);
        result.Items[0].Summary.Should().Be("Active Task");
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterByKeyword()
    {
        var task1 = new TaskSync
        {
            TaskGuid = "task-1",
            Summary = "Important Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var task2 = new TaskSync
        {
            TaskGuid = "task-2",
            Summary = "Regular Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.AddRange(task1, task2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTasksAsync(new TaskQueryParameters { Keyword = "Important" });

        result.Items.Should().HaveCount(1);
        result.Items[0].Summary.Should().Be("Important Task");
    }

    [Fact]
    public async Task GetTasksAsync_ShouldFilterByPriority()
    {
        var task1 = new TaskSync
        {
            TaskGuid = "task-1",
            Summary = "High Priority",
            Priority = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var task2 = new TaskSync
        {
            TaskGuid = "task-2",
            Summary = "Low Priority",
            Priority = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.AddRange(task1, task2);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTasksAsync(new TaskQueryParameters { Priority = 3 });

        result.Items.Should().HaveCount(1);
        result.Items[0].Priority.Should().Be(3);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ShouldReturnNull_WhenTaskNotFound()
    {
        var result = await _sut.GetTaskByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTaskByIdAsync_ShouldReturnTask_WhenTaskExists()
    {
        var task = new TaskSync
        {
            TaskGuid = "task-1",
            Summary = "Test Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetTaskByIdAsync(task.Id);

        result.Should().NotBeNull();
        result!.Summary.Should().Be("Test Task");
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldCreateTask_WhenValidRequest()
    {
        var request = new CreateTaskRequest
        {
            Summary = "New Task",
            Description = "Task Description",
            Priority = 2
        };

        _feishuTaskServiceMock
            .Setup(x => x.CreateTaskAsync(
                request.Summary,
                request.Description,
                It.IsAny<List<string>?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-task-guid");

        var syncedTask = new TaskSync
        {
            Id = 1,
            TaskGuid = "new-task-guid",
            Summary = request.Summary,
            Description = request.Description,
            Priority = request.Priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _taskSyncServiceMock
            .Setup(x => x.SyncTaskAsync("new-task-guid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncedTask);

        var result = await _sut.CreateTaskAsync(request, "user-1");

        result.Should().NotBeNull();
        result.Summary.Should().Be(request.Summary);

        _taskHistoryServiceMock.Verify(
            x => x.RecordTaskCreatedAsync(syncedTask.Id, "user-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldThrowException_WhenFeishuReturnsNull()
    {
        var request = new CreateTaskRequest
        {
            Summary = "New Task",
            Priority = 1
        };

        _feishuTaskServiceMock
            .Setup(x => x.CreateTaskAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = async () => await _sut.CreateTaskAsync(request, "user-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("创建任务失败：飞书 API 返回空值");
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldReturnNull_WhenTaskNotFound()
    {
        var request = new UpdateTaskRequest { Summary = "Updated" };

        var result = await _sut.UpdateTaskAsync(999, request, "user-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateTask_WhenValidRequest()
    {
        var task = new TaskSync
        {
            TaskGuid = "task-1",
            Summary = "Original Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateTaskRequest
        {
            Summary = "Updated Task",
            IsCompleted = true
        };

        _feishuTaskServiceMock
            .Setup(x => x.UpdateTaskAsync(
                task.TaskGuid,
                request.Summary,
                It.IsAny<string?>(),
                request.IsCompleted,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var syncedTask = new TaskSync
        {
            Id = task.Id,
            TaskGuid = task.TaskGuid,
            Summary = request.Summary!,
            IsCompleted = request.IsCompleted!.Value,
            CreatedAt = task.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _taskSyncServiceMock
            .Setup(x => x.SyncTaskAsync(task.TaskGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncedTask);

        var result = await _sut.UpdateTaskAsync(task.Id, request, "user-1");

        result.Should().NotBeNull();
        result!.Summary.Should().Be("Updated Task");
    }

    [Fact]
    public async Task DeleteTaskAsync_ShouldReturnFalse_WhenTaskNotFound()
    {
        var result = await _sut.DeleteTaskAsync(999, "user-1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTaskAsync_ShouldDeleteTask_WhenTaskExists()
    {
        var task = new TaskSync
        {
            TaskGuid = "task-1",
            Summary = "Task to Delete",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        _feishuTaskServiceMock
            .Setup(x => x.DeleteTaskAsync(task.TaskGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteTaskAsync(task.Id, "user-1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AssignTaskAsync_ShouldReturnFalse_WhenTaskNotFound()
    {
        var request = new AssignTaskRequest
        {
            AssigneeIds = new List<string> { "user-1" }
        };

        var result = await _sut.AssignTaskAsync(999, request);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldReturnFalse_WhenTaskNotFound()
    {
        var result = await _sut.UpdateTaskStatusAsync(999, true);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ShouldUpdateStatus_WhenTaskExists()
    {
        var task = new TaskSync
        {
            TaskGuid = "task-1",
            Summary = "Test Task",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        _feishuTaskServiceMock
            .Setup(x => x.UpdateTaskAsync(
                task.TaskGuid,
                null,
                null,
                true,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _taskSyncServiceMock
            .Setup(x => x.SyncTaskAsync(task.TaskGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskSync { TaskGuid = task.TaskGuid, IsCompleted = true });

        var result = await _sut.UpdateTaskStatusAsync(task.Id, true);

        result.Should().BeTrue();
    }
}
