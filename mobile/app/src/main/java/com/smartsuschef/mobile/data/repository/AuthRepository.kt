package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.data.TokenManager
import com.smartsuschef.mobile.network.api.AuthApiService
import com.smartsuschef.mobile.network.dto.ChangePasswordRequest
import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.network.dto.LoginResponse
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepository @Inject constructor(
    private val authApi: AuthApiService,
    private val tokenManager: TokenManager
) {
    /**
     * Executes login and saves the token if successful.
     * Maps to .NET AuthController.Login
     */
    suspend fun login(request: LoginRequest): Resource<LoginResponse> {
        return withContext(Dispatchers.IO) {
            try {
                val response = authApi.login(request)
                if (response.isSuccessful && response.body() != null) {
                    val loginResponse = response.body()!!

                    // Persist the token and user role locally
                    tokenManager.saveToken(loginResponse.token)
                    tokenManager.saveUserRole(loginResponse.user.role)

                    Resource.Success(loginResponse)
                } else {
                    Resource.Error("Invalid username or password")
                }
            } catch (e: Exception) {
                Resource.Error(e.localizedMessage ?: "An unexpected error occurred")
            }
        }
    }

    /**
     * Change user password
     * Requires current password for verification
     * Maps to .NET AuthController.ChangePassword
     */
    suspend fun changePassword(request: ChangePasswordRequest): Resource<Unit> {
        return withContext(Dispatchers.IO) {
            try {
                val response = authApi.changePassword(request)
                if (response.isSuccessful) {
                    Resource.Success(Unit)
                } else {
                    // Parse error message from response
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = when {
                        errorBody?.contains("incorrect", ignoreCase = true) == true ->
                            "Current password is incorrect"
                        errorBody?.contains("invalid", ignoreCase = true) == true ->
                            "Invalid password format"
                        response.code() == 401 ->
                            "Current password is incorrect"
                        response.code() == 400 ->
                            "Password does not meet requirements"
                        else ->
                            "Failed to change password: ${response.message()}"
                    }
                    Resource.Error(errorMessage)
                }
            } catch (e: HttpException) {
                Resource.Error("An unexpected error occurred: ${e.message()}")
            } catch (e: IOException) {
                Resource.Error("Couldn't reach the server. Check your internet connection.")
            }
        }
    }

    /**
     * Clears local session data.
     * Essential for your security requirement to log out after inactivity.
     */
    suspend fun logout() {
        tokenManager.clearSession()
    }

    /**
     * Checks if a user is currently authenticated.
     */
    fun isUserLoggedIn(): Boolean {
        return !tokenManager.getToken().isNullOrEmpty()
    }
}