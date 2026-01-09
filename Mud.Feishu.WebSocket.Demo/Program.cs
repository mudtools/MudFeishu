// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.WebSocket;
using Mud.Feishu.WebSocket.Demo.Handlers;
using Mud.Feishu.WebSocket.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置飞书基础令牌服务
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
                .AddAuthenticationApi()
                .AddTokenManagers()
                .Build();

// 配置飞书WebSocket服务
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
                .AddHandler<DemoDepartmentEventHandler>()
                .AddHandler<DemoDepartmentDeleteEventHandler>()
                .AddHandler<DemoDepartmentUpdateEventHandler>()
                .Build();

// 配置演示服务
builder.Services.AddSingleton<DemoEventService>();
builder.Services.AddHostedService<DemoEventBackgroundService>();

var app = builder.Build();

app.Run();