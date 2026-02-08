package com.smartsuschef.mobile.network.api

import com.smartsuschef.mobile.network.dto.CalendarDayDto
import com.smartsuschef.mobile.network.dto.ChangePasswordRequest
import com.smartsuschef.mobile.network.dto.CreateIngredientRequest
import com.smartsuschef.mobile.network.dto.CreateRecipeRequest
import com.smartsuschef.mobile.network.dto.CreateSalesDataRequest
import com.smartsuschef.mobile.network.dto.CreateWastageDataRequest
import com.smartsuschef.mobile.network.dto.ForecastDto
import com.smartsuschef.mobile.network.dto.ForecastSummaryDto
import com.smartsuschef.mobile.network.dto.HolidayDto
import com.smartsuschef.mobile.network.dto.IngredientDto
import com.smartsuschef.mobile.network.dto.IngredientUsageDto
import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.network.dto.LoginResponse
import com.smartsuschef.mobile.network.dto.ForgotPasswordRequest
import com.smartsuschef.mobile.network.dto.RecipeDto
import com.smartsuschef.mobile.network.dto.RecipeSalesDto
import com.smartsuschef.mobile.network.dto.SalesDataDto
import com.smartsuschef.mobile.network.dto.SalesTrendDto
import com.smartsuschef.mobile.network.dto.StoreDto
import com.smartsuschef.mobile.network.dto.TomorrowForecastDto
import com.smartsuschef.mobile.network.dto.UpdateIngredientRequest
import com.smartsuschef.mobile.network.dto.UpdateProfileRequest
import com.smartsuschef.mobile.network.dto.UpdateRecipeRequest
import com.smartsuschef.mobile.network.dto.UpdateSalesDataRequest
import com.smartsuschef.mobile.network.dto.UpdateWastageDataRequest
import com.smartsuschef.mobile.network.dto.UserDto
import com.smartsuschef.mobile.network.dto.WastageDataDto
import com.smartsuschef.mobile.network.dto.WastageTrendDto
import com.smartsuschef.mobile.network.dto.WeatherDto
import kotlinx.coroutines.delay
import okhttp3.ResponseBody
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
                status = "Active",   // Added missing status field
                createdAt = "2026-02-08T00:00:00",
                updatedAt = "2026-02-08T00:00:00"
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
            return Response.error(401, ResponseBody.create(null, "Invalid credentials"))
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
            status = "Active",   // Added missing status field
            createdAt = "2026-02-08T00:00:00",
            updatedAt = "2026-02-08T00:00:00"
        )
        return Response.success(mockUser)
    }

    override suspend fun updateOwnProfile(request: UpdateProfileRequest): Response<UserDto> {
        TODO("Not yet implemented for mock")
    }

    override suspend fun changePassword(request: ChangePasswordRequest): Response<Unit> {
        TODO("Not yet implemented for mock")
    }

    override suspend fun forgotPassword(request: ForgotPasswordRequest): Response<Unit> {
        TODO("Not yet implemented for mock")
    }
}

class MockSalesApiService : SalesApiService {
    override suspend fun getAll(startDate: String?, endDate: String?): Response<List<SalesDataDto>> { TODO("Not yet implemented") }
    override suspend fun getById(id: String): Response<SalesDataDto> { TODO("Not yet implemented") }
    override suspend fun getTrend(startDate: String, endDate: String): Response<List<SalesTrendDto>> { TODO("Not yet implemented") }
    override suspend fun getIngredientUsageByDate(date: String): Response<List<IngredientUsageDto>> { TODO("Not yet implemented") }
    override suspend fun getRecipeSalesByDate(date: String): Response<List<RecipeSalesDto>> { TODO("Not yet implemented") }
    override suspend fun create(request: CreateSalesDataRequest): Response<SalesDataDto> { TODO("Not yet implemented") }
    override suspend fun update(id: String, request: UpdateSalesDataRequest): Response<SalesDataDto> { TODO("Not yet implemented") }
    override suspend fun delete(id: String): Response<Unit> { TODO("Not yet implemented") }
}

class MockWastageApiService : WastageApiService {
    override suspend fun getAll(startDate: String?, endDate: String?): Response<List<WastageDataDto>> { TODO("Not yet implemented") }
    override suspend fun getById(id: String): Response<WastageDataDto> { TODO("Not yet implemented") }
    override suspend fun getTrend(startDate: String, endDate: String): Response<List<WastageTrendDto>> { TODO("Not yet implemented") }
    override suspend fun create(request: CreateWastageDataRequest): Response<WastageDataDto> { TODO("Not yet implemented") }
    override suspend fun update(id: String, request: UpdateWastageDataRequest): Response<WastageDataDto> { TODO("Not yet implemented") }
    override suspend fun delete(id: String): Response<Unit> { TODO("Not yet implemented") }
}

class MockForecastApiService : ForecastApiService {
    override suspend fun getForecast(days: Int): Response<List<ForecastDto>> { TODO("Not yet implemented") }
    override suspend fun getForecastSummary(days: Int): Response<List<ForecastSummaryDto>> { TODO("Not yet implemented") }
    override suspend fun getWeather(): Response<WeatherDto> { TODO("Not yet implemented") }
    override suspend fun getHolidays(year: Int): Response<List<HolidayDto>> { TODO("Not yet implemented") }
    override suspend fun getTomorrowForecast(): Response<TomorrowForecastDto> { TODO("Not yet implemented") }
    override suspend fun getCalendarDay(date: String): Response<CalendarDayDto> { TODO("Not yet implemented") }
    override suspend fun getCalendarRange(startDate: String, endDate: String): Response<List<CalendarDayDto>> { TODO("Not yet implemented") }
}

class MockRecipeApiService : RecipeApiService {
    override suspend fun getAll(): Response<List<RecipeDto>> { TODO("Not yet implemented") }
    override suspend fun getById(id: String): Response<RecipeDto> { TODO("Not yet implemented") }
    override suspend fun create(request: CreateRecipeRequest): Response<RecipeDto> { TODO("Not yet implemented") }
    override suspend fun update(id: String, request: UpdateRecipeRequest): Response<RecipeDto> { TODO("Not yet implemented") }
    override suspend fun delete(id: String): Response<Unit> { TODO("Not yet implemented") }
}

class MockIngredientApiService : IngredientApiService {
    override suspend fun getAll(): Response<List<IngredientDto>> { TODO("Not yet implemented") }
    override suspend fun getById(id: String): Response<IngredientDto> { TODO("Not yet implemented") }
    override suspend fun create(request: CreateIngredientRequest): Response<IngredientDto> { TODO("Not yet implemented") }
    override suspend fun update(id: String, request: UpdateIngredientRequest): Response<IngredientDto> { TODO("Not yet implemented") }
    override suspend fun delete(id: String): Response<Unit> { TODO("Not yet implemented") }
}

class MockStoreApiService : StoreApiService {
    override suspend fun getStore(): Response<StoreDto> { TODO("Not yet implemented") }
    override suspend fun getStoreStatus(): Response<Map<String, Boolean>> { TODO("Not yet implemented") }
}