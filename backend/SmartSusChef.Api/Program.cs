using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartSusChef.Api.Data;
using SmartSusChef.Api.Services;
using System.Text;
using System.Runtime.CompilerServices;

// Allow the test project to access internal members
[assembly: InternalsVisibleTo("SmartSusChef.Api.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartSusChef API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// CORS - restrict to configured origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Development only: allow localhost origins
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // Production without explicit config: same-origin only (no cross-origin allowed)
            policy.WithOrigins("https://smartsuschef.com", "https://uat.smartsuschef.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Register services
builder.Services.AddHttpContextAccessor(); // Required for ICurrentUserService
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IWastageService, WastageService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IStoreService, StoreService>();

// ML Prediction Service — calls the FastAPI ML backend
var mlApiUrl = builder.Configuration["ExternalApis:MlApiUrl"] ?? "http://localhost:8000";
builder.Services.AddHttpClient<IMlPredictionService, MlPredictionService>(client =>
{
    client.BaseAddress = new Uri(mlApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseProxy = false   // Bypass local proxy (e.g. Clash) for ML service calls
});

// Forecast Service — uses ML backend with fallback to simple heuristic
builder.Services.AddScoped<IForecastService, MlForecastService>();

// Configure HttpClient with timeout for external API calls
builder.Services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<IHolidayService, HolidayService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient();

var app = builder.Build();

// Apply pending migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Check if Users table exists, if not, ensure database is created fresh
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Users'";
        var tableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        await connection.CloseAsync();

        if (!tableExists)
        {
            Console.WriteLine("Users table not found, recreating database schema...");
            // Delete migration history to force re-apply
            await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS `__EFMigrationsHistory`");
        }

        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying migrations: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
