using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Infrastructure.DB;

public class ResourceContext : DbContext
{
    public DbSet<Resource> Resources { get; set; }
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
        modelBuilder.Entity<Resource>().HasIndex(e => e.TenantId);

        modelBuilder.Entity<ResourceGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceGroup>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceGroup>().HasIndex(e => e.TenantId);

        modelBuilder.Entity<ResourceType>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceType>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceType>().HasIndex(e => e.TenantId);

        modelBuilder.Entity<ResourceTypeHierarchy>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceTypeHierarchy>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceTypeHierarchy>().HasIndex(e => e.TenantId);

        modelBuilder.Entity<ResourceResourceGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceResourceGroup>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceResourceGroup>().HasIndex(e => e.TenantId);

        modelBuilder.Entity<ResourceHierarchy>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceHierarchy>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceHierarchy>().HasIndex(e => e.TenantId);

        modelBuilder.Entity<ResourceStatus>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceStatus>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceStatus>().HasIndex(e => e.TenantId);

        modelBuilder.Entity<ResourceStatusHistory>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceStatusHistory>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<ResourceStatusHistory>().HasIndex(e => e.TenantId);

        // modelBuilder.Entity<Resource>()
        //     .HasOne<ResourceType>()
        //     .WithMany()
        //     .HasForeignKey(r => r.ResourceTypeId)
        //     .OnDelete(DeleteBehavior.Restrict);

        // modelBuilder.Entity<ResourceStatusHistory>()
        //     .HasOne<Resource>()
        //     .WithMany()
        //     .HasForeignKey(rsh => rsh.ResourceId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // modelBuilder.Entity<ResourceStatusHistory>()
        //     .HasOne<ResourceStatus>()
        //     .WithMany()
        //     .HasForeignKey(rsh => rsh.ResourceStatusId)
        //     .OnDelete(DeleteBehavior.Restrict);

        // modelBuilder.Entity<ResourceHierarchy>()
        //     .HasOne<Resource>()
        //     .WithMany()
        //     .HasForeignKey(rh => rh.ParentResourceId)
        //     .OnDelete(DeleteBehavior.Restrict);

        // modelBuilder.Entity<ResourceHierarchy>()
        //     .HasOne<Resource>()
        //     .WithMany()
        //     .HasForeignKey(rh => rh.ChildResourceId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // modelBuilder.Entity<ResourceTypeHierarchy>()
        //     .HasOne<ResourceType>()
        //     .WithMany()
        //     .HasForeignKey(rth => rth.ResourceTypeParentId)
        //     .OnDelete(DeleteBehavior.Restrict);

        // modelBuilder.Entity<ResourceTypeHierarchy>()
        //     .HasOne<ResourceType>()
        //     .WithMany()
        //     .HasForeignKey(rth => rth.ResourceTypeChildId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // modelBuilder.Entity<ResourceResourceGroup>()
        //     .HasOne<Resource>()
        //     .WithMany()
        //     .HasForeignKey(rrg => rrg.ResourceId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // modelBuilder.Entity<ResourceResourceGroup>()
        //     .HasOne<ResourceGroup>()
        //     .WithMany()
        //     .HasForeignKey(rrg => rrg.ResourceGroupId)
        //     .OnDelete(DeleteBehavior.Cascade);
    }
}

