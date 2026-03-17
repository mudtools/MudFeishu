namespace FeishuWikiManager.Models;

public class SpaceViewModel
{
    public string SpaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SpaceType { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public DateTime? LastModifiedTime { get; set; }
}

public class NodeTreeViewModel
{
    public string NodeToken { get; set; } = string.Empty;
    public string ObjToken { get; set; } = string.Empty;
    public string ObjType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ParentNodeToken { get; set; }
    public bool HasChildren { get; set; }
    public List<NodeTreeViewModel> Children { get; set; } = new();
    public string? Icon { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? EditTime { get; set; }
    public string? Creator { get; set; }
}

public class CreateDocumentRequest
{
    public string SpaceId { get; set; } = string.Empty;
    public string? ParentNodeToken { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ObjType { get; set; } = "docx";
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string? SpaceId { get; set; }
    public string? NodeId { get; set; }
    public int PageSize { get; set; } = 20;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public bool HasMore { get; set; }
    public string? PageToken { get; set; }
}
