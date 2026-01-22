# SmartSusChef Backend

A .NET 8 Web API for the SmartSusChef demand forecasting and food prep recommendation system.

## 🚀 Features

- **Authentication & Authorization**: JWT-based auth with role-based access control (Employee/Manager)
- **Ingredient Management**: CRUD operations for ingredients with carbon footprint tracking
- **Recipe Management**: Manage recipes with ingredient compositions
- **Sales Data Management**: Track daily sales with CSV import support
- **Wastage Tracking**: Monitor food wastage with carbon footprint calculations
- **Demand Forecasting**: Mock ML service for predicting future sales (7-30 days)
- **External Integrations**: 
  - Weather API (Open-Meteo) for Singapore
  - Holiday API for Singapore public holidays
- **Database**: MySQL with Entity Framework Core
- **API Documentation**: Swagger/OpenAPI

## 📋 Prerequisites

- .NET 8 SDK
- MySQL 8.0+
- (Optional) Docker & Docker Compose

## 🛠️ Setup Instructions

### Local Development

1. **Install MySQL** (or use Docker)

2. **Update Connection String**
   Edit `appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=smartsuschef;User=root;Password=your_password;"
   }
   ```

3. **Install EF Core Tools**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

4. **Create Database & Run Migrations**
   ```bash
   cd backend/SmartSusChef.Api
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

   API will be available at: `http://localhost:5000`
   Swagger UI: `http://localhost:5000/swagger`

### Docker Deployment

1. **Build and Start All Services**
   ```bash
   docker-compose up -d
   ```

   This will start:
   - MySQL on port 3306
   - Backend API on port 5000
   - Frontend on port 3000

2. **Run Database Migrations** (first time only)
   ```bash
   docker exec smartsuschef_backend dotnet ef database update
   ```

## 🔑 Default Users

| Username | Password    | Role     |
|----------|-------------|----------|
| admin    | admin123    | Manager  |
| employee | employee123 | Employee |

## 📚 API Endpoints

### Authentication
- `POST /api/auth/login` - Login
- `GET /api/auth/me` - Get current user

### Ingredients
- `GET /api/ingredients` - Get all ingredients
- `POST /api/ingredients` - Create ingredient (Manager only)
- `PUT /api/ingredients/{id}` - Update ingredient (Manager only)
- `DELETE /api/ingredients/{id}` - Delete ingredient (Manager only)

### Recipes
- `GET /api/recipes` - Get all recipes
- `POST /api/recipes` - Create recipe (Manager only)
- `PUT /api/recipes/{id}` - Update recipe (Manager only)
- `DELETE /api/recipes/{id}` - Delete recipe (Manager only)

### Sales
- `GET /api/sales` - Get sales data
- `GET /api/sales/trend` - Get sales trend
- `GET /api/sales/ingredients/{date}` - Get ingredient usage for date
- `GET /api/sales/recipes/{date}` - Get recipe sales for date
- `POST /api/sales` - Create sales record
- `POST /api/sales/import` - Import sales data (CSV)

### Wastage
- `GET /api/wastage` - Get wastage data
- `GET /api/wastage/trend` - Get wastage trend
- `POST /api/wastage` - Create wastage record

### Forecast
- `GET /api/forecast?days=7` - Get forecast (1-30 days)
- `GET /api/forecast/summary?days=7` - Get forecast summary
- `GET /api/forecast/weather` - Get current weather
- `GET /api/forecast/holidays/{year}` - Get holidays for year

## 🧪 Testing with Swagger

1. Navigate to `http://localhost:5000/swagger`
2. Click "Authorize" button
3. Login using:
   ```json
   {
     "username": "admin",
     "password": "admin123"
   }
   ```
4. Copy the token from the response
5. Enter `Bearer {your-token}` in the authorization dialog
6. Now you can test all endpoints

## 🏗️ Project Structure

```
SmartSusChef.Api/
├── Controllers/          # API Controllers
├── Data/                 # DbContext and migrations
├── DTOs/                 # Data Transfer Objects
├── Models/               # Entity models
├── Services/             # Business logic
│   ├── AuthService.cs
│   ├── IngredientService.cs
│   ├── RecipeService.cs
│   ├── SalesService.cs
│   ├── WastageService.cs
│   ├── MockForecastService.cs  # Mock ML forecasting
│   ├── WeatherService.cs
│   └── HolidayService.cs
├── Program.cs            # Application entry point
└── appsettings.json      # Configuration
```

## 🔮 Mock ML Service

The `MockForecastService` simulates ML predictions using:
- Historical sales data (30-day average)
- Day-of-week patterns (weekends get higher multipliers)
- Random variations (-10% to +10%)
- Weather and holiday context (available via separate endpoints)

**To replace with real ML:**
1. Create a new service implementing `IForecastService`
2. Call your Python ML model via HTTP/gRPC
3. Update service registration in `Program.cs`

## 🌐 External APIs

### Weather API (Open-Meteo)
- Free, no API key required
- Singapore coordinates: 1.3521, 103.8198
- Returns current weather conditions

### Holiday API (Nager.Date)
- Free, no API key required
- Returns Singapore public holidays
- Country code: SG

## 🗄️ Database Schema

Key tables:
- `Users` - User accounts with roles
- `Ingredients` - Ingredients with carbon footprint
- `Recipes` - Recipe definitions
- `RecipeIngredients` - Recipe-ingredient relationships
- `SalesData` - Daily sales records
- `WastageData` - Daily wastage records

## 📝 Environment Variables

```bash
ConnectionStrings__DefaultConnection=Server=mysql;Database=smartsuschef;...
Jwt__Key=YourSecretKey
Jwt__Issuer=SmartSusChef
Jwt__Audience=SmartSusChefClient
Jwt__ExpiryMinutes=1440
```

## 🔧 Development Commands

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Create migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## 📦 NuGet Packages

- `Microsoft.EntityFrameworkCore` - ORM
- `Pomelo.EntityFrameworkCore.MySql` - MySQL provider
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT auth
- `BCrypt.Net-Next` - Password hashing
- `Swashbuckle.AspNetCore` - Swagger/OpenAPI
- `CsvHelper` - CSV import/export

## 🤝 Contributing

1. Create feature branch
2. Make changes
3. Test thoroughly
4. Submit pull request

## 📄 License

MIT License
