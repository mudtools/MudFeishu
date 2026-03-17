// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书服务建造者，用于按需注册飞书相关服务
/// </summary>
public class FeishuServiceBuilder
{
    private readonly IServiceCollection _services;
    private readonly FeishuServiceConfiguration _configuration = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="services">服务集合</param>
    internal FeishuServiceBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// 添加组织管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddOrganizationApi()
    {
        if (!_configuration.OrganizationApiAdded)
        {
            _services.AddOrganizationWebApiHttpClient();
            _configuration.OrganizationApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加消息管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddMessageApi()
    {
        if (!_configuration.MessageApiAdded)
        {
            _services.AddMessageWebApiHttpClient();
            _configuration.MessageApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加群聊管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddChatGroupApi()
    {
        if (!_configuration.ChatGroupApiAdded)
        {
            _services.AddChatGroupWebApiHttpClient();
            _configuration.ChatGroupApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加流程审批管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddApprovalApi()
    {
        if (!_configuration.ApprovalApiAdded)
        {
            _services.AddApprovalWebApiHttpClient();
            _configuration.ApprovalApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加任务管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddTaskApi()
    {
        if (!_configuration.TaskApiAdded)
        {
            _services.AddTaskWebApiHttpClient();
            _configuration.TaskApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加卡片管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddCardApi()
    {
        if (!_configuration.CardApiAdded)
        {
            _services.AddCardsWebApiHttpClient();
            _configuration.CardApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加考勤管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddAttendanceApi()
    {
        if (!_configuration.AttendanceAdded)
        {
            _services.AddAttendanceWebApiHttpClient();
            _configuration.AttendanceAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加知识库 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddWikiApi()
    {
        if (!_configuration.WikeApiAdded)
        {
            _services.AddWikiWebApiHttpClient();
            _configuration.WikeApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加飞书云盘管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddDriveApi()
    {
        if (!_configuration.DriveApiAdded)
        {
            _services.AddDriveWebApiHttpClient();
            _configuration.DriveApiAdded = true;
        }
        return this;
    }

    /// <summary>
    /// 添加所有 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddAllApis()
    {
        return AddOrganizationApi()
               .AddMessageApi()
               .AddChatGroupApi()
               .AddApprovalApi()
               .AddTaskApi()
               .AddCardApi()
               .AddDriveApi()
               .AddWikiApi()
               .AddAttendanceApi();
    }

    /// <summary>
    /// 根据功能模块添加服务
    /// </summary>
    /// <param name="modules">功能模块</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddModules(params FeishuModule[] modules)
    {
        foreach (var module in modules)
        {
            switch (module)
            {
                case FeishuModule.Organization:
                    AddOrganizationApi();
                    break;
                case FeishuModule.Message:
                    AddMessageApi();
                    break;
                case FeishuModule.ChatGroup:
                    AddChatGroupApi();
                    break;
                case FeishuModule.Approval:
                    AddApprovalApi();
                    break;
                case FeishuModule.Attendance:
                    AddAttendanceApi();
                    break;
                case FeishuModule.All:
                    AddAllApis();
                    break;
            }
        }
        return this;
    }

    /// <summary>
    /// 构建服务注册
    /// </summary>
    /// <returns>服务集合，支持链式调用</returns>
    public IServiceCollection Build()
    {
        // 验证至少添加了一个服务
        if (!_configuration.HasAnyService())
        {
            throw new InvalidOperationException("至少需要添加一个服务，请使用相应的 Add 方法。");
        }
        return _services;
    }
}

/// <summary>
/// 飞书功能模块枚举
/// </summary>
public enum FeishuModule
{
    /// <summary>
    /// 组织管理
    /// </summary>
    Organization,

    /// <summary>
    /// 消息管理
    /// </summary>
    Message,

    /// <summary>
    /// 群聊管理
    /// </summary>
    ChatGroup,

    /// <summary>
    /// 流程审批管理
    /// </summary>
    Approval,

    /// <summary>
    /// 考勤管理
    /// </summary>
    Attendance,

    /// <summary>
    /// 飞书云盘管理
    /// </summary>
    Drive,

    /// <summary>
    /// 飞书知识库管理
    /// </summary>
    Wiki,

    /// <summary>
    /// 所有功能
    /// </summary>
    All
}

/// <summary>
/// 飞书服务配置内部状态
/// </summary>
internal class FeishuServiceConfiguration
{
    public bool OrganizationApiAdded { get; set; }
    public bool MessageApiAdded { get; set; }
    public bool ChatGroupApiAdded { get; set; }
    public bool ApprovalApiAdded { get; set; }
    public bool CardApiAdded { get; set; }
    public bool TaskApiAdded { get; set; }
    public bool AuthenticationApiAdded { get; set; }
    public bool AttendanceAdded { get; set; }
    public bool DriveApiAdded { get; set; }
    public bool WikeApiAdded { get; set; }

    /// <summary>
    /// 检查是否添加了任何服务
    /// </summary>
    /// <returns>是否添加了服务</returns>
    public bool HasAnyService()
    {
        return
               OrganizationApiAdded ||
               MessageApiAdded ||
               ChatGroupApiAdded ||
               AuthenticationApiAdded ||
               AttendanceAdded ||
               TaskApiAdded ||
               CardApiAdded ||
               ApprovalApiAdded ||
               WikeApiAdded ||
               DriveApiAdded;
    }
}