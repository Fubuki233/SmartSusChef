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

import android.graphics.Color
import android.widget.Toast
import androidx.core.content.ContextCompat
import androidx.core.view.isVisible
import com.github.mikephil.charting.data.PieData
import com.github.mikephil.charting.data.PieDataSet
import com.github.mikephil.charting.data.PieEntry
import com.github.mikephil.charting.formatter.LargeValueFormatter
import com.github.mikephil.charting.components.Legend

@AndroidEntryPoint
class SalesDetailFragment : Fragment(R.layout.fragment_sales_detail) {
    private var _binding: FragmentSalesDetailBinding? = null
    private val binding get() = _binding!!
    private val viewModel: SalesViewModel by viewModels()
    private val args: SalesDetailFragmentArgs by navArgs()
    private lateinit var ingredientAdapter: IngredientAdapter

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentSalesDetailBinding.bind(view)

        setupUI()
        setupPieChartStyle() // Renamed to clarify its purpose
        setupRecyclerView()

        // Fetch both data sets for the given date
        viewModel.fetchIngredientsForDate(args.date)
        viewModel.fetchRecipeSalesForDate(args.date)
        
        observeData()
    }

    private fun setupUI() {
        (activity as? AppCompatActivity)?.supportActionBar?.apply {
            title = "Sales Details"
        }
        binding.tvDetailTitle.text = "Sales Breakdown: ${args.date}"
    }

    // This function now only sets up the chart's appearance
    private fun setupPieChartStyle() {
        binding.pieChartDishBreakdown.apply {
            description.isEnabled = false
            isDrawHoleEnabled = true
            setHoleColor(Color.TRANSPARENT)
            setDrawEntryLabels(false)
            animateY(1000)

            legend.apply {
                isEnabled = true
                isWordWrapEnabled = true
                horizontalAlignment = Legend.LegendHorizontalAlignment.CENTER
                verticalAlignment = Legend.LegendVerticalAlignment.BOTTOM
                orientation = Legend.LegendOrientation.HORIZONTAL
                setDrawInside(false)
                xEntrySpace = 10f
                yEntrySpace = 5f
                form = Legend.LegendForm.SQUARE
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
            }
        }
    }

    private fun setupRecyclerView() {
        ingredientAdapter = IngredientAdapter(emptyList())
        binding.rvIngredients.layoutManager = LinearLayoutManager(requireContext())
        binding.rvIngredients.adapter = ingredientAdapter
    }

    private fun observeData() {
        // Observer for the ingredient list
        viewModel.ingredientBreakdown.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Success -> {
                    binding.progressBarIngredients.isVisible = false
                    ingredientAdapter.updateData(result.data ?: emptyList())
                }
                is Resource.Error -> {
                    binding.progressBarIngredients.isVisible = false
                    Toast.makeText(requireContext(), result.message, Toast.LENGTH_SHORT).show()
                }
                is Resource.Loading -> {
                    binding.progressBarIngredients.isVisible = true
                }
            }
        }

        // Observer for the pie chart data
        viewModel.recipeSales.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Success -> {
                    binding.progressBarPieChart.isVisible = false
                    val recipeSales = result.data
                    if (recipeSales.isNullOrEmpty()) {
                        // Handle empty state for pie chart
                        binding.pieChartDishBreakdown.clear()
                        binding.pieChartDishBreakdown.invalidate()
                        binding.tvSalesSubtitle.text = "No dishes sold on this date"
                    } else {
                        updatePieChart(recipeSales)
                    }
                }
                is Resource.Error -> {
                    binding.progressBarPieChart.isVisible = false
                    Toast.makeText(requireContext(), result.message, Toast.LENGTH_SHORT).show()
                }
                is Resource.Loading -> {
                    binding.progressBarPieChart.isVisible = true
                }
            }
        }
    }
    
    private fun updatePieChart(data: List<RecipeSalesItem>) {
        val chartPalette = listOf(
            ContextCompat.getColor(requireContext(), R.color.chart_1),
            ContextCompat.getColor(requireContext(), R.color.chart_2),
            ContextCompat.getColor(requireContext(), R.color.chart_3),
            ContextCompat.getColor(requireContext(), R.color.chart_4),
            ContextCompat.getColor(requireContext(), R.color.chart_5),
            ContextCompat.getColor(requireContext(), R.color.chart_6),
            ContextCompat.getColor(requireContext(), R.color.chart_7),
            ContextCompat.getColor(requireContext(), R.color.chart_8),
            ContextCompat.getColor(requireContext(), R.color.chart_9),
            ContextCompat.getColor(requireContext(), R.color.chart_10)
        )

        val rawEntries = data.map { PieEntry(it.quantity.toFloat(), it.name) }

        // Calculate total dishes sold from real data
        val totalDishes = rawEntries.sumOf { it.value.toDouble() }.toInt()
        binding.tvSalesSubtitle.text = "Total dishes sold: $totalDishes"

        // Logic to group smaller slices into "Others"
        val finalEntries = if (rawEntries.size > 10) {
            val topNine = rawEntries.sortedByDescending { it.value }.take(9)
            val othersSum = rawEntries.sortedByDescending { it.value }.drop(9).sumOf { it.value.toDouble() }.toFloat()
            topNine + PieEntry(othersSum, "Others")
        } else {
            rawEntries
        }

        val dataSet = PieDataSet(finalEntries, "").apply {
            colors = chartPalette
            valueTextSize = 11f
            valueTextColor = Color.WHITE
            valueFormatter = LargeValueFormatter()
        }

        binding.pieChartDishBreakdown.data = PieData(dataSet)
        binding.pieChartDishBreakdown.invalidate()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null // Avoid memory leaks
    }
}