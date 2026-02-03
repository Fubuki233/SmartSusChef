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
    public DbSet<Store> Store { get; set; }

    public DbSet<GlobalCalendarSignals> GlobalCalendarSignals { get; set; }

    public DbSet<ForecastData> ForecastData { get; set; }
    public DbSet<HolidayCalendar> HolidayCalendars { get; set; }
    public DbSet<WeatherDaily> WeatherDaily { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Store Configuration (Enhanced with corporate fields)
        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // ID is generated via hash
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UEN).IsRequired().HasMaxLength(20);
            entity.Property(e => e.StoreName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OutletLocation).HasMaxLength(200);
            entity.Property(e => e.Latitude).HasPrecision(10, 7);
            entity.Property(e => e.Longitude).HasPrecision(10, 7);
            entity.Property(e => e.CountryCode).HasMaxLength(2);
            entity.Property(e => e.ContactNumber).HasMaxLength(20);
        });

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserStatus).HasDefaultValue("Active");
            entity.Property(e => e.Role).HasConversion<string>();

            // Link to Store
            entity.HasOne(e => e.Store)
                  .WithMany(s => s.Users)
                  .HasForeignKey(e => e.StoreId);
        });

        // Ingredient Configuration
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CarbonFootprint).HasPrecision(18, 3);

            // Unique index for (StoreId, Name)
            entity.HasIndex(e => new { e.StoreId, e.Name }).IsUnique();

            entity.HasOne(e => e.Store)
                  .WithMany(s => s.Ingredients)
                  .HasForeignKey(e => e.StoreId);
        });

        // Recipe Configuration (Support for sub-recipes)
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique index for (StoreId, Name)
            entity.HasIndex(e => new { e.StoreId, e.Name }).IsUnique();

            entity.HasOne(e => e.Store)
                  .WithMany(s => s.Recipes)
                  .HasForeignKey(e => e.StoreId);
        });

        // RecipeIngredient Configuration (Recursive BOM Logic)
        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 3);

            // Link to Parent Recipe
            entity.HasOne(e => e.Recipe)
                  .WithMany(r => r.RecipeIngredients)
                  .HasForeignKey(e => e.RecipeId);

            // Optional link to Raw Ingredient
            entity.HasOne(e => e.Ingredient)
                  .WithMany(i => i.RecipeIngredients)
                  .HasForeignKey(e => e.IngredientId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Optional link to Child Sub-Recipe
            entity.HasOne(e => e.ChildRecipe)
                  .WithMany()
                  .HasForeignKey(e => e.ChildRecipeId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent circular deletes
        });

        // SalesData Configuration
        modelBuilder.Entity<SalesData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Store)
                  .WithMany(s => s.SalesData)
                  .HasForeignKey(e => e.StoreId);
            entity.HasOne(e => e.Recipe)
                  .WithMany(r => r.SalesRecords)
                  .HasForeignKey(e => e.RecipeId);
            modelBuilder.Entity<SalesData>()
                        .HasIndex(s => new { s.StoreId, s.Date, s.RecipeId })
                        .IsUnique();
        });

        // WastageData Configuration (Either/Or Logic)
        modelBuilder.Entity<WastageData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 3);

            entity.HasOne(e => e.Store)
                  .WithMany(s => s.WastageData)
                  .HasForeignKey(e => e.StoreId);

            // Flexible links
            entity.HasOne(e => e.Ingredient)
                  .WithMany(i => i.WastageRecords)
                  .HasForeignKey(e => e.IngredientId);

            entity.HasOne(e => e.Recipe)
                  .WithMany(r => r.WastageRecords)
                  .HasForeignKey(e => e.RecipeId);
        });

        // ForecastData Configuration (New Predictive Entity)
        modelBuilder.Entity<ForecastData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Store)
                  .WithMany(s => s.ForecastData)
                  .HasForeignKey(e => e.StoreId);
            entity.HasOne(e => e.Recipe)
                  .WithMany()
                  .HasForeignKey(e => e.RecipeId);
        });

        // GlobalCalendarSignals Configuration (Date as Key)
        modelBuilder.Entity<GlobalCalendarSignals>(entity =>
        {
            entity.HasKey(e => e.Date);
            entity.Property(e => e.RainMm).HasPrecision(10, 2);
        });

        // HolidayCalendar Configuration (Country + Year)
        modelBuilder.Entity<HolidayCalendar>(entity =>
        {
            entity.HasKey(e => new { e.CountryCode, e.Year });
            entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(2);
            entity.Property(e => e.HolidaysJson).IsRequired();
        });

        // WeatherDaily Configuration (Store + Date)
        modelBuilder.Entity<WeatherDaily>(entity =>
        {
            entity.HasKey(e => new { e.StoreId, e.Date });
            entity.Property(e => e.Temperature).HasPrecision(10, 2);
            entity.HasOne<Store>()
                  .WithMany()
                  .HasForeignKey(e => e.StoreId);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Store
        var storeId = 1;
        modelBuilder.Entity<Store>().HasData(
            new Store
            {
                Id = storeId,
                CompanyName = "Smart Sus Chef Corp",
                UEN = "202400001A",
                StoreName = "Downtown Outlet",
                OutletLocation = "123 Orchard Road",
                ContactNumber = "+65 6000 0000",
                OpeningDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            }
        );

        // Seed default admin user
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var employeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminId,
                StoreId = storeId,
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
                StoreId = storeId,
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
            new Ingredient { Id = tomatoId, StoreId = storeId, Name = "Tomato", Unit = "kg", CarbonFootprint = 1.1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ingredient { Id = cheeseId, StoreId = storeId, Name = "Cheese", Unit = "kg", CarbonFootprint = 13.5m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ingredient { Id = doughId, StoreId = storeId, Name = "Dough", Unit = "kg", CarbonFootprint = 0.9m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ingredient { Id = lettuceId, StoreId = storeId, Name = "Lettuce", Unit = "kg", CarbonFootprint = 0.5m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Ingredient { Id = beefId, StoreId = storeId, Name = "Beef", Unit = "kg", CarbonFootprint = 27.0m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // Seed sample recipes
        var pizzaId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var burgerId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        modelBuilder.Entity<Recipe>().HasData(
            new Recipe { Id = pizzaId, StoreId = storeId, Name = "Margherita Pizza", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Recipe { Id = burgerId, StoreId = storeId, Name = "Beef Burger", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // Seed recipe ingredients
        modelBuilder.Entity<RecipeIngredient>().HasData(
            new RecipeIngredient { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), RecipeId = pizzaId, IngredientId = doughId, Quantity = 0.3m },
            new RecipeIngredient { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), RecipeId = pizzaId, IngredientId = tomatoId, Quantity = 0.2m },
            new RecipeIngredient { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), RecipeId = pizzaId, IngredientId = cheeseId, Quantity = 0.15m },
            new RecipeIngredient { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), RecipeId = burgerId, IngredientId = beefId, Quantity = 0.2m },
            new RecipeIngredient { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), RecipeId = burgerId, IngredientId = lettuceId, Quantity = 0.05m },
            new RecipeIngredient { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), RecipeId = burgerId, IngredientId = tomatoId, Quantity = 0.05m }
        );
    }
}
