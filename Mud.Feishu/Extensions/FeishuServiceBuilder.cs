// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
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
    private readonly Dictionary<FeishuModule, IFeishuModuleRegistrar> _registrars;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="services">服务集合</param>
    internal FeishuServiceBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _registrars = InitializeRegistrars();
    }

    /// <summary>
    /// 初始化模块注册器
    /// </summary>
    /// <returns>模块注册器字典</returns>
    private static Dictionary<FeishuModule, IFeishuModuleRegistrar> InitializeRegistrars()
    {
        return new Dictionary<FeishuModule, IFeishuModuleRegistrar>
        {
            [FeishuModule.Organization] = new OrganizationModuleRegistrar(),
            [FeishuModule.Message] = new MessageModuleRegistrar(),
            [FeishuModule.ChatGroup] = new ChatGroupModuleRegistrar(),
            [FeishuModule.Approval] = new ApprovalModuleRegistrar(),
            [FeishuModule.Task] = new TaskModuleRegistrar(),
            [FeishuModule.Card] = new CardModuleRegistrar(),
            [FeishuModule.Attendance] = new AttendanceModuleRegistrar(),
            [FeishuModule.Drive] = new DriveModuleRegistrar(),
            [FeishuModule.Wiki] = new WikiModuleRegistrar(),
            [FeishuModule.Docx] = new DocxModuleRegistrar(),
            [FeishuModule.Spreadsheets] = new SpreadsheetsModuleRegistrar(),
            [FeishuModule.Bitable] = new BitableRegistrar(),
            [FeishuModule.Calendar] = new CalendarRegistrar(),
        };
    }

    /// <summary>
    /// 添加组织管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddOrganizationApi() => AddModule(FeishuModule.Organization);

    /// <summary>
    /// 添加消息管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddMessageApi() => AddModule(FeishuModule.Message);

    /// <summary>
    /// 添加群聊管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddChatGroupApi() => AddModule(FeishuModule.ChatGroup);

    /// <summary>
    /// 添加流程审批管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddApprovalApi() => AddModule(FeishuModule.Approval);

    /// <summary>
    /// 添加任务管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddTaskApi() => AddModule(FeishuModule.Task);

    /// <summary>
    /// 添加卡片管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddCardApi() => AddModule(FeishuModule.Card);

    /// <summary>
    /// 添加考勤管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddAttendanceApi() => AddModule(FeishuModule.Attendance);

    /// <summary>
    /// 添加知识库 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddWikiApi() => AddModule(FeishuModule.Wiki);

    /// <summary>
    /// 添加文档管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddDocxApi() => AddModule(FeishuModule.Docx);

    /// <summary>
    /// 添加电子表格管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddSpreadsheetsApi() => AddModule(FeishuModule.Spreadsheets);

    /// <summary>
    /// 添加多维表格管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddBiTableApi() => AddModule(FeishuModule.Bitable);

    /// <summary>
    /// 添加飞书云盘管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddDriveApi() => AddModule(FeishuModule.Drive);


    /// <summary>
    /// 添加飞书日历管理 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddCalendarApi() => AddModule(FeishuModule.Calendar);

    /// <summary>
    /// 添加所有 API 服务
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddAllApis()
    {
        foreach (var module in _registrars.Keys)
        {
            AddModule(module);
        }
        return this;
    }

    /// <summary>
    /// 根据功能模块添加服务
    /// </summary>
    /// <param name="modules">功能模块</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder AddModules(params FeishuModule[] modules)
    {
        if (modules == null || modules.Length == 0)
            return this;

        foreach (var module in modules)
        {
            if (module == FeishuModule.All)
            {
                AddAllApis();
            }
            else
            {
                AddModule(module);
            }
        }
        return this;
    }

    /// <summary>
    /// 添加单个模块（核心方法）
    /// </summary>
    /// <param name="module">模块类型</param>
    /// <returns>建造者实例，支持链式调用</returns>
    private FeishuServiceBuilder AddModule(FeishuModule module)
    {
        if (_configuration.TryAdd(module) && _registrars.TryGetValue(module, out var registrar))
        {
            registrar.Register(_services);
        }
        return this;
    }

    /// <summary>
    /// 注册自定义模块
    /// </summary>
    /// <param name="registrar">模块注册器</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuServiceBuilder RegisterModule(IFeishuModuleRegistrar registrar)
    {
        if (registrar == null)
            throw new ArgumentNullException(nameof(registrar));

        if (_configuration.TryAdd(registrar.Module))
        {
            registrar.Register(_services);
            _registrars[registrar.Module] = registrar;
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
