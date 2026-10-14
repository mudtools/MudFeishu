// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

// N1 中立化：本包以 record（ConversationScope / EventFact / FrameworkToolApprovalRequest …）
// 承载编译期契约，netstandard2.0 目标需要 IsExternalInit polyfill。
// internal ⇒ 不会与消费方编译单元内的同类 polyfill 冲突（同名 internal 类型跨程序集不可见）。
#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    /// <summary>netstandard2.0 的 init 访问器 / record 支撑类型（编译器识别用，无成员）。</summary>
    internal static class IsExternalInit
    {
    }
}
#endif
