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
            title = "Sales for ${args.date}"
        }
        binding.tvDetailTitle.text = "Sales Breakdown: ${args.date}"
    }

    private fun setupPieChart() {
        binding.pieChartDishBreakdown.apply {
            description.isEnabled = false
            isDrawHoleEnabled = true
            setHoleColor(Color.TRANSPARENT)
            setEntryLabelColor(Color.BLACK)
            legend.isEnabled = true
            animateY(1000)

            val chartPalette = listOf(
                ContextCompat.getColor(requireContext(), R.color.chart_1_sage),
                ContextCompat.getColor(requireContext(), R.color.chart_2_orange),
                ContextCompat.getColor(requireContext(), R.color.chart_3_azure),
                ContextCompat.getColor(requireContext(), R.color.chart_4_olive),
                ContextCompat.getColor(requireContext(), R.color.chart_5_gold),
                ContextCompat.getColor(requireContext(), R.color.chart_6_teal),
                ContextCompat.getColor(requireContext(), R.color.chart_7_darkorange),
                ContextCompat.getColor(requireContext(), R.color.chart_8_lightorange),
                ContextCompat.getColor(requireContext(), R.color.chart_9_lavender),
                ContextCompat.getColor(requireContext(), R.color.chart_10_grey)
                )

            // Mock Data for Distribution
            val entries = listOf(
                PieEntry(10f, "Hainese Chicken Rice"),
                PieEntry(5f, "Laksa"),
                PieEntry(20f, "Beef Rendang"),
                PieEntry(5f, "Nasi Lemak"),
                PieEntry(30f, "Mala Xiang Guo"),
                PieEntry(12f, "Carrot Cake"),
                PieEntry(18f, "Char Kway Teow"),
                )

            val dataSet = PieDataSet(entries, "").apply {
                colors = chartPalette // Use the full list, it will cycle if needed
                valueTextSize = 11f
                valueTextColor = Color.WHITE
                valueFormatter = LargeValueFormatter()
            }

            // Use the Legend instead of labels on the chart for a cleaner look
            legend.isEnabled = true
            setDrawEntryLabels(false)

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