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
        val mockWastageTrend = (0..6).map { day ->
            val date = java.time.LocalDate.now().minusDays(day.toLong()).toString()
            val totalQuantity = (1..10).random().toDouble()
            val totalCarbonFootprint = totalQuantity * (1..5).random()

            val itemBreakdown = listOf(
                ItemWastageDto(
                    ingredientId = "ing-tomato",
                    displayName = "Tomato",
                    unit = "kg",
                    quantity = (1..5).random().toDouble(),
                    carbonFootprint = (1..5).random().toDouble()
                ),
                ItemWastageDto(
                    recipeId = "sub-recipe-1",
                    displayName = "Chicken Stock",
                    unit = "litre",
                    quantity = (1..5).random().toDouble(),
                    carbonFootprint = (1..5).random().toDouble()
                ),
                ItemWastageDto(
                    recipeId = "recipe-1",
                    displayName = "Hainanese Chicken Rice",
                    unit = "plate",
                    quantity = (1..5).random().toDouble(),
                    carbonFootprint = (1..5).random().toDouble()
                )
            )
            WastageTrendDto(date, totalQuantity, totalCarbonFootprint, itemBreakdown)
        }
        return Resource.Success(mockWastageTrend.reversed())

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
