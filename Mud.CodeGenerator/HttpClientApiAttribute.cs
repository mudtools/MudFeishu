// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.CodeGenerator;

/// <summary>
/// HTTP客户端API特性，用于标记需要生成HTTP客户端包装类的接口
/// </summary>
/// <remarks>
/// 此特性应用于接口上，指示代码生成器为该接口创建HTTP客户端实现类。
/// 支持配置内容类型、超时时间和服务注册分组等选项。
/// </remarks>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
internal class HttpClientApiAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="HttpClientApiAttribute"/> 类的新实例
    /// </summary>
    /// <remarks>
    /// 使用默认配置初始化，内容类型为 "application/json"，超时时间为50秒
    /// </remarks>
    public HttpClientApiAttribute()
    {
    }

    /// <summary>
    /// 使用指定的基地址初始化 <see cref="HttpClientApiAttribute"/> 类的新实例
    /// </summary>
    /// <param name="baseAddress">HTTP客户端的基地址</param>
    /// <remarks>
    /// 设置基地址已被弃用，建议通过其他方式配置API端点
    /// </remarks>
    public HttpClientApiAttribute(string baseAddress)
    {
        BaseAddress = baseAddress;
    }

    /// <summary>
    /// 获取或设置HTTP请求的内容类型
    /// </summary>
    /// <value>内容类型，默认值为 "application/json"</value>
    /// <remarks>
    /// 用于设置HTTP请求头的 Content-Type，指定请求体的数据格式
    /// </remarks>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// 获取或设置HTTP客户端的基地址
    /// </summary>
    /// <value>API的基地址</value>
    /// <remarks>
    /// 此属性已被弃用，请使用其他方式配置API端点
    /// </remarks>
    public string? BaseAddress { get; set; }

    /// <summary>
    /// 获取或设置HTTP连接超时时间（秒）
    /// </summary>
    /// <value>超时时间，默认值为50秒</value>
    /// <remarks>
    /// 设置HTTP请求的超时时间，单位为秒。超过此时间将抛出超时异常
    /// </remarks>
    public int Timeout { get; set; } = 50;

    /// <summary>
    /// 获取或设置服务注册的分组名称
    /// </summary>
    /// <value>分组名称，用于依赖注入容器中的服务分组</value>
    /// <remarks>
    /// 可选属性，用于将生成的HTTP客户端服务分组注册到依赖注入容器中
    /// </remarks>
    public string? RegistryGroupName { get; set; }

    /// <summary>
    /// 获取或设置令牌管理服务接口的名称。
    /// </summary>
    public string? TokenManage { get; set; }

    /// <summary>
    /// 生成的客户端类是否为抽象类
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// 生成的客户端类继承自哪个类
    /// </summary>
    public string InheritedFrom { get; set; }
}