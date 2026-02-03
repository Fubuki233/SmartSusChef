package com.smartsuschef.mobile.ui.datainput

import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsuschef.mobile.data.repository.IngredientsRepository
import com.smartsuschef.mobile.data.repository.RecipesRepository
import com.smartsuschef.mobile.data.repository.SalesRepository
import com.smartsuschef.mobile.data.repository.WastageRepository
import com.smartsuschef.mobile.network.dto.CreateSalesDataRequest
import com.smartsuschef.mobile.network.dto.CreateWastageDataRequest
import com.smartsuschef.mobile.network.dto.IngredientDto
import com.smartsuschef.mobile.network.dto.RecipeDto
import com.smartsuschef.mobile.network.dto.SalesDataDto
import com.smartsuschef.mobile.network.dto.UpdateSalesDataRequest
import com.smartsuschef.mobile.network.dto.UpdateWastageDataRequest
import com.smartsuschef.mobile.network.dto.WastageDataDto
import com.smartsuschef.mobile.ui.datainput.RecentEntry
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import javax.inject.Inject

enum class WastageType {
    MAIN_DISH, SUB_RECIPE, INGREDIENT
}

@HiltViewModel
class DataInputViewModel @Inject constructor(
    private val salesRepository: SalesRepository,
    private val wastageRepository: WastageRepository,
    private val ingredientsRepository: IngredientsRepository,
    private val recipesRepository: RecipesRepository
) : ViewModel() {

    // Tracks whether we are in "Sales" or "Wastage" mode
    private val _isSalesMode = MutableLiveData(true)
    val isSalesMode: LiveData<Boolean> = _isSalesMode

    // Tracks the selected wastage type
    private val _wastageType = MutableLiveData(WastageType.INGREDIENT)
    val wastageType: LiveData<WastageType> = _wastageType

    // Data for dropdowns
    private val _ingredients = MutableLiveData<Resource<List<IngredientDto>>>()
    val ingredients: LiveData<Resource<List<IngredientDto>>> = _ingredients

    private val _mainRecipes = MutableLiveData<Resource<List<RecipeDto>>>()
    val mainRecipes: LiveData<Resource<List<RecipeDto>>> = _mainRecipes

    private val _subRecipes = MutableLiveData<Resource<List<RecipeDto>>>()
    val subRecipes: LiveData<Resource<List<RecipeDto>>> = _subRecipes

    // Currently selected item in the spinner
    private val _selectedItemId = MutableLiveData<String?>()
    val selectedItemId: LiveData<String?> = _selectedItemId

    private val _selectedItemName = MutableLiveData<String?>()
    val selectedItemName: LiveData<String?> = _selectedItemName
    
    // Recent entries to display in the list below the form
    private val _recentEntries = MutableLiveData<List<RecentEntry>>()
    val recentEntries: LiveData<List<RecentEntry>> = _recentEntries

    private val _submitStatus = MutableLiveData<Resource<Unit>>()
    val submitStatus: LiveData<Resource<Unit>> = _submitStatus

    // In-memory list to hold entries for the session
    private val submittedEntries = mutableListOf<RecentEntry>()

    init {
        fetchIngredients()
        fetchRecipes()
        loadRecentEntries() // Initial load
    }

    fun setMode(isSales: Boolean) {
        _isSalesMode.value = isSales
        _selectedItemId.value = null // Clear selection when mode changes
        _selectedItemName.value = null
        // In a real app, you might want to clear or separate the list based on mode
        loadRecentEntries() // Refresh list based on mode
    }

    fun setWastageType(type: WastageType) {
        _wastageType.value = type
        _selectedItemId.value = null // Clear selection when type changes
        _selectedItemName.value = null
    }

    fun onItemSelected(itemId: String, itemName: String) {
        _selectedItemId.value = itemId
        _selectedItemName.value = itemName
    }

    fun findExistingEntry(itemName: String): RecentEntry? {
        val currentMode = _isSalesMode.value ?: true
        return submittedEntries.find { it.name == itemName && it.isSales == currentMode }
    }

    fun submitData(quantity: Double, existingEntryId: String? = null) {
        viewModelScope.launch {
            _submitStatus.value = Resource.Loading<Unit>()
            val id = _selectedItemId.value
            val name = _selectedItemName.value
            val isSalesMode = _isSalesMode.value ?: true
            val unit = if (isSalesMode) "plates" else "units"

            if (id == null || name == null) {
                _submitStatus.value = Resource.Error<Unit>("No item selected.")
                return@launch
            }

            val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
            val currentTimeStr = SimpleDateFormat("HH:mm", Locale.getDefault()).format(Date())

            val result: Resource<out Any> = if (isSalesMode) {
                if (existingEntryId != null) {
                    salesRepository.update(existingEntryId, UpdateSalesDataRequest(date = todayStr, recipeId = id, quantity = quantity.toInt()))
                } else {
                    salesRepository.create(CreateSalesDataRequest(date = todayStr, recipeId = id, quantity = quantity.toInt()))
                }
            } else { // Wastage mode
                val wastageRequest = when (_wastageType.value) {
                    WastageType.MAIN_DISH, WastageType.SUB_RECIPE -> CreateWastageDataRequest(date = todayStr, recipeId = id, quantity = quantity)
                    WastageType.INGREDIENT -> CreateWastageDataRequest(date = todayStr, ingredientId = id, quantity = quantity)
                    else -> null // This case implies an invalid or unhandled wastageType
                }

                if (existingEntryId != null) {
                    wastageRepository.update(existingEntryId, UpdateWastageDataRequest(date = todayStr, ingredientId = if (_wastageType.value == WastageType.INGREDIENT) id else null, recipeId = if (_wastageType.value != WastageType.INGREDIENT) id else null, quantity = quantity))
                } else { // If no existingEntryId, then it's a creation or an error
                    if (wastageRequest != null) {
                        wastageRepository.create(wastageRequest)
                    } else {
                        // This else corresponds to when wastageRequest is null (i.e., invalid wastage type)
                        Resource.Error<WastageDataDto>("Invalid wastage type selected or request could not be formed.")
                    }
                }
            }


            when (result) {
                is Resource.Success -> {
                    val newEntryId = when (result.data) {
                        is SalesDataDto -> result.data.id
                        is WastageDataDto -> result.data.id
                        else -> existingEntryId // For updates, use existing ID
                    } ?: return@launch // Should not happen for successful creation/update

                    val newEntry = RecentEntry(
                        id = newEntryId,
                        name = name,
                        quantity = quantity,
                        unit = unit,
                        date = todayStr, // Or result.data.date if available and different
                        time = currentTimeStr, // Added time
                        isSales = isSalesMode
                    )

                    // Update or add the entry
                    val existingIndex = submittedEntries.indexOfFirst { it.id == newEntry.id }
                    if (existingIndex != -1) {
                        submittedEntries[existingIndex] = newEntry
                    } else {
                        submittedEntries.add(0, newEntry)
                    }

                    _submitStatus.value = Resource.Success<Unit>(Unit, "Entry saved successfully!")
                    loadRecentEntries() // Refresh after successful submission
                }
                is Resource.Error -> {
                    _submitStatus.value = Resource.Error<Unit>(result.message ?: "Unknown error")
                }
                is Resource.Loading -> { /* Do nothing */ }
            }
        }
    }

    fun deleteEntry(entry: RecentEntry) {
        viewModelScope.launch {
            _submitStatus.value = Resource.Loading<Unit>()
            val result = if (entry.isSales) {
                salesRepository.delete(entry.id)
            } else {
                wastageRepository.delete(entry.id)
            }

            when (result) {
                is Resource.Success -> {
                    submittedEntries.remove(entry)
                    _submitStatus.value = Resource.Success<Unit>(Unit, "Entry deleted successfully!")
                    loadRecentEntries()
                }
                is Resource.Error -> {
                    _submitStatus.value = Resource.Error<Unit>(result.message ?: "Failed to delete entry.")
                }
                is Resource.Loading -> { /* Do nothing */ }
            }
        }
    }

    // Existing functions...

    private fun fetchIngredients() {
        viewModelScope.launch {
            _ingredients.value = Resource.Loading<List<IngredientDto>>()
            _ingredients.value = ingredientsRepository.getAll()
        }
    }

    private fun fetchRecipes() {
        viewModelScope.launch {
             _mainRecipes.value = Resource.Loading<List<RecipeDto>>()
             _subRecipes.value = Resource.Loading<List<RecipeDto>>()
            when(val result = recipesRepository.getAll()) {
                is Resource.Success -> {
                    val allRecipes = result.data ?: emptyList()
                    _mainRecipes.value = Resource.Success(allRecipes.filter { !it.isSubRecipe })
                    _subRecipes.value = Resource.Success(allRecipes.filter { it.isSubRecipe })
                }
                is Resource.Error -> {
                    val error = Resource.Error<List<RecipeDto>>(result.message ?: "Failed to load recipes")
                    _mainRecipes.value = error
                    _subRecipes.value = error
                }
                is Resource.Loading -> { /* Loading */ }
            }
        }
    }

    private fun loadRecentEntries() {
        val currentMode = _isSalesMode.value ?: true
        _recentEntries.value = submittedEntries.filter { it.isSales == currentMode }.toList()
    }
}