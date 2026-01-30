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

@HiltViewModel
class WastageViewModel @Inject constructor(
    private val wastageRepository: WastageRepository
) : ViewModel() {

    private val _wastageTrend = MutableLiveData<Resource<List<WastageTrendData>>>()
    val wastageTrend: LiveData<Resource<List<WastageTrendData>>> = _wastageTrend

    private val _impactDistribution = MutableLiveData<List<ImpactData>>()
    val impactDistribution: LiveData<List<ImpactData>> = _impactDistribution

    init {
        // Load initial data, e.g., for the last 7 days
        getWastageData(7)
    }

    fun getWastageData(days: Int) {
        viewModelScope.launch {
            _wastageTrend.value = Resource.Loading()

            val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
            val calendar = Calendar.getInstance()
            val endDate = dateFormat.format(calendar.time)
            calendar.add(Calendar.DAY_OF_YEAR, -(days - 1))
            val startDate = dateFormat.format(calendar.time)

            when(val result = wastageRepository.getTrend(startDate, endDate)) {
                is Resource.Success -> {
                    val trendData = result.data ?: emptyList()
                    val mappedTrendData = trendData.map {
                        WastageTrendData(it.date, it.totalQuantity, it.totalCarbonFootprint)
                    }
                    _wastageTrend.value = Resource.Success(mappedTrendData)

                    // Process the breakdown for the pie chart and top wasted list
                    val impactData = trendData
                        .flatMap { it.itemBreakdown } // Get a single list of all wasted items
                        .groupBy { it.displayName } // Group by name
                        .map { (name, items) ->
                            // Sum the carbon footprint for each item
                            ImpactData(name, items.sumOf { it.carbonFootprint })
                        }
                        .sortedByDescending { it.carbonValue } // Sort to get top wasted items
                    
                    _impactDistribution.value = impactData
                }
                is Resource.Error -> {
                    _wastageTrend.value = Resource.Error(result.message ?: "Failed to load wastage trend")
                }
                else -> { /* Loading state already set */ }
            }
        }
    }
}

// Data models based on UI needs
data class WastageTrendData(val date: String, val weightKg: Double, val carbonKg: Double)
data class ImpactData(val category: String, val carbonValue: Double)