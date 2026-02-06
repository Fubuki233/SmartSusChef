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

    suspend fun create(request: CreateWastageDataRequest): Resource<WastageDataDto> {
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
    }

    suspend fun update(id: String, request: UpdateWastageDataRequest): Resource<WastageDataDto> {
        return withContext(Dispatchers.IO) {
            try {
                val response = wastageApiService.update(id, request)
                if (response.isSuccessful) {
                    Resource.Success(response.body()!!)
                } else {
                    Resource.Error("Failed to update wastage: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }

    suspend fun delete(id: String): Resource<Unit> {
        return withContext(Dispatchers.IO) {
            try {
                val response = wastageApiService.delete(id)
                if (response.isSuccessful) {
                    Resource.Success(Unit)
                } else {
                    Resource.Error("Failed to delete wastage: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }
}
