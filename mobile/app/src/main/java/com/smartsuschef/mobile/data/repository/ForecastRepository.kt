package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.ForecastApiService
import com.smartsuschef.mobile.network.dto.ForecastDto
import com.smartsuschef.mobile.network.dto.ForecastIngredientDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import javax.inject.Inject

class ForecastRepository @Inject constructor(
    private val forecastApiService: ForecastApiService
) {
    suspend fun getForecast(days: Int): Resource<List<ForecastDto>> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val today = java.time.LocalDate.now()
        val mockForecasts = listOf(
            ForecastDto(
                date = today.toString(),
                recipeId = "recipe-chicken-rice",
                recipeName = "Chicken Rice",
                quantity = 50,
                ingredients = listOf(
                    ForecastIngredientDto(ingredientId = "ing-chicken", ingredientName = "Chicken", unit = "kg", quantity = 10.0),
                    ForecastIngredientDto(ingredientId = "ing-rice", ingredientName = "Rice", unit = "kg", quantity = 5.0)
                )
            ),
            ForecastDto(
                date = today.plusDays(1).toString(),
                recipeId = "recipe-laksa",
                recipeName = "Laksa",
                quantity = 30,
                ingredients = listOf(
                    ForecastIngredientDto(ingredientId = "ing-noodles", ingredientName = "Noodles", unit = "kg", quantity = 6.0),
                    ForecastIngredientDto(ingredientId = "ing-prawns", ingredientName = "Prawns", unit = "kg", quantity = 3.0)
                )
            )
        )
        return Resource.Success(mockForecasts)

        /*
        // --- ORIGINAL IMPLEMENTATION ---
        return withContext(Dispatchers.IO) {
            try {
                val response = forecastApiService.getForecast(days)
                if (response.isSuccessful) {
                    Resource.Success(response.body() ?: emptyList())
                } else {
                    Resource.Error("Failed to fetch forecast: ${response.message()}")
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
