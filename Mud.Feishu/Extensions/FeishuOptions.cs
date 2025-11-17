namespace Mud.Feishu;

/// <summary>
/// 飞书配置参数。
/// </summary>
public class FeishuOptions
{
    /// <summary>
    /// 飞书应用唯一标识，创建应用后获得。
    /// <para>示例值： "cli_slkdjalasdkjasd"</para>
    /// </summary>
    public required string AppId { get; set; }

    /// <summary>
    /// 应用秘钥，创建应用后获得。
    /// <para>示例值： "dskLLdkasdjlasdKK"</para>
    /// </summary>
    public required string AppSecret { get; set; }

    /// <summary>
    /// 飞书应用类型。
    /// </summary>
    public FeishuAppType AppType { get; set; } = FeishuAppType.SelfBuiltApp;
}

/// <summary>
/// 飞书应用类型。
/// </summary>
public enum FeishuAppType
{
    /// <summary>
    /// 自建应用。
    /// </summary>
    SelfBuiltApp = 0,
    /// <summary>
    /// 自建租户应用。
    /// </summary>
    SelfTenantBuiltApp = 1,

}