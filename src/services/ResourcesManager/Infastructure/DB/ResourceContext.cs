using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.DataModel;

namespace ResourcesManager.Infrastructure.DB;

public class ResourceContext : DbContext
{
    public DbSet<Resource> Resources { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<ResourceGroup> ResourceGroups { get; set; }
    public DbSet<ResourceHierarchy> ResourcesHierarchy { get; set; }
    public DbSet<ResourceType> ResourceTypes { get; set; }
    public DbSet<ResourceTypeHierarchy> ResourceTypesHierarchy { get; set; }
    public DbSet<ResourceResourceGroup> ResourceResourceGroups { get; set; }
    public DbSet<ResourceStatus> ResourceStatuses { get; set; }
    public DbSet<ResourceStatusHistory> ResourceStatusHistories { get; set; }

    public ResourceContext(DbContextOptions<ResourceContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Resource>().HasKey(e => e.Id);
        modelBuilder.Entity<Resource>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<Tenant>().HasKey(e => e.Id);
        modelBuilder.Entity<Tenant>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceGroup>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceType>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceType>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceTypeHierarchy>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceTypeHierarchy>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceResourceGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceResourceGroup>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceHierarchy>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceHierarchy>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceStatus>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceStatus>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceStatusHistory>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceStatusHistory>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceStatusHistory>()
            .HasOne<Resource>()
            .WithMany()
            .HasForeignKey(rsh => rsh.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResourceStatusHistory>()
            .HasOne<ResourceStatus>()
            .WithMany()
            .HasForeignKey(rsh => rsh.ResourceStatusEnumId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Resource>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceHierarchy>()
            .HasOne<Resource>()
            .WithMany()
            .HasForeignKey(rh => rh.ParentResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceHierarchy>()
            .HasOne<Resource>()
            .WithMany()
            .HasForeignKey(rh => rh.ChildResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResourceTypeHierarchy>()
            .HasOne<ResourceType>()
            .WithMany()
            .HasForeignKey(rth => rth.ResourceTypeParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceTypeHierarchy>()
            .HasOne<ResourceType>()
            .WithMany()
            .HasForeignKey(rth => rth.ResourceTypeChildId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResourceResourceGroup>()
            .HasOne<Resource>()
            .WithMany()
            .HasForeignKey(rrg => rrg.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResourceResourceGroup>()
            .HasOne<ResourceGroup>()
            .WithMany()
            .HasForeignKey(rrg => rrg.ResourceGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Resource>()
            .HasOne<ResourceType>()
            .WithMany()
            .HasForeignKey(r => r.ResourceTypeId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<ResourceGroup>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(rg => rg.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceType>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(rt => rt.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceTypeHierarchy>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(rth => rth.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceHierarchy>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(rh => rh.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceResourceGroup>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(rrg => rrg.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceStatus>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(rs => rs.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResourceStatusHistory>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(rsh => rsh.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

