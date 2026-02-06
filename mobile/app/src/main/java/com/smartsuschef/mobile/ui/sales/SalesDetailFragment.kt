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
import androidx.core.content.ContextCompat
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
        setupPieChart()
        setupRecyclerView()
        // Pass the date to the ViewModel to fetch mock or API data
        viewModel.fetchIngredientsForDate(args.date)
        observeData()
    }

    // Set up the toolbar
    private fun setupUI() {
        (activity as? AppCompatActivity)?.supportActionBar?.apply {
            title = "Sales Details"
        }
        binding.tvDetailTitle.text = "Sales Breakdown: ${args.date}"
    }

    private fun setupPieChart() {
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

            // Mock Data for Distribution
            val rawEntries = listOf(
                PieEntry(10f, "Hainese Chicken Rice"),
                PieEntry(5f, "Laksa"),
                PieEntry(20f, "Beef Rendang"),
                PieEntry(5f, "Nasi Lemak"),
                PieEntry(30f, "Mala Xiang Guo"),
                PieEntry(12f, "Carrot Cake"),
                PieEntry(8f, "Char Kway Teow"),
                PieEntry(10f, "Fish Porridge")
            )

            // Calculate total dishes sold
            val totalDishes = rawEntries.sumOf { it.value.toDouble() }.toInt()
            binding.tvSalesSubtitle.text = "Total dishes sold: $totalDishes"

            val finalEntries = if (rawEntries.size > 10) {
                val topNine = rawEntries.take(9)
                val othersSum = rawEntries.drop(9).sumOf { it.value.toDouble() }.toFloat()
                topNine + PieEntry(othersSum, "Others")
            } else {
                rawEntries
            }

            val dataSet = PieDataSet(finalEntries, "").apply {
                colors = chartPalette // Use the full list, it will cycle if needed
                valueTextSize = 11f
                valueTextColor = Color.WHITE
                valueFormatter = LargeValueFormatter()
            }

            data = PieData(dataSet)
            invalidate()
        }
    }
    private fun setupRecyclerView() {
        ingredientAdapter = IngredientAdapter(emptyList())
        binding.rvIngredients.layoutManager = LinearLayoutManager(requireContext())
        binding.rvIngredients.adapter = ingredientAdapter
    }

    private fun observeData() {
        viewModel.ingredientBreakdown.observe(viewLifecycleOwner) { result ->
            if (result is Resource.Success) {
                ingredientAdapter.updateData(result.data ?: emptyList())
            }
        }
    }
}