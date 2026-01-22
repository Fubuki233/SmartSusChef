# SmartSusChef

A Demand Forecasting and Food Prep Recommendation Tool for sustainable F&B operations in Singapore.

## 📋 Overview

SmartSusChef helps small and medium F&B operators reduce food wastage through intelligent demand forecasting. The system combines historical sales data, weather information, and holiday patterns to predict future demand and recommend optimal ingredient preparation quantities.

### Key Features

- 📊 **Sales Tracking**: Record and analyze daily sales data by recipe
- 🗑️ **Wastage Management**: Track food wastage with carbon footprint calculations
- 🔮 **Demand Forecasting**: AI-powered predictions for next 7-30 days
- 🌤️ **Weather Integration**: Real-time Singapore weather data
- 📅 **Holiday Calendar**: Singapore public holidays integration
- 👥 **Multi-Role Access**: Separate interfaces for employees and managers
- 📈 **Trend Analytics**: Visual insights into sales and wastage patterns
- 💾 **Data Import/Export**: CSV support for bulk operations

## 🏗️ Architecture

### Technology Stack

**Frontend:**
- React + TypeScript
- Vite
- TailwindCSS
- Recharts (for visualizations)
- Shadcn/ui components

**Backend:**
- .NET 8 Web API
- Entity Framework Core
- MySQL 8.0
- JWT Authentication
- Swagger/OpenAPI

**External APIs:**
- Open-Meteo (Weather)
- Nager.Date (Holidays)

**DevOps:**
- Docker & Docker Compose
- Nginx (Frontend proxy)

## 🚀 Quick Start

### Option 1: Docker (Recommended)

**Prerequisites:**
- Docker Desktop
- Docker Compose

**Windows:**
```powershell
.\quick-start.ps1
```

**Linux/Mac:**
```bash
chmod +x quick-start.sh
./quick-start.sh
```

This will:
1. Build all services (frontend, backend, MySQL)
2. Start containers
3. Run database migrations
4. Display access URLs

**Access the application:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### Option 2: Local Development

**Prerequisites:**
- .NET 8 SDK
- Node.js 18+
- MySQL 8.0

**Backend Setup:**
```bash
cd backend/SmartSusChef.Api

# Windows
.\setup.ps1

# Linux/Mac
chmod +x setup.sh
./setup.sh
```

**Frontend Setup:**
```bash
cd frontend
npm install
npm run dev
```

## 🔑 Default Credentials

| Username | Password    | Role     | Permissions |
|----------|-------------|----------|-------------|
| admin    | admin123    | Manager  | Full access |
| employee | employee123 | Employee | Limited (7-day data) |

## 📁 Project Structure

```
SmartSusChef/
├── frontend/                # React TypeScript frontend
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/  # UI components
│   │   │   ├── context/     # React context
│   │   │   └── types/       # TypeScript types
│   │   └── styles/          # CSS styles
│   ├── Dockerfile
│   └── nginx.conf
│
├── backend/                 # .NET 8 Web API
│   ├── SmartSusChef.Api/
│   │   ├── Controllers/     # API endpoints
│   │   ├── Services/        # Business logic
│   │   ├── Models/          # Database entities
│   │   ├── DTOs/            # Data transfer objects
│   │   ├── Data/            # DbContext & migrations
│   │   └── Program.cs
│   ├── Dockerfile
│   ├── README.md
│   └── API_DOCUMENTATION.md
│
├── database/                # Database scripts
│   └── init.sql
│
├── dataset/                 # Sample data & scripts
│   ├── *.csv
│   └── visualize_*.py
│
├── architecture/            # System architecture docs
│   └── architecture.md
│
├── docker-compose.yml       # Docker orchestration
├── quick-start.ps1         # Windows quick start
└── quick-start.sh          # Linux/Mac quick start
```

## 📚 Documentation

- [Architecture Overview](architecture/architecture.md) - System design and requirements
- [Backend README](backend/README.md) - .NET API setup and development
- [API Documentation](backend/API_DOCUMENTATION.md) - Complete API reference
- [Frontend Guidelines](frontend/guidelines/Guidelines.md) - UI/UX guidelines

## 🔧 Development

### Backend Development

```bash
cd backend/SmartSusChef.Api

# Run the API
dotnet run

# Watch mode (auto-reload)
dotnet watch run

# Create migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

**API available at:** http://localhost:5000
**Swagger UI:** http://localhost:5000/swagger

### Frontend Development

```bash
cd frontend

# Development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

**Dev server:** http://localhost:5173

### Database Management

**Connection String:**
```
Server=localhost;Database=smartsuschef;User=root;Password=your_password;
```

**Access MySQL:**
```bash
# Via Docker
docker exec -it smartsuschef_mysql mysql -u smartsuschef -p

# Direct
mysql -u root -p smartsuschef
```

## 🧪 Testing

### Test API with Swagger

1. Navigate to http://localhost:5000/swagger
2. Click "Authorize"
3. Login with credentials
4. Copy the token
5. Paste in format: `Bearer {token}`
6. Test endpoints

### Sample API Calls

```bash
# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Get ingredients (with token)
curl -X GET http://localhost:5000/api/ingredients \
  -H "Authorization: Bearer {your-token}"

# Get forecast
curl -X GET "http://localhost:5000/api/forecast?days=7" \
  -H "Authorization: Bearer {your-token}"
```

## 🤖 Machine Learning Integration

The current MVP uses a **mock ML service** (`MockForecastService.cs`) that simulates predictions using:
- 30-day historical averages
- Day-of-week patterns
- Random variations

### To Integrate Real ML Model:

1. **Create Python ML Service:**
   ```python
   # ml_service/app.py
   from flask import Flask, request, jsonify
   import pandas as pd
   from your_model import forecast
   
   app = Flask(__name__)
   
   @app.route('/predict', methods=['POST'])
   def predict():
       data = request.json
       predictions = forecast(data)
       return jsonify(predictions)
   ```

2. **Update Backend Service:**
   ```csharp
   // Create RealForecastService.cs
   public class RealForecastService : IForecastService
   {
       private readonly HttpClient _httpClient;
       
       public async Task<List<ForecastDto>> GetForecastAsync(int days)
       {
           var response = await _httpClient.PostAsync(
               "http://ml-service:5000/predict",
               new StringContent(JsonSerializer.Serialize(new { days }))
           );
           // Process response
       }
   }
   ```

3. **Update Service Registration:**
   ```csharp
   // Program.cs
   builder.Services.AddScoped<IForecastService, RealForecastService>();
   ```

## 🌐 Deployment

### Production Deployment

1. **Update Configuration:**
   ```bash
   # backend/SmartSusChef.Api/appsettings.Production.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=prod-mysql;Database=smartsuschef;..."
     },
     "Jwt": {
       "Key": "your-production-secret-key"
     }
   }
   ```

2. **Build & Deploy:**
   ```bash
   docker-compose -f docker-compose.prod.yml up -d
   ```

3. **Set up SSL/TLS** (using Let's Encrypt)

### Environment Variables

```bash
# Backend
ConnectionStrings__DefaultConnection=...
Jwt__Key=...
Jwt__Issuer=...
Jwt__Audience=...

# Frontend
VITE_API_URL=http://localhost:5000/api
```

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📝 License

MIT License - see LICENSE file for details

## 👥 Team

Developed for sustainable F&B operations in Singapore

## 🔗 External Resources

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet)
- [React Documentation](https://react.dev)
- [Open-Meteo API](https://open-meteo.com/en/docs)
- [Nager.Date Holiday API](https://date.nager.at)

## 📞 Support

For issues and questions:
- Open an issue on GitHub
- Check documentation in `/architecture` and `/backend` folders
- Review API documentation for endpoint details

---

**Note:** This is an MVP. The ML forecasting is currently mocked. For production use, integrate with a real machine learning model trained on your historical data.

## Servers (For Deployment Reference):

```bash
#server only for deployment(no video card, 2GB RAM):
ssh smartsuschef@oversea.zyh111.icu -p 234 
#password: smartsuschef

#server for cacaluation(ML model training, with video card, 64GB RAM):
ssh zyh@oversea.zyh111.icu -p 233
#password: 1
```
