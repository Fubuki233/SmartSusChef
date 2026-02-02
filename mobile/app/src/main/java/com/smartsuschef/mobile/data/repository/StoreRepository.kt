package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.StoreApiService
import com.smartsuschef.mobile.network.dto.StoreDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import javax.inject.Inject

class StoreRepository @Inject constructor(
    private val storeApiService: StoreApiService
) {
    suspend fun getStore(): Resource<StoreDto> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val fakeStore = StoreDto(
            id = 1,
            companyName = "Smart Sus Chef Corp",
            uen = "202400001A",
            storeName = "Downtown Outlet",
            outletLocation = "123 Orchard Road",
            contactNumber = "+65 6000 0000",
            openingDate = "2024-01-01T00:00:00Z",
            latitude = 1.2839,
            longitude = 103.8457,
            countryCode = "SG",
            address = "123 Orchard Road, Singapore 238839",
            isActive = true,
            createdAt = "2024-01-01T00:00:00Z",
            updatedAt = "2024-01-01T00:00:00Z"
        )
        return Resource.Success(fakeStore)

        /*
        // --- ORIGINAL IMPLEMENTATION ---
        return withContext(Dispatchers.IO) {
            try {
                val response = storeApiService.getStore()
                if (response.isSuccessful) {
                    Resource.Success(response.body()!!)
                } else {
                    Resource.Error("Failed to fetch store details: ${response.message()}")
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
