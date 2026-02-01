package com.smartsuschef.mobile.ui.sales

import android.graphics.Color
import android.os.Bundle
import android.view.View
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.navigation.fragment.findNavController
import androidx.fragment.app.viewModels

// import com.github.mikephil.charting.charts.LineChart
// import com.github.mikephil.charting.components.XAxis
// import com.github.mikephil.charting.data.Entry
// import com.github.mikephil.charting.data.LineData
// import com.github.mikephil.charting.data.LineDataSet
// import com.github.mikephil.charting.formatter.IndexAxisValueFormatter

import com.github.mikephil.charting.data.BarData
import com.github.mikephil.charting.data.BarDataSet
import com.github.mikephil.charting.data.BarEntry
import com.github.mikephil.charting.formatter.IndexAxisValueFormatter
import com.github.mikephil.charting.highlight.Highlight
import com.github.mikephil.charting.listener.OnChartValueSelectedListener
import com.github.mikephil.charting.data.Entry

import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.FragmentSalesOverviewBinding
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.showToast
import com.smartsuschef.mobile.util.visible
import com.smartsuschef.mobile.util.gone
import dagger.hilt.android.AndroidEntryPoint
import java.text.SimpleDateFormat
import java.util.Locale

@AndroidEntryPoint
class SalesOverviewFragment : Fragment(R.layout.fragment_sales_overview) {

    private var _binding: FragmentSalesOverviewBinding? = null
    private val binding get() = _binding!!
    private val viewModel: SalesViewModel by viewModels()
    // private lateinit var lineChart: LineChart

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentSalesOverviewBinding.bind(view)

        setupBarChart()
        observeViewModel()
        setWeatherIcon("cloudy")
    }

    private fun setupBarChart() {
        binding.salesBarChart.apply {
            description.isEnabled = false
            setDrawGridBackground(false)
            setExtraOffsets(10f, 10f, 10f, 20f) // Padding

            xAxis.apply {
                position = com.github.mikephil.charting.components.XAxis.XAxisPosition.BOTTOM
                setDrawGridLines(false)
                granularity = 1f // Ensures only whole numbers are displayed for entries
                isGranularityEnabled = true
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                setLabelCount(7, true) // Show exactly 7 labels for 7 days
                setAvoidFirstLastClipping(true) // Prevent first/last labels from being cut off
            }

            axisLeft.apply {
                setDrawGridLines(true)
                gridColor = ContextCompat.getColor(requireContext(), R.color.border_grey)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                // Ensure min value starts at 0, max is dynamic based on data
                axisMinimum = 0f
            }
            // Add padding to the left axis so numbers don't touch the edge
            axisLeft.setSpaceBottom(15f)

            // Ensure bars are centered over the labels
            setFitBars(true)

            axisRight.isEnabled = false // Disable right y-axis

            setOnChartValueSelectedListener(object : OnChartValueSelectedListener {
                override fun onValueSelected(e: Entry?, h: Highlight?) {
                    // Get the actual date label from the X-axis (e.g., "Jan 31")
                    val selectedDateIndex = e?.x?.toInt() ?: return
                    val dateLabel = xAxis.valueFormatter.getFormattedValue(selectedDateIndex.toFloat(), xAxis)

                    // Find the original full date string from the ViewModel's salesTrend
                    val fullDate = (viewModel.salesTrend.value as? Resource.Success)?.data?.getOrNull(selectedDateIndex)?.date

                    if (fullDate != null) {
                        val action = SalesOverviewFragmentDirections
                            .actionNavSalesToSalesDetailFragment(date = fullDate)
                        findNavController().navigate(action)
                    } else {
                        requireContext().showToast("Could not retrieve full date for selected entry.")
                    }
                }
                override fun onNothingSelected() {}
            })
        }
    }

    private fun observeViewModel() {
        viewModel.salesTrend.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Loading -> {
                    binding.progressBar.visible()
                }
                is Resource.Success -> {
                    binding.progressBar.gone()
                    val salesData = result.data ?: emptyList()
                    if (salesData.isNotEmpty()) {
                        val entries = salesData.mapIndexed { index, item ->
                            BarEntry(index.toFloat(), item.sales.toFloat())
                        }

                        val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
                        val outputFormat = SimpleDateFormat("d MMM", Locale.getDefault())
                        val labels = salesData.map {
                            inputFormat.parse(it.date)?.let { date -> outputFormat.format(date) } ?: ""
                        }
                        binding.salesBarChart.xAxis.valueFormatter = IndexAxisValueFormatter(labels)

                        val dataSet = BarDataSet(entries, "Sales Trend").apply {
                            color = ContextCompat.getColor(requireContext(), R.color.primary)
                            valueTextColor = ContextCompat.getColor(requireContext(), R.color.foreground)
                            valueTextSize = 10f
                            setDrawValues(true)
                        }

                        binding.salesBarChart.data = BarData(dataSet)
                        binding.salesBarChart.invalidate() // Refresh chart
                    } else {
                        // Handle empty data case, e.g., show a message
                        requireContext().showToast("No sales data available for the last 7 days.")
                    }
                }
                is Resource.Error -> {
                    binding.progressBar.gone()
                    requireContext().showToast(result.message ?: "Failed to load sales trend.")
                }
            }
        }
    }

    private fun setWeatherIcon(condition: String) {
        val drawableRes = when (condition.lowercase()) {
            "cloudy", "partly cloudy" -> R.drawable.cloud
            "rain", "showers" -> R.drawable.cloud_rain
            "storm" -> R.drawable.cloud_hail
            "sunny" -> R.drawable.sun
            else -> R.drawable.cloud // Default
        }
        binding.ivWeatherIcon.setImageResource(drawableRes)
    }
    /*
    private fun setupChart() {
        lineChart = LineChart(requireContext())
        binding.chartContainer.addView(lineChart)

        // Basic chart styling
        lineChart.apply {
            description.isEnabled = false
            legend.isEnabled = false
            setDrawGridBackground(false)
            setExtraOffsets(10f, 10f, 10f, 20f) // Padding

            xAxis.apply {
                position = XAxis.XAxisPosition.BOTTOM
                setDrawGridLines(false)
                granularity = 1f
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
            }

            axisLeft.apply {
                setDrawGridLines(true)
                gridColor = ContextCompat.getColor(requireContext(), R.color.border_grey)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
            }

            axisRight.isEnabled = false
        }
    }

    private fun observeViewModel() {
        viewModel.salesTrend.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Success -> {
                    val salesData = result.data ?: emptyList()
                    if (salesData.isNotEmpty()) {
                        val entries = salesData.mapIndexed { index, item ->
                            Entry(index.toFloat(), item.sales.toFloat())
                        }
                        
                        val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
                        val outputFormat = SimpleDateFormat("d MMM", Locale.getDefault())
                        val labels = salesData.map {
                            inputFormat.parse(it.date)?.let { date -> outputFormat.format(date) } ?: ""
                        }
                        lineChart.xAxis.valueFormatter = IndexAxisValueFormatter(labels)

                        val dataSet = LineDataSet(entries, "Sales Trend").apply {
                            color = ContextCompat.getColor(requireContext(), R.color.sales_primary)
                            valueTextColor = ContextCompat.getColor(requireContext(), R.color.foreground)
                            setCircleColor(ContextCompat.getColor(requireContext(), R.color.sales_primary))
                            circleRadius = 4f
                            lineWidth = 2.5f
                            valueTextSize = 10f
                            setDrawValues(true)
                        }

                        lineChart.data = LineData(dataSet)
                        lineChart.invalidate() // Refresh chart
                    }
                }
                is Resource.Error -> {
                    requireContext().showToast("Error loading sales trend: ${result.message}")
                }
                is Resource.Loading -> {
                    // You could show a ProgressBar here
                }
            }
        }
    }
     */

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}