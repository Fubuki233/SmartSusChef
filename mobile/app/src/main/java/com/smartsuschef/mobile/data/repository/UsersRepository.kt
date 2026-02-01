package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.AuthApiService
import com.smartsuschef.mobile.network.dto.UpdateProfileRequest
import com.smartsuschef.mobile.network.dto.UserDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import javax.inject.Inject

class UsersRepository @Inject constructor(
    private val authApiService: AuthApiService
) {
    suspend fun getCurrentUser(): Resource<UserDto> {
        // --- MOCK IMPLEMENTATION FOR UI TESTING ---
        val fakeUser = UserDto(
            id = "user-123",
            name = "Test Employee",
            username = "test",
            email = "test@test.com",
            role = "employee",
            status = "Active"
        )
        return Resource.Success(fakeUser)

        /*
        // --- ORIGINAL IMPLEMENTATION ---
        return withContext(Dispatchers.IO) {
            try {
                val response = authApiService.getCurrentUser()
                if (response.isSuccessful) {
                    Resource.Success(response.body()!!)
                } else {
                    Resource.Error("Failed to fetch user profile: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
        */
    }

    suspend fun updateUser(request: UpdateProfileRequest): Resource<UserDto> {
        return withContext(Dispatchers.IO) {
            try {
                val response = authApiService.updateOwnProfile(request)
                if (response.isSuccessful) {
                    Resource.Success(response.body()!!)
                } else {
                    Resource.Error("Failed to update user: ${response.message()}")
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }
}
