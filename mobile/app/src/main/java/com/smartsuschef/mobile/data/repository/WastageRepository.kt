package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.WastageApiService
import com.smartsuschef.mobile.network.dto.CreateWastageDataRequest
import com.smartsuschef.mobile.network.dto.ItemWastageDto
import com.smartsuschef.mobile.network.dto.UpdateWastageDataRequest
import com.smartsuschef.mobile.network.dto.WastageDataDto
import com.smartsuschef.mobile.network.dto.WastageTrendDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import java.time.LocalDate
import javax.inject.Inject

class WastageRepository @Inject constructor(
    private val wastageApiService: WastageApiService) {
    suspend fun getTrend(startDate: String, endDate: String): Resource<List<WastageTrendDto>> {
        return withContext(Dispatchers.IO) {
            val trend = mutableListOf<WastageTrendDto>()

            if (startDate == endDate) {
                val totalQuantity = (1..10).random().toDouble()
                val totalCarbon = totalQuantity * (1..5).random()
                trend.add(
                    WastageTrendDto(
                        date = endDate,
                        totalQuantity = totalQuantity,
                        totalCarbonFootprint = totalCarbon,
                        itemBreakdown = createMockBreakdown()
                    )
                )
            } else {
                val weekTrend = (0..6).map { dayOffset ->
                    val date = LocalDate.now().minusDays(dayOffset.toLong()).toString()
                    val totalQuantity = (5..15).random().toDouble()
                    val totalCarbon = totalQuantity * (1..5).random()

                    WastageTrendDto(
                        date = date,
                        totalQuantity = totalQuantity,
                        totalCarbonFootprint = totalCarbon,
                        itemBreakdown = createMockBreakdown()
                    )
                }
                trend.addAll(weekTrend)
            }
            Resource.Success(trend.reversed())
        }
    }

    private val mockIngredients = listOf(
        ItemWastageDto(ingredientId = "ing-001", displayName = "Carrot", unit = "kg", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-002", displayName = "Potato", unit = "kg", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-003", displayName = "Onion", unit = "kg", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-004", displayName = "Chicken Breast", unit = "kg", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-005", displayName = "Beef Mince", unit = "kg", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-006", displayName = "Tomato", unit = "kg", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-007", displayName = "Lettuce", unit = "head", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-008", displayName = "Cheese", unit = "kg", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-009", displayName = "Bread", unit = "loaf", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(ingredientId = "ing-010", displayName = "Milk", unit = "litre", quantity = 0.0, carbonFootprint = 0.0)
    )

    private val mockRecipes = listOf(
        ItemWastageDto(recipeId = "rec-001", displayName = "Chicken Soup", unit = "portion", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(recipeId = "rec-002", displayName = "Beef Stew", unit = "portion", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(recipeId = "rec-003", displayName = "Caesar Salad", unit = "portion", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(recipeId = "rec-004", displayName = "Spaghetti Bolognese", unit = "portion", quantity = 0.0, carbonFootprint = 0.0),
        ItemWastageDto(recipeId = "rec-005", displayName = "Fish and Chips", unit = "portion", quantity = 0.0, carbonFootprint = 0.0)
    )

    private fun createMockBreakdown(): List<ItemWastageDto> {
        val breakdown = mutableListOf<ItemWastageDto>()
        val itemCount = (1..5).random() // Generate between 1 and 5 items

        repeat(itemCount) {
            val isRecipe = (0..1).random() == 1
            val item = if (isRecipe) {
                mockRecipes.random()
            } else {
                mockIngredients.random()
            }

            breakdown.add(item.copy(
                quantity = (1..5).random().toDouble(),
                carbonFootprint = (1..5).random().toDouble()
            ))
        }
        return breakdown
    }

    suspend fun create(request: CreateWastageDataRequest): Resource<WastageDataDto> {
        val fakeDto = WastageDataDto(
            id = "waste-${System.currentTimeMillis()}",
            date = request.date,
            ingredientId = request.ingredientId,
            recipeId = request.recipeId,
            displayName = "Mocked Item",
            unit = "unit",
            quantity = request.quantity,
            carbonFootprint = request.quantity * 1.5
        )
        return Resource.Success(fakeDto)
    }

    suspend fun update(id: String, request: UpdateWastageDataRequest): Resource<WastageDataDto> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val fakeDto = WastageDataDto(
            id = id,
            date = request.date,
            ingredientId = request.ingredientId,
            recipeId = request.recipeId,
            displayName = "Mocked Item",
            unit = "unit",
            quantity = request.quantity,
            carbonFootprint = request.quantity * 1.5
        )
        return Resource.Success(fakeDto)
    }

    suspend fun delete(id: String): Resource<Unit> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        return Resource.Success(Unit)
    }
}
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
        }
        */
