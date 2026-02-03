package com.smartsuschef.mobile.ui.wastage

import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsuschef.mobile.data.repository.WastageRepository
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale
import javax.inject.Inject

enum class WastageFilter { TODAY, LAST_7_DAYS }

@HiltViewModel
class WastageViewModel @Inject constructor(
    private val wastageRepository: WastageRepository
) : ViewModel() {

    private val _wastageTrend = MutableLiveData<Resource<List<WastageTrendItem>>>()
    val wastageTrend: LiveData<Resource<List<WastageTrendItem>>> = _wastageTrend

    private val _wastageBreakdown = MutableLiveData<Resource<List<WastageBreakdownItem>>>()
    val wastageBreakdown: LiveData<Resource<List<WastageBreakdownItem>>> = _wastageBreakdown

    private val _currentFilter = MutableLiveData(WastageFilter.LAST_7_DAYS)
    val currentFilter: LiveData<WastageFilter> = _currentFilter

    init {
        fetchWastageTrend()
    }

    fun setFilter(filter: WastageFilter) {
        _currentFilter.value = filter
        fetchWastageTrend(filter)
    }

    private fun fetchWastageTrend(filter: WastageFilter = WastageFilter.LAST_7_DAYS) {
        viewModelScope.launch {
            _wastageTrend.value = Resource.Loading()

            val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
            val calendar = Calendar.getInstance()
            val endDate = dateFormat.format(calendar.time)

            val startDate = if (filter == WastageFilter.TODAY) {
                endDate
            } else {
                calendar.add(Calendar.DAY_OF_YEAR, -6)
                dateFormat.format(calendar.time)
            }

            when (val result = wastageRepository.getTrend(startDate, endDate)) {
                is Resource.Success -> {
                    val trendItems = result.data?.map { WastageTrendItem(it.date, it.totalQuantity, it.itemBreakdown) }
                    _wastageTrend.value = Resource.Success(trendItems ?: emptyList())
                }
                is Resource.Error -> {
                    _wastageTrend.value = Resource.Error(result.message ?: "Failed to load wastage trend")
                }
                else -> {
                }
            }
        }
    }

    fun fetchWastageBreakdownForDate(date: String) {
        viewModelScope.launch {
            _wastageBreakdown.value = Resource.Loading()
            // In a real app, you'd fetch this from the repository.
            // For now, we'll filter the existing data.
            val trendData = (wastageTrend.value as? Resource.Success)?.data
            val dayData = trendData?.find { it.date == date }

            if (dayData != null) {
                val breakdownItems = dayData.itemBreakdown.map {
                    WastageBreakdownItem(
                        name = it.displayName,
                        quantity = it.quantity,
                        unit = it.unit,
                        carbonFootprint = it.carbonFootprint,
                        // This is a simplification. In a real app, you'd have a more robust way to determine the type.
                        type = when {
                            it.recipeId?.startsWith("sub-recipe") == true -> "Sub-Recipe"
                            it.recipeId != null -> "Main Dish"
                            else -> "Raw Ingredient"
                        }
                    )
                }
                _wastageBreakdown.value = Resource.Success(breakdownItems)
            } else {
                _wastageBreakdown.value = Resource.Error("No wastage data found for this date.")
            }
        }
    }
}

data class WastageTrendItem(
    val date: String,
    val totalQuantity: Double,
    val itemBreakdown: List<com.smartsuschef.mobile.network.dto.ItemWastageDto>
)

data class WastageBreakdownItem(
    val name: String,
    val quantity: Double,
    val unit: String,
    val carbonFootprint: Double,
    val type: String
)
