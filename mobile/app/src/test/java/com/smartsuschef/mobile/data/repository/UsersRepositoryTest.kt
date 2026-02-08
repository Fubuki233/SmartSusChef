package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.AuthApiService
import com.smartsuschef.mobile.network.dto.UpdateProfileRequest
import com.smartsuschef.mobile.network.dto.UserDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.runTest
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Before
import org.junit.Test
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.runner.RunWith
import org.mockito.Mock
import org.mockito.junit.MockitoJUnitRunner
import org.mockito.kotlin.any
import org.mockito.kotlin.whenever
import retrofit2.HttpException
import retrofit2.Response
import java.io.IOException

@ExperimentalCoroutinesApi
@RunWith(MockitoJUnitRunner::class)
class UsersRepositoryTest {

    @Mock
    private lateinit var mockAuthApiService: AuthApiService

    private lateinit var usersRepository: UsersRepository
    private val testDispatcher = UnconfinedTestDispatcher()

    @Before
    fun setUp() {
        usersRepository = UsersRepository(mockAuthApiService)
    }

    // --- getCurrentUser() Tests ---

    @Test
    fun `getCurrentUser success should return success resource with UserDto`() = runTest(testDispatcher) {
        // Arrange
        val userDto = UserDto("id1", "user1", "Name1", "email1", "role1", "status1", "2026-02-08T00:00:00", "2026-02-08T00:00:00")
        whenever(mockAuthApiService.getCurrentUser()).thenReturn(Response.success(userDto))

        // Act
        val result = usersRepository.getCurrentUser()

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(userDto, (result as Resource.Success).data)
    }

    @Test
    fun `getCurrentUser API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Not Found"
        val errorResponse = Response.error<UserDto>(404, errorMessage.toResponseBody(null))
        whenever(mockAuthApiService.getCurrentUser()).thenReturn(errorResponse)

        // Act
        val result = usersRepository.getCurrentUser()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to fetch user profile: $errorMessage", result.message)
    }

    @Test
    fun `getCurrentUser network error should return error resource with network message`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockAuthApiService.getCurrentUser()).thenAnswer { throw IOException("No internet") }

        // Act
        val result = usersRepository.getCurrentUser()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }

    @Test
    fun `getCurrentUser HttpException should return error resource with unexpected message`() = runTest(testDispatcher) {
        // Arrange
        val httpException = HttpException(Response.error<UserDto>(500, "{}".toResponseBody(null)))
        whenever(mockAuthApiService.getCurrentUser()).thenAnswer { throw httpException }

        // Act
        val result = usersRepository.getCurrentUser()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("An unexpected error occurred: ${httpException.message()}", result.message)
    }

    // --- updateUser() Tests ---

    @Test
    fun `updateUser success should return success resource with UserDto`() = runTest(testDispatcher) {
        // Arrange
        val request = UpdateProfileRequest("New Name", "new@example.com")
        val updatedUserDto = UserDto("id1", "user1", "New Name", "new@example.com", "role1", "status1", "2026-02-08T00:00:00", "2026-02-08T00:00:00")
        whenever(mockAuthApiService.updateOwnProfile(any())).thenReturn(Response.success(updatedUserDto))

        // Act
        val result = usersRepository.updateUser(request)

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(updatedUserDto, (result as Resource.Success).data)
    }

    @Test
    fun `updateUser API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val request = UpdateProfileRequest("New Name", "new@example.com")
        val errorMessage = "Bad Request"
        val errorResponse = Response.error<UserDto>(400, errorMessage.toResponseBody(null))
        whenever(mockAuthApiService.updateOwnProfile(any())).thenReturn(errorResponse)

        // Act
        val result = usersRepository.updateUser(request)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to update user: $errorMessage", result.message)
    }

    @Test
    fun `updateUser network error should return error resource with network message`() = runTest(testDispatcher) {
        // Arrange
        val request = UpdateProfileRequest("New Name", "new@example.com")
        whenever(mockAuthApiService.updateOwnProfile(any())).thenAnswer { throw IOException("No network") }

        // Act
        val result = usersRepository.updateUser(request)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }

    @Test
    fun `updateUser HttpException should return error resource with unexpected message`() = runTest(testDispatcher) {
        // Arrange
        val request = UpdateProfileRequest("New Name", "new@example.com")
        val httpException = HttpException(Response.error<UserDto>(500, "{}".toResponseBody(null)))
        whenever(mockAuthApiService.updateOwnProfile(any())).thenAnswer { throw httpException }

        // Act
        val result = usersRepository.updateUser(request)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("An unexpected error occurred: ${httpException.message()}", result.message)
    }
}
