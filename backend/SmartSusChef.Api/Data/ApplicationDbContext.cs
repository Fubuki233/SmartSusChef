using Microsoft.EntityFrameworkCore;
using SmartSusChef.Api.Models;

namespace SmartSusChef.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<SalesData> SalesData { get; set; }
    public DbSet<WastageData> WastageData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).HasConversion<string>();
        });

        // Ingredient configuration
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CarbonFootprint).HasPrecision(10, 3);
        });

        // Recipe configuration
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // RecipeIngredient configuration
        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(e => e.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Ingredient)
                .WithMany(i => i.RecipeIngredients)
                .HasForeignKey(e => e.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Quantity).HasPrecision(10, 3);
        });

        // SalesData configuration
        modelBuilder.Entity<SalesData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Recipe)
                .WithMany(r => r.SalesRecords)
                .HasForeignKey(e => e.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Date);
        });

        // WastageData configuration
        modelBuilder.Entity<WastageData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Ingredient)
                .WithMany(i => i.WastageRecords)
                .HasForeignKey(e => e.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Quantity).HasPrecision(10, 3);
            entity.HasIndex(e => e.Date);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default admin user
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var employeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminId,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Name = "Administrator",
                Role = UserRole.Manager,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = employeeId,
                Username = "employee",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("employee123"),
                Name = "Employee User",
                Role = UserRole.Employee,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed sample ingredients
        var tomatoId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var cheeseId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var doughId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var lettuceId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var beefId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        modelBuilder.Entity<Ingredient>().HasData(
            new Ingredient { Id = tomatoId, Name = "Tomato", Unit = "kg", CarbonFootprint = 1.1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ingredient { Id = cheeseId, Name = "Cheese", Unit = "kg", CarbonFootprint = 13.5m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ingredient { Id = doughId, Name = "Dough", Unit = "kg", CarbonFootprint = 0.9m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ingredient { Id = lettuceId, Name = "Lettuce", Unit = "kg", CarbonFootprint = 0.5m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ingredient { Id = beefId, Name = "Beef", Unit = "kg", CarbonFootprint = 27.0m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // Seed sample recipes
        var pizzaId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var burgerId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        modelBuilder.Entity<Recipe>().HasData(
            new Recipe { Id = pizzaId, Name = "Margherita Pizza", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Recipe { Id = burgerId, Name = "Beef Burger", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // Seed recipe ingredients
        modelBuilder.Entity<RecipeIngredient>().HasData(
            new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = pizzaId, IngredientId = doughId, Quantity = 0.3m },
            new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = pizzaId, IngredientId = tomatoId, Quantity = 0.2m },
            new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = pizzaId, IngredientId = cheeseId, Quantity = 0.15m },
            new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = burgerId, IngredientId = beefId, Quantity = 0.2m },
            new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = burgerId, IngredientId = lettuceId, Quantity = 0.05m },
            new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = burgerId, IngredientId = tomatoId, Quantity = 0.05m }
        );
    }
}
