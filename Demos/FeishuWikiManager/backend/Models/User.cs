using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FeishuWikiManager.Models;

[Index(nameof(OpenId), IsUnique = true)]
public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public string OpenId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? UnionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Avatar { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Mobile { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public string? FeishuAccessToken { get; set; }

    public string? FeishuRefreshToken { get; set; }

    public DateTime? TokenExpiresAt { get; set; }

    public string? TenantKey { get; set; }

    public ICollection<UserPreference> Preferences { get; set; } = new List<UserPreference>();
    public ICollection<FavoriteNode> FavoriteNodes { get; set; } = new List<FavoriteNode>();
}

public class UserPreference
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Value { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

public class FavoriteNode
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SpaceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NodeToken { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ObjToken { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ObjType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
