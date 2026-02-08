package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.IngredientApiService
import com.smartsuschef.mobile.network.dto.CreateIngredientRequest
import com.smartsuschef.mobile.network.dto.IngredientDto
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
class IngredientsRepositoryTest {

    @Mock
    private lateinit var mockIngredientApiService: IngredientApiService

    private lateinit var ingredientsRepository: IngredientsRepository
    private val testDispatcher = UnconfinedTestDispatcher()

    private val sampleIngredientDto = IngredientDto(
        id = "ing1",
        name = "Flour",
        unit = "kg",
        carbonFootprint = 0.5,
        createdAt = "2026-02-08T00:00:00",
        updatedAt = "2026-02-08T00:00:00"
    )

    @Before
    fun setUp() {
        ingredientsRepository = IngredientsRepository(mockIngredientApiService)
    }

    // --- getAll() Tests ---

    @Test
    fun `getAll success should return success resource with list of IngredientDto`() = runTest(testDispatcher) {
        // Arrange
        val ingredientList = listOf(sampleIngredientDto)
        whenever(mockIngredientApiService.getAll()).thenReturn(Response.success(ingredientList))

        // Act
        val result = ingredientsRepository.getAll()

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(ingredientList, (result as Resource.Success).data)
    }

    @Test
    fun `getAll API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Unauthorized"
        val errorResponse = Response.error<List<IngredientDto>>(401, errorMessage.toResponseBody(null))
        whenever(mockIngredientApiService.getAll()).thenReturn(errorResponse)

        // Act
        val result = ingredientsRepository.getAll()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to fetch ingredients: $errorMessage", result.message)
    }

    @Test
    fun `getAll network error should return error resource with network message`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockIngredientApiService.getAll()).thenAnswer { throw IOException("No internet") }

        // Act
        val result = ingredientsRepository.getAll()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }

    // --- create() Tests ---

    @Test
    fun `create success should return success resource with IngredientDto`() = runTest(testDispatcher) {
        // Arrange
        val createRequest = CreateIngredientRequest("Sugar", "kg", 0.3)
        whenever(mockIngredientApiService.create(any())).thenReturn(Response.success(sampleIngredientDto.copy(name = "Sugar")))

        // Act
        val result = ingredientsRepository.create(createRequest)

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(sampleIngredientDto.copy(name = "Sugar"), (result as Resource.Success).data)
    }

    @Test
    fun `create API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val createRequest = CreateIngredientRequest("Sugar", "kg", 0.3)
        val errorMessage = "Invalid data"
        val errorResponse = Response.error<IngredientDto>(400, errorMessage.toResponseBody(null))
        whenever(mockIngredientApiService.create(any())).thenReturn(errorResponse)

        // Act
        val result = ingredientsRepository.create(createRequest)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to add ingredient: $errorMessage", result.message)
    }
}
