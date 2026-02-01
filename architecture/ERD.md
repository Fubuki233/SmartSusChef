erDiagram
    Store {
        int Id PK
        string CompanyName
        string UEN
        string StoreName
        string OutletLocation
        decimal Latitude
        decimal Longitude
        string CountryCode
        string ContactNumber
        datetime OpeningDate
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }

    User {
        Guid Id PK
        int StoreId FK
        string Username
        string PasswordHash
        string Name
        string Email
        string Role
        string UserStatus
        datetime CreatedAt
        datetime UpdatedAt
    }

    Ingredient {
        Guid Id PK
        int StoreId FK
        string Name
        string Unit
        decimal CarbonFootprint
        datetime CreatedAt
        datetime UpdatedAt
    }

    Recipe {
        Guid Id PK
        int StoreId FK
        string Name
        bool IsSellable
        bool IsSubRecipe
        datetime CreatedAt
        datetime UpdatedAt
    }

    RecipeIngredient {
        Guid Id PK
        Guid RecipeId FK
        Guid IngredientId FK
        Guid ChildRecipeId FK
        decimal Quantity
    }

    SalesData {
        Guid Id PK
        int StoreId FK
        Guid RecipeId FK
        datetime Date
        int Quantity
    }

    WastageData {
        Guid Id PK
        int StoreId FK
        Guid IngredientId FK
        Guid RecipeId FK
        datetime Date
        decimal Quantity
    }

    ForecastData {
        Guid Id PK
        int StoreId FK
        Guid RecipeId FK
        datetime Date
        int Quantity
    }

    GlobalCalendarSignals {
        datetime Date PK
        decimal RainMm
        bool IsHoliday
        string HolidayName
    }

    HolidayCalendar {
        string CountryCode PK
        int Year PK
        string HolidaysJson
    }

    WeatherDaily {
        int StoreId PK, FK
        datetime Date PK
        decimal Temperature
    }

    Store ||--o{ User : "has"
    Store ||--o{ Ingredient : "has"
    Store ||--o{ Recipe : "has"
    Store ||--o{ SalesData : "has"
    Store ||--o{ WastageData : "has"
    Store ||--o{ ForecastData : "has"
    Store ||--o{ WeatherDaily : "has"
    Recipe ||--o{ RecipeIngredient : "contains"
    Recipe ||--o{ SalesData : "sold in"
    Recipe ||--o{ WastageData : "wasted in"
    Recipe ||--o{ ForecastData : "forecasted for"
    Recipe }|..o{ RecipeIngredient : "is sub-recipe of"
    Ingredient ||--o{ RecipeIngredient : "used in"
    Ingredient ||--o{ WastageData : "wasted as"
