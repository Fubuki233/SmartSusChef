package com.smartsuschef.mobile.network.api

import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.network.dto.LoginResponse
import com.smartsuschef.mobile.network.dto.UserDto
import kotlinx.coroutines.delay
import retrofit2.Response

/**
 * A mock implementation of the AuthApiService for development and testing.
 * This avoids the need to run the real backend.
 */
class MockAuthApiService : AuthApiService {

    override suspend fun login(request: LoginRequest): Response<LoginResponse> {
        // Simulate network delay
        delay(500)

        // Simulate a successful login
        if (request.username == "test" && request.password == "password") {
            val mockUser = UserDto(
                id = "user-123",
                username = "test",
                name = "Test User",
                email = "test@example.com",
                role = "employee", // Corrected to lowercase
                status = "Active"   // Added missing status field
            )
            val mockResponse = LoginResponse(
                token = "fake-jwt-token-for-testing",
                user = mockUser,
                storeSetupRequired = false // Added missing storeSetupRequired field
            )
            // Return a successful HTTP 200 response
            return Response.success(mockResponse)
        } else {
            // Simulate a login failure
            // The `null` body with an error code is how Retrofit signals an error
            return Response.error(401, okhttp3.ResponseBody.create(null, "Invalid credentials"))
        }
    }

    override suspend fun getCurrentUser(): Response<UserDto> {
        delay(500)
        val mockUser = UserDto(
            id = "user-123",
            username = "test",
            name = "Test User",
            email = "test@example.com",
            role = "employee", // Corrected to lowercase
            status = "Active"   // Added missing status field
        )
        return Response.success(mockUser)
    }

    override suspend fun updateOwnProfile(request: com.smartsuschef.mobile.network.dto.UpdateProfileRequest): Response<UserDto> {
        TODO("Not yet implemented for mock")
    }

    override suspend fun changePassword(request: com.smartsuschef.mobile.network.dto.ChangePasswordRequest): Response<Unit> {
        TODO("Not yet implemented for mock")
    }

    override suspend fun resetPassword(request: com.smartsuschef.mobile.network.dto.PasswordResetRequest): Response<Unit> {
        TODO("Not yet implemented for mock")
    }
}

class MockSalesApiService : SalesApiService {
    override suspend fun getAll(startDate: String?, endDate: String?): Response<List<com.smartsuschef.mobile.network.dto.SalesDataDto>> { TODO("Not yet implemented") }
    override suspend fun getById(id: String): Response<com.smartsuschef.mobile.network.dto.SalesDataDto> { TODO("Not yet implemented") }
    override suspend fun getTrend(startDate: String, endDate: String): Response<List<com.smartsuschef.mobile.network.dto.SalesTrendDto>> { TODO("Not yet implemented") }
    override suspend fun getIngredientUsageByDate(date: String): Response<List<com.smartsuschef.mobile.network.dto.IngredientUsageDto>> { TODO("Not yet implemented") }
    override suspend fun getRecipeSalesByDate(date: String): Response<List<com.smartsuschef.mobile.network.dto.RecipeSalesDto>> { TODO("Not yet implemented") }
    override suspend fun create(request: com.smartsuschef.mobile.network.dto.CreateSalesDataRequest): Response<com.smartsuschef.mobile.network.dto.SalesDataDto> { TODO("Not yet implemented") }
    override suspend fun update(id: String, request: com.smartsuschef.mobile.network.dto.UpdateSalesDataRequest): Response<com.smartsuschef.mobile.network.dto.SalesDataDto> { TODO("Not yet implemented") }
    override suspend fun delete(id: String): Response<Unit> { TODO("Not yet implemented") }
}

class MockWastageApiService : WastageApiService {
    override suspend fun getAll(startDate: String?, endDate: String?): Response<List<com.smartsuschef.mobile.network.dto.WastageDataDto>> { TODO("Not yet implemented") }
    override suspend fun getById(id: String): Response<com.smartsuschef.mobile.network.dto.WastageDataDto> { TODO("Not yet implemented") }
    override suspend fun getTrend(startDate: String, endDate: String): Response<List<com.smartsuschef.mobile.network.dto.WastageTrendDto>> { TODO("Not yet implemented") }
    override suspend fun create(request: com.smartsuschef.mobile.network.dto.CreateWastageDataRequest): Response<com.smartsuschef.mobile.network.dto.WastageDataDto> { TODO("Not yet implemented") }
    override suspend fun update(id: String, request: com.smartsuschef.mobile.network.dto.UpdateWastageDataRequest): Response<com.smartsuschef.mobile.network.dto.WastageDataDto> { TODO("Not yet implemented") }
    override suspend fun delete(id: String): Response<Unit> { TODO("Not yet implemented") }
}

class MockForecastApiService : ForecastApiService {
    override suspend fun getForecast(days: Int): Response<List<com.smartsuschef.mobile.network.dto.ForecastDto>> { TODO("Not yet implemented") }
    override suspend fun getForecastSummary(days: Int): Response<List<com.smartsuschef.mobile.network.dto.ForecastSummaryDto>> { TODO("Not yet implemented") }
    override suspend fun getWeather(): Response<com.smartsuschef.mobile.network.dto.WeatherDto> { TODO("Not yet implemented") }
    override suspend fun getHolidays(year: Int): Response<List<com.smartsuschef.mobile.network.dto.HolidayDto>> { TODO("Not yet implemented") }
    override suspend fun getTomorrowForecast(): Response<com.smartsuschef.mobile.network.dto.TomorrowForecastDto> { TODO("Not yet implemented") }
    override suspend fun getCalendarDay(date: String): Response<com.smartsuschef.mobile.network.dto.CalendarDayDto> { TODO("Not yet implemented") }
    override suspend fun getCalendarRange(startDate: String, endDate: String): Response<List<com.smartsuschef.mobile.network.dto.CalendarDayDto>> { TODO("Not yet implemented") }
}

class MockRecipeApiService : RecipeApiService {
    override suspend fun getAll(): Response<List<com.smartsuschef.mobile.network.dto.RecipeDto>> { TODO("Not yet implemented") }
    override suspend fun getById(id: String): Response<com.smartsuschef.mobile.network.dto.RecipeDto> { TODO("Not yet implemented") }
    override suspend fun create(request: com.smartsuschef.mobile.network.dto.CreateRecipeRequest): Response<com.smartsuschef.mobile.network.dto.RecipeDto> { TODO("Not yet implemented") }
    override suspend fun update(id: String, request: com.smartsuschef.mobile.network.dto.UpdateRecipeRequest): Response<com.smartsuschef.mobile.network.dto.RecipeDto> { TODO("Not yet implemented") }
    override suspend fun delete(id: String): Response<Unit> { TODO("Not yet implemented") }
}

class MockIngredientApiService : IngredientApiService {
    override suspend fun getAll(): Response<List<com.smartsuschef.mobile.network.dto.IngredientDto>> { TODO("Not yet implemented") }
    override suspend fun getById(id: String): Response<com.smartsuschef.mobile.network.dto.IngredientDto> { TODO("Not yet implemented") }
    override suspend fun create(request: com.smartsuschef.mobile.network.dto.CreateIngredientRequest): Response<com.smartsuschef.mobile.network.dto.IngredientDto> { TODO("Not yet implemented") }
    override suspend fun update(id: String, request: com.smartsuschef.mobile.network.dto.UpdateIngredientRequest): Response<com.smartsuschef.mobile.network.dto.IngredientDto> { TODO("Not yet implemented") }
    override suspend fun delete(id: String): Response<Unit> { TODO("Not yet implemented") }
}

class MockStoreApiService : StoreApiService {
    override suspend fun getStore(): Response<com.smartsuschef.mobile.network.dto.StoreDto> { TODO("Not yet implemented") }
    override suspend fun getStoreStatus(): Response<Map<String, Boolean>> { TODO("Not yet implemented") }
}
