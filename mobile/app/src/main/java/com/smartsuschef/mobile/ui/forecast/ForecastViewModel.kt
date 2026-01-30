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

    // Daily dish recommendations, grouped by date
    private val _dishForecasts = MutableLiveData<Resource<List<DailyDishForecast>>>()
    val dishForecasts: LiveData<Resource<List<DailyDishForecast>>> = _dishForecasts

    // Ingredient needs for next 7 days
    private val _ingredientForecast = MutableLiveData<Resource<List<IngredientForecast>>>()
    val ingredientForecast: LiveData<Resource<List<IngredientForecast>>> = _ingredientForecast

    init {
        loadPredictions()
    }

    fun loadPredictions() {
        viewModelScope.launch {
            _dishForecasts.value = Resource.Loading()
            _ingredientForecast.value = Resource.Loading()

            when (val result = forecastRepository.getForecast(7)) {
                is Resource.Success -> {
                    val forecastData = result.data ?: emptyList()
                    
                    // Process for Dish Forecasts (Group by date)
                    val dishData = forecastData
                        .groupBy { it.date }
                        .map { (date, forecasts) ->
                            val dishes = forecasts.map { DishForecast(it.recipeName, it.quantity) }
                            DailyDishForecast(date, dishes)
                        }
                        .sortedBy { it.date } // Sort by date
                    _dishForecasts.value = Resource.Success(dishData)

                    // TODO: This is a simplified transformation. A full implementation would aggregate
                    // all ingredient quantities across all predicted dishes for each day.
                    val ingredientData = forecastData
                        .flatMap { it.ingredients }
                        .groupBy { it.ingredientName }
                        .map { (name, ingredients) ->
                            IngredientForecast(
                                name = name,
                                unit = ingredients.first().unit,
                                totalQuantity = ingredients.sumOf { it.quantity }
                            )
                        }
                    _ingredientForecast.value = Resource.Success(ingredientData)
                }
                is Resource.Error -> {
                    val errorMessage = result.message ?: "Failed to load forecast"
                    _dishForecasts.value = Resource.Error(errorMessage)
                    _ingredientForecast.value = Resource.Error(errorMessage)
                }
                is Resource.Loading -> { /* Already handled */ }
            }
        }
    }
}

// Represents the forecast for a single day, containing a list of dishes
data class DailyDishForecast(
    val date: String,
    val dishes: List<DishForecast>
)

// Represents a single dish prediction
data class DishForecast(
    val name: String,
    val predictedSales: Int
)

// Represents total ingredient need over the forecast period (simplified)
data class IngredientForecast(
    val name: String,
    val unit: String,
    val totalQuantity: Double
)