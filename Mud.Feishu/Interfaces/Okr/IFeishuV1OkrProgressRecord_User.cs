// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;


/// <summary>
/// 飞书 OKR「进展记录」SDK 是一组服务端 OpenAPI 的封装，用于创建、更新、查询、删除 OKR 进展记录，以及上传进展记录中的图片附件（用户令牌）。
/// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/progress_record/create">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Okr", InheritedFrom = nameof(FeishuV1OkrProgressRecord))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1OkrProgressRecord : IFeishuV1OkrProgressRecord, ICurrentUserId
{
}
