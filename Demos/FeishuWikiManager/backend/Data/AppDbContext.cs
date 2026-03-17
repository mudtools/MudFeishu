using FeishuWikiManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FeishuWikiManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserPreference> UserPreferences { get; set; }
    public DbSet<FavoriteNode> FavoriteNodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OpenId).IsUnique();
            entity.HasIndex(e => e.UnionId);
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Key }).IsUnique();
        });

        modelBuilder.Entity<FavoriteNode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.NodeToken }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
