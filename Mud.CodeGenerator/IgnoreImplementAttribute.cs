namespace Mud.CodeGenerator;

/// <summary>
/// 忽略生成函数的实现代码。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IgnoreImplementAttribute : Attribute
{
}
