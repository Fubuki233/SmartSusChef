package com.smartsuschef.mobile.data.repository

import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import com.smartsuschef.mobile.data.TokenManager
import com.smartsuschef.mobile.network.api.AuthApiService
import com.smartsuschef.mobile.network.dto.ChangePasswordRequest
import com.smartsuschef.mobile.network.dto.LoginRequest
import com.smartsuschef.mobile.network.dto.LoginResponse
import com.smartsuschef.mobile.network.dto.UserDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.TestDispatcher
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.mockito.Mock
import org.mockito.junit.MockitoJUnitRunner
import org.mockito.kotlin.any
import org.mockito.kotlin.mock
import org.mockito.kotlin.never
import org.mockito.kotlin.verify
import org.mockito.kotlin.whenever
import retrofit2.HttpException
import retrofit2.Response
import java.io.IOException

/**
 * Unit tests for AuthRepository.
 */
@ExperimentalCoroutinesApi
@RunWith(MockitoJUnitRunner::class)
class AuthRepositoryTest {

    // Mocks for the repository's dependencies
    @Mock
    private lateinit var mockAuthApi: AuthApiService
    @Mock
    private lateinit var mockTokenManager: TokenManager

    // The repository under test
    private lateinit var repository: AuthRepository

    private val testDispatcher: TestDispatcher = UnconfinedTestDispatcher()

    @Before
    fun setUp() {
        Dispatchers.setMain(testDispatcher)
        // Initialize the repository with mocks before each test
        repository = AuthRepository(mockAuthApi, mockTokenManager)
    }

    @After
    fun tearDown() {
        Dispatchers.resetMain()
    }

    @Test
    fun `login success should return success resource and save token`() = runTest {
        // ARRANGE
        val loginRequest = LoginRequest("user", "pass")
        val userDto = UserDto("id", "user", "Test User", "email", "employee", "Active", "2026-02-08T00:00:00", "2026-02-08T00:00:00")
        val loginResponse = LoginResponse("test-token", userDto, false)
        val successResponse = Response.success(loginResponse)
        whenever(mockAuthApi.login(any())).thenReturn(successResponse)

        // ACT
        val result = repository.login(loginRequest)

        // ASSERT
        assertTrue(result is Resource.Success)
        assertEquals("test-token", (result as Resource.Success).data?.token)
        verify(mockTokenManager).saveToken("test-token")
        verify(mockTokenManager).saveUserRole("employee")
    }

    @Test
    fun `login api error should return error resource and not save token`() = runTest {
        // ARRANGE
        val loginRequest = LoginRequest("user", "pass")
        val errorResponse = Response.error<LoginResponse>(401, "".toResponseBody(null))
        whenever(mockAuthApi.login(any())).thenReturn(errorResponse)

        // ACT
        val result = repository.login(loginRequest)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertEquals("Invalid username or password", result.message)
        verify(mockTokenManager, never()).saveToken(any())
        verify(mockTokenManager, never()).saveUserRole(any())
    }

    @Test
    fun `login network exception should return error resource`() = runTest {
        // ARRANGE
        val loginRequest = LoginRequest("user", "pass")
        whenever(mockAuthApi.login(any())).thenAnswer { throw IOException("Network error") }

        // ACT
        val result = repository.login(loginRequest)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertEquals("Network error", result.message)
        verify(mockTokenManager, never()).saveToken(any())
        verify(mockTokenManager, never()).saveUserRole(any())
    }

    @Test
    fun `changePassword success should return success resource`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("currentPass", "newPass")
        whenever(mockAuthApi.changePassword(request)).thenReturn(Response.success(Unit))

        // ACT
        val result = repository.changePassword(request)

        // ASSERT
        assertTrue(result is Resource.Success)
    }

    @Test
    fun `changePassword api error with incorrect password message should return specific error`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("currentPass", "newPass")
        val errorBodyContent = "current password incorrect"
        val errorResponse = Response.error<Unit>(400, errorBodyContent.toResponseBody(null))
        whenever(mockAuthApi.changePassword(request)).thenReturn(errorResponse)

        // ACT
        val result = repository.changePassword(request)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertEquals("Current password is incorrect", result.message)
    }

    @Test
    fun `changePassword api error with invalid format message should return specific error`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("currentPass", "newPass")
        val errorBodyContent = "password invalid format"
        val errorResponse = Response.error<Unit>(400, errorBodyContent.toResponseBody(null))
        whenever(mockAuthApi.changePassword(request)).thenReturn(errorResponse)

        // ACT
        val result = repository.changePassword(request)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertEquals("Invalid password format", result.message)
    }

    @Test
    fun `changePassword api error with 401 code should return specific error`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("currentPass", "newPass")
        val errorResponse = Response.error<Unit>(401, "".toResponseBody(null))
        whenever(mockAuthApi.changePassword(request)).thenReturn(errorResponse)

        // ACT
        val result = repository.changePassword(request)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertEquals("Current password is incorrect", result.message)
    }

    @Test
    fun `changePassword api error with 400 code should return specific error`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("currentPass", "newPass")
        val errorResponse = Response.error<Unit>(400, "".toResponseBody(null))
        whenever(mockAuthApi.changePassword(request)).thenReturn(errorResponse)

        // ACT
        val result = repository.changePassword(request)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertEquals("Password does not meet requirements", result.message)
    }

    @Test
    fun `changePassword api error generic should return generic error message`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("currentPass", "newPass")
        val apiErrorMessage = "Internal Server Error"
        
        val mockResponse: Response<Unit> = mock()
        whenever(mockResponse.isSuccessful).thenReturn(false)
        whenever(mockResponse.code()).thenReturn(500)
        whenever(mockResponse.message()).thenReturn(apiErrorMessage)
        whenever(mockResponse.errorBody()).thenReturn("".toResponseBody(null))

        whenever(mockAuthApi.changePassword(request)).thenReturn(mockResponse)

        // ACT
        val result = repository.changePassword(request)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertEquals("Failed to change password: $apiErrorMessage", result.message)
    }

    @Test
    fun `changePassword http exception should return unexpected error`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("currentPass", "newPass")
        val errorResponse = Response.error<Unit>(404, "".toResponseBody(null))
        whenever(mockAuthApi.changePassword(request)).thenAnswer {
            throw HttpException(errorResponse)
        }

        // ACT
        val result = repository.changePassword(request)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertTrue(result.message?.contains("An unexpected error occurred") == true)
    }

    @Test
    fun `changePassword io exception should return network error`() = runTest {
        // ARRANGE
        val request = ChangePasswordRequest("currentPass", "newPass")
        whenever(mockAuthApi.changePassword(request)).thenAnswer {
            throw IOException("Network unavailable")
        }

        // ACT
        val result = repository.changePassword(request)

        // ASSERT
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }
}