package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.ForecastApiService
import com.smartsuschef.mobile.network.dto.ForecastDto
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
}