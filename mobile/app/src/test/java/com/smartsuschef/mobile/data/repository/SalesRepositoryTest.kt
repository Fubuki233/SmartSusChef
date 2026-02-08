package com.smartsuschef.mobile.data.repository

import com.smartsuschef.mobile.network.api.SalesApiService
import com.smartsuschef.mobile.network.dto.*
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
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

@ExperimentalCoroutinesApi
@RunWith(MockitoJUnitRunner::class)
class SalesRepositoryTest {

    @Mock
    private lateinit var mockSalesApiService: SalesApiService

    private lateinit var salesRepository: SalesRepository
    private val testDispatcher = UnconfinedTestDispatcher()

    private val sampleSalesDataDto = SalesDataDto(
        id = "sale1",
        date = "2023-01-01",
        recipeId = "recipe1",
        recipeName = "Dish 1",
        quantity = 10,
        createdAt = "2023-01-01T10:00:00Z",
        updatedAt = "2023-01-01T10:00:00Z"
    )

    private val sampleSalesTrendDto = SalesTrendDto(
        date = "2023-01-01",
        totalQuantity = 100,
        recipeBreakdown = listOf(RecipeSalesDto("recipe1", "Dish 1", 50))
    )

    private val sampleIngredientUsageDto = IngredientUsageDto(
        ingredientId = "ing1",
        ingredientName = "Ingredient 1",
        unit = "kg",
        quantity = 5.0
    )

    private val sampleRecipeSalesDto = RecipeSalesDto(
        recipeId = "recipe1",
        recipeName = "Dish 1",
        quantity = 50
    )


    @Before
    fun setUp() {
        salesRepository = SalesRepository(mockSalesApiService)
    }

    // --- getAll() Tests ---

    @Test
    fun `getAll success should return success resource with list of SalesDataDto`() = runTest(testDispatcher) {
        // Arrange
        val salesList = listOf(sampleSalesDataDto)
        whenever(mockSalesApiService.getAll(any(), any())).thenReturn(Response.success(salesList))

        // Act
        val result = salesRepository.getAll("2023-01-01", "2023-01-07")

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(salesList, (result as Resource.Success).data)
    }

    @Test
    fun `getAll API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Unauthorized"
        val errorResponse = Response.error<List<SalesDataDto>>(401, errorMessage.toResponseBody(null))
        whenever(mockSalesApiService.getAll(any(), any())).thenReturn(errorResponse)

        // Act
        val result = salesRepository.getAll(null, null)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to fetch sales history: $errorMessage", result.message)
    }

    @Test
    fun `getAll network error should return error resource with network message`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockSalesApiService.getAll(any(), any())).thenAnswer { throw IOException("No internet") }

        // Act
        val result = salesRepository.getAll(null, null)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Couldn't reach the server. Check your internet connection.", result.message)
    }

    // --- getTrend() Tests ---

    @Test
    fun `getTrend success should return success resource with list of SalesTrendDto`() = runTest(testDispatcher) {
        // Arrange
        val trendList = listOf(sampleSalesTrendDto)
        whenever(mockSalesApiService.getTrend(any(), any())).thenReturn(Response.success(trendList))

        // Act
        val result = salesRepository.getTrend("2023-01-01", "2023-01-07")

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(trendList, (result as Resource.Success).data)
    }

    @Test
    fun `getTrend API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Forbidden"
        val errorResponse = Response.error<List<SalesTrendDto>>(403, errorMessage.toResponseBody(null))
        whenever(mockSalesApiService.getTrend(any(), any())).thenReturn(errorResponse)

        // Act
        val result = salesRepository.getTrend("2023-01-01", "2023-01-07")

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to fetch sales trend: $errorMessage", result.message)
    }

    // --- getIngredientUsageByDate() Tests ---

    @Test
    fun `getIngredientUsageByDate success should return success resource with list of IngredientUsageDto`() = runTest(testDispatcher) {
        // Arrange
        val usageList = listOf(sampleIngredientUsageDto)
        whenever(mockSalesApiService.getIngredientUsageByDate(any())).thenReturn(Response.success(usageList))

        // Act
        val result = salesRepository.getIngredientUsageByDate("2023-01-01")

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(usageList, (result as Resource.Success).data)
    }

    // --- getRecipeSalesByDate() Tests ---

    @Test
    fun `getRecipeSalesByDate success should return success resource with list of RecipeSalesDto`() = runTest(testDispatcher) {
        // Arrange
        val recipeSalesList = listOf(sampleRecipeSalesDto)
        whenever(mockSalesApiService.getRecipeSalesByDate(any())).thenReturn(Response.success(recipeSalesList))

        // Act
        val result = salesRepository.getRecipeSalesByDate("2023-01-01")

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(recipeSalesList, (result as Resource.Success).data)
    }

    // --- create() Tests ---

    @Test
    fun `create success should return success resource with SalesDataDto`() = runTest(testDispatcher) {
        // Arrange
        val createRequest = CreateSalesDataRequest("2023-01-01", "recipe1", 10)
        whenever(mockSalesApiService.create(any())).thenReturn(Response.success(sampleSalesDataDto))

        // Act
        val result = salesRepository.create(createRequest)

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(sampleSalesDataDto, (result as Resource.Success).data)
    }

    @Test
    fun `create API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val createRequest = CreateSalesDataRequest("2023-01-01", "recipe1", 10)
        val errorMessage = "Invalid data"
        val errorResponse = Response.error<SalesDataDto>(400, errorMessage.toResponseBody(null))
        whenever(mockSalesApiService.create(any())).thenReturn(errorResponse)

        // Act
        val result = salesRepository.create(createRequest)

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to add sale: $errorMessage", result.message)
    }

    // --- update() Tests ---

    @Test
    fun `update success should return success resource with SalesDataDto`() = runTest(testDispatcher) {
        // Arrange
        val updateRequest = UpdateSalesDataRequest(15)
        whenever(mockSalesApiService.update(any(), any())).thenReturn(Response.success(sampleSalesDataDto.copy(quantity = 15)))

        // Act
        val result = salesRepository.update("sale1", updateRequest)

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(sampleSalesDataDto.copy(quantity = 15), (result as Resource.Success).data)
    }

    // --- delete() Tests ---

    @Test
    fun `delete success should return success resource`() = runTest(testDispatcher) {
        // Arrange
        whenever(mockSalesApiService.delete(any())).thenReturn(Response.success(Unit))

        // Act
        val result = salesRepository.delete("sale1")

        // Assert
        assertTrue(result is Resource.Success)
        assertEquals(Unit, (result as Resource.Success).data)
    }

    @Test
    fun `delete API error should return error resource with message`() = runTest(testDispatcher) {
        // Arrange
        val errorMessage = "Not allowed"
        val errorResponse = Response.error<Unit>(403, errorMessage.toResponseBody(null))
        whenever(mockSalesApiService.delete(any())).thenReturn(errorResponse)

        // Act
        val result = salesRepository.delete("sale1")

        // Assert
        assertTrue(result is Resource.Error)
        assertEquals("Failed to delete sale: $errorMessage", result.message)
    }
}
