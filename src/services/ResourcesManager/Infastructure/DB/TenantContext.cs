using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.DataModel.Tenants;

namespace ResourcesManager.Infrastructure.DB;

public class TenantContext : DbContext
{

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantHierarchy> TenantsHierarchy { get; set; }
    public DbSet<TenantType> TenantsTypes { get; set; }

    public DbSet<User> Users { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<UserUserGroup> UserUserGroups { get; set; }

    public DbSet<Role> Roles { get; set; }
    public DbSet<RoleUser> RoleUsers { get; set; }
    public DbSet<RoleGroup> RoleGroups { get; set; }

    public TenantContext(DbContextOptions<TenantContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>().HasKey(e => e.Id);
        modelBuilder.Entity<Tenant>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<TenantHierarchy>().HasKey(e => e.Id);
        modelBuilder.Entity<TenantHierarchy>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<TenantType>().HasKey(e => e.Id);
        modelBuilder.Entity<TenantType>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<User>().HasKey(e => e.Id);
        modelBuilder.Entity<User>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<UserGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<UserGroup>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<UserUserGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<UserUserGroup>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<Role>().HasKey(e => e.Id);
        modelBuilder.Entity<Role>().Property(e => e.Id).ValueGeneratedOnAdd();  

        modelBuilder.Entity<RoleUser>().HasKey(e => e.Id);
        modelBuilder.Entity<RoleUser>().Property(e => e.Id).ValueGeneratedOnAdd();  

        modelBuilder.Entity<RoleGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<RoleGroup>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<Tenant>()
            .HasOne<TenantType>()
            .WithMany()
            .HasForeignKey(t => t.TenantTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TenantHierarchy>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(th => th.TenantParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TenantHierarchy>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(th => th.TenantChildId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserUserGroup>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(uug => uug.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserUserGroup>()
            .HasOne<UserGroup>()
            .WithMany()
            .HasForeignKey(uug => uug.UserGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserGroup>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(ug => ug.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoleUser>()
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(ru => ru.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoleUser>()
            .HasOne<User>()            
            .WithMany()
            .HasForeignKey(ru => ru.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoleGroup>()
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(rg => rg.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoleGroup>()
            .HasOne<UserGroup>()
            .WithMany()
            .HasForeignKey(rg => rg.UserGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Role>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

