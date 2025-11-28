// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.WsEndpoint;


/// <summary>
/// WebSocket应用凭证
/// </summary>
public class WsAppCredentials
{
    /// <summary>
    /// <para> 应用唯一标识，创建应用后获得。有关app_id 的详细介绍。请参考通用参数介绍</para>
    /// <para>示例值： "cli_slkdjalasdkjasd"</para>
    /// </summary>
    [JsonPropertyName("AppID")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? AppId
    { get; set; }

    /// <summary>
    /// <para>应用秘钥，创建应用后获得。有关 app_secret 的详细介绍，请参考通用参数介绍</para>
    /// <para>示例值： "dskLLdkasdjlasdKK"</para>
    /// </summary>
    [JsonPropertyName("AppSecret")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? AppSecret
    { get; set; }

}