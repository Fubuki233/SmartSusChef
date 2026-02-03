package com.smartsuschef.mobile.ui.datainput

import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import androidx.appcompat.app.AlertDialog
import androidx.core.view.isVisible
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.network.dto.IngredientDto
import com.smartsuschef.mobile.network.dto.RecipeDto
import com.smartsuschef.mobile.databinding.FragmentDataInputBinding
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.showToast
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class DataInputFragment : Fragment(R.layout.fragment_data_input), RecentEntryActions {

    private var _binding: FragmentDataInputBinding? = null
    private val binding get() = _binding!!
    private val viewModel: DataInputViewModel by viewModels()
    private lateinit var recentEntriesAdapter: RecentEntriesAdapter

    private lateinit var ingredientsAdapter: ArrayAdapter<String>
    private lateinit var mainRecipesAdapter: ArrayAdapter<String>
    private lateinit var subRecipesAdapter: ArrayAdapter<String>

    private var currentEditedEntry: RecentEntry? = null

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentDataInputBinding.bind(view)

        setupSpinners()
        setupTabs()
        setupRecyclerView()
        observeViewModel()
    }

    private fun setupSpinners() {
        val spinnerLayout = android.R.layout.simple_spinner_dropdown_item
        ingredientsAdapter = ArrayAdapter(requireContext(), spinnerLayout, mutableListOf())
        mainRecipesAdapter = ArrayAdapter(requireContext(), spinnerLayout, mutableListOf())
        subRecipesAdapter = ArrayAdapter(requireContext(), spinnerLayout, mutableListOf())

        binding.itemSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                if (position == 0) { // Hint position
                    viewModel.onItemSelected("", "")
                    return
                }
                val selectedName = parent?.getItemAtPosition(position).toString()
                val isSalesMode = viewModel.isSalesMode.value ?: true
                val wastageType = viewModel.wastageType.value

                val (selectedId, selectedItemName) = when {
                    isSalesMode -> {
                        val item = viewModel.mainRecipes.value?.data?.find { it.name == selectedName }
                        Pair(item?.id, item?.name)
                    }
                    else -> when (wastageType) {
                        WastageType.MAIN_DISH -> {
                            val item = viewModel.mainRecipes.value?.data?.find { it.name == selectedName }
                            Pair(item?.id, item?.name)
                        }
                        WastageType.SUB_RECIPE -> {
                            val item = viewModel.subRecipes.value?.data?.find { it.name == selectedName }
                            Pair(item?.id, item?.name)
                        }
                        WastageType.INGREDIENT -> {
                            val item = viewModel.ingredients.value?.data?.find { it.name == selectedName }
                            Pair(item?.id, item?.name)
                        }
                        else -> Pair(null, null)
                    }
                }

                if (selectedId != null && selectedItemName != null) {
                    viewModel.onItemSelected(selectedId, selectedItemName)
                }
            }
            override fun onNothingSelected(parent: AdapterView<*>?) {
                viewModel.onItemSelected("", "")
            }
        }
    }

    private fun setupTabs() {
        binding.toggleGroup.addOnButtonCheckedListener { _, checkedId, isChecked ->
            if (isChecked) {
                val isSales = checkedId == R.id.btnSalesTab
                viewModel.setMode(isSales)
                resetForm()
            }
        }

        binding.wastageTypeToggleGroup.addOnButtonCheckedListener { group, checkedId, isChecked ->
            if (isChecked) {
                val type = when (checkedId) {
                    R.id.btnMainDishType -> WastageType.MAIN_DISH
                    R.id.btnSubRecipeType -> WastageType.SUB_RECIPE
                    else -> WastageType.INGREDIENT
                }
                viewModel.setWastageType(type)
                resetForm()
            }
        }
    }

    private fun updateSpinnerAdapter(isSales: Boolean, wastageType: WastageType?) {
        binding.itemSpinner.adapter = if (isSales) {
            mainRecipesAdapter
        } else {
            when (wastageType) {
                WastageType.MAIN_DISH -> mainRecipesAdapter
                WastageType.SUB_RECIPE -> subRecipesAdapter
                WastageType.INGREDIENT -> ingredientsAdapter
                else -> null
            }
        }
        binding.itemSpinner.setSelection(0)
    }

    private fun setupRecyclerView() {
        recentEntriesAdapter = RecentEntriesAdapter(this) // Pass this as listener
        binding.rvRecentEntries.apply {
            adapter = recentEntriesAdapter
            layoutManager = LinearLayoutManager(requireContext())
        }
    }

    private fun observeViewModel() {
        // --- Observers for Spinner Data ---
        viewModel.ingredients.observe(viewLifecycleOwner) { resource ->
            handleResource(resource, ingredientsAdapter, "Select Ingredient...")
        }
        viewModel.mainRecipes.observe(viewLifecycleOwner) { resource ->
            handleResource(resource, mainRecipesAdapter, "Select Main Dish...")
        }
        viewModel.subRecipes.observe(viewLifecycleOwner) { resource ->
            handleResource(resource, subRecipesAdapter, "Select Sub-Recipe...")
        }

        // --- Observers for UI State ---
        viewModel.isSalesMode.observe(viewLifecycleOwner) { isSales ->
            binding.wastageTypeToggleGroup.isVisible = !isSales
            binding.tvStep1Label.text = if (isSales) "Step 1: Select Dish" else "Step 1: Select Item Type"
            updateSpinnerAdapter(isSales, viewModel.wastageType.value)
        }
        viewModel.wastageType.observe(viewLifecycleOwner) { wastageType ->
            if (viewModel.isSalesMode.value == false) {
                updateSpinnerAdapter(false, wastageType)
            }
        }

        // --- Observers for Actions and Results ---
        viewModel.recentEntries.observe(viewLifecycleOwner) { entries ->
            recentEntriesAdapter.submitList(entries)
        }
        viewModel.submitStatus.observe(viewLifecycleOwner) { resource ->
            when (resource) {
                is Resource.Success -> {
                    requireContext().showToast(resource.message ?: "Entry saved successfully!")
                    resetForm()
                }
                is Resource.Error -> requireContext().showToast("Error saving entry: ${resource.message}")
                is Resource.Loading -> { /* Optionally show a loading dialog/spinner */ }
            }
        }

        // --- Click Listener ---
        binding.btnSaveData.setOnClickListener {
            val qty = binding.etQuantity.text.toString().toDoubleOrNull()

            if (qty == null || qty <= 0) {
                requireContext().showToast("Please enter a valid quantity")
                return@setOnClickListener
            }
            val selectedItemId = viewModel.selectedItemId.value
            val selectedItemName = viewModel.selectedItemName.value

            if (selectedItemId.isNullOrEmpty() || selectedItemName.isNullOrEmpty()) {
                requireContext().showToast("Please select an item first.")
                return@setOnClickListener
            }

            // If we are currently editing an entry, just submit the update
            if (currentEditedEntry != null) {
                viewModel.submitData(qty, currentEditedEntry!!.id)
                return@setOnClickListener
            }

            // Check for existing entry if not in edit mode
            val existingEntry = viewModel.findExistingEntry(selectedItemName)
            if (existingEntry != null) {
                AlertDialog.Builder(requireContext())
                    .setTitle("Entry Already Exists")
                    .setMessage("You have already entered data for ${existingEntry.name}. Do you want to overwrite the existing quantity?")
                    .setPositiveButton("Overwrite Entry") { dialog, _ ->
                        viewModel.submitData(qty, existingEntry.id)
                        dialog.dismiss()
                    }
                    .setNegativeButton("Cancel") { dialog, _ ->
                        dialog.dismiss()
                    }
                    .show()
            } else {
                viewModel.submitData(qty)
            }
        }
    }

    private fun resetForm() {
        binding.etQuantity.text.clear()
        binding.itemSpinner.setSelection(0)
        binding.itemSpinner.isEnabled = true // Enable spinner
        binding.btnSaveData.text = "Save" // Reset button text
        currentEditedEntry = null // Clear edited entry
    }

    override fun onEditClick(entry: RecentEntry) {
        currentEditedEntry = entry
        // Populate form fields
        binding.etQuantity.setText(entry.quantity.toString())

        // Set spinner selection
        val adapter = binding.itemSpinner.adapter as ArrayAdapter<String>
        val position = adapter.getPosition(entry.name)
        if (position != -1) {
            binding.itemSpinner.setSelection(position)
        }
        binding.itemSpinner.isEnabled = false // Lock item selection

        binding.btnSaveData.text = "Update" // Change button text
    }

    override fun onDeleteClick(entry: RecentEntry) {
        AlertDialog.Builder(requireContext())
            .setTitle("Delete Entry")
            .setMessage("Are you sure you want to delete the entry for ${entry.name}? This action cannot be undone.")
            .setPositiveButton("Delete") { dialog, _ ->
                viewModel.deleteEntry(entry)
                dialog.dismiss()
            }
            .setNegativeButton("Cancel") { dialog, _ ->
                dialog.dismiss()
            }
            .show()
    }

    private fun <T> handleResource(resource: Resource<List<T>>, adapter: ArrayAdapter<String>, hint: String) {
        when (resource) {
            is Resource.Success -> {
                adapter.clear()
                adapter.add(hint)
                adapter.addAll(resource.data?.mapNotNull { item ->
                    when(item) {
                        is RecipeDto -> item.name
                        is IngredientDto -> item.name
                        else -> null
                    }
                } ?: emptyList())
                adapter.notifyDataSetChanged()
            }
            is Resource.Error -> requireContext().showToast("Error loading data: ${resource.message}")
            is Resource.Loading -> { /* Optionally show a loading indicator */ }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}