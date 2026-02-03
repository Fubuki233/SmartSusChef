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
import kotlin.random.Random

class ForecastRepository @Inject constructor(
    private val forecastApiService: ForecastApiService
) {
    suspend fun getForecast(days: Int): Resource<List<ForecastDto>> {
        // --- MOCK IMPLEMENTATION FOR 7-DAY DASHBOARD ---
        val today = java.time.LocalDate.now()
        val mockForecasts = mutableListOf<ForecastDto>()

        // Define a consistent set of dishes and ingredients to populate the table and stacks
        val dishes = listOf("Hainanese Chicken Rice", "Laksa", "Beef Rendang")

        for (i in 0 until days) {
            val forecastDate = today.plusDays(i.toLong()).toString()

            dishes.forEach { dishName ->
                mockForecasts.add(
                    ForecastDto(
                        date = forecastDate,
                        recipeId = "recipe-${dishName.lowercase().replace(" ", "-")}",
                        recipeName = dishName,
                        quantity = (20..60).random(), // Varied quantities for stacked bar segments
                        ingredients = listOf(
                            ForecastIngredientDto("ing-1", "Beef", "kg", Random.nextDouble(1.0, 5.1)), // Correct way for Double
                            ForecastIngredientDto("ing-2", "Coconut Milk", "L", Random.nextDouble(2.0, 8.1)),
                            ForecastIngredientDto("ing-3", "Shrimp Paste", "kg", Random.nextDouble(0.5, 2.1)),
                            ForecastIngredientDto("ing-4", "Chicken", "kg", Random.nextDouble(1.5, 7.1)),
                            ForecastIngredientDto("ing-6", "Lemongrass", "kg", Random.nextDouble(0.5, 2.1)),
                            ForecastIngredientDto("ing-7", "Ginger", "kg", Random.nextDouble(0.5, 2.1)),
                            ForecastIngredientDto("ing-8", "Garlic", "kg", Random.nextDouble(0.5, 2.1)),
                            ForecastIngredientDto("ing-9", "Cooking Oil", "L", Random.nextDouble(0.5, 2.1))
                        )
                    )
                )
            }
        }
        return Resource.Success(mockForecasts)
    }
}
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