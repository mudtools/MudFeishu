// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 电子表格数据保护用于设置电子表格保护范围指对工作表中的任意行或列进行保护，并可设置其他协作者是否有权限编辑该数据，有效保障数据信息安全。
/// <para>本接口提供飞书开放平台电子表格中数据保护能力相关方法。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spreadsheets", InheritedFrom = nameof(FeishuV2SpreadsheetProtected))]
[Header(Consts.Authorization)]
[Token(TokenType.UserAccessToken)]
public interface IFeishuUserV2SpreadsheetProtected : IFeishuV2SpreadsheetProtected, ICurrentUserId
{
}
