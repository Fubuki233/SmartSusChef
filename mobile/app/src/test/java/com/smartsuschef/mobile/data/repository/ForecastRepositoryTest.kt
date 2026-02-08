package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.ForecastApiService
import com.smartsuschef.mobile.network.dto.ForecastDto
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
class ForecastRepositoryTest {

    @Mock
    private lateinit var mockForecastApiService: ForecastApiService

    private lateinit var forecastRepository: ForecastRepository
    private val testDispatcher = UnconfinedTestDispatcher()

    private val sampleForecastDto = ForecastDto(
        date = "2023-01-01",
        recipeId = "recipe1",
        recipeName = "Dish 1",
        quantity = 10,
        confidence = "Medium",
        ingredients = emptyList(), // Can add more detail if needed
    )

    @Before
    fun setUp() {
        forecastRepository = ForecastRepository(mockForecastApiService)
    }

    // --- getForecast() Tests ---

    @Test
    fun `getForecast success should return success resource with list of ForecastDto`() = runTest(testDispatcher) {
        // Arrange
        val forecastList = listOf(sampleForecastDto)
        whenever(mockForecastApiService.getForecast(any())).thenReturn(Response.success(forecastList))

        // Act
        val result = forecastRepository.getForecast(7)

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(forecastList, (result as Resource.Success).data)
    }

    @Test
    fun `getForecast API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Unauthorized"
        val errorResponse = Response.error<List<ForecastDto>>(401, errorMessage.toResponseBody(null))
        whenever(mockForecastApiService.getForecast(any())).thenReturn(errorResponse)

        // Act
        val result = forecastRepository.getForecast(7)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to fetch forecast: $errorMessage", result.message)
    }

    @Test
    fun `getForecast network error should return error resource with network message`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockForecastApiService.getForecast(any())).thenAnswer { throw IOException("No internet") }

        // Act
        val result = forecastRepository.getForecast(7)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }

    @Test
    fun `getForecast HttpException should return error resource with unexpected message`() = runTest(testDispatcher) {
        // Arrange
        val httpException = HttpException(Response.error<List<ForecastDto>>(500, "{}".toResponseBody(null)))
        whenever(mockForecastApiService.getForecast(any())).thenAnswer { throw httpException }

        // Act
        val result = forecastRepository.getForecast(7)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("An unexpected error occurred: ${httpException.message()}", result.message)
    }
}
