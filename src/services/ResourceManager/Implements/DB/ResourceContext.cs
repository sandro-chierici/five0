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

        modelBuilder.Entity<Resource>().HasKey(e => e.Id);
        modelBuilder.Entity<Resource>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceGroup>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceType>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceType>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceTypeHierarchy>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceTypeHierarchy>().Property(e => e.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<ResourceResourceGroup>().HasKey(e => e.Id);
        modelBuilder.Entity<ResourceResourceGroup>().Property(e => e.Id).ValueGeneratedOnAdd();


    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(@"Host=dbpostgres;Username=five0_rm;Password=five0_rm;Database=five0.resources");
    }

}

