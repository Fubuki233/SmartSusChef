package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.WastageApiService
import com.smartsuschef.mobile.network.dto.CreateWastageDataRequest
import com.smartsuschef.mobile.network.dto.ItemWastageDto
import com.smartsuschef.mobile.network.dto.WastageDataDto
import com.smartsuschef.mobile.network.dto.WastageTrendDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import javax.inject.Inject

class WastageRepository @Inject constructor(
    private val wastageApiService: WastageApiService) {
    suspend fun getTrend(startDate: String, endDate: String): Resource<List<WastageTrendDto>> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val mockWastageTrend = listOf(
            WastageTrendDto(
                date = "2024-01-01",
                totalQuantity = 5.2,
                totalCarbonFootprint = 12.5,
                itemBreakdown = listOf(
                    ItemWastageDto(
                        ingredientId = "ing-tomato",
                        displayName = "Tomato",
                        unit = "kg",
                        quantity = 2.0,
                        carbonFootprint = 3.0
                    ),
                    ItemWastageDto(
                        recipeId = "rec-fried-rice",
                        displayName = "Fried Rice",
                        unit = "plate",
                        quantity = 1.0,
                        carbonFootprint = 5.0
                    )
                )
            ),
            WastageTrendDto(
                date = "2024-01-02",
                totalQuantity = 3.8,
                totalCarbonFootprint = 8.1,
                itemBreakdown = listOf(
                    ItemWastageDto(
                        ingredientId = "ing-lettuce",
                        displayName = "Lettuce",
                        unit = "kg",
                        quantity = 1.5,
                        carbonFootprint = 2.1
                    )
                )
            )
        )
        return Resource.Success(mockWastageTrend)

        /*
        // --- ORIGINAL IMPLEMENTATION ---
        return withContext(Dispatchers.IO) {
            try {
                val response = wastageApiService.getTrend(startDate, endDate)
                if (response.isSuccessful) {
                    Resource.Success(response.body() ?: emptyList())
                } else {
                    Resource.Error("Failed to fetch wastage trend: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
        */
    }

    suspend fun create(request: CreateWastageDataRequest): Resource<WastageDataDto> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val fakeDto = WastageDataDto(
            id = "waste-${System.currentTimeMillis()}",
            date = request.date,
            ingredientId = request.ingredientId,
            recipeId = request.recipeId,
            displayName = "Mocked Item", // In a real scenario, you might fetch this
            unit = "unit",
            quantity = request.quantity,
            carbonFootprint = request.quantity * 1.5 // Fake carbon calculation
        )
        return Resource.Success(fakeDto)
        
        /*
        // --- ORIGINAL IMPLEMENTATION ---
        return withContext(Dispatchers.IO) {
            try {
                val response = wastageApiService.create(request)
                if (response.isSuccessful) {
                    Resource.Success(response.body()!!)
                } else {
                    Resource.Error("Failed to add wastage: ${response.message()}")
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
