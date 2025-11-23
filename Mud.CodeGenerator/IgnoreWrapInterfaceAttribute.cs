namespace Mud.CodeGenerator;

/// <summary>
/// 忽略生成函数的包装接口及实现。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IgnoreWrapInterfaceAttribute : Attribute
{
}
