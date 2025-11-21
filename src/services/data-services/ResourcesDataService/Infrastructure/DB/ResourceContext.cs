using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Infrastructure.DB;

public class ResourceContext : DbContext
{
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ResourceGroup> ResourceGroups { get; set; }
    public DbSet<ResourceHierarchy> ResourcesHierarchy { get; set; }
    public DbSet<ResourceType> ResourceTypes { get; set; }
    public DbSet<ResourceResourceGroup> ResourceResourceGroups { get; set; }
    public DbSet<ResourceStatus> ResourceStatuses { get; set; }
    public DbSet<ResourceStatusStore> ResourceStatusStores { get; set; }


    public ResourceContext(DbContextOptions<ResourceContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Resource>().HasKey(e => e.ResourceId);
        modelBuilder.Entity<Resource>().Property(e => e.ResourceId).ValueGeneratedOnAdd();
        modelBuilder.Entity<Resource>().HasIndex(e => e.TenantCode);
        modelBuilder.Entity<Resource>().HasIndex(e => new { e.TenantCode, e.ResourceCode }).IsUnique();
        modelBuilder.Entity<Resource>().HasForeignKey(e => e.ResourceTypeId);

        modelBuilder.Entity<ResourceGroup>().HasKey(e => e.ResourceGroupId);
        modelBuilder.Entity<ResourceGroup>().Property(e => e.ResourceGroupId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceGroup>().HasIndex(e => e.TenantCode);
        modelBuilder.Entity<ResourceGroup>().HasIndex(e => new { e.TenantCode, e.ResourceGroupCode }).IsUnique();

        modelBuilder.Entity<ResourceType>().HasKey(e => e.ResourceTypeId);
        modelBuilder.Entity<ResourceType>().Property(e => e.ResourceTypeId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceType>().HasIndex(e => e.TenantCode);
        modelBuilder.Entity<ResourceType>().HasIndex(e => new { e.TenantCode, e.ResourceTypeCode }).IsUnique();

        modelBuilder.Entity<ResourceResourceGroup>().HasKey(e => e.ResourceResourceGroupId);
        modelBuilder.Entity<ResourceResourceGroup>().Property(e => e.ResourceResourceGroupId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceResourceGroup>().HasIndex(e => e.TenantCode);

        modelBuilder.Entity<ResourceHierarchy>().HasKey(e => e.ResourceHierarchyId);
        modelBuilder.Entity<ResourceHierarchy>().Property(e => e.ResourceHierarchyId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceHierarchy>().HasIndex(e => e.TenantCode);

        modelBuilder.Entity<ResourceStatus>().HasKey(e => e.ResourceStatusId);
        modelBuilder.Entity<ResourceStatus>().Property(e => e.ResourceStatusId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceStatus>().HasIndex(e => e.TenantCode);
        modelBuilder.Entity<ResourceStatus>().HasIndex(e => new { e.TenantCode, e.ResourceStatusCode }).IsUnique();        

        modelBuilder.Entity<ResourceStatusStore>().HasKey(e => e.ResourceStatusStoreId);
        modelBuilder.Entity<ResourceStatusStore>().Property(e => e.ResourceStatusStoreId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceStatusStore>().HasIndex(e => e.TenantCode);

    }
}

