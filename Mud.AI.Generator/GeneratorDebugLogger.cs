// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Mud.AI;

/// <summary>
/// 源生成器调试日志辅助类
/// </summary>
/// <remarks>
/// Log 使用 [Conditional("DEBUG")] 确保 Release 构建中所有调用（包括字符串插值参数）
/// 都会被编译器完全移除，避免热路径上的隐藏分配开销。
/// LogError 不使用 [Conditional("DEBUG")]，确保 Release 构建中异常不会被静默吞噬。
/// </remarks>
internal static class GeneratorDebugLogger
{
    /// <summary>
    /// 获取当前是否处于 DEBUG 构建中。
    /// </summary>
    public static bool IsDebug =>
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>
    /// 记录调试日志（仅在 DEBUG 构建中生效）
    /// </summary>
    /// <param name="message">日志消息</param>
    [Conditional("DEBUG")]
    public static void Log(string message)
    {
        Debug.WriteLine(message);
    }

    /// <summary>
    /// 记录错误日志（在所有构建配置中生效，使用 Trace 确保 Release 中也可输出）
    /// </summary>
    /// <param name="context">错误上下文描述</param>
    /// <param name="ex">异常对象</param>
    public static void LogError(string context, Exception ex)
    {
        Trace.WriteLine($"[GeneratorError][{context}] {ex.GetType().Name}: {ex.Message}");
    }

    /// <summary>
    /// 格式化异常消息，在 DEBUG 构建中包含完整堆栈信息便于调试。
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <returns>格式化后的异常描述字符串</returns>
    public static string FormatExceptionMessage(Exception? ex)
    {
        if (ex == null)
            return string.Empty;

        if (IsDebug)
            return $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";

        return ex.Message;
    }
}