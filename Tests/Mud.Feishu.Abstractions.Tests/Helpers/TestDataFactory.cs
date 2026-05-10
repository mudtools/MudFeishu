// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels;

namespace Mud.Feishu.Abstractions.Tests.Helpers;

/// <summary>
/// 测试数据工厂类
/// 用于统一管理测试中使用的测试数据，避免魔法字符串和硬编码值
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// 应用配置测试数据
    /// </summary>
    public static class AppConfigs
    {
        public static class Secrets
        {
            public const string Empty = "";
            public const string Valid = "test_secret_12345678";
            public const string VerySecret = "very_secret_key_12345";
            public const string App1 = "secret1_12345678";
            public const string App2 = "secret2_12345678";
            public const string Default = "default_secret_123456";
            public const string Hr = "hr_secret_123456";
            public const string Finance = "finance_secret_123456";
        }

        public static class AppIds
        {
            public const string Default = "cli_default_id_1234567890";
            public const string Hr = "cli_hr_id_1234567890";
            public const string Finance = "cli_finance_id_1234567890";
            public const string App1 = "cli_app1_id_1234567890";
            public const string App2 = "cli_app2_id_1234567890";
        }

        public static class AppKeys
        {
            public const string Default = "default";
            public const string Hr = "hr-app";
            public const string Finance = "finance-app";
            public const string App1 = "app1";
            public const string App2 = "app2";
        }
    }

    /// <summary>
    /// 事件数据测试数据
    /// </summary>
    public static class Events
    {
        public static class EventIds
        {
            public const string Valid = "test-event-id";
            public const string Duplicate = "duplicate_event";
            public const string Event1 = "test_event_123";
            public const string Event2 = "test_event_2";
            public const string Event3 = "test_event_3";
        }

        public static class EventTypes
        {
            public const string Test = "test.event.type";
            public const string Idempotent = "test.idempotent.event";
            public const string Invalid = "invalid_type";
        }

        public static class Tokens
        {
            public const string Valid = "test_token";
            public const string Wrong = "wrong_token";
            public const string Correct = "correct_token";
        }

        public static class Nonces
        {
            public const string Valid = "test_nonce";
        }

        public static class Timestamps
        {
            public static readonly long Valid = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            public static readonly long MillisecondValid = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            public const long Zero = 0;
        }

        public static class Challenges
        {
            public const string Valid = "test_challenge";
        }
    }

    /// <summary>
    /// 用户和部门测试数据
    /// </summary>
    public static class Users
    {
        public const string UserId = "test-user-id";
        public const string DepartmentId = "test-dept-id";
        public const string TenantKey = "test-tenant-key";
    }

    /// <summary>
    /// 令牌管理器测试数据
    /// </summary>
    public static class Tokens
    {
        public const string AppAccessToken = "test-app-access-token";
        public const string CachedToken = "cached-token";
        public const string BearerTokenPrefix = "Bearer ";
        public const int TokenExpire = 7200;
    }

    /// <summary>
    /// Webhook 测试数据
    /// </summary>
    public static class Webhook
    {
        public const string EncryptKey = "test_encrypt_key";
        public const string VerificationToken = "test_token";
    }

    /// <summary>
    /// Redis 测试数据
    /// </summary>
    public static class Redis
    {
        public const string Password = "letmein";
        public const string ServerAddress = "localhost:6379";
        public const string TestKey = "test_event_123";
        public const ulong TestSeqId = 12345UL;
    }

    /// <summary>
    /// WebSocket 测试数据
    /// </summary>
    public static class WebSocket
    {
        public const string PingMessage = "{\"type\":\"ping\",\"timestamp\":1234567890}";
        public const string PongMessage = "{\"type\":\"pong\",\"timestamp\":1234567890}";
        public const string InvalidJson = "{ invalid json }";
        public const string EmptyMessage = "";
    }
}
