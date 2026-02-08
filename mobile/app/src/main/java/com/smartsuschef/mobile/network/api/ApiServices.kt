package com.smartsuschef.mobile.network.api

import com.smartsuschef.mobile.network.dto.*
import retrofit2.Response
import retrofit2.http.*

/**
 * API Services for SmartSus Chef (Mobile)
 * // Old Base URL: http://oversea.zyh111.icu:234/api/
 * Base URL: http://192.168.50.133:5001/api/
 * All endpoints require JWT token in Authorization header (except login/resetPassword)
 *
 * Employee Restrictions:
 * - Can only view data for TODAY or last 7 days
 * - Can only input sales/wastage data for TODAY
 * - Cannot create/edit/delete recipes or ingredients
 *
 * Manager Additional Access (if using mobile):
 * - Can view up to 30 days of data
 * - Can manage recipes/ingredients (though typically done on web)
 */

/**
 * Authentication API Service
 * Maps to: AuthController.cs
 * Base Route: /api/auth
 */
interface AuthApiService {

    /**
     * Login with username and password
     * POST /api/auth/login
     */
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    /**
     * Get current user information
     * GET /api/auth/me
     * Requires: Authorization header with JWT token
     */
    @GET("auth/me")
    suspend fun getCurrentUser(): Response<UserDto>

    /**
     * Update own profile (name and email)
     * PUT /api/auth/profile
     * Requires: Authorization header with JWT token
     * Note: Username cannot be changed
     */
    @PUT("auth/profile")
    suspend fun updateOwnProfile(@Body request: UpdateProfileRequest): Response<UserDto>

    /**
     * Change own password
     * PUT /api/auth/password
     * Requires: Authorization header with JWT token
     */
    @PUT("auth/password")
    suspend fun changePassword(@Body request: ChangePasswordRequest): Response<Unit>

    /**
     * Request password reset (Forgot Password)
     * POST /api/auth/forgot-password
     */
    @POST("auth/forgot-password")
    suspend fun forgotPassword(@Body request: ForgotPasswordRequest): Response<Unit>
}

/**
 * Sales Data API Service
 * Maps to: SalesController.cs
 * Base Route: /api/sales
 *
 * Employee Usage: Primarily for inputting TODAY's sales data
 */
interface SalesApiService {

    /**
     * Get all sales data with optional date filtering
     * GET /api/sales?startDate={startDate}&endDate={endDate}
     * Employee: Use last 7 days max
     * Manager: Use last 30 days max
     */
    @GET("sales")
    suspend fun getAll(
        @Query("startDate") startDate: String? = null,
        @Query("endDate") endDate: String? = null
    ): Response<List<SalesDataDto>>

    /**
     * Get sales data by ID
     * GET /api/sales/{id}
     */
    @GET("sales/{id}")
    suspend fun getById(@Path("id") id: String): Response<SalesDataDto>

    /**
     * Get sales trend (grouped by date with recipe breakdown)
     * GET /api/sales/trend?startDate={startDate}&endDate={endDate}
     * Used for dashboard charts
     */
    @GET("sales/trend")
    suspend fun getTrend(
        @Query("startDate") startDate: String,
        @Query("endDate") endDate: String
    ): Response<List<SalesTrendDto>>

    /**
     * Get ingredient usage for a specific date
     * GET /api/sales/ingredients/{date}
     * Used for "Ingredient Breakdown" section
     */
    @GET("sales/ingredients/{date}")
    suspend fun getIngredientUsageByDate(@Path("date") date: String): Response<List<IngredientUsageDto>>

    /**
     * Get recipe sales for a specific date
     * GET /api/sales/recipes/{date}
     * Used for "Sales Distribution" pie chart
     */
    @GET("sales/recipes/{date}")
    suspend fun getRecipeSalesByDate(@Path("date") date: String): Response<List<RecipeSalesDto>>

    /**
     * Create new sales data (Employee primary function)
     * POST /api/sales
     * Employee: Can only create for TODAY
     */
    @POST("sales")
    suspend fun create(@Body request: CreateSalesDataRequest): Response<SalesDataDto>

    /**
     * Update existing sales data
     * PUT /api/sales/{id}
     * Employee: Can only update TODAY's entries
     */
    @PUT("sales/{id}")
    suspend fun update(
        @Path("id") id: String,
        @Body request: UpdateSalesDataRequest
    ): Response<SalesDataDto>

    /**
     * Delete sales data
     * DELETE /api/sales/{id}
     * Employee: Can only delete TODAY's entries
     */
    @DELETE("sales/{id}")
    suspend fun delete(@Path("id") id: String): Response<Unit>
}

/**
 * Wastage Data API Service
 * Maps to: WastageController.cs
 * Base Route: /api/wastage
 *
 * Employee Usage: Primarily for inputting TODAY's wastage data
 */
interface WastageApiService {

    /**
     * Get all wastage data with optional date filtering
     * GET /api/wastage?startDate={startDate}&endDate={endDate}
     * Employee: Use last 7 days max
     * Manager: Use last 30 days max
     */
    @GET("wastage")
    suspend fun getAll(
        @Query("startDate") startDate: String? = null,
        @Query("endDate") endDate: String? = null
    ): Response<List<WastageDataDto>>

    /**
     * Get wastage data by ID
     * GET /api/wastage/{id}
     */
    @GET("wastage/{id}")
    suspend fun getById(@Path("id") id: String): Response<WastageDataDto>

    /**
     * Get wastage trend (grouped by date with item breakdown)
     * GET /api/wastage/trend?startDate={startDate}&endDate={endDate}
     * Used for wastage dashboard charts
     */
    @GET("wastage/trend")
    suspend fun getTrend(
        @Query("startDate") startDate: String,
        @Query("endDate") endDate: String
    ): Response<List<WastageTrendDto>>

    /**
     * Create new wastage data (Employee primary function)
     * POST /api/wastage
     * Employee: Can only create for TODAY
     */
    @POST("wastage")
    suspend fun create(@Body request: CreateWastageDataRequest): Response<WastageDataDto>

    /**
     * Update existing wastage data
     * PUT /api/wastage/{id}
     * Employee: Can only update TODAY's entries
     */
    @PUT("wastage/{id}")
    suspend fun update(
        @Path("id") id: String,
        @Body request: UpdateWastageDataRequest
    ): Response<WastageDataDto>

    /**
     * Delete wastage data
     * DELETE /api/wastage/{id}
     * Employee: Can only delete TODAY's entries
     */
    @DELETE("wastage/{id}")
    suspend fun delete(@Path("id") id: String): Response<Unit>
}

/**
 * Forecast Data API Service
 * Maps to: ForecastController.cs
 * Base Route: /api/forecast
 *
 * Employee/Manager Usage: View ML predictions and calendar signals
 */
interface ForecastApiService {

    /**
     * Get forecast for next N days (1-30)
     * GET /api/forecast?days={days}
     * Employee: Typically views 7 days
     * Manager: Can view up to 30 days
     */
    @GET("forecast")
    suspend fun getForecast(@Query("days") days: Int = 7): Response<List<ForecastDto>>

    /**
     * Get forecast summary for next N days
     * GET /api/forecast/summary?days={days}
     * Used for "Prediction Summary" card
     */
    @GET("forecast/summary")
    suspend fun getForecastSummary(@Query("days") days: Int = 7): Response<List<ForecastSummaryDto>>

    /**
     * Get current weather
     * GET /api/forecast/weather
     * Used for dashboard "Current Weather" widget
     */
    @GET("forecast/weather")
    suspend fun getWeather(): Response<WeatherDto>

    /**
     * Get holidays for a specific year
     * GET /api/forecast/holidays/{year}
     * Used for "Upcoming Events" section
     */
    @GET("forecast/holidays/{year}")
    suspend fun getHolidays(@Path("year") year: Int): Response<List<HolidayDto>>

    /**
     * Get tomorrow's forecast and calendar information
     * GET /api/forecast/tomorrow
     * Primary endpoint for employee dashboard
     */
    @GET("forecast/tomorrow")
    suspend fun getTomorrowForecast(): Response<TomorrowForecastDto>

    /**
     * Get calendar and weather information for a specific date
     * GET /api/forecast/calendar/{date}
     */
    @GET("forecast/calendar/{date}")
    suspend fun getCalendarDay(@Path("date") date: String): Response<CalendarDayDto>

    /**
     * Get calendar and weather information for a date range
     * GET /api/forecast/calendar?startDate={startDate}&endDate={endDate}
     * Used for dashboard overview
     */
    @GET("forecast/calendar")
    suspend fun getCalendarRange(
        @Query("startDate") startDate: String,
        @Query("endDate") endDate: String
    ): Response<List<CalendarDayDto>>
}

/**
 * Recipe API Service
 * Maps to: RecipesController.cs
 * Base Route: /api/recipes
 *
 * Employee Usage: READ-ONLY (for viewing recipes in dropdowns)
 * Manager Usage: Full CRUD (typically done on web)
 */
interface RecipeApiService {

    /**
     * Get all recipes
     * GET /api/recipes
     * Employee: Uses this to populate "Select Dish" dropdown
     */
    @GET("recipes")
    suspend fun getAll(): Response<List<RecipeDto>>

    /**
     * Get recipe by ID
     * GET /api/recipes/{id}
     * Used to view recipe details
     */
    @GET("recipes/{id}")
    suspend fun getById(@Path("id") id: String): Response<RecipeDto>

    /**
     * Create new recipe (Manager only - typically web-only)
     * POST /api/recipes
     */
    @POST("recipes")
    suspend fun create(@Body request: CreateRecipeRequest): Response<RecipeDto>

    /**
     * Update existing recipe (Manager only - typically web-only)
     * PUT /api/recipes/{id}
     */
    @PUT("recipes/{id}")
    suspend fun update(
        @Path("id") id: String,
        @Body request: UpdateRecipeRequest
    ): Response<RecipeDto>

    /**
     * Delete recipe (Manager only - typically web-only)
     * DELETE /api/recipes/{id}
     */
    @DELETE("recipes/{id}")
    suspend fun delete(@Path("id") id: String): Response<Unit>
}

/**
 * Ingredient API Service
 * Maps to: IngredientsController.cs
 * Base Route: /api/ingredients
 *
 * Employee Usage: READ-ONLY (for viewing ingredients in wastage dropdowns)
 * Manager Usage: Full CRUD (typically done on web)
 */
interface IngredientApiService {

    /**
     * Get all ingredients
     * GET /api/ingredients
     * Employee: Uses this to populate wastage "Select Item" dropdown
     */
    @GET("ingredients")
    suspend fun getAll(): Response<List<IngredientDto>>

    /**
     * Get ingredient by ID
     * GET /api/ingredients/{id}
     */
    @GET("ingredients/{id}")
    suspend fun getById(@Path("id") id: String): Response<IngredientDto>

    /**
     * Create new ingredient (Manager only - typically web-only)
     * POST /api/ingredients
     */
    @POST("ingredients")
    suspend fun create(@Body request: CreateIngredientRequest): Response<IngredientDto>

    /**
     * Update existing ingredient (Manager only - typically web-only)
     * PUT /api/ingredients/{id}
     */
    @PUT("ingredients/{id}")
    suspend fun update(
        @Path("id") id: String,
        @Body request: UpdateIngredientRequest
    ): Response<IngredientDto>

    /**
     * Delete ingredient (Manager only - typically web-only)
     * DELETE /api/ingredients/{id}
     */
    @DELETE("ingredients/{id}")
    suspend fun delete(@Path("id") id: String): Response<Unit>
}

/**
 * Store API Service
 * Maps to: StoreController.cs
 * Base Route: /api/store
 *
 * Employee/Manager Usage: View store info (shown in top bar: "Aunty May's Cafe | Orchard Central")
 * Manager Usage: Setup/update store (typically web-only)
 */
interface StoreApiService {

    /**
     * Get store information for current user
     * GET /api/store
     * Used to display store name in app header
     */
    @GET("store")
    suspend fun getStore(): Response<StoreDto>

    /**
     * Check if store setup is complete
     * GET /api/store/status
     * Used after login to check if onboarding needed
     */
    @GET("store/status")
    suspend fun getStoreStatus(): Response<Map<String, Boolean>>
}