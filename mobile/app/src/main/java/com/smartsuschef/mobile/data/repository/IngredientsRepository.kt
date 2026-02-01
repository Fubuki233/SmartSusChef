package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.IngredientApiService
import com.smartsuschef.mobile.network.dto.CreateIngredientRequest
import com.smartsuschef.mobile.network.dto.IngredientDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import javax.inject.Inject

class IngredientsRepository @Inject constructor(
    private val ingredientApiService: IngredientApiService
) {
    suspend fun getAll(): Resource<List<IngredientDto>> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val mockIngredients = listOf(
            IngredientDto(id = "ing-1", name = "Chicken", unit = "kg", carbonFootprint = 5.0),
            IngredientDto(id = "ing-2", name = "Rice", unit = "kg", carbonFootprint = 1.2),
            IngredientDto(id = "ing-3", name = "Tomato", unit = "kg", carbonFootprint = 0.8),
            IngredientDto(id = "ing-4", name = "Cucumber", unit = "kg", carbonFootprint = 0.5),
            IngredientDto(id = "ing-5", name = "Chili Paste", unit = "L", carbonFootprint = 1.5)
        )
        return Resource.Success(mockIngredients)

        /*
        // --- ORIGINAL IMPLEMENTATION ---
        return withContext(Dispatchers.IO) {
            try {
                val response = ingredientApiService.getAll()
                if (response.isSuccessful) {
                    Resource.Success(response.body() ?: emptyList())
                } else {
                    Resource.Error("Failed to fetch ingredients: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
        */
    }

    suspend fun create(request: CreateIngredientRequest): Resource<IngredientDto> {
        return withContext(Dispatchers.IO) {
            try {
                val response = ingredientApiService.create(request)
                if (response.isSuccessful) {
                    Resource.Success(response.body()!!)
                } else {
                    Resource.Error("Failed to add ingredient: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }
}
