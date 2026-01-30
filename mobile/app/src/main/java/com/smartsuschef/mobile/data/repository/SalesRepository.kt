package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.SalesApiService
import com.smartsuschef.mobile.network.dto.CreateSalesDataRequest
import com.smartsuschef.mobile.network.dto.SalesDataDto
import com.smartsuschef.mobile.network.dto.SalesTrendDto
import com.smartsuschef.mobile.network.dto.IngredientUsageDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import javax.inject.Inject

class SalesRepository @Inject constructor(
    private val salesApiService: SalesApiService
) {
    suspend fun getAll(startDate: String?, endDate: String?): Resource<List<SalesDataDto>> {
        return withContext(Dispatchers.IO) {
            try {
                val response = salesApiService.getAll(startDate, endDate)
                if (response.isSuccessful) {
                    Resource.Success(response.body() ?: emptyList())
                } else {
                    Resource.Error("Failed to fetch sales history: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }

    suspend fun getTrend(startDate: String, endDate: String): Resource<List<SalesTrendDto>> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val fakeTrend = listOf(
            SalesTrendDto(date = "2024-01-01", totalQuantity = 100, recipeBreakdown = emptyList()),
            SalesTrendDto(date = "2024-01-02", totalQuantity = 120, recipeBreakdown = emptyList()),
            SalesTrendDto(date = "2024-01-03", totalQuantity = 90, recipeBreakdown = emptyList())
        )
        return Resource.Success(fakeTrend)

        /*
        // --- ORIGINAL IMPLEMENTATION ---
        return withContext(Dispatchers.IO) {
            try {
                val response = salesApiService.getTrend(startDate, endDate)
                if (response.isSuccessful) {
                    Resource.Success(response.body() ?: emptyList())
                } else {
                    Resource.Error("Failed to fetch sales trend: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
        */
    }

    suspend fun getIngredientUsageByDate(date: String): Resource<List<IngredientUsageDto>> {
        return withContext(Dispatchers.IO) {
            try {
                val response = salesApiService.getIngredientUsageByDate(date)
                if (response.isSuccessful) {
                    Resource.Success(response.body() ?: emptyList())
                } else {
                    Resource.Error("Failed to fetch ingredient usage: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }

    suspend fun create(request: CreateSalesDataRequest): Resource<SalesDataDto> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val fakeDto = SalesDataDto(
            id = "sales-${System.currentTimeMillis()}",
            date = request.date,
            recipeId = request.recipeId,
            recipeName = "Mocked Recipe", // In a real scenario, you might fetch this
            quantity = request.quantity
        )
        return Resource.Success(fakeDto)

        /*
        // --- ORIGINAL IMPLEMENTATION ---
        return withContext(Dispatchers.IO) {
            try {
                val response = salesApiService.create(request)
                if (response.isSuccessful) {
                    Resource.Success(response.body()!!)
                } else {
                    Resource.Error("Failed to add sale: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
        */
    }
}
