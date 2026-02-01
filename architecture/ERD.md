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
        string CountryCode "nullable"
        string Address "nullable"
        string ContactNumber
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }

    User {
        Guid Id PK
        int StoreId FK
        string Username
        string Email
        string PasswordHash
        string Name
        UserRole Role
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
        bool IsSubRecipe
        bool IsSellable
        datetime CreatedAt
        datetime UpdatedAt
    }

    RecipeIngredient {
        Guid Id PK
        Guid RecipeId FK
        Guid IngredientId FK "nullable"
        Guid ChildRecipeId FK "nullable"
        decimal Quantity
    }

    SalesData {
        Guid Id PK
        int StoreId FK
        datetime Date
        Guid RecipeId FK
        int Quantity
        datetime CreatedAt
        datetime UpdatedAt
    }

    WastageData {
        Guid Id PK
        int StoreId FK
        datetime Date
        Guid IngredientId FK "nullable"
        Guid RecipeId FK "nullable"
        decimal Quantity
        datetime CreatedAt
        datetime UpdatedAt
    }

    ForecastData {
        Guid Id PK
        int StoreId FK
        Guid RecipeId FK
        datetime ForecastDate
        int PredictedQuantity
        datetime CreatedAt
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

    HolidayCalendar {
        string CountryCode PK
        int Year PK
        string HolidaysJson
        datetime UpdatedAt
    }

    WeatherDaily {
        int StoreId PK, FK
        datetime Date PK
        decimal Temperature
        string Condition
        int Humidity
        string Description
        datetime UpdatedAt
    }

    Store ||--o{ User : "has"
    Store ||--o{ Ingredient : "has"
    Store ||--o{ Recipe : "has"
    Store ||--o{ SalesData : "records"
    Store ||--o{ WastageData : "records"
    Store ||--o{ ForecastData : "generates"
    Store ||--o{ WeatherDaily : "experiences"
    Recipe ||--o{ RecipeIngredient : "contains"
    Recipe ||--o{ SalesData : "sold in"
    Recipe ||--o{ WastageData : "wasted as"
    Recipe ||--o{ ForecastData : "forecasted for"
    Recipe }|..o{ RecipeIngredient : "is sub-recipe of"
    Ingredient ||--o{ RecipeIngredient : "used in"
    Ingredient ||--o{ WastageData : "wasted as"
