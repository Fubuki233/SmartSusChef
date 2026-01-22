# SmartSusChef API Documentation

## Base URL
- Development: `http://localhost:5000/api`
- Production: `https://your-domain.com/api`

## Authentication

All endpoints (except login) require JWT authentication.

### Headers
```
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
```

## Endpoints

### 🔐 Authentication

#### Login
```http
POST /api/auth/login
```

**Request:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "11111111-1111-1111-1111-111111111111",
    "username": "admin",
    "name": "Administrator",
    "role": "manager"
  }
}
```

#### Get Current User
```http
GET /api/auth/me
```

**Response:**
```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "username": "admin",
  "name": "Administrator",
  "role": "manager"
}
```

---

### 🥬 Ingredients

#### Get All Ingredients
```http
GET /api/ingredients
```

**Response:**
```json
[
  {
    "id": "33333333-3333-3333-3333-333333333333",
    "name": "Tomato",
    "unit": "kg",
    "carbonFootprint": 1.1
  }
]
```

#### Get Ingredient by ID
```http
GET /api/ingredients/{id}
```

#### Create Ingredient (Manager Only)
```http
POST /api/ingredients
```

**Request:**
```json
{
  "name": "Onion",
  "unit": "kg",
  "carbonFootprint": 0.4
}
```

#### Update Ingredient (Manager Only)
```http
PUT /api/ingredients/{id}
```

**Request:**
```json
{
  "name": "Onion",
  "unit": "kg",
  "carbonFootprint": 0.5
}
```

#### Delete Ingredient (Manager Only)
```http
DELETE /api/ingredients/{id}
```

---

### 🍕 Recipes

#### Get All Recipes
```http
GET /api/recipes
```

**Response:**
```json
[
  {
    "id": "88888888-8888-8888-8888-888888888888",
    "name": "Margherita Pizza",
    "ingredients": [
      {
        "ingredientId": "55555555-5555-5555-5555-555555555555",
        "ingredientName": "Dough",
        "unit": "kg",
        "quantity": 0.3
      },
      {
        "ingredientId": "33333333-3333-3333-3333-333333333333",
        "ingredientName": "Tomato",
        "unit": "kg",
        "quantity": 0.2
      }
    ]
  }
]
```

#### Create Recipe (Manager Only)
```http
POST /api/recipes
```

**Request:**
```json
{
  "name": "Caesar Salad",
  "ingredients": [
    {
      "ingredientId": "66666666-6666-6666-6666-666666666666",
      "quantity": 0.15
    }
  ]
}
```

---

### 📊 Sales Data

#### Get All Sales Data
```http
GET /api/sales?startDate=2026-01-15&endDate=2026-01-22
```

**Response:**
```json
[
  {
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "date": "2026-01-22",
    "recipeId": "88888888-8888-8888-8888-888888888888",
    "recipeName": "Margherita Pizza",
    "quantity": 25
  }
]
```

#### Get Sales Trend
```http
GET /api/sales/trend?startDate=2026-01-15&endDate=2026-01-22
```

**Response:**
```json
[
  {
    "date": "2026-01-22",
    "totalQuantity": 50,
    "recipeBreakdown": [
      {
        "recipeId": "88888888-8888-8888-8888-888888888888",
        "recipeName": "Margherita Pizza",
        "quantity": 25
      },
      {
        "recipeId": "99999999-9999-9999-9999-999999999999",
        "recipeName": "Beef Burger",
        "quantity": 25
      }
    ]
  }
]
```

#### Get Ingredient Usage by Date
```http
GET /api/sales/ingredients/2026-01-22
```

**Response:**
```json
[
  {
    "ingredientId": "33333333-3333-3333-3333-333333333333",
    "ingredientName": "Tomato",
    "unit": "kg",
    "quantity": 6.25
  }
]
```

#### Get Recipe Sales by Date
```http
GET /api/sales/recipes/2026-01-22
```

#### Create Sales Record
```http
POST /api/sales
```

**Request:**
```json
{
  "date": "2026-01-22",
  "recipeId": "88888888-8888-8888-8888-888888888888",
  "quantity": 25
}
```

#### Import Sales Data (Manager Only)
```http
POST /api/sales/import
```

**Request:**
```json
{
  "salesData": [
    {
      "date": "2026-01-22",
      "recipeId": "88888888-8888-8888-8888-888888888888",
      "quantity": 25
    },
    {
      "date": "2026-01-22",
      "recipeId": "99999999-9999-9999-9999-999999999999",
      "quantity": 30
    }
  ]
}
```

---

### 🗑️ Wastage Data

#### Get All Wastage Data
```http
GET /api/wastage?startDate=2026-01-15&endDate=2026-01-22
```

**Response:**
```json
[
  {
    "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "date": "2026-01-22",
    "ingredientId": "33333333-3333-3333-3333-333333333333",
    "ingredientName": "Tomato",
    "unit": "kg",
    "quantity": 2.5,
    "carbonFootprint": 2.75
  }
]
```

#### Get Wastage Trend
```http
GET /api/wastage/trend?startDate=2026-01-15&endDate=2026-01-22
```

**Response:**
```json
[
  {
    "date": "2026-01-22",
    "totalQuantity": 5.0,
    "totalCarbonFootprint": 45.5,
    "ingredientBreakdown": [
      {
        "ingredientId": "33333333-3333-3333-3333-333333333333",
        "ingredientName": "Tomato",
        "unit": "kg",
        "quantity": 2.5,
        "carbonFootprint": 2.75
      }
    ]
  }
]
```

#### Create Wastage Record
```http
POST /api/wastage
```

**Request:**
```json
{
  "date": "2026-01-22",
  "ingredientId": "33333333-3333-3333-3333-333333333333",
  "quantity": 2.5
}
```

---

### 🔮 Forecast & External Data

#### Get Forecast
```http
GET /api/forecast?days=7
```

**Parameters:**
- `days` (optional): Number of days to forecast (1-30, default: 7)

**Response:**
```json
[
  {
    "date": "2026-01-23",
    "recipeId": "88888888-8888-8888-8888-888888888888",
    "recipeName": "Margherita Pizza",
    "quantity": 28,
    "ingredients": [
      {
        "ingredientId": "55555555-5555-5555-5555-555555555555",
        "ingredientName": "Dough",
        "unit": "kg",
        "quantity": 8.4
      }
    ]
  }
]
```

#### Get Forecast Summary
```http
GET /api/forecast/summary?days=7
```

**Response:**
```json
[
  {
    "date": "2026-01-23",
    "totalQuantity": 55,
    "changePercentage": 8.5
  }
]
```

#### Get Current Weather
```http
GET /api/forecast/weather
```

**Response:**
```json
{
  "temperature": 28.5,
  "condition": "Partly Cloudy",
  "humidity": 75,
  "description": "Warm and humid with partial cloud cover"
}
```

#### Get Holidays
```http
GET /api/forecast/holidays/2026
```

**Response:**
```json
[
  {
    "date": "2026-01-01",
    "name": "New Year's Day"
  },
  {
    "date": "2026-08-09",
    "name": "National Day"
  }
]
```

---

## Error Responses

### 400 Bad Request
```json
{
  "message": "Invalid request parameters"
}
```

### 401 Unauthorized
```json
{
  "message": "Invalid username or password"
}
```

### 403 Forbidden
```json
{
  "message": "You don't have permission to access this resource"
}
```

### 404 Not Found
```json
{
  "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred processing your request"
}
```

---

## Rate Limiting

Currently no rate limiting is implemented. For production, consider implementing rate limiting using ASP.NET Core middleware.

## CORS

CORS is configured to allow requests from:
- `http://localhost:5173` (Vite dev server)
- `http://localhost:3000` (Production frontend)

## Data Formats

### Dates
All dates use ISO 8601 format: `YYYY-MM-DD`

Example: `2026-01-22`

### Decimals
All decimal values use dot (.) as decimal separator.

Example: `12.5`

### GUIDs
All IDs are GUIDs in standard format.

Example: `88888888-8888-8888-8888-888888888888`
