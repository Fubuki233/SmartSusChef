package com.smartsuschef.mobile.ui.forecast

// Android
import android.graphics.Color
import android.os.Bundle
import android.view.View

// AndroidX
import androidx.core.content.ContextCompat
import androidx.core.view.isVisible
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.recyclerview.widget.LinearLayoutManager

// MPAndroidChart
import com.github.mikephil.charting.components.Legend
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.*
import com.github.mikephil.charting.formatter.IndexAxisValueFormatter

// Hilt
import dagger.hilt.android.AndroidEntryPoint

// App-specific
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.FragmentForecastBinding
import com.smartsuschef.mobile.network.dto.ForecastDto
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.showToast

// Kotlin utils
import java.text.SimpleDateFormat
import java.util.*

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
        forecastSummaryAdapter = ForecastSummaryAdapter(emptyList(), emptyList())
        binding.rvIngredientForecast.apply {
            adapter = forecastSummaryAdapter
            layoutManager = LinearLayoutManager(requireContext())
            setHasFixedSize(true)
        }
    }

    private fun observeViewModel() {
        // Summary Trend Chart
        viewModel.summaryTrend.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Success -> {
                    binding.progressBar.isVisible = false
                    result.data?.let { setupSummaryChart(it) }
                }
                is Resource.Loading -> binding.progressBar.isVisible = true
                is Resource.Error -> {
                    binding.progressBar.isVisible = false
                    requireContext().showToast(result.message ?: "Error loading forecast")
                }
            }
        }

        // Dish Breakdown Chart
        viewModel.dishForecasts.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Success -> result.data?.let { setupStackedBarChart(it) }
                is Resource.Error -> requireContext().showToast(result.message ?: "Error")
                else -> {}
            }
        }

        // Ingredient Table
        viewModel.ingredientForecast.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Success -> {
                    result.data?.let { ingredientData ->
                        val dates = viewModel.dateHeaders.value ?: emptyList()
                        forecastSummaryAdapter.updateData(ingredientData, dates)
                    }
                }
                is Resource.Error -> requireContext().showToast(result.message ?: "Error")
                else -> {}
            }
        }

        // Comparison Chart
        viewModel.comparisonData.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Success -> result.data?.let { setupComparisonChart(it) }
                is Resource.Error -> requireContext().showToast(result.message ?: "Error")
                else -> {}
            }
        }
    }

    /**
     * CHART 1: Prediction Summary (Combined Bar + Line)
     */
    private fun setupSummaryChart(trendData: List<ForecastDto>) {
        val barEntries = mutableListOf<BarEntry>()
        val lineEntries = mutableListOf<Entry>()
        val labels = mutableListOf<String>()
        var totalDishes = 0

        trendData.forEachIndexed { index, dto ->
            barEntries.add(BarEntry(index.toFloat(), dto.quantity.toFloat()))
            lineEntries.add(Entry(index.toFloat(), dto.quantity.toFloat()))
            labels.add(formatDate(dto.date))
            totalDishes += dto.quantity
        }

        binding.tvTotalWeeklyDishes.text = totalDishes.toString()

        val barSet = BarDataSet(barEntries, "Predicted Sales").apply {
            color = ContextCompat.getColor(requireContext(), R.color.forecast_gold)
            setDrawValues(false)
        }

        val lineSet = LineDataSet(lineEntries, "Trend").apply {
            color = ContextCompat.getColor(requireContext(), R.color.forecast_trend)
            lineWidth = 2.5f
            circleRadius = 4f
            setCircleColor(ContextCompat.getColor(requireContext(), R.color.forecast_trend))
            mode = LineDataSet.Mode.CUBIC_BEZIER
            setDrawValues(false)
            setDrawCircleHole(false)
        }

        binding.summaryCombinedChart.apply {
            data = CombinedData().apply {
                setData(BarData(barSet))
                setData(LineData(lineSet))
            }
            description.isEnabled = false
            setDrawGridBackground(false)
            setExtraOffsets(10f, 10f, 10f, 20f)

            // Add marker for interactivity
            val markerView = ForecastMarkerView(requireContext(), R.layout.marker_view)
            markerView.chartView = this
            markerView.chartType = ForecastMarkerView.ChartType.SIMPLE
            markerView.labels = labels // Pass date labels
            marker = markerView

            // Disable highlight color change
            isHighlightPerTapEnabled = true
            isHighlightPerDragEnabled = false

            xAxis.apply {
                position = XAxis.XAxisPosition.BOTTOM
                valueFormatter = IndexAxisValueFormatter(labels)
                labelRotationAngle = -45f
                textSize = 8f
                granularity = 1f
                setDrawGridLines(false)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                setAvoidFirstLastClipping(false)
                axisMinimum = -0.5f
                axisMaximum = (labels.size - 1).toFloat() + 0.5f
                setLabelCount(labels.size, false)
            }

            axisLeft.apply {
                setDrawGridLines(true)
                gridColor = ContextCompat.getColor(requireContext(), R.color.border_grey)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                axisMinimum = 0f
            }

            axisRight.isEnabled = false

            legend.apply {
                verticalAlignment = Legend.LegendVerticalAlignment.BOTTOM
                horizontalAlignment = Legend.LegendHorizontalAlignment.CENTER
                orientation = Legend.LegendOrientation.HORIZONTAL
                setDrawInside(false)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
            }

            invalidate()
        }
    }

    /**
     * CHART 2: Dishes Forecast (Stacked Bar Chart)
     */
    private fun setupStackedBarChart(forecastData: List<DailyDishForecast>) {
        val entries = mutableListOf<BarEntry>()
        val labels = mutableListOf<String>()
        val totalDishes = forecastData.sumOf { it.dishes.sumOf { d -> d.predictedSales } }
        val avg = if (forecastData.isNotEmpty()) totalDishes / forecastData.size else 0

        binding.tvDishesSubtitle.text = "AI-predicted main dishes breakdown for next 7 days · Avg: $avg dishes/day"

        forecastData.forEachIndexed { index, daily ->
            val values = daily.dishes.map { it.predictedSales.toFloat() }.toFloatArray()
            entries.add(BarEntry(index.toFloat(), values))
            labels.add(formatDate(daily.date))
        }

        val dishNames = if (forecastData.isNotEmpty()) {
            forecastData[0].dishes.map { it.name }.toTypedArray()
        } else {
            arrayOf()
        }

        val dataSet = BarDataSet(entries, "").apply {
            colors = listOf(
                ContextCompat.getColor(requireContext(), R.color.primary),
                ContextCompat.getColor(requireContext(), R.color.wastage_primary),
                ContextCompat.getColor(requireContext(), R.color.forecast_gold)
            )
            stackLabels = dishNames
            setDrawValues(false)
            highLightAlpha = 0 // Disable highlight color change
        }

        binding.dishForecastStackedChart.apply {
            data = BarData(dataSet).apply { barWidth = 0.8f }
            description.isEnabled = false
            setDrawGridBackground(false)
            setExtraOffsets(5f, 10f, 10f, 20f) // Reduced left offset from 10f to 5f

            // Add marker
            val markerView = ForecastMarkerView(requireContext(), R.layout.marker_view)
            markerView.chartView = this
            markerView.chartType = ForecastMarkerView.ChartType.STACKED
            markerView.dishNames = dishNames // Pass dish names for breakdown
            markerView.labels = labels // Pass date labels
            marker = markerView

            // Enable tap but disable drag
            isHighlightPerTapEnabled = true
            isHighlightPerDragEnabled = false

            xAxis.apply {
                position = XAxis.XAxisPosition.BOTTOM
                valueFormatter = IndexAxisValueFormatter(labels)
                labelRotationAngle = -45f
                textSize = 8f
                granularity = 1f
                setDrawGridLines(false)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                setAvoidFirstLastClipping(false)
                axisMinimum = -0.5f
                axisMaximum = (labels.size - 1).toFloat() + 0.5f
                setLabelCount(labels.size, false)
            }

            axisLeft.apply {
                setDrawGridLines(true)
                gridColor = ContextCompat.getColor(requireContext(), R.color.border_grey)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                axisMinimum = 0f
                textSize = 8f // Reduce Y-axis label size
            }

            axisRight.isEnabled = false

            legend.apply {
                verticalAlignment = Legend.LegendVerticalAlignment.BOTTOM
                horizontalAlignment = Legend.LegendHorizontalAlignment.CENTER
                orientation = Legend.LegendOrientation.HORIZONTAL
                setDrawInside(false)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
            }

            invalidate()
        }
    }

    /**
     * CHART 4: Comparison (Grouped Bar - Predicted vs Actual)
     */
    private fun setupComparisonChart(comparisonData: List<ForecastDto>) {
        val predictedEntries = mutableListOf<BarEntry>()
        val actualEntries = mutableListOf<BarEntry>()
        val labels = mutableListOf<String>()

        comparisonData.forEachIndexed { index, dto ->
            // Attach comparison data to BOTH predicted and actual entries
            val compData = ForecastMarkerView.ComparisonData(dto.quantity, dto.actualQuantity)

            val predictedEntry = BarEntry(index.toFloat(), dto.quantity.toFloat())
            predictedEntry.data = compData
            predictedEntries.add(predictedEntry)

            val actualEntry = BarEntry(index.toFloat(), dto.actualQuantity.toFloat())
            actualEntry.data = compData
            actualEntries.add(actualEntry)

            labels.add(formatDate(dto.date))
        }

        val predictedSet = BarDataSet(predictedEntries, "Predicted").apply {
            color = Color.LTGRAY
            setDrawValues(false)
            highLightAlpha = 0 // Disable highlight
        }

        val actualSet = BarDataSet(actualEntries, "Actual").apply {
            color = ContextCompat.getColor(requireContext(), R.color.primary)
            setDrawValues(false)
            highLightAlpha = 0 // Disable highlight
        }

        val barWidth = 0.35f
        val groupSpace = 0.3f
        val barSpace = 0.05f

        binding.comparisonBarChart.apply {
            data = BarData(predictedSet, actualSet).apply {
                this.barWidth = barWidth
            }
            description.isEnabled = false
            setDrawGridBackground(false)
            setExtraOffsets(10f, 10f, 10f, 20f)

            // Add marker
            val markerView = ForecastMarkerView(requireContext(), R.layout.marker_view)
            markerView.chartView = this
            markerView.chartType = ForecastMarkerView.ChartType.COMPARISON
            markerView.labels = labels // Pass date labels
            marker = markerView

            // Enable tap
            isHighlightPerTapEnabled = true
            isHighlightPerDragEnabled = false

            xAxis.apply {
                position = XAxis.XAxisPosition.BOTTOM
                valueFormatter = IndexAxisValueFormatter(labels)
                labelRotationAngle = -45f
                textSize = 8f
                granularity = 1f
                setCenterAxisLabels(true)
                setDrawGridLines(false)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                axisMinimum = 0f
                axisMaximum = labels.size.toFloat()
            }

            axisLeft.apply {
                setDrawGridLines(true)
                gridColor = ContextCompat.getColor(requireContext(), R.color.border_grey)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                axisMinimum = 0f
            }

            axisRight.isEnabled = false

            legend.apply {
                verticalAlignment = Legend.LegendVerticalAlignment.BOTTOM
                horizontalAlignment = Legend.LegendHorizontalAlignment.CENTER
                orientation = Legend.LegendOrientation.HORIZONTAL
                setDrawInside(false)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
            }

            groupBars(0f, groupSpace, barSpace)
            invalidate()
        }
    }

    private fun formatDate(dateStr: String): String {
        return try {
            val parser = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
            val formatter = SimpleDateFormat("d MMM", Locale.getDefault())
            parser.parse(dateStr)?.let { formatter.format(it) } ?: dateStr
        } catch (e: Exception) {
            dateStr
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}