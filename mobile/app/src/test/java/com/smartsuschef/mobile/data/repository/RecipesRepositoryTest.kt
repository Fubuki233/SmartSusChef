package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.RecipeApiService
import com.smartsuschef.mobile.network.dto.CreateRecipeRequest
import com.smartsuschef.mobile.network.dto.RecipeDto
import com.smartsuschef.mobile.network.dto.RecipeIngredientDto
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
class RecipesRepositoryTest {

    @Mock
    private lateinit var mockRecipeApiService: RecipeApiService

    private lateinit var recipesRepository: RecipesRepository
    private val testDispatcher = UnconfinedTestDispatcher()

    private val sampleRecipeDto = RecipeDto(
        id = "recipe1",
        name = "Chicken Curry",
        isSellable = true,
        isSubRecipe = false,
        ingredients = listOf(
            RecipeIngredientDto("ing1", null, "Chicken", "g", 500.0),
            RecipeIngredientDto("ing2", null, "Curry Powder", "g", 50.0)
        ),
        createdAt = "2026-02-08T00:00:00",
        updatedAt = "2026-02-08T00:00:00"
    )

    @Before
    fun setUp() {
        recipesRepository = RecipesRepository(mockRecipeApiService)
    }

    // --- getAll() Tests ---

    @Test
    fun `getAll success should return success resource with list of RecipeDto`() = runTest(testDispatcher) {
        // Arrange
        val recipeList = listOf(sampleRecipeDto)
        whenever(mockRecipeApiService.getAll()).thenReturn(Response.success(recipeList))

        // Act
        val result = recipesRepository.getAll()

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(recipeList, (result as Resource.Success).data)
    }

    @Test
    fun `getAll API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Unauthorized"
        val errorResponse = Response.error<List<RecipeDto>>(401, errorMessage.toResponseBody(null))
        whenever(mockRecipeApiService.getAll()).thenReturn(errorResponse)

        // Act
        val result = recipesRepository.getAll()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to fetch recipes: $errorMessage", result.message)
    }

    @Test
    fun `getAll network error should return error resource with network message`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockRecipeApiService.getAll()).thenAnswer { throw IOException("No internet") }

        // Act
        val result = recipesRepository.getAll()

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }

    // --- create() Tests ---

    @Test
    fun `create success should return success resource with RecipeDto`() = runTest(testDispatcher) {
        // Arrange
        val createRequest = CreateRecipeRequest(
            name = "Fish Curry",
            isSellable = true,
            isSubRecipe = false,
            ingredients = emptyList()
        )
        whenever(mockRecipeApiService.create(any())).thenReturn(Response.success(sampleRecipeDto.copy(name = "Fish Curry")))

        // Act
        val result = recipesRepository.create(createRequest)

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(sampleRecipeDto.copy(name = "Fish Curry"), (result as Resource.Success).data)
    }

    @Test
    fun `create API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val createRequest = CreateRecipeRequest(
            name = "Fish Curry",
            isSellable = true,
            isSubRecipe = false,
            ingredients = emptyList()
        )
        val errorMessage = "Invalid data"
        val errorResponse = Response.error<RecipeDto>(400, errorMessage.toResponseBody(null))
        whenever(mockRecipeApiService.create(any())).thenReturn(errorResponse)

        // Act
        val result = recipesRepository.create(createRequest)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to add recipe: $errorMessage", result.message)
    }
}
