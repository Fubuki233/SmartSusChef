erDiagram

  Store {
    int Id PK
    string CompanyName
    string UEN
    string StoreName
    string OutletLocation
    datetime OpeningDate
    decimal Latitude
    decimal Longitude
    string CountryCode
    string Address
    string ContactNumber
    bool IsActive
    datetime CreatedAt
    datetime UpdatedAt
  }

  User {
    guid Id PK
    int StoreId FK
    string Username
    string Email
    string PasswordHash
    string Name
    enum Role
    string UserStatus
    datetime CreatedAt
    datetime UpdatedAt
  }

  Ingredient {
    guid Id PK
    int StoreId FK
    string Name
    string Unit
    decimal CarbonFootprint
    datetime CreatedAt
    datetime UpdatedAt
  }

  Recipe {
    guid Id PK
    int StoreId FK
    string Name
    bool IsSubRecipe
    bool IsSellable
    datetime CreatedAt
    datetime UpdatedAt
  }

  RecipeIngredient {
    guid Id PK
    guid RecipeId FK
    guid IngredientId FK
    guid ChildRecipeId FK
    decimal Quantity
  }

  SalesData {
    guid Id PK
    int StoreId FK
    datetime Date
    guid RecipeId FK
    int Quantity
    datetime CreatedAt
    datetime UpdatedAt
  }

  WastageData {
    guid Id PK
    int StoreId FK
    datetime Date
    guid IngredientId FK
    guid RecipeId FK
    decimal Quantity
    datetime CreatedAt
    datetime UpdatedAt
  }

  ForecastData {
    guid Id PK
    int StoreId FK
    guid RecipeId FK
    datetime ForecastDate
    int PredictedQuantity
    datetime CreatedAt
    datetime UpdatedAt
  }

  HolidayCalendar {
    string CountryCode PK
    int Year PK
    string HolidaysJson
    datetime UpdatedAt
  }

  WeatherDaily {
    int StoreId PK
    datetime Date PK
    decimal Temperature
    string Condition
    int Humidity
    string Description
    datetime UpdatedAt
  }

  GlobalCalendarSignals {
    datetime Date PK
    bool IsHoliday
    string HolidayName
    bool IsSchoolHoliday
    decimal RainMm
    string WeatherDesc
  }

  %% Relationships
  Store ||--o{ User : has
  Store ||--o{ Ingredient : has
  Store ||--o{ Recipe : has
  Store ||--o{ SalesData : generates
  Store ||--o{ WastageData : generates
  Store ||--o{ ForecastData : generates
  Store ||--o{ WeatherDaily : records

  Recipe ||--o{ SalesData : sold_as
  Recipe ||--o{ WastageData : wasted_as
  Ingredient ||--o{ WastageData : wasted

  Recipe ||--o{ RecipeIngredient : composed_of
  Ingredient }o--|| RecipeIngredient : used_in
  Recipe }o--|| RecipeIngredient : ChildRecipe

  Recipe ||--o{ ForecastData : forecasted
