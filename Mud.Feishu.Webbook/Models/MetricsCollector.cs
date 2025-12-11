// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Mud.Feishu.Webbook.Models;

/// <summary>
/// 性能指标收集器
/// </summary>
public class MetricsCollector
{
    private readonly ActivitySource _activitySource;
    private readonly object _lock = new();
    private readonly Dictionary<string, long> _counters = new();

    /// <summary>
    /// 性能指标收集器
    /// </summary>
    public MetricsCollector()
    {
        _activitySource = new ActivitySource("Mud.Feishu.Webbook");
    }

    /// <summary>
    /// 开始事件处理活动
    /// </summary>
    /// <returns>活动对象</returns>
    public Activity? StartEventHandlingActivity()
    {
        return _activitySource.StartActivity("HandleFeishuEvent");
    }

    /// <summary>
    /// 增加验证请求数量
    /// </summary>
    public void IncrementVerificationRequests()
    {
        IncrementCounter("verification_requests");
    }

    /// <summary>
    /// 增加成功事件数量
    /// </summary>
    public void IncrementSuccessfulEvents()
    {
        IncrementCounter("successful_events");
    }

    /// <summary>
    /// 增加失败事件数量
    /// </summary>
    public void IncrementFailedEvents()
    {
        IncrementCounter("failed_events");
    }

    /// <summary>
    /// 增加取消事件数量
    /// </summary>
    public void IncrementCancelledEvents()
    {
        IncrementCounter("cancelled_events");
    }

    /// <summary>
    /// 增加签名验证失败数量
    /// </summary>
    public void IncrementSignatureValidationFailures()
    {
        IncrementCounter("signature_validation_failures");
    }

    /// <summary>
    /// 增加解密失败数量
    /// </summary>
    public void IncrementDecryptionFailures()
    {
        IncrementCounter("decryption_failures");
    }

    /// <summary>
    /// 获取所有计数器值
    /// </summary>
    /// <returns>计数器字典</returns>
    public Dictionary<string, long> GetAllCounters()
    {
        lock (_lock)
        {
            return new Dictionary<string, long>(_counters);
        }
    }

    /// <summary>
    /// 重置所有计数器
    /// </summary>
    public void ResetCounters()
    {
        lock (_lock)
        {
            _counters.Clear();
        }
    }

    private void IncrementCounter(string name)
    {
        lock (_lock)
        {
            if (_counters.ContainsKey(name))
            {
                _counters[name]++;
            }
            else
            {
                _counters[name] = 1;
            }
        }
    }
}