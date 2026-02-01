package com.smartsuschef.mobile.ui.sales

import android.os.Bundle
import android.view.View
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.navigation.fragment.navArgs
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.FragmentSalesDetailBinding
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.AndroidEntryPoint
import androidx.appcompat.app.AppCompatActivity

@AndroidEntryPoint
class SalesDetailFragment : Fragment(R.layout.fragment_sales_detail) {
    private var _binding: FragmentSalesDetailBinding? = null
    private val binding get() = _binding!!
    private val viewModel: SalesViewModel by viewModels()

    // Automatically retrieves the "date" passed from the Overview
    private val args: SalesDetailFragmentArgs by navArgs()

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentSalesDetailBinding.bind(view)

        // Set up the toolbar
        (activity as? AppCompatActivity)?.supportActionBar?.apply {
            title = "Sales for ${args.date}"
            setDisplayHomeAsUpEnabled(true)
        }

        binding.tvDetailTitle.text = "Sales Breakdown: ${args.date}"

        setupRecyclerView()

        // Pass the date to the ViewModel to fetch mock or API data
        viewModel.fetchIngredientsForDate(args.date)
        observeIngredients()
    }

    private fun setupRecyclerView() {
        binding.rvIngredients.layoutManager = LinearLayoutManager(requireContext())
        // Adapter initialization here...
    }

    private fun observeIngredients() {
        viewModel.ingredientBreakdown.observe(viewLifecycleOwner) { result ->
            if (result is Resource.Success) {
                // Update adapter with result.data
            }
        }
    }
}