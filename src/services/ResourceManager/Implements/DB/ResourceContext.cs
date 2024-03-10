using Microsoft.EntityFrameworkCore;
using ResourceManager.Business.DataModel;

namespace ResourceManager.Implements.DB;

public class ResourceContext : DbContext
{
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ResourceGroup> ResourceGroups { get; set; }
    public DbSet<ResourceHierarchy> ResourcesHierarchy { get; set; }
    public DbSet<ResourceType> ResourceTypes { get; set; }
    public DbSet<ResourceTypeHierarchy> ResourceTypesHierarchy { get; set; }
    public DbSet<ResourceResourceGroup> ResourceResourceGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(@"Host=myserver;Username=five0_rm;Password=five0_rm;Database=five0.resources");
    }

}
