using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Infrastructure.DB;

public class ResourceContext : DbContext
{
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ResourceGroup> ResourceGroups { get; set; }
    public DbSet<ResourceTree> ResourceTrees { get; set; }
    public DbSet<ResourceType> ResourceTypes { get; set; }
    public DbSet<ResourceToGroup> ResourceToGroups { get; set; }
    public DbSet<ResourceStatus> ResourceStatuses { get; set; }
    public DbSet<ResourceEvent> ResourceEvents { get; set; }


    public ResourceContext(DbContextOptions<ResourceContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Resource>().HasKey(e => e.ResourceId);
        modelBuilder.Entity<Resource>().Property(e => e.ResourceId).ValueGeneratedOnAdd();
        modelBuilder.Entity<Resource>().HasIndex(e => e.TenantCode);
        modelBuilder.Entity<Resource>().HasIndex(e => new { e.TenantCode, e.ResourceCode }).IsUnique();
        modelBuilder.Entity<Resource>()
        .HasOne<ResourceType>()
        .WithMany()
        .HasForeignKey(e => e.ResourceTypeId)
        .HasPrincipalKey(e => e.ResourceTypeId);


        modelBuilder.Entity<ResourceGroup>().HasKey(e => e.ResourceGroupId);
        modelBuilder.Entity<ResourceGroup>().Property(e => e.ResourceGroupId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceGroup>().HasIndex(e => e.TenantCode);
        modelBuilder.Entity<ResourceGroup>().HasIndex(e => new { e.TenantCode, e.ResourceGroupCode }).IsUnique();
        modelBuilder.Entity<ResourceGroup>()
        .HasOne<ResourceGroup>()
        .WithMany()
        .HasForeignKey(e => e.ParentResourceGroupId)
        .HasPrincipalKey(e => e.ResourceGroupId);   

        modelBuilder.Entity<ResourceType>().HasKey(e => e.ResourceTypeId);
        modelBuilder.Entity<ResourceType>().Property(e => e.ResourceTypeId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceType>().HasIndex(e => e.TenantCode);
        modelBuilder.Entity<ResourceType>().HasIndex(e => new { e.TenantCode, e.ResourceTypeCode }).IsUnique();

        modelBuilder.Entity<ResourceToGroup>().HasKey(e => e.ResourceToGroupId);
        modelBuilder.Entity<ResourceToGroup>().Property(e => e.ResourceToGroupId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceToGroup>().HasIndex(e => e.TenantCode);

        modelBuilder.Entity<ResourceTree>().HasKey(e => e.ResourceTreeId);
        modelBuilder.Entity<ResourceTree>().Property(e => e.ResourceTreeId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceTree>().HasIndex(e => e.TenantCode);

        modelBuilder.Entity<ResourceStatus>().HasKey(e => e.ResourceStatusId);
        modelBuilder.Entity<ResourceStatus>().Property(e => e.ResourceStatusId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceStatus>().HasIndex(e => e.TenantCode);
        modelBuilder.Entity<ResourceStatus>().HasIndex(e => new { e.TenantCode, e.ResourceStatusCode }).IsUnique();        

        modelBuilder.Entity<ResourceEvent>().HasKey(e => e.ResourceEventId);
        modelBuilder.Entity<ResourceEvent>().Property(e => e.ResourceEventId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceEvent>().HasIndex(e => e.TenantCode);

    }
}

