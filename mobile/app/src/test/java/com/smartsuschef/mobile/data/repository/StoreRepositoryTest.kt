package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.StoreApiService
import com.smartsuschef.mobile.network.dto.StoreDto
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
class StoreRepositoryTest {

    @Mock
    private lateinit var mockStoreApiService: StoreApiService

    private lateinit var storeRepository: StoreRepository
    private val testDispatcher = UnconfinedTestDispatcher()

    @Before
    fun setUp() {
        storeRepository = StoreRepository(mockStoreApiService)
    }

    // --- getStore() Tests ---

    @Test
    fun `getStore success should return success resource with StoreDto`() = runTest(testDispatcher) {
        // Arrange
        val storeDto = StoreDto(
            id = 1,
            companyName = "Test Co",
            uen = "UEN123",
            storeName = "Main Store",
            outletLocation = "Location A",
            contactNumber = "12345678",
            openingDate = "2023-01-01",
            latitude = 1.0,
            longitude = 103.0,
            address = "123 Test St",
            countryCode = "SG",
            isActive = true,
            createdAt = "2023-01-01T00:00:00Z",
            updatedAt = "2023-01-01T00:00:00Z"
        )
        whenever(mockStoreApiService.getStore()).thenReturn(Response.success(storeDto))

        // Act
        val result = storeRepository.getStore()

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(storeDto, (result as Resource.Success).data)
    }

    @Test
    fun `getStore API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Not Found"
        val errorResponse = Response.error<StoreDto>(404, errorMessage.toResponseBody(null))
        whenever(mockStoreApiService.getStore()).thenReturn(errorResponse)

        // Act
        val result = storeRepository.getStore()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to fetch store details: $errorMessage", result.message)
    }

    @Test
    fun `getStore network error should return error resource with network message`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockStoreApiService.getStore()).thenAnswer { throw IOException("No internet") }

        // Act
        val result = storeRepository.getStore()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }

    @Test
    fun `getStore HttpException should return error resource with unexpected message`() = runTest(testDispatcher) {
        // Arrange
        val httpException = HttpException(Response.error<StoreDto>(500, "{}".toResponseBody(null)))
        whenever(mockStoreApiService.getStore()).thenAnswer { throw httpException }

        // Act
        val result = storeRepository.getStore()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("An unexpected error occurred: ${httpException.message()}", result.message)
    }
}
