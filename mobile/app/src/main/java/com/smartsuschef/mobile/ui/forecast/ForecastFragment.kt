package com.smartsuschef.mobile.ui.forecast

import android.os.Bundle
import android.view.View
import androidx.core.view.isVisible
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.recyclerview.widget.LinearLayoutManager
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.FragmentForecastBinding
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.showToast
import dagger.hilt.android.AndroidEntryPoint
import androidx.core.content.ContextCompat
import com.github.mikephil.charting.components.Legend
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.BarData
import com.github.mikephil.charting.data.BarDataSet
import com.github.mikephil.charting.data.BarEntry
import com.github.mikephil.charting.data.CombinedData
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.LineData
import com.github.mikephil.charting.data.LineDataSet
import com.github.mikephil.charting.formatter.IndexAxisValueFormatter
import com.smartsuschef.mobile.network.dto.ForecastDto

@AndroidEntryPoint
class ForecastFragment : Fragment(R.layout.fragment_forecast) {

    private var _binding: FragmentForecastBinding? = null
    private val binding get() = _binding!!
    private val viewModel: ForecastViewModel by viewModels()
    private lateinit var forecastSummaryAdapter: ForecastSummaryAdapter

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentForecastBinding.bind(view)

        setupRecyclerView()
        observeViewModel()
    }

    private fun setupRecyclerView() {
        forecastSummaryAdapter = ForecastSummaryAdapter(emptyList())
        binding.rvIngredientForecast.apply {
            adapter = forecastSummaryAdapter
            layoutManager = LinearLayoutManager(requireContext())
            setHasFixedSize(true)
        }
    }

    private fun observeViewModel() {
        // Observe Dish Forecasts for the Stacked Bar Chart
        viewModel.dishForecasts.observe(viewLifecycleOwner) { result ->
            if (result is Resource.Success) {
                binding.progressBar.isVisible = false
                result.data?.let { setupStackedBarChart(it) }
            } else if (result is Resource.Loading) {
                binding.progressBar.isVisible = true
            }
        }

        // Observe Ingredient Forecasts for the Bottom Table
        viewModel.ingredientForecast.observe(viewLifecycleOwner) { result ->
            if (result is Resource.Success) {
                forecastSummaryAdapter.updateData(result.data ?: emptyList())
            }
        }

        // Observe Summary Trend for the Prediction Summary Chart
        viewModel.summaryTrend.observe(viewLifecycleOwner) { result ->
            if (result is Resource.Success) {
                result.data?.let { setupSummaryChart(it) }
            }
        }
    }

    private fun setupSummaryChart(trendData: List<ForecastDto>) {
        val entries = trendData.mapIndexed { index, dto ->
            Entry(index.toFloat(), dto.quantity.toFloat())
        }

        val lineDataSet = LineDataSet(entries, "Weekly Trend").apply {
            color = ContextCompat.getColor(requireContext(), R.color.forecast_trend)
            setCircleColor(ContextCompat.getColor(requireContext(), R.color.forecast_trend))
            lineWidth = 2f
            mode = LineDataSet.Mode.CUBIC_BEZIER
            setDrawValues(false)
        }

        binding.summaryCombinedChart.apply {
            data = CombinedData().apply { setData(LineData(lineDataSet)) }
            description.isEnabled = false
            xAxis.position = XAxis.XAxisPosition.BOTTOM
            xAxis.valueFormatter = IndexAxisValueFormatter(trendData.map { it.date.takeLast(5) })
            invalidate()
        }
    }
    private fun setupStackedBarChart(forecastData: List<DailyDishForecast>) {
        val entries = mutableListOf<BarEntry>()
        val dishNames = mutableListOf<String>()

        forecastData.forEachIndexed { index, daily ->
            // Convert predicted sales into a float array for the stack
            val values = daily.dishes.map { it.predictedSales.toFloat() }.toFloatArray()
            entries.add(BarEntry(index.toFloat(), values))

            // Capture unique dish names for the legend
            daily.dishes.forEach { if (!dishNames.contains(it.name)) dishNames.add(it.name) }
        }

        val dataSet = BarDataSet(entries, "Predicted Prep Counts").apply {
            // Use your gold and trend colors for the stack segments
            colors = listOf(
                ContextCompat.getColor(requireContext(), R.color.forecast_gold),
                ContextCompat.getColor(requireContext(), R.color.forecast_trend),
                ContextCompat.getColor(requireContext(), R.color.primary)
            )
            setDrawValues(false)
        }

        binding.dishForecastStackedChart.apply {
            data = BarData(dataSet)
            description.isEnabled = false

            xAxis.apply {
                position = XAxis.XAxisPosition.BOTTOM
                granularity = 1f
                valueFormatter = IndexAxisValueFormatter(forecastData.map { it.date.takeLast(5) }) // "01-31"
                setDrawGridLines(false)
            }

            axisLeft.setDrawGridLines(true)
            axisRight.isEnabled = false
            legend.isEnabled = true
            legend.verticalAlignment = Legend.LegendVerticalAlignment.BOTTOM

            animateY(1000)
            invalidate()
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}