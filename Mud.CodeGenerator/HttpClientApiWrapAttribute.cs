namespace Mud.CodeGenerator;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class HttpClientApiWrapAttribute : Attribute
{
    public HttpClientApiWrapAttribute()
    {
    }

    public HttpClientApiWrapAttribute(string tokenManage)
    {
        TokenManage = tokenManage;
    }

    public HttpClientApiWrapAttribute(string tokenManage, string wrapInterface)
    {
        WrapInterface = wrapInterface;
        TokenManage = tokenManage;
    }

    public string WrapInterface { get; set; }

#if NET8_0_OR_GREATER
    public required string TokenManage { get; set; }
#else

    public string TokenManage { get; set; }
#endif
}
