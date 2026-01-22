# SmartSusChef

## Architecture

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

## Quick Start

### Option 1: Docker 

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

## Default Credentials

| Username | Password    | Role     | Permissions |
|----------|-------------|----------|-------------|
| admin    | admin123    | Manager  | Full access |
| employee | employee123 | Employee | Limited (7-day data) |


## Documentation

- [Architecture Overview](architecture/architecture.md) - System design and requirements
- [Backend README](backend/README.md) - .NET API setup and development
- [API Documentation](backend/API_DOCUMENTATION.md) - Complete API reference\

## Development

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


## Deployment

### Production Deployment

1. **Update Configuration:**
   ```bash
   # backend/SmartSusChef.Api/appsettings.Production.json
   
     "ConnectionStrings": {
       "DefaultConnection": "Server=prod-mysql;Database=smartsuschef;..."
     },
     "Jwt": {
       "Key": "your-production-secret-key"
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


---

## Servers (For Deployment Reference):

```bash
#server only for deployment(no video card, 2GB RAM):
ssh smartsuschef@oversea.zyh111.icu -p 234 
#password: smartsuschef

#server for cacaluation(ML model training, with video card, 64GB RAM):
ssh zyh@oversea.zyh111.icu -p 233
#password: 1

#Mysql Server:
oversea.zyh111.icu Port: 33333 Username: grp4 Password: grp4
```

## Weather API:

```bash
https://open-meteo.com/en/docs
```

## Calendar API:

```bash
https://github.com/nager/Nager.Date
```
