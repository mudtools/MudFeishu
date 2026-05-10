// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using AutoMapper;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services.Sync;
using TaskManageDemo.Backend.Services.Templates;

namespace TaskManageDemo.Backend.Mapping;

/// <summary>
/// AutoMapper 映射配置
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // TaskSync -> TaskDto
        CreateMap<TaskSync, TaskDto>()
            .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.Members));

        // TaskMemberEntity -> TaskMemberDto
        CreateMap<TaskMemberEntity, TaskMemberDto>()
            .ForMember(dest => dest.FeishuId, opt => opt.MapFrom(src => src.User != null ? src.User.FeishuId : string.Empty))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null));

        // TaskList -> TaskListDto
        CreateMap<TaskList, TaskListDto>()
            .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.Members));

        // TaskListMember -> TaskMemberDto
        CreateMap<TaskListMember, TaskMemberDto>()
            .ForMember(dest => dest.FeishuId, opt => opt.MapFrom(src => src.User != null ? src.User.FeishuId : string.Empty))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "member"));

        // TaskTemplate -> TaskTemplateDto
        CreateMap<TaskTemplate, TaskTemplateDto>();

        // Department -> DepartmentTreeNode
        CreateMap<Department, DepartmentTreeNode>()
            .ForMember(dest => dest.Children, opt => opt.Ignore());

        // User 相关映射
        CreateMap<User, TaskMemberDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "member"));

        // TaskHistory -> TaskHistoryDto
        // 注意：UserName 需要在服务层根据 OperatorId 查询用户表后填充
        CreateMap<TaskHistory, TaskHistoryDto>()
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore());
    }
}
