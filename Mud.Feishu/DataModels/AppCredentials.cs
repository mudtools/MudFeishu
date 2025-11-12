namespace Mud.Feishu.DataModels;

/// <summary>
/// 应用凭证
/// </summary>
public class AppCredentials
{
    /// <summary>
    /// <para> 应用唯一标识，创建应用后获得。有关app_id 的详细介绍。请参考通用参数介绍</para>
    /// <para>示例值： "cli_slkdjalasdkjasd"</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public required string AppId { get; set; }

    /// <summary>
    /// <para>应用秘钥，创建应用后获得。有关 app_secret 的详细介绍，请参考通用参数介绍</para>
    /// <para>示例值： "dskLLdkasdjlasdKK"</para>
    /// </summary>
    [JsonPropertyName("app_secret")]
    public required string AppSecret { get; set; }
}