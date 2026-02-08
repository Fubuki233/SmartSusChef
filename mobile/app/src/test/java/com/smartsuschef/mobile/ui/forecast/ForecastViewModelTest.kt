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
    fun `init when repository call succeeds should process and update all LiveData`() = runTest {
        // ARRANGE
        // 1. Create mock data for the single getForecast(7) repository call
        val today = "2023-01-01"
        val tomorrow = "2023-01-02"

        val mockForecastData = listOf(
            ForecastDto(
                date = today,
                recipeId = "rec-1",
                recipeName = "Pizza",
                quantity = 10,
                confidence = "Medium",
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
                confidence = "High",
                ingredients = listOf(
                    ForecastIngredientDto("ing-3", "Pasta", "kg", 1.0),
                    ForecastIngredientDto("ing-2", "Cheese", "kg", 0.5) // Cheese used again
                )
            )
        )

        // 2. Program the mock repository to return success for the getForecast call
        whenever(mockForecastRepository.getForecast(7)).thenReturn(Resource.Success(mockForecastData))

        // ACT
        // 3. Initialize the ViewModel
        val viewModel = ForecastViewModel(mockForecastRepository)

        // ASSERT
        // 4. Verify all LiveData states based on the ViewModel's internal processing

        // Summary Trend (should contain the forecast data)
        val summaryResult = viewModel.summaryTrend.value
        assertTrue(summaryResult is Resource.Success)
        assertEquals(2, (summaryResult as Resource.Success).data?.size)
        assertEquals(10, summaryResult.data?.find { it.date == today }?.quantity)

        // Dish Forecasts (processed from the forecast data)
        val dishResult = viewModel.dishForecasts.value
        assertTrue(dishResult is Resource.Success)
        assertEquals(2, (dishResult as Resource.Success).data?.size)
        assertEquals("Pizza", dishResult.data?.find { it.date == today }?.dishes?.first()?.name)

        // Comparison Data (is now stubbed in the ViewModel, should be empty)
        val comparisonResult = viewModel.comparisonData.value
        assertTrue(comparisonResult is Resource.Success)
        assertTrue((comparisonResult as Resource.Success).data?.isEmpty() == true)

        // Ingredient Forecast (processed from the forecast data)
        val ingredientResult = viewModel.ingredientForecast.value
        assertTrue(ingredientResult is Resource.Success)
        val ingredients = (ingredientResult as Resource.Success).data
        assertEquals(3, ingredients?.size) // Dough, Cheese, Pasta

        val cheese = ingredients?.find { it.name == "Cheese" }
        assertEquals("kg", cheese?.unit)
        assertEquals(2, cheese?.totalQuantity?.size)
        assertEquals(1.0, cheese?.totalQuantity?.get(0)) // Qty for today
        assertEquals(0.5, cheese?.totalQuantity?.get(1)) // Qty for tomorrow
    }
        
    @Test
    fun `init when getForecast fails should update all dependent LiveData to Error`() = runTest {
        // ARRANGE
        // 1. Program the main forecast repository call to fail
        val errorMessage = "Network Error"
        whenever(mockForecastRepository.getForecast(7)).thenReturn(Resource.Error(errorMessage))

        // ACT
        // 2. Initialize the ViewModel
        val viewModel = ForecastViewModel(mockForecastRepository)

        // ASSERT
        // 3. Verify that all dependent LiveData objects are in an Error state
        val summaryResult = viewModel.summaryTrend.value
        assertTrue(summaryResult is Resource.Error && summaryResult.message == errorMessage)

        val ingredientResult = viewModel.ingredientForecast.value
        assertTrue(ingredientResult is Resource.Error && ingredientResult.message == errorMessage)

        val dishResult = viewModel.dishForecasts.value
        assertTrue(dishResult is Resource.Error && dishResult.message == errorMessage)

        val comparisonResult = viewModel.comparisonData.value
        assertTrue(comparisonResult is Resource.Error && comparisonResult.message == errorMessage)
    }
                
                    
                        }
                        