package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.ForecastApiService
import com.smartsuschef.mobile.network.dto.ForecastDto
import com.smartsuschef.mobile.network.dto.ForecastIngredientDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.time.LocalDate
import javax.inject.Inject
import kotlin.random.Random

class ForecastRepository @Inject constructor(
    private val forecastApiService: ForecastApiService
) {
    /**
     * Get forecast for NEXT 7 days (future predictions)
     * Returns 7 dates with aggregated dish quantities per date
     */
    suspend fun getForecast(days: Int): Resource<List<ForecastDto>> {
        return withContext(Dispatchers.IO) {
            val today = LocalDate.now()
            val mockForecasts = mutableListOf<ForecastDto>()

            // Create ONE aggregated forecast per day for next 7 days
            for (i in 0 until days) {
                val date = today.plusDays(i.toLong()).toString()

                // Aggregate all dishes into ONE entry per day
                mockForecasts.add(
                    ForecastDto(
                        date = date,
                        recipeId = "aggregated",
                        recipeName = "All Dishes",
                        quantity = Random.nextInt(200, 350), // Total predicted for the day
                        actualQuantity = 0, // Not applicable for future
                        ingredients = generateMockIngredients()
                    )
                )
            }
            Resource.Success(mockForecasts)
        }
    }

    /**
     * Get PAST 7 days comparison data (predicted vs actual)
     * Returns 7 dates with both predicted and actual values
     */
    suspend fun getPastComparison(): Resource<List<ForecastDto>> {
        return withContext(Dispatchers.IO) {
            val today = LocalDate.now()
            val pastData = mutableListOf<ForecastDto>()

            // Previous 7 days with predicted vs actual
            for (i in 7 downTo 1) {
                val date = today.minusDays(i.toLong()).toString()
                val predicted = Random.nextInt(200, 350)
                val actual = (predicted * Random.nextDouble(0.85, 1.15)).toInt() // ±15% variance

                pastData.add(
                    ForecastDto(
                        date = date,
                        recipeId = "aggregated",
                        recipeName = "All Dishes",
                        quantity = predicted,
                        actualQuantity = actual,
                        ingredients = emptyList() // Not needed for comparison
                    )
                )
            }
            Resource.Success(pastData)
        }
    }

    /**
     * Get dish breakdown for stacked bar chart (next 7 days)
     * Returns individual dish forecasts per day
     */
    suspend fun getDishBreakdown(days: Int): Resource<Map<String, List<ForecastDto>>> {
        return withContext(Dispatchers.IO) {
            val today = LocalDate.now()
            val dishNames = listOf("Hainanese Chicken Rice", "Laksa", "Beef Rendang")
            val breakdown = mutableMapOf<String, List<ForecastDto>>()

            for (i in 0 until days) {
                val date = today.plusDays(i.toLong()).toString()
                val dishForecasts = dishNames.map { dishName ->
                    ForecastDto(
                        date = date,
                        recipeId = "recipe-${dishName.lowercase().replace(" ", "-")}",
                        recipeName = dishName,
                        quantity = Random.nextInt(40, 120), // Individual dish quantity
                        actualQuantity = 0,
                        ingredients = emptyList()
                    )
                }
                breakdown[date] = dishForecasts
            }
            Resource.Success(breakdown)
        }
    }

    private fun generateMockIngredients() = listOf(
        ForecastIngredientDto("ing-1", "Chicken", "kg", Random.nextDouble(3.0, 8.0)),
        ForecastIngredientDto("ing-2", "Beef", "kg", Random.nextDouble(2.0, 6.0)),
        ForecastIngredientDto("ing-3", "Coconut Milk", "L", Random.nextDouble(3.0, 9.0)),
        ForecastIngredientDto("ing-4", "Shrimp Paste", "kg", Random.nextDouble(0.5, 2.0)),
        ForecastIngredientDto("ing-5", "Lemongrass", "kg", Random.nextDouble(0.5, 2.0)),
        ForecastIngredientDto("ing-6", "Ginger", "kg", Random.nextDouble(0.5, 2.0)),
        ForecastIngredientDto("ing-7", "Garlic", "kg", Random.nextDouble(0.5, 2.0)),
        ForecastIngredientDto("ing-8", "Cooking Oil", "L", Random.nextDouble(1.0, 3.0))
    )

    /*
    // --- ORIGINAL API IMPLEMENTATION ---
    suspend fun getForecast(days: Int): Resource<List<ForecastDto>> {
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
    }
    */
}