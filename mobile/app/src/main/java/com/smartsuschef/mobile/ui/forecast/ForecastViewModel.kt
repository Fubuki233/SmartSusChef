package com.smartsuschef.mobile.ui.forecast

import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsuschef.mobile.data.repository.ForecastRepository
import com.smartsuschef.mobile.network.dto.ForecastDto
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class ForecastViewModel @Inject constructor(
    private val forecastRepository: ForecastRepository
) : ViewModel() {

    // Prediction Summary (Next 7 days aggregated)
    private val _summaryTrend = MutableLiveData<Resource<List<ForecastDto>>>()
    val summaryTrend: LiveData<Resource<List<ForecastDto>>> = _summaryTrend

    // Daily dish breakdown for stacked bar chart
    private val _dishForecasts = MutableLiveData<Resource<List<DailyDishForecast>>>()
    val dishForecasts: LiveData<Resource<List<DailyDishForecast>>> = _dishForecasts

    // Ingredient forecast table (7 days)
    private val _ingredientForecast = MutableLiveData<Resource<List<IngredientForecast>>>()
    val ingredientForecast: LiveData<Resource<List<IngredientForecast>>> = _ingredientForecast

    // Date headers for ingredient table
    private val _dateHeaders = MutableLiveData<List<String>>()
    val dateHeaders: LiveData<List<String>> = _dateHeaders

    // Comparison data (Past 7 days: Predicted vs Actual)
    private val _comparisonData = MutableLiveData<Resource<List<ForecastDto>>>()
    val comparisonData: LiveData<Resource<List<ForecastDto>>> = _comparisonData

    init {
        loadPredictions()
    }

    fun loadPredictions() {
        viewModelScope.launch {
            _summaryTrend.value = Resource.Loading()
            _dishForecasts.value = Resource.Loading()
            _ingredientForecast.value = Resource.Loading()
            _comparisonData.value = Resource.Loading()

            // PART A: Load Future Forecast Data (Next 7 days)
            when (val result = forecastRepository.getForecast(7)) {
                is Resource.Success -> {
                    val forecastData = result.data ?: emptyList()

                    // 1. Summary Trend (already aggregated)
                    _summaryTrend.value = Resource.Success(forecastData)

                    // 2. Store date headers for ingredient table
                    _dateHeaders.value = forecastData.map { it.date }

                    // 3. Process Ingredient Forecast Table
                    processIngredientTable(forecastData)
                }
                is Resource.Error -> {
                    val errorMessage = result.message ?: "Failed to load forecast"
                    _summaryTrend.value = Resource.Error(errorMessage)
                    _ingredientForecast.value = Resource.Error(errorMessage)
                }
                is Resource.Loading -> { /* Already set */ }
            }

            // PART B: Load Dish Breakdown for Stacked Bar (Next 7 days)
            when (val dishResult = forecastRepository.getDishBreakdown(7)) {
                is Resource.Success -> {
                    val breakdown = dishResult.data ?: emptyMap()
                    val dailyForecasts = breakdown.map { (date, dishes) ->
                        DailyDishForecast(
                            date = date,
                            dishes = dishes.map { DishForecast(it.recipeName, it.quantity) }
                        )
                    }.sortedBy { it.date }
                    _dishForecasts.value = Resource.Success(dailyForecasts)
                }
                is Resource.Error -> {
                    _dishForecasts.value = Resource.Error(dishResult.message ?: "Failed to load dish breakdown")
                }
                is Resource.Loading -> { /* Already set */ }
            }

            // PART C: Load Past Comparison Data (Previous 7 days)
            when (val comparisonResult = forecastRepository.getPastComparison()) {
                is Resource.Success -> {
                    _comparisonData.value = Resource.Success(comparisonResult.data ?: emptyList())
                }
                is Resource.Error -> {
                    _comparisonData.value = Resource.Error(comparisonResult.message ?: "Failed to load comparison")
                }
                is Resource.Loading -> { /* Already set */ }
            }
        }
    }

    private fun processIngredientTable(forecastData: List<ForecastDto>) {
        val dates = forecastData.map { it.date }.sorted()

        // Group ingredients across all days
        val ingredientMap = mutableMapOf<String, MutableMap<String, Double>>()

        forecastData.forEach { forecast ->
            forecast.ingredients.forEach { ingredient ->
                val ingName = ingredient.ingredientName
                val ingUnit = ingredient.unit
                val key = "$ingName ($ingUnit)"

                if (!ingredientMap.containsKey(key)) {
                    ingredientMap[key] = mutableMapOf()
                }

                ingredientMap[key]!![forecast.date] = ingredient.quantity
            }
        }

        // Convert to IngredientForecast list
        val ingredientList = ingredientMap.map { (nameWithUnit, dateMap) ->
            // Extract name and unit
            val parts = nameWithUnit.split(" (")
            val name = parts[0]
            val unit = parts.getOrNull(1)?.removeSuffix(")") ?: ""

            // Map quantities to the 7 days
            val quantities = dates.map { date -> dateMap[date] ?: 0.0 }

            IngredientForecast(name, unit, quantities)
        }

        _ingredientForecast.value = Resource.Success(ingredientList)
    }
}

// Represents forecast for a single day with dish breakdown
data class DailyDishForecast(
    val date: String,
    val dishes: List<DishForecast>
)

// Represents a single dish prediction
data class DishForecast(
    val name: String,
    val predictedSales: Int
)

// Represents ingredient requirements over 7 days
data class IngredientForecast(
    val name: String,
    val unit: String,
    val totalQuantity: List<Double> // 7 values for 7 days
)