using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.DataModel.Tenants;

namespace ResourcesManager.Infrastructure.DB;

public class TenantContext : DbContext
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationHierarchy> OrganizationHierarchies { get; set; }
    public DbSet<OrganizationType> OrganizationTypes { get; set; }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantHierarchy> TenantsHierarchy { get; set; }
    public DbSet<TenantType> TenantsTypes { get; set; }

    public DbSet<User> Users { get; set; }
    public DbSet<UserType> UserTypes { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<UserUserGroup> UserUserGroups { get; set; }

    public DbSet<Role> Roles { get; set; }
    public DbSet<RoleType> RoleTypes { get; set; }
    public DbSet<RoleUser> RoleUsers { get; set; }
    public DbSet<RoleUserGroup> RoleGroups { get; set; }

    // Added tenant-scoped status entities
    public DbSet<UserStatus> UserStatuses { get; set; }
    public DbSet<UserStatusHistory> UserStatusHistories { get; set; }

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
        modelBuilder.Entity<User>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<User>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<UserGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<UserGroup>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<UserGroup>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<UserUserGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<UserUserGroup>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<UserUserGroup>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<UserUserGroup>().HasIndex(e => e.UserId);
        modelBuilder.Entity<UserUserGroup>().HasIndex(e => e.UserGroupId);

        modelBuilder.Entity<Role>().HasKey(e => e.Id);
        modelBuilder.Entity<Role>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<Role>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<RoleUser>().HasKey(e => e.Id);
        modelBuilder.Entity<RoleUser>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<RoleUser>().HasIndex(e => e.TenantId);

        modelBuilder.Entity<RoleUserGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<RoleUserGroup>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<RoleUserGroup>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<RoleUserGroup>().HasIndex(e => e.UserGroupId);
        modelBuilder.Entity<RoleUserGroup>().HasIndex(e => e.RoleId);

        // UserStatus entity (enumerative)
        modelBuilder.Entity<UserStatus>().HasKey(e => e.Id);
        modelBuilder.Entity<UserStatus>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<UserStatus>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<UserStatus>().HasIndex(e => e.Code);

        // UserStatusHistory entity (status change history)
        modelBuilder.Entity<UserStatusHistory>().HasKey(e => e.Id);
        modelBuilder.Entity<UserStatusHistory>().Property(e => e.Id).ValueGeneratedOnAdd();
        modelBuilder.Entity<UserStatusHistory>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<UserStatusHistory>().HasIndex(e => e.UserId);
        modelBuilder.Entity<UserStatusHistory>().HasIndex(e => e.UserStatusId);
    }
}

