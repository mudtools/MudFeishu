// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Bitable;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests.Bitable;

/// <summary>
/// 用于测试多维表格记录相关接口
/// </summary>
public class IFeishuV1BitableRecordTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 新增记录
    [Fact]
    public void TestAddRecordAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "fields": {
                "任务名称": "拜访潜在客户",
                "条码": "+$$3170930509104X512356",
                "工时": 10,
                "货币": 3,
                "评分": 3,
                "进度": 0.25,
                "单选": "选项1",
                "多选": [
                  "选项1",
                  "选项2"
                ],
                "日期": 1674206443000,
                "复选框": true,
                "人员": [
                  {
                    "id": "ou_2910013f1e6456f16a0ce75ede9abcef"
                  },
                  {
                    "id": "ou_e04138c9633dd0d2ea166d79f54abcef"
                  }
                ],
                "群组": [
                  {
                    "id": "oc_cd07f55f14d6f4a4f1b51504e7e97f48"
                  }
                ],
                "电话号码": "1302616xxxx",
                "超链接": {
                  "text": "飞书多维表格官网",
                  "link": "https://www.feishu.cn/product/base"
                },
                "附件": [
                  {
                    "file_token": "DRiFbwaKsoZaLax4WKZbEGCccoe"
                  },
                  {
                    "file_token": "BZk3bL1Enoy4pzxaPL9bNeKqcLe"
                  },
                  {
                    "file_token": "EmL4bhjFFovrt9xZgaSbjJk9c1b"
                  },
                  {
                    "file_token": "Vl3FbVkvnowlgpxpqsAbBrtFcrd"
                  }
                ],
                "单向关联": [
                  "recHTLvO7x",
                  "recbS8zb2m"
                ],
                "双向关联": [
                  "recHTLvO7x",
                  "recbS8zb2m"
                ],
                "地理位置": "116.397755,39.903179"
              }
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RecordOpsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.Fields);
    }

    [Fact]
    public void TestAddRecordAsyncResult()
    {
        string resultStr = """
                        {
              "code": 0,
              "data": {
                "record": {
                  "fields": {
                    "任务名称": "维护客户关系",
                    "创建日期": 1674206443000,
                    "截止日期": 1674206443000
                  },
                  "id": "recusutYZm4ulo",
                  "record_id": "recusutYZm4ulo"
                }
              },
              "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RecordOpsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Record);
    }
    #endregion

    #region 更新记录
    [Fact]
    public void TestUpdateRecordAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "fields": {
                "索引": "索引列文本类型",
                "文本": "文本内容",
                "条码": "qawqe",
                "数字": 100,
                "单选": "选项3",
                "多选": [
                  "选项1",
                  "选项2"
                ],
                "货币": 3,
                "评分": 3,
                "进度": 0.25,
                "日期": 1674206443000,
                "复选框": true,
                "人员": [
                  {
                    "id": "ou_2910013f1e6456f16a0ce75ede950a0a"
                  },
                  {
                    "id": "ou_e04138c9633dd0d2ea166d79f548ab5d"
                  }
                ],
                "群组": [
                  {
                    "id": "oc_cd07f55f14d6f4a4f1b51504e7e97f48"
                  }
                ],
                "电话号码": "13026162666",
                "超链接": {
                  "text": "飞书多维表格官网",
                  "link": "https://www.feishu.cn/product/base"
                },
                "附件": [
                  {
                    "file_token": "Vl3FbVkvnowlgpxpqsAbBrtFcrd"
                  }
                ],
                "单向关联": [
                  "recHTLvO7x",
                  "recbS8zb2m"
                ],
                "双向关联": [
                  "recHTLvO7x",
                  "recbS8zb2m"
                ],
                "地理位置": "116.397755,39.903179"
              }
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RecordOpsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.Fields);
    }

    [Fact]
    public void TestUpdateRecordAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {
                    "record": {
                        "fields": {
                            "人员": [
                                {
                                    "id": "ou_2910013f1e6456f16a0ce75ede950a0a"
                                },
                                {
                                    "id": "ou_e04138c9633dd0d2ea166d79f548ab5d"
                                }
                            ],
                            "群组": [
                                {
                                    "id": "oc_cd07f55f14d6f4a4f1b51504e7e97f48"
                                }
                            ],
                            "单向关联": [
                                "recHTLvO7x",
                                "recbS8zb2m"
                            ],
                            "单选": "选项3",
                            "双向关联": [
                                "recHTLvO7x",
                                "recbS8zb2m"
                            ],
                            "地理位置": "116.397755,39.903179",
                            "复选框": true,
                            "多行文本": "多行文本内容",
                            "多选": [
                                "选项1",
                                "选项2"
                            ],
                            "数字": 100,
                            "日期": 1674206443000,
                            "条码": "qawqe",
                            "电话号码": "13026162666",
                            "索引": "索引列多行文本类型",
                            "超链接": {
                                "link": "https://www.feishu.cn/product/base",
                                "text": "飞书多维表格官网"
                            },
                            "附件": [
                                {
                                    "file_token": "Vl3FbVkvnowlgpxpqsAbBrtFcrd"
                                }
                            ],
                            "评分": 3,
                            "货币": 3,
                            "进度": 0.25
                        },
                        "id": "reclAqylTN",
                        "record_id": "reclAqylTN"
                    }
                },
                "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RecordOpsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Record);
    }
    #endregion

    #region 查询记录
    [Fact]
    public void TestQueryRecordsPageListAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "view_id": "vewqhz51lk",
              "field_names": [
                "字段1",
                "字段2"
              ],
              "sort": [
                {
                  "field_name": "多行文本",
                  "desc": true
                }
              ],
              "filter": {
                "conjunction": "and",
                "conditions": [
                  {
                    "field_name": "职位",
                    "operator": "is",
                    "value": [
                      "初级销售员"
                    ]
                  },
                  {
                    "field_name": "销售额",
                    "operator": "isGreater",
                    "value": [
                      "10000.0"
                    ]
                  }
                ]
              },
              "automatic_fields": false
            }
            """;
        var requestBody = JsonSerializer.Deserialize<QueryRecordsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestQueryRecordsPageListAsyncResult()
    {
        string resultStr = """
                        {
                "code":0,
                "data":{
                    "has_more":false,
                    "items":[
                        {
                            "created_by":{
                                "avatar_url":"https://internal-api-lark-file.feishu.cn/static-resource/v1/06d568cb-f464-4c2e-bd03-76512c545c5j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                                "email":"",
                                "en_name":"测试1",
                                "id":"ou_92945f86a98bba075174776959c90eda",
                                "name":"测试1"
                            },
                            "created_time":1691049973000,
                            "fields":{
                                "人员":[
                                    {
                                        "avatar_url":"https://internal-api-lark-file.feishu.cn/static-resource/v1/b2-7619-4b8a-b27b-c72d90b06a2j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                                        "email":"zhangsan.leben@bytedance.com",
                                        "en_name":"ZhangSan",
                                        "id":"ou_2910013f1e6456f16a0ce75ede950a0a",
                                        "name":"张三"
                                    },
                                    {
                                        "avatar_url":"https://internal-api-lark-file.feishu.cn/static-resource/v1/v2_q86-fcb6-4f18-85c7-87ca8881e50j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                                        "email":"lisi.00@bytedance.com",
                                        "en_name":"LiSi",
                                        "id":"ou_e04138c9633dd0d2ea166d79f548ab5d",
                                        "name":"李四"
                                    }
                                ],
                                "修改人":[
                                    {
                                        "avatar_url":"https://internal-api-lark-file.feishu.cn/static-resource/v1/06d568cb-f464-4c2e-bd03-76512c545c5j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                                        "email":"",
                                        "en_name":"测试1",
                                        "id":"ou_92945f86a98bba075174776959c90eda",
                                        "name":"测试1"
                                    }
                                ],
                                "创建人":[
                                    {
                                        "avatar_url":"https://internal-api-lark-file.feishu.cn/static-resource/v1/06d568cb-f464-4c2e-bd03-76512c545c5j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                                        "email":"",
                                        "en_name":"测试1",
                                        "id":"ou_92945f86a98bba075174776959c90eda",
                                        "name":"测试1"
                                    }
                                ],
                                "创建时间":1691049973000,
                                "单向关联":{
                                    "link_record_ids":[
                                        "recnVYsuqV"
                                    ]
                                },
                                "单选":"选项1",
                                "双向关联":{
                                    "link_record_ids":[
                                        "recqLvMaXT",
                                        "recrdld32q"
                                    ]
                                },
                                "地理位置":{
                                    "address":"东长安街",
                                    "adname":"东城区",
                                    "cityname":"北京市",
                                    "full_address":"天安门广场，北京市东城区东长安街",
                                    "location":"116.397755,39.903179",
                                    "name":"天安门广场",
                                    "pname":"北京市"
                                },
                                "复选框":true,
                                "多行文本":[
                                    {
                                        "text":"多行文本内容1",
                                        "type":"text"
                                    },
                                    {
                                        "mentionNotify":false,
                                        "mentionType":"User",
                                        "name":"张三",
                                        "text":"@张三",
                                        "token":"ou_2910013f1e6456f16a0ce75ede950a0a",
                                        "type":"mention"
                                    }
                                ],
                                "多选":[
                                    "选项1",
                                    "选项2"
                                ],
                                "数字":2323.2323,
                                "日期":1690992000000,
                                "最后更新时间":1702455191000,
                                "条码":[
                                    {
                                        "text":"123",
                                        "type":"text"
                                    }
                                ],
                                "电话号码":"131xxxx6666",
                                "自动编号":"17",
                                "群组":[
                                    {
                                        "avatar_url":"https://internal-api-lark-file.feishu-boe.cn/static-resource/v1/v2_c8d2cd50-ba29-476f-b7f1-5b5917cb18ej~?image_size=72x72&amp;cut_type=&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                                        "id":"oc_cd07f55f14d6f4a4f1b51504e7e97f48",
                                        "name":"武侠聊天组"
                                    }
                                ],
                                "评分":3,
                                "货币":1,
                                "超链接":{
                                    "link":"https://bitable.feishu.cn",
                                    "text":"飞书多维表格官网"
                                },
                                "进度":0.66,
                                "附件":[
                                    {
                                        "file_token":"Vl3FbVkvnowlgpxpqsAbBrtFcrd",
                                        "name":"飞书.jpeg",
                                        "size":32975,
                                        "tmp_url":"https://open.feishu.cn/open-apis/drive/v1/medias/batch_get_tmp_download_url?file_tokens=Vl3FbVk11owlgpxpqsAbBrtFcrd&amp;extra={\"bitablePerm\":{\"tableId\":\"tblBJyX6jZteblYv\",\"rev\":90}}",
                                        "type":"image/jpeg",
                                        "url":"https://open.feishu.cn/open-apis/drive/v1/medias/Vl3FbVk11owlgpxpqsAbBrtFcrd/download?extra={\"bitablePerm\":{\"tableId\":\"tblBJyX6jZteblYv\",\"rev\":90}}"
                                    }
                                ]
                            },
                            "last_modified_by":{
                                "avatar_url":"https://internal-api-lark-file.feishu.cn/static-resource/v1/06d568cb-f464-4c2e-bd03-76512c545c5j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                                "email":"",
                                "en_name":"测试1",
                                "id":"ou_92945f86a98bba075174776959c90eda",
                                "name":"测试1"
                            },
                            "last_modified_time":1702455191000,
                            "record_id":"recyOaMB2F"
                        }
                    ],
                    "total":1
                },
                "msg":"success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListTotalResult<AppTableRecord>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 删除记录
    [Fact]
    public void TestDeleteRecordAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "deleted": true,
                    "record_id": "recpCsf4ME"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<DeleteRecordResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 新增多条记录
    [Fact]
    public void TestAddRecordsAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "records": [
                {
                  "fields": {
                    "文本": "文本内容",
                    "条码": "qawqe",
                    "数字": 100,
                    "货币": 3,
                    "评分": 3,
                    "进度": 0.25,
                    "单选": "选项1",
                    "多选": [
                      "选项1",
                      "选项2"
                    ],
                    "日期": 1674206443000,
                    "复选框": true,
                    "人员": [
                      {
                        "id": "ou_2910013f1e6456f16a0ce75ede950a0a"
                      },
                      {
                        "id": "ou_e04138c9633dd0d2ea166d79f548ab5d"
                      }
                    ],
                    "群组": [
                      {
                        "id": "oc_cd07f55f14d6f4a4f1b51504e7e97f48"
                      }
                    ],
                    "电话号码": "13026162666",
                    "超链接": {
                      "text": "飞书多维表格官网",
                      "link": "https://www.feishu.cn/product/base"
                    },
                    "附件": [
                      {
                        "file_token": "Vl3FbVkvnowlgpxpqsAbBrtFcrd"
                      }
                    ],
                    "单向关联": [
                      "recHTLvO7x",
                      "recbS8zb2m"
                    ],
                    "双向关联": [
                      "recHTLvO7x",
                      "recbS8zb2m"
                    ],
                    "地理位置": "116.397755,39.903179"
                  }
                },
                {
                  "fields": {
                    "文本": "文本内容2"
                  }
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<AddRecordsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestAddRecordsAsyncResult()
    {
        string resultStr = """
                        {
              "code": 0,
              "data": {
                "records": [
                  {
                    "fields": {
                      "任务名称": "维护客户关系",
                      "创建日期": 1674206443000,
                      "截止日期": 1674206443000
                    },
                    "id": "recusyQbB0fVL5",
                    "record_id": "recusyQbB0fVL5"
                  },
                  {
                    "fields": {
                      "任务名称": "跟进与谈判",
                      "创建日期": 1674206443000,
                      "截止日期": 1674206443000
                    },
                    "id": "recusyQbB0CJjX",
                    "record_id": "recusyQbB0CJjX"
                  }
                ]
              },
              "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RecordsOpsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 更新多条记录
    [Fact]
    public void TestUpdateRecordsAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "records": [
                {
                  "record_id": "reclAqylTN",
                  "fields": {
                    "索引": "索引列多行文本类型",
                    "多行文本": "多行文本内容",
                    "数字": 100,
                    "单选": "选项3",
                    "多选": [
                      "选项1",
                      "选项2"
                    ],
                    "日期": 1674206443000,
                    "条码": "qawqe",
                    "复选框": true,
                    "人员": [
                      {
                        "id": "ou_2910013f1e6456f16a0ce75ede950a0a"
                      },
                      {
                        "id": "ou_e04138c9633dd0d2ea166d79f548ab5d"
                      }
                    ],
                    "群组": [
                      {
                        "id": "oc_cd07f55f14d6f4a4f1b51504e7e97f48"
                      }
                    ],
                    "电话号码": "13026162666",
                    "超链接": {
                      "text": "飞书多维表格官网",
                      "link": "https://www.feishu.cn/product/base"
                    },
                    "附件": [
                      {
                        "file_token": "Vl3FbVkvnowlgpxpqsAbBrtFcrd"
                      }
                    ],
                    "单向关联": [
                      "recHTLvO7x",
                      "recbS8zb2m"
                    ],
                    "双向关联": [
                      "recHTLvO7x",
                      "recbS8zb2m"
                    ],
                    "地理位置": "116.397755,39.903179",
                    "评分": 3,
                    "货币": 3,
                    "进度": 0.25
                  }
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateRecordsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUpdateRecordsAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {
                    "records": [
                        {
                            "fields": {
                                "人员": [
                                    {
                                        "id": "ou_2910013f1e6456f16a0ce75ede950a0a"
                                    },
                                    {
                                        "id": "ou_e04138c9633dd0d2ea166d79f548ab5d"
                                    }
                                ],
                                "群组": [
                                    {
                                        "id": "oc_cd07f55f14d6f4a4f1b51504e7e97f48"
                                    }
                                ],
                                "单向关联": [
                                    "recHTLvO7x",
                                    "recbS8zb2m"
                                ],
                                "单选": "选项3",
                                "双向关联": [
                                    "recHTLvO7x",
                                    "recbS8zb2m"
                                ],
                                "地理位置": "116.397755,39.903179",
                                "复选框": true,
                                "多行文本": "多行文本内容",
                                "多选": [
                                    "选项1",
                                    "选项2"
                                ],
                                "数字": 100,
                                "日期": 1674206443000,
                                "条码": "qawqe",
                                "电话号码": "13026162666",
                                "索引": "索引列多行文本类型",
                                "超链接": {
                                    "link": "https://www.feishu.cn/product/base",
                                    "text": "飞书多维表格官网"
                                },
                                "附件": [
                                    {
                                        "file_token": "Vl3FbVkvnowlgpxpqsAbBrtFcrd"
                                    }
                                ],
                                "评分": 3,
                                "货币": 3,
                                "进度": 0.25
                            },
                            "id": "reclAqylTN",
                            "record_id": "reclAqylTN"
                        }
                    ]
                },
                "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RecordsOpsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 批量获取记录
    [Fact]
    public void TestGetRecordsAsyncRequestBody()
    {
        string bodyStr = """
             {
              "record_ids": [
                "recyOaMB2F",
                "rec111111",
                "recyOaMB2F"
              ],
              "user_id_type": "open_id",
              "with_shared_url": true,
              "automatic_fields": true
            }
            """;
        var requestBody = JsonSerializer.Deserialize<GetRecordsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestGetRecordsAsyncResult()
    {
        string resultStr = """
             {
              "code": 0,
              "msg": "success",
              "data": {
                "forbidden_record_ids": [
                  "recyOaMB2F"
                ],
                "absent_record_ids": [
                  "rec111111"
                ],
                "records": [
                  {
                    "created_by": {
                      "avatar_url": "https://internal-api-lark-file.feishu.cn/static-resource/v1/06d568cb-f464-4c2e-bd03-76512c545c5j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                      "email": "",
                      "en_name": "Min Zhang",
                      "id": "ou_92945f86a98bba075174776959c90eda",
                      "name": "张敏"
                    },
                    "created_time": 1691049973000,
                    "fields": {
                      "人员": [
                        {
                          "avatar_url": "https://internal-api-lark-file.feishu.cn/static-resource/v1/b2-7619-4b8a-b27b-c72d90b06a2j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                          "email": "minzhang.leben@bytedance.com",
                          "en_name": "Min Zhang",
                          "id": "ou_2910013f1e6456f16a0ce75ede950a0a",
                          "name": "张敏"
                        },
                        {
                          "avatar_url": "https://internal-api-lark-file.feishu.cn/static-resource/v1/v2_q86-fcb6-4f18-85c7-87ca8881e50j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                          "email": "minzhang.00@bytedance.com",
                          "en_name": "Min Zhang",
                          "id": "ou_e04138c9633dd0d2ea166d79f548ab5d",
                          "name": "张敏"
                        }
                      ],
                      "修改人": [
                        {
                          "avatar_url": "https://internal-api-lark-file.feishu.cn/static-resource/v1/06d568cb-f464-4c2e-bd03-76512c545c5j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                          "email": "",
                          "en_name": "Min Zhang",
                          "id": "ou_92945f86a98bba075174776959c90eda",
                          "name": "张敏"
                        }
                      ],
                      "创建人": [
                        {
                          "avatar_url": "https://internal-api-lark-file.feishu.cn/static-resource/v1/06d568cb-f464-4c2e-bd03-76512c545c5j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                          "email": "",
                          "en_name": "Min Zhang",
                          "id": "ou_92945f86a98bba075174776959c90eda",
                          "name": "张敏"
                        }
                      ],
                      "创建时间": 1691049973000,
                      "单向关联": {
                        "link_record_ids": [
                          "recnVYsuqV"
                        ]
                      },
                      "单选": "选项1",
                      "双向关联": {
                        "link_record_ids": [
                          "recqLvMaXT",
                          "recrdld32q"
                        ]
                      },
                      "地理位置": {
                        "address": "东长安街",
                        "adname": "东城区",
                        "cityname": "北京市",
                        "full_address": "天安门广场，北京市东城区东长安街",
                        "location": "116.397755,39.903179",
                        "name": "天安门广场",
                        "pname": "北京市"
                      },
                      "复选框": true,
                      "多行文本": [
                        {
                          "text": "多行文本内容1",
                          "type": "text"
                        },
                        {
                          "mentionNotify": false,
                          "mentionType": "User",
                          "name": "张敏",
                          "text": "@张敏",
                          "token": "ou_2910013f1e6456f16a0ce75ede950a0a",
                          "type": "mention"
                        }
                      ],
                      "多选": [
                        "选项1",
                        "选项2"
                      ],
                      "数字": 2323.2323,
                      "日期": 1690992000000,
                      "最后更新时间": 1702455191000,
                      "条码": [
                        {
                          "text": "123",
                          "type": "text"
                        }
                      ],
                      "电话号码": "131xxxx6666",
                      "自动编号": "17",
                      "群组": [
                        {
                          "avatar_url": "https://internal-api-lark-file.feishu-boe.cn/static-resource/v1/v2_c8d2cd50-ba29-476f-b7f1-5b5917cb18ej~?image_size=72x72&amp;cut_type=&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                          "id": "oc_cd07f55f14d6f4a4f1b51504e7e97f48",
                          "name": "武侠聊天组"
                        }
                      ],
                      "评分": 3,
                      "货币": 1,
                      "超链接": {
                        "link": "https://bitable.feishu.cn",
                        "text": "飞书多维表格官网"
                      },
                      "进度": 0.66,
                      "附件": [
                        {
                          "file_token": "Vl3FbVkvnowlgpxpqsAbBrtFcrd",
                          "name": "飞书.jpeg",
                          "size": 32975,
                          "tmp_url": "https://open.feishu.cn/open-apis/drive/v1/medias/batch_get_tmp_download_url?file_tokens=Vl3FbVk11owlgpxpqsAbBrtFcrd&amp;extra={\"bitablePerm\":{\"tableId\":\"tblBJyX6jZteblYv\",\"rev\":90}}",
                          "type": "image/jpeg",
                          "url": "https://open.feishu.cn/open-apis/drive/v1/medias/Vl3FbVk11owlgpxpqsAbBrtFcrd/download?extra={\"bitablePerm\":{\"tableId\":\"tblBJyX6jZteblYv\",\"rev\":90}}"
                        }
                      ]
                    },
                    "last_modified_by": {
                      "avatar_url": "https://internal-api-lark-file.feishu.cn/static-resource/v1/06d568cb-f464-4c2e-bd03-76512c545c5j~?image_size=72x72&amp;cut_type=default-face&amp;quality=&amp;format=jpeg&amp;sticker_format=.webp",
                      "email": "",
                      "en_name": "Min Zhang",
                      "id": "ou_92945f86a98bba075174776959c90eda",
                      "name": "张敏"
                    },
                    "last_modified_time": 1702455191000,
                    "record_id": "recyOaMB2F",
                    "shared_url": "https://example.feishu.cn/record/KBcNrNtpWePAlscCvdmb6ZcSc5b"
                  }
                ]
              }
            }      
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetRecordsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 批量删除记录
    [Fact]
    public void TestDeleteRecordsAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "records": [
                "recwNXzPQv"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DeleteRecordsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestDeleteRecordsAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "records": [
                        {
                            "deleted": true,
                            "record_id": "recpCsf4ME"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<DeleteRecordsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion
}
