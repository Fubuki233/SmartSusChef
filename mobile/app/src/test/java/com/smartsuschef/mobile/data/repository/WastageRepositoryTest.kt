package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.WastageApiService
import com.smartsuschef.mobile.network.dto.CreateWastageDataRequest
import com.smartsuschef.mobile.network.dto.UpdateWastageDataRequest
import com.smartsuschef.mobile.network.dto.WastageDataDto
import com.smartsuschef.mobile.network.dto.WastageTrendDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.runTest
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test
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
class WastageRepositoryTest {

    @Mock
    private lateinit var mockWastageApiService: WastageApiService

    private lateinit var wastageRepository: WastageRepository
    private val testDispatcher = UnconfinedTestDispatcher()

    private val sampleWastageDataDto = WastageDataDto(
        id = "wastage1",
        date = "2023-01-01",
        ingredientId = "ing1",
        recipeId = null,
        displayName = "Apple",
        unit = "kg",
        quantity = 5.0,
        carbonFootprint = 0.5,
        createdAt = "2023-01-01T10:00:00Z",
        updatedAt = "2023-01-01T10:00:00Z"
    )

    private val sampleWastageTrendDto = WastageTrendDto(
        date = "2023-01-01",
        totalQuantity = 10.0,
        totalCarbonFootprint = 1.0,
        itemBreakdown = emptyList() // Details can be added if needed
    )

    @Before
    fun setUp() {
        wastageRepository = WastageRepository(mockWastageApiService)
    }

    // --- getTrend() Tests ---

    @Test
    fun `getTrend success should return success resource with list of WastageTrendDto`() = runTest(testDispatcher) {
        // Arrange
        val trendList = listOf(sampleWastageTrendDto)
        whenever(mockWastageApiService.getTrend(any(), any())).thenReturn(Response.success(trendList))

        // Act
        val result = wastageRepository.getTrend("2023-01-01", "2023-01-07")

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(trendList, (result as Resource.Success).data)
    }

    @Test
    fun `getTrend API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Unauthorized"
        val errorResponse = Response.error<List<WastageTrendDto>>(401, errorMessage.toResponseBody(null))
        whenever(mockWastageApiService.getTrend(any(), any())).thenReturn(errorResponse)

        // Act
        val result = wastageRepository.getTrend("2023-01-01", "2023-01-07")

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to fetch wastage trend: $errorMessage", result.message)
    }

    @Test
    fun `getTrend network error should return error resource with network message`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockWastageApiService.getTrend(any(), any())).thenAnswer { throw IOException("No internet") }

        // Act
        val result = wastageRepository.getTrend("2023-01-01", "2023-01-07")

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }

    // --- create() Tests ---

    @Test
    fun `create success should return success resource with WastageDataDto`() = runTest(testDispatcher) {
        // Arrange
        val createRequest = CreateWastageDataRequest("2023-01-01", "ing1", null, 5.0)
        whenever(mockWastageApiService.create(any())).thenReturn(Response.success(sampleWastageDataDto))

        // Act
        val result = wastageRepository.create(createRequest)

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(sampleWastageDataDto, (result as Resource.Success).data)
    }

    @Test
    fun `create API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val createRequest = CreateWastageDataRequest("2023-01-01", "ing1", null, 5.0)
        val errorMessage = "Invalid data"
        val errorResponse = Response.error<WastageDataDto>(400, errorMessage.toResponseBody(null))
        whenever(mockWastageApiService.create(any())).thenReturn(errorResponse)

        // Act
        val result = wastageRepository.create(createRequest)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to add wastage: $errorMessage", result.message)
    }

    // --- update() Tests ---

    @Test
    fun `update success should return success resource with WastageDataDto`() = runTest(testDispatcher) {
        // Arrange
        val updateRequest = UpdateWastageDataRequest("2023-01-01", "ing1", null, 6.0)
        whenever(mockWastageApiService.update(any(), any())).thenReturn(Response.success(sampleWastageDataDto.copy(quantity = 6.0)))

        // Act
        val result = wastageRepository.update("wastage1", updateRequest)

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(sampleWastageDataDto.copy(quantity = 6.0), (result as Resource.Success).data)
    }

    // --- delete() Tests ---

    @Test
    fun `delete success should return success resource`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockWastageApiService.delete(any())).thenReturn(Response.success(Unit))

        // Act
        val result = wastageRepository.delete("wastage1")

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(Unit, (result as Resource.Success).data)
    }

    @Test
    fun `delete API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Not allowed"
        val errorResponse = Response.error<Unit>(403, errorMessage.toResponseBody(null))
        whenever(mockWastageApiService.delete(any())).thenReturn(errorResponse)

        // Act
        val result = wastageRepository.delete("wastage1")

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to delete wastage: $errorMessage", result.message)
    }
}
