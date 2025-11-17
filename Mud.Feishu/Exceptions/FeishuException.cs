namespace Mud.Feishu.Exceptions;

public class FeishuException : Exception
{
    public int ErrorCode { get; set; }

    public FeishuException() { }

    public FeishuException(int errorCode) : this(errorCode, $"飞书API接口调用失败，错误代码 {errorCode} 。")
    {
    }

    public FeishuException(int errorCode, string message) : base(message)
    {
        this.ErrorCode = errorCode;
    }

    public FeishuException(int errorCode, string message, Exception inner) : base(message, inner)
    {
        this.ErrorCode = errorCode;
    }

    public FeishuException(string message) : base(message) { }

    public FeishuException(string message, Exception inner) : base(message, inner) { }

}

