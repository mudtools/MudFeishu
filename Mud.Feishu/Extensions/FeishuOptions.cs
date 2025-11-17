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
}