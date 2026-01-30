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

    fun submitData(quantity: Double) {
        viewModelScope.launch {
            _submitStatus.value = Resource.Loading()
            val id = _selectedItemId.value
            val name = _selectedItemName.value
            if (id == null || name == null) {
                _submitStatus.value = Resource.Error("No item selected.")
                return@launch
            }

            val todayStr = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())

            val result = if (_isSalesMode.value == true) {
                salesRepository.create(CreateSalesDataRequest(date = todayStr, recipeId = id, quantity = quantity.toInt()))
            } else {
                val wastageRequest = when (_wastageType.value) {
                    WastageType.MAIN_DISH, WastageType.SUB_RECIPE -> CreateWastageDataRequest(date = todayStr, recipeId = id, quantity = quantity)
                    WastageType.INGREDIENT -> CreateWastageDataRequest(date = todayStr, ingredientId = id, quantity = quantity)
                    else -> null // Should not happen
                }
                if (wastageRequest != null) {
                    wastageRepository.create(wastageRequest)
                } else {
                    Resource.Error("Invalid wastage type.")
                }
            }

            when (result) {
                is Resource.Success -> {
                    // Add the new entry to our in-memory list
                    val newEntry = RecentEntry(name, quantity, if (_isSalesMode.value == true) "plates" else "units")
                    submittedEntries.add(0, newEntry) // Add to the top of the list

                    _submitStatus.value = Resource.Success(Unit)
                    loadRecentEntries() // Refresh after successful submission
                }
                is Resource.Error -> {
                    _submitStatus.value = Resource.Error(result.message ?: "Unknown error")
                }
                is Resource.Loading -> { /* Do nothing */ }
            }
        }
    }

    private fun fetchIngredients() {
        viewModelScope.launch {
            _ingredients.value = Resource.Loading()
            _ingredients.value = ingredientsRepository.getAll()
        }
    }

    private fun fetchRecipes() {
        viewModelScope.launch {
             _mainRecipes.value = Resource.Loading()
             _subRecipes.value = Resource.Loading()
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
                else -> { /* Loading */ }
            }
        }
    }

    private fun loadRecentEntries() {
        // Update the LiveData with the current list of submitted entries
        _recentEntries.value = submittedEntries.toList()
    }
}