// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Tasks;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV2Task"/>接口的相关函数。
/// </summary>
public class IFeishuV2TaskTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV2TaskTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.CreateTaskAsync(CreateTaskRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateTaskAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "summary": "针对全年销售进行一次复盘",
              "description": "需要事先阅读复盘总结文档",
              "due": {
                "timestamp": "1675454764000",
                "is_all_day": true
              },
              "origin": {
                "platform_i18n_name": {
                  "en_us": "workbench",
                  "zh_cn": "工作台",
                  "zh_hk": "工作臺",
                  "zh_tw": "工作臺",
                  "ja_jp": "作業台",
                  "fr_fr": "Table de travail",
                  "it_it": "banco di lavoro",
                  "de_de": "Werkbank",
                  "ru_ru": "верстак",
                  "th_th": "โต๊ะทำงาน",
                  "es_es": "banco de trabajo",
                  "ko_kr": "작업대"
                },
                "href": {
                  "url": "https://www.example.com",
                  "title": "反馈一个问题，需要协助排查"
                }
              },
              "extra": "dGVzdA==",
              "completed_at": "1675742789470",
              "members": [
                {
                  "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "user",
                  "role": "assignee",
                  "name": "张明德（明德）"
                }
              ],
              "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
              "custom_complete": {
                "pc": {
                  "href": "https://www.example.com",
                  "tip": {
                    "en_us": "workbench",
                    "zh_cn": "工作台",
                    "zh_hk": "工作臺",
                    "zh_tw": "工作臺",
                    "ja_jp": "作業台",
                    "fr_fr": "Table de travail",
                    "it_it": "banco di lavoro",
                    "de_de": "Werkbank",
                    "ru_ru": "верстак",
                    "th_th": "โต๊ะทำงาน",
                    "es_es": "banco de trabajo",
                    "ko_kr": "작업대"
                  }
                },
                "ios": {
                  "href": "https://www.example.com",
                  "tip": {
                    "en_us": "workbench",
                    "zh_cn": "工作台",
                    "zh_hk": "工作臺",
                    "zh_tw": "工作臺",
                    "ja_jp": "作業台",
                    "fr_fr": "Table de travail",
                    "it_it": "banco di lavoro",
                    "de_de": "Werkbank",
                    "ru_ru": "верстак",
                    "th_th": "โต๊ะทำงาน",
                    "es_es": "banco de trabajo",
                    "ko_kr": "작업대"
                  }
                },
                "android": {
                  "href": "https://www.example.com",
                  "tip": {
                    "en_us": "workbench",
                    "zh_cn": "工作台",
                    "zh_hk": "工作臺",
                    "zh_tw": "工作臺",
                    "ja_jp": "作業台",
                    "fr_fr": "Table de travail",
                    "it_it": "banco di lavoro",
                    "de_de": "Werkbank",
                    "ru_ru": "верстак",
                    "th_th": "โต๊ะทำงาน",
                    "es_es": "banco de trabajo",
                    "ko_kr": "작업대"
                  }
                }
              },
              "tasklists": [
                {
                  "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                  "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                }
              ],
              "client_token": "daa2237f-8310-4707-a83b-52c8a81e0fb7",
              "start": {
                "timestamp": "1675454764000",
                "is_all_day": true
              },
              "reminders": [
                {
                  "relative_fire_minute": 30
                }
              ],
              "mode": 2,
              "is_milestone": false,
              "custom_fields": [
                {
                  "guid": "73b21903-0041-4796-a11e-f8be919a7063",
                  "number_value": "10.23",
                  "member_value": [
                    {
                      "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                      "type": "user",
                      "name": "张明德（明德）"
                    }
                  ],
                  "datetime_value": "1698192000000",
                  "single_select_value": "73b21903-0041-4796-a11e-f8be919a7063",
                  "multi_select_value": [
                    "73b21903-0041-4796-a11e-f8be919a7063"
                  ],
                  "text_value": "这是一段文本描述。"
                }
              ],
              "docx_source": {
                "token": "OvZCdFYVHo5ArFxJKHjcnRbtnKd",
                "block_id": "O6wwd22uIoG8acxwxGtbljaUcfc"
              },
              "positive_reminders": [
                {
                  "relative_fire_minute": 30
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateTaskRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.Tasklists);
        Assert.NotEmpty(requestBody.Summary);
        Assert.NotNull(requestBody.PositiveReminders);
        Assert.NotNull(requestBody.CustomFields);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.CreateTaskAsync(CreateTaskRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateTaskAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee",
                            "name": "张明德（明德）"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "follower",
                                "name": "张明德（明德）"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "assignee",
                                    "name": "张明德（明德）"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://www.example.com",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1,
                        "is_milestone": false,
                        "custom_fields": [
                            {
                                "guid": "a4f648d7-76ef-477f-bc8e-0601b5a60093",
                                "type": "number",
                                "number_value": "10.23",
                                "datetime_value": "1687708260000",
                                "member_value": [
                                    {
                                        "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                        "type": "user",
                                        "role": "editor",
                                        "name": "张明德（明德）"
                                    }
                                ],
                                "single_select_value": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                "multi_select_value": [
                                    "4216f79b-3fda-4dc6-a0c4-a16022e47152"
                                ],
                                "name": "优先级",
                                "text_value": "这是一段文本介绍。"
                            }
                        ],
                        "dependencies": [
                            {
                                "type": "next",
                                "task_guid": "93b7bd05-35e6-4371-b3c9-6b7cbd7100c0"
                            }
                        ],
                        "assignee_related": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "completed_at": "1675742789470"
                            }
                        ],
                        "positive_reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotNull(result.Data.Task.Summary);
        Assert.NotNull(result.Data.Task.Dependencies);
        Assert.NotNull(result.Data.Task.Attachments);
        Assert.NotNull(result.Data.Task.Due);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.UpdateTaskAsync(string, UpdateTaskRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateTaskAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "task": {
                "summary": "针对全年销售进行一次复盘",
                "description": "需要事先阅读复盘总结文档",
                "due": {
                  "timestamp": "1675454764000",
                  "is_all_day": true
                },
                "extra": "dGVzdA==",
                "completed_at": "1675742789470",
                "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                "custom_complete": {
                  "pc": {
                    "href": "https://www.example.com",
                    "tip": {
                      "en_us": "workbench",
                      "zh_cn": "工作台",
                      "zh_hk": "工作臺",
                      "zh_tw": "工作臺",
                      "ja_jp": "作業台",
                      "fr_fr": "Table de travail",
                      "it_it": "banco di lavoro",
                      "de_de": "Werkbank",
                      "ru_ru": "верстак",
                      "th_th": "โต๊ะทำงาน",
                      "es_es": "banco de trabajo",
                      "ko_kr": "작업대"
                    }
                  },
                  "ios": {
                    "href": "https://www.example.com",
                    "tip": {
                      "en_us": "workbench",
                      "zh_cn": "工作台",
                      "zh_hk": "工作臺",
                      "zh_tw": "工作臺",
                      "ja_jp": "作業台",
                      "fr_fr": "Table de travail",
                      "it_it": "banco di lavoro",
                      "de_de": "Werkbank",
                      "ru_ru": "верстак",
                      "th_th": "โต๊ะทำงาน",
                      "es_es": "banco de trabajo",
                      "ko_kr": "작업대"
                    }
                  },
                  "android": {
                    "href": "https://www.example.com",
                    "tip": {
                      "en_us": "workbench",
                      "zh_cn": "工作台",
                      "zh_hk": "工作臺",
                      "zh_tw": "工作臺",
                      "ja_jp": "作業台",
                      "fr_fr": "Table de travail",
                      "it_it": "banco di lavoro",
                      "de_de": "Werkbank",
                      "ru_ru": "верстак",
                      "th_th": "โต๊ะทำงาน",
                      "es_es": "banco de trabajo",
                      "ko_kr": "작업대"
                    }
                  }
                },
                "start": {
                  "timestamp": "1675454764000",
                  "is_all_day": true
                },
                "mode": 2,
                "is_milestone": false,
                "custom_fields": [
                  {
                    "guid": "73b21903-0041-4796-a11e-f8be919a7063",
                    "number_value": "10.23",
                    "member_value": [
                      {
                        "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                        "type": "user",
                        "name": "张明德（明德）"
                      }
                    ],
                    "datetime_value": "1698192000000",
                    "single_select_value": "73b21903-0041-4796-a11e-f8be919a7063",
                    "multi_select_value": [
                      "73b21903-0041-4796-a11e-f8be919a7063"
                    ],
                    "text_value": "文本类型字段值。可以输入一段文本。空字符串表示清空。"
                  }
                ]
              },
              "update_fields": [
                "summary"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateTaskRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.UpdateFields);
        Assert.NotNull(requestBody.Task);
        Assert.NotEmpty(requestBody.Task.Summary!);
        Assert.NotNull(requestBody.Task.Due);
        Assert.NotNull(requestBody.Task.CustomFields);
        Assert.NotNull(requestBody.Task.CustomComplete);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.UpdateTaskAsync(string, UpdateTaskRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateTaskAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee",
                            "name": "张明德（明德）"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "assignee",
                                "name": "张明德（明德）"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "editor",
                                    "name": "张明德（明德）"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://www.example.com",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1,
                        "is_milestone": false,
                        "custom_fields": [
                            {
                                "guid": "a4f648d7-76ef-477f-bc8e-0601b5a60093",
                                "type": "number",
                                "number_value": "10.23",
                                "datetime_value": "1687708260000",
                                "member_value": [
                                    {
                                        "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                        "type": "user",
                                        "role": "editor",
                                        "name": "张明德（明德）"
                                    }
                                ],
                                "single_select_value": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                "multi_select_value": [
                                    "4216f79b-3fda-4dc6-a0c4-a16022e47152"
                                ],
                                "name": "优先级",
                                "text_value": "这是一段文本介绍。"
                            }
                        ],
                        "dependencies": [
                            {
                                "type": "next",
                                "task_guid": "93b7bd05-35e6-4371-b3c9-6b7cbd7100c0"
                            }
                        ],
                        "assignee_related": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "completed_at": "1675742789470"
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotEmpty(result.Data.Task.Summary!);
        Assert.NotNull(result.Data.Task.CustomFields);
        Assert.NotEmpty(result.Data.Task.CustomFields[0].Name!);
        Assert.NotNull(result.Data.Task.AssigneeRelateds);
        Assert.NotNull(result.Data.Task.Dependencies);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.GetTaskByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTaskByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee",
                            "name": "张明德（明德）"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "assignee",
                                "name": "张明德（明德）"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "assignee",
                                    "name": "张明德（明德）"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://www.example.com",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1,
                        "is_milestone": false,
                        "custom_fields": [
                            {
                                "guid": "a4f648d7-76ef-477f-bc8e-0601b5a60093",
                                "type": "number",
                                "number_value": "10.23",
                                "datetime_value": "1687708260000",
                                "member_value": [
                                    {
                                        "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                        "type": "user",
                                        "role": "editor",
                                        "name": "张明德（明德）"
                                    }
                                ],
                                "single_select_value": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                "multi_select_value": [
                                    "4216f79b-3fda-4dc6-a0c4-a16022e47152"
                                ],
                                "name": "优先级",
                                "text_value": "这是一段文本介绍。"
                            }
                        ],
                        "dependencies": [
                            {
                                "type": "next",
                                "task_guid": "93b7bd05-35e6-4371-b3c9-6b7cbd7100c0"
                            }
                        ],
                        "assignee_related": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "completed_at": "1675742789470"
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotEmpty(result.Data.Task.Summary!);
        Assert.NotNull(result.Data.Task.CustomFields);
        Assert.NotEmpty(result.Data.Task.CustomFields[0].Name!);
        Assert.NotNull(result.Data.Task.AssigneeRelateds);
        Assert.NotNull(result.Data.Task.Dependencies);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.DeleteTaskByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteTaskByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.AddMembersByIdAsync(string, AddMembersRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddMembersByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "members": [
                {
                  "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "user",
                  "role": "assignee",
                  "name": "张明德（明德）"
                }
              ],
              "client_token": "6d99f59c-4d7d-4452-98d6-3d0556393cf6"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<AddMembersRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Members);
        Assert.NotEmpty(requestBody.Members[0].Id);
        Assert.NotEmpty(requestBody.ClientToken!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.AddMembersByIdAsync(string, AddMembersRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddMembersByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee",
                            "name": "张明德（明德）"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "assignee",
                                "name": "张明德（明德）"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "assignee",
                                    "name": "张明德（明德）"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://www.example.com",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1,
                        "is_milestone": false,
                        "custom_fields": [
                            {
                                "guid": "a4f648d7-76ef-477f-bc8e-0601b5a60093",
                                "type": "number",
                                "number_value": "10.23",
                                "datetime_value": "1687708260000",
                                "member_value": [
                                    {
                                        "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                        "type": "user",
                                        "role": "editor",
                                        "name": "张明德（明德）"
                                    }
                                ],
                                "single_select_value": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                "multi_select_value": [
                                    "4216f79b-3fda-4dc6-a0c4-a16022e47152"
                                ],
                                "name": "优先级",
                                "text_value": "这是一段文本介绍。"
                            }
                        ],
                        "dependencies": [
                            {
                                "type": "next",
                                "task_guid": "93b7bd05-35e6-4371-b3c9-6b7cbd7100c0"
                            }
                        ],
                        "assignee_related": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "completed_at": "1675742789470"
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotEmpty(result.Data.Task.Summary!);
        Assert.NotNull(result.Data.Task.CustomFields);
        Assert.NotEmpty(result.Data.Task.CustomFields[0].Name!);
        Assert.NotNull(result.Data.Task.AssigneeRelateds);
        Assert.NotNull(result.Data.Task.Dependencies);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.RemoveMembersByIdAsync(string, RemoveMembersRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveMembersByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "members": [
                {
                  "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "user",
                  "role": "assignee"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RemoveMembersRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Members);
        Assert.NotEmpty(requestBody.Members[0].Id);
        Assert.NotEmpty(requestBody.Members[0].Type!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.RemoveMembersByIdAsync(string, RemoveMembersRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveMembersByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "editor"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "editor"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://www.example.com",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotEmpty(result.Data.Task.Summary!);
        Assert.NotNull(result.Data.Task.Attachments);
        Assert.NotEmpty(result.Data.Task.Attachments[0].Name!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.GetTaskListsByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTaskListsByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "tasklists": [
                        {
                            "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                            "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskGuidTaskListsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Tasklists);
        Assert.NotEmpty(result.Data.Tasklists[0].TasklistGuid!);
        Assert.NotEmpty(result.Data.Tasklists[0].SectionGuid!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.AddTaskListsByIdAsync(string, AddTasklistRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddTaskListsByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "tasklist_guid": "d300a75f-c56a-4be9-80d1-e47653028ceb",
              "section_guid": "d300a75f-c56a-4be9-80d1-e47653028ceb"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<AddTasklistRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.TasklistGuid!);
        Assert.NotEmpty(requestBody.SectionGuid!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.AddTaskListsByIdAsync(string, AddTasklistRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddTaskListsByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "assignee"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "assignee"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://support.foo.com/internal/bar",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<AddTaskListResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotNull(result.Data.Task.Due);
        Assert.NotEmpty(result.Data.Task.Guid!);
        Assert.NotEmpty(result.Data.Task.Summary!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.RemoveTaskListsByIdAsync(string, RemoveTasklistRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveTaskListsByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "tasklist_guid": "d300a75f-c56a-4be9-80d1-e47653028ceb"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RemoveTasklistRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.TasklistGuid);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.RemoveTaskListsByIdAsync(string, RemoveTasklistRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveTaskListsByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "assignee"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "assignee"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://support.foo.com/internal/bar",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RemoveTaskListResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotNull(result.Data.Task.Due);
        Assert.NotEmpty(result.Data.Task.Guid!);
        Assert.NotEmpty(result.Data.Task.Summary!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.AddTaskReminderByIdAsync(string, AddTaskReminderRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddTaskReminderByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "reminders": [
                {
                  "relative_fire_minute": 30
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<AddTaskReminderRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Reminders);
        Assert.Equal(requestBody.Reminders[0].RelativeFireMinute, 30);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.AddTaskReminderByIdAsync(string, AddTaskReminderRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddTaskReminderByIdAsync_Result()
    {
        string resultStr = """
            {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "assignee"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "editor"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://www.example.com",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1
                    }
                }
            }          
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<AddTaskReminderResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotNull(result.Data.Task.Due);
        Assert.NotEmpty(result.Data.Task.Guid!);
        Assert.NotEmpty(result.Data.Task.Summary!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.RemoveTaskReminderByIdAsync(string, RemoveReminderRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveTaskReminderByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "reminder_ids": [
                "7202449098622894100"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RemoveReminderRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ReminderIds);
        Assert.NotEmpty(requestBody.ReminderIds[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.RemoveTaskReminderByIdAsync(string, RemoveReminderRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveTaskReminderByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "task": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "assignee"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "assignee"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://support.foo.com/internal/bar",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RemoveTaskReminderResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Task);
        Assert.NotNull(result.Data.Task.Due);
        Assert.NotEmpty(result.Data.Task.Guid!);
        Assert.NotEmpty(result.Data.Task.Summary!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.AddTaskDependenciesByIdAsync(string, AddTaskDependenciesRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddTaskDependenciesByIdAsync_RequestBody()
    {
        string bodyStr = """
                {
                  "dependencies": [
                    {
                      "type": "next",
                      "task_guid": "93b7bd05-35e6-4371-b3c9-6b7cbd7100c0"
                    }
                  ]
                }
                """;
        var requestBody = JsonSerializer.Deserialize<AddTaskDependenciesRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Dependencies!);
        Assert.NotEmpty(requestBody.Dependencies![0].Type);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.AddTaskDependenciesByIdAsync(string, AddTaskDependenciesRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddTaskDependenciesByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "dependencies": [
                        {
                            "type": "next",
                            "task_guid": "93b7bd05-35e6-4371-b3c9-6b7cbd7100c0"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskDependenciesOpreationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Dependencies!);
        Assert.NotEmpty(result.Data.Dependencies![0].TaskGuid);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.RemoveTaskDependenciesByIdAsync(string, RemoveTaskDependenciesRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveTaskDependenciesByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "dependencies": [
                {
                  "task_guid": "93b7bd05-35e6-4371-b3c9-6b7cbd7100c0"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RemoveTaskDependenciesRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Dependencies!);
        Assert.NotEmpty(requestBody.Dependencies![0].TaskGuid);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.RemoveTaskDependenciesByIdAsync(string, RemoveTaskDependenciesRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveTaskDependenciesByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "dependencies": [
                        {
                            "type": "next",
                            "task_guid": "93b7bd05-35e6-4371-b3c9-6b7cbd7100c0"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskDependenciesOpreationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Dependencies!);
        Assert.NotEmpty(result.Data.Dependencies![0].TaskGuid);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.CreateSubTaskAsync(string, CreateSubTaskRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateSubTaskAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "summary": "针对全年销售进行一次复盘",
              "description": "需要事先阅读复盘总结文档",
              "due": {
                "timestamp": "1675454764000",
                "is_all_day": true
              },
              "origin": {
                "platform_i18n_name": {
                  "en_us": "workbench",
                  "zh_cn": "工作台",
                  "zh_hk": "工作臺",
                  "zh_tw": "工作臺",
                  "ja_jp": "作業台",
                  "fr_fr": "Table de travail",
                  "it_it": "banco di lavoro",
                  "de_de": "Werkbank",
                  "ru_ru": "верстак",
                  "th_th": "โต๊ะทำงาน",
                  "es_es": "banco de trabajo",
                  "ko_kr": "작업대"
                },
                "href": {
                  "url": "https://www.example.com",
                  "title": "反馈一个问题，需要协助排查"
                }
              },
              "extra": "dGVzdA==",
              "completed_at": "1675742789470",
              "members": [
                {
                  "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "user",
                  "role": "assignee"
                }
              ],
              "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
              "custom_complete": {
                "pc": {
                  "href": "https://www.example.com",
                  "tip": {
                    "en_us": "workbench",
                    "zh_cn": "工作台",
                    "zh_hk": "工作臺",
                    "zh_tw": "工作臺",
                    "ja_jp": "作業台",
                    "fr_fr": "Table de travail",
                    "it_it": "banco di lavoro",
                    "de_de": "Werkbank",
                    "ru_ru": "верстак",
                    "th_th": "โต๊ะทำงาน",
                    "es_es": "banco de trabajo",
                    "ko_kr": "작업대"
                  }
                },
                "ios": {
                  "href": "https://www.example.com",
                  "tip": {
                    "en_us": "workbench",
                    "zh_cn": "工作台",
                    "zh_hk": "工作臺",
                    "zh_tw": "工作臺",
                    "ja_jp": "作業台",
                    "fr_fr": "Table de travail",
                    "it_it": "banco di lavoro",
                    "de_de": "Werkbank",
                    "ru_ru": "верстак",
                    "th_th": "โต๊ะทำงาน",
                    "es_es": "banco de trabajo",
                    "ko_kr": "작업대"
                  }
                },
                "android": {
                  "href": "https://www.example.com",
                  "tip": {
                    "en_us": "workbench",
                    "zh_cn": "工作台",
                    "zh_hk": "工作臺",
                    "zh_tw": "工作臺",
                    "ja_jp": "作業台",
                    "fr_fr": "Table de travail",
                    "it_it": "banco di lavoro",
                    "de_de": "Werkbank",
                    "ru_ru": "верстак",
                    "th_th": "โต๊ะทำงาน",
                    "es_es": "banco de trabajo",
                    "ko_kr": "작업대"
                  }
                }
              },
              "tasklists": [
                {
                  "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                  "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                }
              ],
              "client_token": "daa2237f-8310-4707-a83b-52c8a81e0fb7",
              "start": {
                "timestamp": "1675454764000",
                "is_all_day": true
              },
              "reminders": [
                {
                  "relative_fire_minute": 30
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateSubTaskRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotEmpty(requestBody.Tasklists!);
        Assert.NotEmpty(requestBody.Tasklists![0].TasklistGuid!);
        Assert.NotEmpty(requestBody.Tasklists![0].SectionGuid!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.CreateSubTaskAsync(string, CreateSubTaskRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateSubTaskAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "subtask": {
                        "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                        "summary": "进行销售年中总结",
                        "description": "进行销售年中总结",
                        "due": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "reminders": [
                            {
                                "id": "10",
                                "relative_fire_minute": 30
                            }
                        ],
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "assignee"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "类型"
                            }
                        ],
                        "completed_at": "1675742789470",
                        "attachments": [
                            {
                                "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                "name": "foo.jpg",
                                "size": 62232,
                                "resource": {
                                    "type": "task",
                                    "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                },
                                "uploader": {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "assignee"
                                },
                                "is_cover": false,
                                "uploaded_at": "1675742789470"
                            }
                        ],
                        "origin": {
                            "platform_i18n_name": {
                                "en_us": "workbench",
                                "zh_cn": "工作台",
                                "zh_hk": "工作臺",
                                "zh_tw": "工作臺",
                                "ja_jp": "作業台",
                                "fr_fr": "Table de travail",
                                "it_it": "banco di lavoro",
                                "de_de": "Werkbank",
                                "ru_ru": "верстак",
                                "th_th": "โต๊ะทำงาน",
                                "es_es": "banco de trabajo",
                                "ko_kr": "작업대"
                            },
                            "href": {
                                "url": "https://www.example.com",
                                "title": "反馈一个问题，需要协助排查"
                            }
                        },
                        "extra": "dGVzdA==",
                        "tasklists": [
                            {
                                "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            }
                        ],
                        "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                        "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                        "mode": 2,
                        "source": 6,
                        "custom_complete": {
                            "pc": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "ios": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            },
                            "android": {
                                "href": "https://www.example.com",
                                "tip": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                }
                            }
                        },
                        "task_id": "t6272302",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "status": "todo",
                        "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                        "start": {
                            "timestamp": "1675454764000",
                            "is_all_day": true
                        },
                        "subtask_count": 1
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateSubTaskResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Subtask);
        Assert.NotEmpty(result.Data.Subtask.Guid!);
        Assert.NotEmpty(result.Data.Subtask.Summary!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2Task.GetSubTasksPageListByIdAsync(string, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetSubTasksPageListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "guid": "83912691-2e43-47fc-94a4-d512e03984fa",
                            "summary": "进行销售年中总结",
                            "description": "进行销售年中总结",
                            "due": {
                                "timestamp": "1675454764000",
                                "is_all_day": true
                            },
                            "reminders": [
                                {
                                    "id": "10",
                                    "relative_fire_minute": 30
                                }
                            ],
                            "creator": {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "assignee"
                            },
                            "members": [
                                {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "assignee"
                                }
                            ],
                            "completed_at": "1675742789470",
                            "attachments": [
                                {
                                    "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                                    "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                                    "name": "foo.jpg",
                                    "size": 62232,
                                    "resource": {
                                        "type": "task",
                                        "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                    },
                                    "uploader": {
                                        "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                        "type": "user",
                                        "role": "assignee"
                                    },
                                    "is_cover": false,
                                    "uploaded_at": "1675742789470"
                                }
                            ],
                            "origin": {
                                "platform_i18n_name": {
                                    "en_us": "workbench",
                                    "zh_cn": "工作台",
                                    "zh_hk": "工作臺",
                                    "zh_tw": "工作臺",
                                    "ja_jp": "作業台",
                                    "fr_fr": "Table de travail",
                                    "it_it": "banco di lavoro",
                                    "de_de": "Werkbank",
                                    "ru_ru": "верстак",
                                    "th_th": "โต๊ะทำงาน",
                                    "es_es": "banco de trabajo",
                                    "ko_kr": "작업대"
                                },
                                "href": {
                                    "url": "https://www.example.com",
                                    "title": "反馈一个问题，需要协助排查"
                                }
                            },
                            "extra": "dGVzdA==",
                            "tasklists": [
                                {
                                    "tasklist_guid": "cc371766-6584-cf50-a222-c22cd9055004",
                                    "section_guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                                }
                            ],
                            "repeat_rule": "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO,TU,WE,TH,FR",
                            "parent_task_guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                            "mode": 2,
                            "source": 6,
                            "custom_complete": {
                                "pc": {
                                    "href": "https://www.example.com",
                                    "tip": {
                                        "en_us": "workbench",
                                        "zh_cn": "工作台",
                                        "zh_hk": "工作臺",
                                        "zh_tw": "工作臺",
                                        "ja_jp": "作業台",
                                        "fr_fr": "Table de travail",
                                        "it_it": "banco di lavoro",
                                        "de_de": "Werkbank",
                                        "ru_ru": "верстак",
                                        "th_th": "โต๊ะทำงาน",
                                        "es_es": "banco de trabajo",
                                        "ko_kr": "작업대"
                                    }
                                },
                                "ios": {
                                    "href": "https://www.example.com",
                                    "tip": {
                                        "en_us": "workbench",
                                        "zh_cn": "工作台",
                                        "zh_hk": "工作臺",
                                        "zh_tw": "工作臺",
                                        "ja_jp": "作業台",
                                        "fr_fr": "Table de travail",
                                        "it_it": "banco di lavoro",
                                        "de_de": "Werkbank",
                                        "ru_ru": "верстак",
                                        "th_th": "โต๊ะทำงาน",
                                        "es_es": "banco de trabajo",
                                        "ko_kr": "작업대"
                                    }
                                },
                                "android": {
                                    "href": "https://www.example.com",
                                    "tip": {
                                        "en_us": "workbench",
                                        "zh_cn": "工作台",
                                        "zh_hk": "工作臺",
                                        "zh_tw": "工作臺",
                                        "ja_jp": "作業台",
                                        "fr_fr": "Table de travail",
                                        "it_it": "banco di lavoro",
                                        "de_de": "Werkbank",
                                        "ru_ru": "верстак",
                                        "th_th": "โต๊ะทำงาน",
                                        "es_es": "banco de trabajo",
                                        "ko_kr": "작업대"
                                    }
                                }
                            },
                            "task_id": "t6272302",
                            "created_at": "1675742789470",
                            "updated_at": "1675742789470",
                            "status": "todo",
                            "url": "https://applink.feishu.cn/client/todo/detail?guid=70577c8f-91ab-4c91-b359-a21a751054e8&suite_entity_num=t192012",
                            "start": {
                                "timestamp": "1675454764000",
                                "is_all_day": true
                            },
                            "subtask_count": 1
                        }
                    ],
                    "page_token": "aWQ9NzEwMjMzMjMxMDE=",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<SubTaskInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.PageToken!);
        Assert.NotEmpty(result.Data.Items);
        Assert.NotEmpty(result.Data.Items[0].Guid!);
        Assert.NotNull(result.Data.Items[0].Due);
    }
}
