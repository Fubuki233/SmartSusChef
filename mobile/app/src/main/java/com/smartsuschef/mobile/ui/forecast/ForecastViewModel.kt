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

            // Load forecast data (future predictions + past actuals)
            when (val result = forecastRepository.getForecast(7)) {
                is Resource.Success -> {
                    val allForecastData = result.data.orEmpty()

                    val futureForecastData = allForecastData.filter { it.quantity > 0 }
                    val pastComparisonRawData = allForecastData.filter { it.actualQuantity > 0 }

                    // 1. Summary trend
                    _summaryTrend.value = Resource.Success(futureForecastData)

                    // 2. Date headers for ingredient table
                    _dateHeaders.value = futureForecastData.map { it.date }

                    // 3. Ingredient forecast table
                    processIngredientTable(futureForecastData)

                    // 4. Daily dish breakdown
                    val dailyDishForecasts = futureForecastData
                        .groupBy { it.date }
                        .map { (date, dailyForecasts) ->
                            DailyDishForecast(
                                date = date,
                                dishes = dailyForecasts.map {
                                    DishForecast(
                                        name = it.recipeName,
                                        predictedSales = it.quantity
                                    )
                                }
                            )
                        }
                        .sortedBy { it.date }

                    _dishForecasts.value = Resource.Success(dailyDishForecasts)

                    // 5. Past comparison data (Predicted vs Actual)
                    val comparisonDataProcessed = pastComparisonRawData.map { forecast ->
                        forecast.copy(
                            quantity = forecast.quantity,
                            actualQuantity = forecast.actualQuantity
                        )
                    }

                    _comparisonData.value = Resource.Success(comparisonDataProcessed)
                }

                is Resource.Error -> {
                    val errorMessage = result.message ?: "Failed to load forecast"
                    _summaryTrend.value = Resource.Error(errorMessage)
                    _ingredientForecast.value = Resource.Error(errorMessage)
                    _dishForecasts.value = Resource.Error(errorMessage)
                    _comparisonData.value = Resource.Error(errorMessage)
                }

                is Resource.Loading -> {
                    // Already handled
                }
            }
        }
    }

    private fun processIngredientTable(forecastData: List<ForecastDto>) {
        val dates = forecastData.map { it.date }.sorted()

        val ingredientMap = mutableMapOf<String, MutableMap<String, Double>>()

        forecastData.forEach { forecast ->
            forecast.ingredients.forEach { ingredient ->
                val key = "${ingredient.ingredientName} (${ingredient.unit})"
                val dateMap = ingredientMap.getOrPut(key) { mutableMapOf() }
                dateMap[forecast.date] = ingredient.quantity
            }
        }

        val ingredientList = ingredientMap.map { (nameWithUnit, dateMap) ->
            val parts = nameWithUnit.split(" (")
            val name = parts[0]
            val unit = parts.getOrNull(1)?.removeSuffix(")") ?: ""

            val quantities = dates.map { date ->
                dateMap[date] ?: 0.0
            }

            IngredientForecast(
                name = name,
                unit = unit,
                totalQuantity = quantities
            )
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
    val totalQuantity: List<Double>
)
