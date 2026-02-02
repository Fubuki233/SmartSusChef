# Software Architecture for SmartSus Chef

## Overview: 3-Layer Monolith + ML Service

```
┌─────────────┐         ┌─────────────┐
│   Mobile    │         │   Web App   │
│  (Android)  │         │   (React)   │
└──────┬──────┘         └──────┬──────┘
       │                       │
       │    HTTP/REST API      │
       └───────────┬───────────┘
                   │
          ┌────────▼────────┐
          │   Nginx / ALB   │  (Load Balancer)
          └────────┬────────┘
                   │
          ┌────────▼────────────────────┐
          │   Backend API (.NET 8)      │
          │                             │
          │  ┌───────────────────────┐  │
          │  │ Presentation Layer    |  |
          |  | - Controllers         |  |
          |  | - API Endpoints       |  |
          |  | - DTOs                |  |
          │  └──────────┬────────────┘  │
          │             │               │
          │  ┌──────────▼────────────┐  │
          │  │ Business Logic Layer  │  │
          │  │ - Services            │  │
          │  │ - Domain Models       │  │
          │  │ - Validation          |  |
          │  └──────────┬────────────┘  │
          │             │               │
          │  ┌──────────▼────────────┐  │
          │  │ Data Access Layer     │  │
          │  │ - EF Core DbContext   |  |
          │  │ (DbSet->Repositories) |  |
          │  └──────────┬────────────┘  │
          └─────────────┼───────────────┘
                        │
          ┌─────────────▼────────────┐
          │    MySQL Database        │
          └──────────────────────────┘

          External Services:
          ┌────────────────┐
          │  ML Service    │  ← Called via HTTP when predictions needed
          │  (Python)      │
          └────────────────┘
          ┌────────────────┐
          │  Weather API   │  ← External third-party
          └────────────────┘
          ┌────────────────┐
          │  Holiday API   │  ← External third-party
          └────────────────┘
```
<hr>

## Rationale for Chosen Approach

#### 3-Layer Monolith for Backend, Frontend and Mobile
- Easiest to develop and deploy within a tight timeline, most suitable for Minimum Viable Product
- Single codebase for backend logic, with one database connection to manage
- Lower infrastructure costs
- Built-in transactions across all features
- Easier to debug as everything is in one place

#### Machine Learning (ML) as a Separate Service
- Python is the preferred language to use for ML
- ML model requires different libraries and frameworks (scikit-learn, pandas, tensorflow)
- More flexible to develop and deploy this as a service, then get the backend to call it like any external API

#### Deployment
- Single Docker container or single EC2 instance

#### CI/CD Pipeline
- One pipeline to build and deploy everything

<hr>

## Example of Communication Flows
### Example 1: Employee Logs Sales Data
```
[Mobile App]
  ↓ User enters: Recipe = "Chicken Rice", Quantity = 25
  
[LoginViewModel]
  ↓ salesRepository.createSales(CreateSalesRequest(...))
  
[SalesRepository]
  ↓ apiService.createSales(...)
  
[Retrofit + OkHttp]
  ↓ POST [server-url]/api/sales
  ↓ Headers: Authorization: Bearer <JWT_TOKEN>
  ↓ Body: {"date":"2026-01-28","recipeId":"123","quantity":25}
  
[Nginx]
  ↓ Routes /api/* → backend:5000
  
[Backend - SalesController]
  ↓ Validates JWT token
  ↓ Calls SalesService.CreateAsync(request)

[Backend - SalesService]
  ↓ Map DTO to SalesData entity
  ↓ Assigns StoreId from _currentUserService
  ↓ Calls _context.SalesData.Add(salesData)  <-- This is the "Repository" action
  ↓ Calls _context.SaveChangesAsync()        <-- This is the "Unit of Work" action

[MySQL Database]
  ↓ Saves data
  
[Response flows back up the chain]
  ↓ 200 OK, {"id":"456","date":"2026-01-28",...}
  
[Mobile App]
  ✓ Shows success message
  ✓ Refreshes sales list
```

### Example 2: Manager Views Predictions
```
[Web App]
  ↓ User clicks "View Predictions"
  
[React Component]
  ↓ useEffect(() => fetchPredictions())
  
[API Service (Axios)]
  ↓ GET [server-url]/api/forecast?days=7
  
[Backend - ForecastController]
  ↓ Calls ForecastService.GetForecast()
  
[Backend - ForecastService]
  ↓ Fetches historical sales from database
  ↓ Gets weather forecast from Open-Meteo API
  ↓ Gets holidays from Nager.Date API
  ↓ POST http://ml-service:8000/predict
  ↓ Body: {"salesHistory":[...],"weather":[...],"holidays":[...]}
  
[ML Service]
  ↓ Loads trained model
  ↓ Runs prediction algorithm
  ↓ Returns: {"predictions":[{"date":"2026-01-29","quantity":23},...]}
  
[Backend - ForecastService]
  ↓ Saves predictions to database
  ↓ Returns formatted response
  
[Web App]
  ✓ Displays chart and table with predicted values
```