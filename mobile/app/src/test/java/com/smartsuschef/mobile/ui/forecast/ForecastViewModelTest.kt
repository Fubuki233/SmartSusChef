package com.smartsuschef.mobile.ui.forecast

import androidx.arch.core.executor.testing.InstantTaskExecutorRule
import com.smartsuschef.mobile.data.repository.ForecastRepository
import com.smartsuschef.mobile.network.dto.ForecastDto
import com.smartsuschef.mobile.network.dto.ForecastIngredientDto
import com.smartsuschef.mobile.util.Resource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.TestDispatcher
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.mockito.Mock
import org.mockito.junit.MockitoJUnitRunner
import org.mockito.kotlin.whenever

@ExperimentalCoroutinesApi
@RunWith(MockitoJUnitRunner::class)
class ForecastViewModelTest {

    @get:Rule
    val instantTaskExecutorRule = InstantTaskExecutorRule()

    @Mock
    private lateinit var mockForecastRepository: ForecastRepository

    private val testDispatcher: TestDispatcher = UnconfinedTestDispatcher()

    @Before
    fun setUp() {
        Dispatchers.setMain(testDispatcher)
    }

    @After
    fun tearDown() {
        Dispatchers.resetMain()
    }

    @Test
    fun `init when all repository calls succeed should process and update all LiveData`() = runTest {
        // ARRANGE
        // 1. Create comprehensive mock data for all three repository calls
        val today = "2023-01-01"
        val tomorrow = "2023-01-02"

        // Corrected mock data for getForecast()
        val mockForecastData = listOf(
            ForecastDto(
                date = today,
                recipeId = "rec-1",
                recipeName = "Pizza",
                quantity = 10,
                actualQuantity = 0, // Added required field
                ingredients = listOf(
                    ForecastIngredientDto("ing-1", "Dough", "kg", 2.0),
                    ForecastIngredientDto("ing-2", "Cheese", "kg", 1.0)
                )
            ),
            ForecastDto(
                date = tomorrow,
                recipeId = "rec-2",
                recipeName = "Pasta",
                quantity = 5,
                actualQuantity = 0, // Added required field
                ingredients = listOf(
                    ForecastIngredientDto("ing-3", "Pasta", "kg", 1.0),
                    ForecastIngredientDto("ing-2", "Cheese", "kg", 0.5) // Cheese used again
                )
            )
        )

        // Corrected mock data for getDishBreakdown() to return Map<String, List<ForecastDto>>
        val mockDishBreakdown = mapOf(
            today to listOf(mockForecastData[0]),
            tomorrow to listOf(mockForecastData[1])
        )

        // Mock data for getPastComparison()
        val mockComparisonData = listOf(
            ForecastDto( "2022-12-31", "rec-1", "Pizza", 8, 9, emptyList()) // Added actualQuantity
        )

        // 2. Program the mock repository to return success for all calls
        whenever(mockForecastRepository.getForecast(7)).thenReturn(Resource.Success(mockForecastData))
        whenever(mockForecastRepository.getDishBreakdown(7)).thenReturn(Resource.Success(mockDishBreakdown))
        whenever(mockForecastRepository.getPastComparison()).thenReturn(Resource.Success(mockComparisonData))

        // ACT
        // 3. Initialize the ViewModel
        val viewModel = ForecastViewModel(mockForecastRepository)

        // ASSERT
        // 4. Verify all LiveData states and transformations

        // Summary Trend
        val summaryResult = viewModel.summaryTrend.value
        assertTrue(summaryResult is Resource.Success)
        assertEquals(2, (summaryResult as Resource.Success).data?.size)
        assertEquals(10, summaryResult.data?.find { it.date == today }?.quantity)

        // Dish Forecasts
        val dishResult = viewModel.dishForecasts.value
        assertTrue(dishResult is Resource.Success)
        assertEquals(2, (dishResult as Resource.Success).data?.size)
        // Note: The mapping logic in the ViewModel uses recipeName and quantity from ForecastDto now
        // Let's assume the ViewModel's DishForecast takes (name, quantity)
        assertEquals("Pizza", dishResult.data?.find { it.date == today }?.dishes?.first()?.name)

        // Comparison Data
        val comparisonResult = viewModel.comparisonData.value
        assertTrue(comparisonResult is Resource.Success)
        assertEquals(1, (comparisonResult as Resource.Success).data?.size)

        // Ingredient Forecast (tests the processIngredientTable logic)
        val ingredientResult = viewModel.ingredientForecast.value
        assertTrue(ingredientResult is Resource.Success)
        val ingredients = (ingredientResult as Resource.Success).data
        assertEquals(3, ingredients?.size) // Dough, Cheese, Pasta

        val cheese = ingredients?.find { it.name == "Cheese" }
        assertEquals("kg", cheese?.unit)
        assertEquals(2, cheese?.totalQuantity?.size)
        assertEquals(1.0, cheese?.totalQuantity?.get(0)) // Qty for today
        assertEquals(0.5, cheese?.totalQuantity?.get(1)) // Qty for tomorrow

        val dough = ingredients?.find { it.name == "Dough" }
                assertEquals(2.0, dough?.totalQuantity?.get(0))
                assertEquals(0.0, dough?.totalQuantity?.get(1)) // Dough not used tomorrow
            }
        
            @Test
            fun `init_whenGetForecastFails_shouldUpdateRelevantLiveDataToError`() = runTest {
                // ARRANGE
                // 1. Program the main forecast repository call to fail
                val errorMessage = "Network Error"
                whenever(mockForecastRepository.getForecast(7)).thenReturn(Resource.Error(errorMessage))
        
                // 2. Program the other calls to succeed so we can ensure they still run
                whenever(mockForecastRepository.getDishBreakdown(7)).thenReturn(Resource.Success(emptyMap()))
                whenever(mockForecastRepository.getPastComparison()).thenReturn(Resource.Success(emptyList()))
        
                // ACT
                // 3. Initialize the ViewModel
                val viewModel = ForecastViewModel(mockForecastRepository)
        
                // ASSERT
                // 4. Verify that the LiveData dependent on the failing call are in an Error state
                val summaryResult = viewModel.summaryTrend.value
                assertTrue(summaryResult is Resource.Error)
                assertEquals(errorMessage, (summaryResult as Resource.Error).message)
        
                val ingredientResult = viewModel.ingredientForecast.value
                assertTrue(ingredientResult is Resource.Error)
                assertEquals(errorMessage, (ingredientResult as Resource.Error).message)
        
                // 5. Verify that the other LiveData that depend on successful calls are still updated
                        assertTrue(viewModel.dishForecasts.value is Resource.Success)
                        assertTrue(viewModel.comparisonData.value is Resource.Success)
                    }
                
                    @Test
                    fun `init_whenGetDishBreakdownFails_shouldUpdateRelevantLiveDataToError`() = runTest {
                        // ARRANGE
                        val errorMessage = "Dish breakdown failed"
                        val today = "2023-01-01"
                        val mockForecastData = listOf(ForecastDto(today, "rec-1", "Pizza", 10, 0, emptyList()))
                        val mockComparisonData = listOf(ForecastDto("2022-12-31", "rec-1", "Pizza", 8, 9, emptyList()))
                
                        // 1. Program getDishBreakdown to fail
                        whenever(mockForecastRepository.getForecast(7)).thenReturn(Resource.Success(mockForecastData))
                        whenever(mockForecastRepository.getDishBreakdown(7)).thenReturn(Resource.Error(errorMessage))
                        whenever(mockForecastRepository.getPastComparison()).thenReturn(Resource.Success(mockComparisonData))
                
                        // ACT
                        val viewModel = ForecastViewModel(mockForecastRepository)
                
                        // ASSERT
                        // 2. Verify that dishForecasts is in an Error state
                        val dishResult = viewModel.dishForecasts.value
                        assertTrue(dishResult is Resource.Error)
                        assertEquals(errorMessage, (dishResult as Resource.Error).message)
                
                        // 3. Verify that other LiveData that depend on successful calls are still updated
                        assertTrue(viewModel.summaryTrend.value is Resource.Success)
                        assertTrue(viewModel.ingredientForecast.value is Resource.Success)
                                assertTrue(viewModel.comparisonData.value is Resource.Success)
                            }
                        
                            @Test
                            fun `init_whenGetPastComparisonFails_shouldUpdateRelevantLiveDataToError`() = runTest {
                                // ARRANGE
                                val errorMessage = "Comparison data failed"
                                val today = "2023-01-01"
                                val mockForecastData = listOf(ForecastDto(today, "rec-1", "Pizza", 10, 0, emptyList()))
                                val mockDishBreakdown = mapOf(today to listOf(mockForecastData[0]))
                        
                                // 1. Program getPastComparison to fail
                                whenever(mockForecastRepository.getForecast(7)).thenReturn(Resource.Success(mockForecastData))
                                whenever(mockForecastRepository.getDishBreakdown(7)).thenReturn(Resource.Success(mockDishBreakdown))
                                whenever(mockForecastRepository.getPastComparison()).thenReturn(Resource.Error(errorMessage))
                        
                                // ACT
                                val viewModel = ForecastViewModel(mockForecastRepository)
                        
                                // ASSERT
                                // 2. Verify that comparisonData is in an Error state
                                val comparisonResult = viewModel.comparisonData.value
                                assertTrue(comparisonResult is Resource.Error)
                                assertEquals(errorMessage, (comparisonResult as Resource.Error).message)
                        
                                // 3. Verify that other LiveData that depend on successful calls are still updated
                                assertTrue(viewModel.summaryTrend.value is Resource.Success)
                                assertTrue(viewModel.ingredientForecast.value is Resource.Success)
                                assertTrue(viewModel.dishForecasts.value is Resource.Success)
                            }
                        }
                        