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
        return withContext(Dispatchers.IO) {
            val trend = mutableListOf<SalesTrendDto>()
            val calendar = java.util.Calendar.getInstance()
            val dateFormat = java.text.SimpleDateFormat("yyyy-MM-dd", java.util.Locale.getDefault())

            if (startDate == endDate) {
                trend.add(SalesTrendDto(endDate, (80..150).random(), emptyList()))
            } else {
                // Start from 7 days ago
                calendar.add(java.util.Calendar.DAY_OF_YEAR, -6)
                for (i in 0..6) {
                    val date = dateFormat.format(calendar.time)
                    trend.add(
                        SalesTrendDto(
                            date = date,
                            totalQuantity = (80..150).random(), // Random sales data
                            recipeBreakdown = emptyList()
                        )
                    )
                    calendar.add(java.util.Calendar.DAY_OF_YEAR, 1)
                }
            }
            Resource.Success(trend)
        }

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

        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        return withContext(Dispatchers.IO) {
            val fakeIngredients = listOf(
                IngredientUsageDto("ing-1", "Chicken (Whole)", "kg", 25.5),
                IngredientUsageDto("ing-2", "Fragrant Rice", "kg", 15.0),
                IngredientUsageDto("ing-3", "Cucumber", "pcs", 10.0),
                IngredientUsageDto("ing-4", "Ginger", "kg", 2.5),
                IngredientUsageDto("ing-5", "Garlic", "kg", 1.8),
                IngredientUsageDto("ing-6", "Pandan Leaves", "bundle", 5.0),
                IngredientUsageDto("ing-7", "Sesame Oil", "litres", 0.5)
            )
            Resource.Success(fakeIngredients)
        }
        /*
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
        */
    }

    suspend fun create(request: CreateSalesDataRequest): Resource<SalesDataDto> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val fakeDto = SalesDataDto(
            id = "sales-${System.currentTimeMillis()}",
            date = request.date,
            recipeId = request.recipeId,
            recipeName = "Mocked Recipe",
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
