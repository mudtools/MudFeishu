// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书开放平台电子表格分为表格（spreadsheet）、工作表（sheet）和范围（range）。
/// <para>表格是承载数据的容器，提供数据处理、展示、分析的功能。一个表格包含一个或多个工作表。每个表格都有一个 spreadsheetToken 作为唯一标识。</para>
/// <para>工作表（sheet）是表格中的单独页面。每个工作表都有自己的行和列，形成一个网格，用于组织和存储数据。每一个工作表都有唯一的 sheetId 作为标识。</para>
/// <para>在工作表中进行读取数据、写入数据、筛选数据等各类操作时，需要通过 范围 range 参数指定操作数据的范围。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/sheets-v3/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spreadsheets", InheritedFrom = nameof(FeishuV3Spreadsheets))]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV3Spreadsheets : IFeishuV3Spreadsheets
{
}
