package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.RecipeApiService
import com.smartsuschef.mobile.network.dto.CreateRecipeRequest
import com.smartsuschef.mobile.network.dto.RecipeDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import javax.inject.Inject

class RecipesRepository @Inject constructor(
    private val recipeApiService: RecipeApiService
) {
    suspend fun getAll(): Resource<List<RecipeDto>> {
        return withContext(Dispatchers.IO) {
            try {
                val response = recipeApiService.getAll()
                if (response.isSuccessful) {
                    Resource.Success(response.body() ?: emptyList())
                } else {
                    Resource.Error("Failed to fetch recipes: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }

    suspend fun create(request: CreateRecipeRequest): Resource<RecipeDto> {
        return withContext(Dispatchers.IO) {
            try {
                val response = recipeApiService.create(request)
                if (response.isSuccessful) {
                    Resource.Success(response.body()!!)
                } else {
                    Resource.Error("Failed to add recipe: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }
}
