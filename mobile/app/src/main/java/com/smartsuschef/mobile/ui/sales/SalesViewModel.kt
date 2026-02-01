package com.smartsuschef.mobile.ui.sales
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsuschef.mobile.data.repository.SalesRepository
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale
import javax.inject.Inject
import com.smartsuschef.mobile.util.DateUtils

@HiltViewModel
class SalesViewModel @Inject constructor(
    private val salesRepository: SalesRepository
) : ViewModel() {

    // Sales Trend (7 days)
    private val _salesTrend = MutableLiveData<Resource<List<SalesTrendItem>>>()
    val salesTrend: LiveData<Resource<List<SalesTrendItem>>> = _salesTrend

    // Ingredient breakdown for a specific date
    private val _ingredientBreakdown = MutableLiveData<Resource<List<IngredientRequirement>>>()
    val ingredientBreakdown: LiveData<Resource<List<IngredientRequirement>>> = _ingredientBreakdown

    init {
        fetchOverviewData()
    }

    fun fetchOverviewData() {
        viewModelScope.launch {
            _salesTrend.value = Resource.Loading()
            
            val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
            val calendar = Calendar.getInstance()
            val endDate = dateFormat.format(calendar.time)
            calendar.add(Calendar.DAY_OF_YEAR, -6) // For a 7-day trend
            val startDate = dateFormat.format(calendar.time)

            when(val result = salesRepository.getTrend(startDate, endDate)) {
                is Resource.Success -> {
                    val trendItems = result.data?.map { SalesTrendItem(it.date, it.totalQuantity) }
                    _salesTrend.value = Resource.Success(trendItems ?: emptyList())
                }
                is Resource.Error -> {
                    _salesTrend.value = Resource.Error(result.message ?: "Failed to load sales trend")
                }
                else -> { /* Loading state is already set */ }
            }
        }
    }

    fun fetchIngredientsForDate(date: String) {
//        val formattedDate = DateUtils.formatDateForApi(date)
        viewModelScope.launch {
            _ingredientBreakdown.value = Resource.Loading()
            when(val result = salesRepository.getIngredientUsageByDate(date)) {
                is Resource.Success -> {
                    val requirements = result.data?.map {
                        IngredientRequirement(it.ingredientName, it.quantity, it.unit)
                    }
                    _ingredientBreakdown.value = Resource.Success(requirements ?: emptyList())
                }
                is Resource.Error -> {
                    _ingredientBreakdown.value = Resource.Error(result.message ?: "Failed to load ingredient breakdown")
                }
                else -> { /* Loading state is already set */ }
            }
        }
    }
}

// Data models matching UI needs
data class SalesTrendItem(val date: String, val sales: Int)
data class IngredientRequirement(val name: String, val quantity: Double, val unit: String)