// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI;

/// <summary>
/// SDK 工具生成剖面标记接口（工具面引导契约）。
/// </summary>
/// <remarks>
/// <para>
/// v2.3 随引擎进仓（原 <c>Mud.HttpUtils.ISdkToolProfile</c>，组件包 <c>Mud.HttpUtils.Abstractions</c>）：
/// 引擎（<c>Mud.AI.Generator</c>）以<b>硬编码全名 <c>Mud.AI.ISdkToolProfile</c></b> 经
/// <c>GetTypeByMetadataName</c> 发现剖面——命名空间即契约，改名须同批改引擎常量
/// （<c>SdkToolProfileModel.ProfileInterfaceFullName</c> / <c>ProfileContractGuard</c> 两处）。
/// </para>
/// <para>
/// 实现本接口 = 声明「当前类型是一个工具 Schema 生成剖面」，且<b>必须</b>同时标注
/// <c>[SdkToolProfile]</c>（位于 <c>Mud.AI.Attributes</c>）。成对性由编译期守卫诊断
/// <c>SDKT001</c>（<c>Mud.AI.Generator</c> 的 <c>ToolSurface/ProfileContractGuard</c>）强制。
/// </para>
/// <para>
/// <b>为什么是「空标记接口 + 数据特性」而非「接口带 get-only 属性」</b>：源生成器只能读
/// <c>IFieldSymbol.ConstantValue</c> 或 <c>AttributeData</c>，<b>不能执行属性 getter</b>。
/// 用特性承载常量 = 编译器可读 + 消费方可写 + 无运行时语义泄漏。
/// 本接口因此没有任何成员，仅承担「类型即剖面」的编译期声明职责与守卫锚点。
/// </para>
/// </remarks>
public interface ISdkToolProfile
{
}
