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
        modelBuilder.Entity<Resource>().Property(e => e.ResourceId).HasColumnType("uuid");
        modelBuilder.Entity<Resource>().Property(e => e.TenantId).HasColumnType("uuid");
        modelBuilder.Entity<Resource>().Property(e => e.ResourceTypeId).HasColumnType("uuid");
        modelBuilder.Entity<Resource>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<Resource>().HasIndex(e => new { e.TenantId, e.ResourceCode }).IsUnique();
        modelBuilder.Entity<Resource>()
        .HasOne<ResourceType>()
        .WithMany()
        .HasForeignKey(e => e.ResourceTypeId)
        .HasPrincipalKey(e => e.ResourceTypeId);

        modelBuilder.Entity<ResourceGroup>().HasKey(e => e.ResourceGroupId);
        modelBuilder.Entity<ResourceGroup>().Property(e => e.ResourceGroupId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceGroup>().Property(e => e.TenantId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceGroup>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<ResourceGroup>().HasIndex(e => new { e.TenantId, e.ResourceGroupCode }).IsUnique();
        modelBuilder.Entity<ResourceGroup>()
        .HasOne<ResourceGroup>()
        .WithMany()
        .HasForeignKey(e => e.ParentResourceGroupId)
        .HasPrincipalKey(e => e.ResourceGroupId);

        modelBuilder.Entity<ResourceType>().HasKey(e => e.ResourceTypeId);
        modelBuilder.Entity<ResourceType>().Property(e => e.ResourceTypeId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceType>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<ResourceType>().Property(e => e.TenantId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceType>().HasIndex(e => new { e.TenantId, e.ResourceTypeCode }).IsUnique();
        modelBuilder.Entity<ResourceType>()
        .HasOne<ResourceType>()
        .WithMany()
        .HasForeignKey(e => e.ParentResourceTypeId)
        .HasPrincipalKey(e => e.ResourceTypeId);

        modelBuilder.Entity<ResourceToGroup>().HasKey(e => e.ResourceToGroupId);
        modelBuilder.Entity<ResourceToGroup>().Property(e => e.ResourceToGroupId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceToGroup>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<ResourceToGroup>().Property(e => e.TenantId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceToGroup>().HasIndex(e => e.ResourceId);
        modelBuilder.Entity<ResourceToGroup>().Property(e => e.ResourceId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceToGroup>().HasIndex(e => e.ResourceGroupId);
        modelBuilder.Entity<ResourceToGroup>().Property(e => e.ResourceGroupId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceToGroup>()
        .HasOne<Resource>()
        .WithMany()
        .HasForeignKey(e => e.ResourceId)
        .HasPrincipalKey(e => e.ResourceId);
        modelBuilder.Entity<ResourceToGroup>()
        .HasOne<ResourceGroup>()
        .WithMany()
        .HasForeignKey(e => e.ResourceGroupId)
        .HasPrincipalKey(e => e.ResourceGroupId);

        modelBuilder.Entity<ResourceTree>().HasKey(e => e.ResourceTreeId);
        modelBuilder.Entity<ResourceTree>().Property(e => e.ResourceTreeId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceTree>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<ResourceTree>().Property(e => e.TenantId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceTree>().HasIndex(e => e.ParentResourceId);
        modelBuilder.Entity<ResourceTree>().Property(e => e.ParentResourceId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceTree>().HasIndex(e => e.ChildResourceId);
        modelBuilder.Entity<ResourceTree>().Property(e => e.ChildResourceId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceTree>()
        .HasOne<Resource>()
        .WithMany()
        .HasForeignKey(e => e.ParentResourceId)
        .HasPrincipalKey(e => e.ResourceId);
        modelBuilder.Entity<ResourceTree>()
        .HasOne<Resource>()
        .WithMany()
        .HasForeignKey(e => e.ChildResourceId)
        .HasPrincipalKey(e => e.ResourceId);

        modelBuilder.Entity<ResourceStatus>().HasKey(e => e.ResourceStatusId);
        modelBuilder.Entity<ResourceStatus>().Property(e => e.ResourceStatusId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceStatus>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<ResourceStatus>().Property(e => e.TenantId).HasColumnType("uuid");

        modelBuilder.Entity<ResourceEvent>().HasKey(e => e.ResourceEventId);
        modelBuilder.Entity<ResourceEvent>().Property(e => e.ResourceEventId).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceEvent>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<ResourceEvent>().Property(e => e.TenantId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceEvent>().HasIndex(e => e.ResourceId);
        modelBuilder.Entity<ResourceEvent>().Property(e => e.ResourceId).HasColumnType("uuid");
        modelBuilder.Entity<ResourceEvent>().HasIndex(e => e.ResourceStatusId);
        modelBuilder.Entity<ResourceEvent>().Property(e => e.ResourceStatusId).HasColumnType("uuid");
    }
}

