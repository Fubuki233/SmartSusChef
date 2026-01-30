package com.smartsuschef.mobile.data.repository
import com.smartsuschef.mobile.data.TokenManager
import com.smartsuschef.mobile.network.api.AuthApiService
import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.network.dto.LoginResponse
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
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