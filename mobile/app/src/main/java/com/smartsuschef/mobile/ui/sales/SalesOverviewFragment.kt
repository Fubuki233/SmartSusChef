package com.smartsuschef.mobile.ui.sales

import android.graphics.Color
import android.os.Bundle
import android.view.View
import androidx.appcompat.widget.PopupMenu
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.navigation.fragment.findNavController
import androidx.fragment.app.viewModels
import com.github.mikephil.charting.charts.*
import com.github.mikephil.charting.components.*
import com.github.mikephil.charting.data.*
import com.github.mikephil.charting.formatter.*
import com.github.mikephil.charting.highlight.*
import com.github.mikephil.charting.listener.*
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.FragmentSalesOverviewBinding
import com.smartsuschef.mobile.util.*
import com.smartsuschef.mobile.ui.sales.SalesFilter
import dagger.hilt.android.AndroidEntryPoint
import java.text.SimpleDateFormat
import java.util.Locale

@AndroidEntryPoint
class SalesOverviewFragment : Fragment(R.layout.fragment_sales_overview) {

    private var _binding: FragmentSalesOverviewBinding? = null
    private val binding get() = _binding!!
    private val viewModel: SalesViewModel by viewModels()

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentSalesOverviewBinding.bind(view)

        setupCombinedChart()
        observeViewModel()
        observeFilter()
        binding.tvDateContext.setOnClickListener { v -> showFilterMenu(v) }
        setWeatherIcon("cloudy")
    }

    private fun setupCombinedChart() {
        binding.salesCombinedChart.apply {
            description.isEnabled = false
            setDrawGridBackground(false)
            setExtraOffsets(10f, 10f, 10f, 20f)

            // Ensures the line is drawn on top of the bars
            setDrawOrder(arrayOf(CombinedChart.DrawOrder.BAR, CombinedChart.DrawOrder.LINE))

            xAxis.apply {
                position = XAxis.XAxisPosition.BOTTOM
                setDrawGridLines(false)
                granularity = 1f // Ensures only whole numbers are displayed for entries
                isGranularityEnabled = true
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                axisMinimum = -0.5f
                axisMaximum = 6.5f
                setLabelCount(7, true) // Show exactly 7 labels for 7 days
                setAvoidFirstLastClipping(true) // Prevent first/last labels from being cut off

                labelRotationAngle = -45f
            }

            axisLeft.apply {
                setDrawGridLines(true)
                gridColor = ContextCompat.getColor(requireContext(), R.color.border_grey)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                // Ensure min value starts at 0, max is dynamic based on data
                axisMinimum = 0f
                setSpaceBottom(15f)
            }
            axisRight.isEnabled = false // Disable right y-axis
            legend.isEnabled = true

            setOnChartValueSelectedListener(object : OnChartValueSelectedListener {
                override fun onValueSelected(e: Entry?, h: Highlight?) {
                    // Get the actual date label from the X-axis (e.g. "31 Jan")
                    val selectedDateIndex = e?.x?.toInt() ?: return

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
                is Resource.Loading -> binding.progressBar.visible()
                is Resource.Success -> {
                    binding.progressBar.gone()
                    val salesData = result.data ?: emptyList()
                    if (salesData.isNotEmpty()) {
                        val avgSales = salesData.map { it.sales }.average().toInt()
                        binding.tvSalesSubtitle.text = "Total dishes sold · Average: $avgSales per day"

                        val barEntries = mutableListOf<BarEntry>()
                        val lineEntries = mutableListOf<Entry>()
                        val labels = mutableListOf<String>()

                        val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
                        val outputFormat = SimpleDateFormat("d MMM", Locale.getDefault())

                        salesData.forEachIndexed { index, item ->
                            barEntries.add(BarEntry(index.toFloat(), item.sales.toFloat()))
                            lineEntries.add(Entry(index.toFloat(), item.sales.toFloat()))
                            labels.add(inputFormat.parse(item.date)?.let { outputFormat.format(it) } ?: "")
                        }

                        val combinedData = CombinedData()

                        val barDataSet = BarDataSet(barEntries, "Daily Sales").apply {
                            color = ContextCompat.getColor(requireContext(), R.color.primary)
                            setDrawValues(false)
                        }
                        combinedData.setData(BarData(barDataSet))

                        val lineDataSet = LineDataSet(lineEntries, "Trend").apply {
                            color = ContextCompat.getColor(requireContext(), R.color.destructive)
                            setCircleColor(Color.RED)
                            lineWidth = 2.5f
                            circleRadius = 3f
                            setDrawCircleHole(false)
                            mode = LineDataSet.Mode.CUBIC_BEZIER
                            setDrawValues(false)
                        }
                        combinedData.setData(LineData(lineDataSet))

                        binding.salesCombinedChart.xAxis.valueFormatter = IndexAxisValueFormatter(labels)
                        binding.salesCombinedChart.data = combinedData
                        binding.salesCombinedChart.notifyDataSetChanged()
                        binding.salesCombinedChart.invalidate()
                    }
                }
                is Resource.Error -> {
                    binding.progressBar.gone()
                    requireContext().showToast(result.message ?: "Error")
                }
            }
        }
    }

        private fun showFilterMenu(view: View) {
            val popup = PopupMenu(requireContext(), view)
            popup.menuInflater.inflate(R.menu.sales_filter_menu, popup.menu)

            popup.setOnMenuItemClickListener { item ->
                when (item.itemId) {
                    R.id.filter_today -> {
                        viewModel.setFilter(SalesFilter.TODAY)
                        true
                    }
                    R.id.filter_7_days -> {
                        viewModel.setFilter(SalesFilter.LAST_7_DAYS)
                        true
                    }
                    else -> false
                }
            }
            popup.show()
        }

        private fun observeFilter() {
            viewModel.currentFilter.observe(viewLifecycleOwner) { filter ->
                binding.tvDateContext.text = if (filter == SalesFilter.TODAY) "Today" else "Last 7 Days"
            }
        }

    /*
    private fun observeViewModel() {
        viewModel.salesTrend.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Loading -> binding.progressBar.visible()
                is Resource.Success -> {
                    binding.progressBar.gone()
                    val salesData = result.data ?: emptyList()
                    if (salesData.isNotEmpty()) {

                        // 1. Calculate Average for Subtitle
                        val avgSales = salesData.map { it.sales }.average().toInt()
                        binding.tvSalesSubtitle.text = "Total dishes sold · Average: $avgSales per day"

                        val barEntries = mutableListOf<BarEntry>()
                        val lineEntries = mutableListOf<Entry>()
                        val labels = mutableListOf<String>()

                        val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
                        val outputFormat = SimpleDateFormat("d MMM", Locale.getDefault())

                        salesData.forEachIndexed { index, item ->
                            barEntries.add(BarEntry(index.toFloat(), item.sales.toFloat()))
                            lineEntries.add(Entry(index.toFloat(), item.sales.toFloat()))
                            labels.add(inputFormat.parse(item.date)?.let { outputFormat.format(it) } ?: "")
                        }

                        // 2. Build Combined Data
                        val combinedData = CombinedData()

                        // Add Bars
                        val barDataSet = BarDataSet(barEntries, "Daily Sales").apply {
                            color = ContextCompat.getColor(requireContext(), R.color.primary)
                            valueTextColor = ContextCompat.getColor(requireContext(), R.color.foreground)
                            setDrawValues(true) // toggle to view or hide values above bar
                        }
                        combinedData.setData(BarData(barDataSet))

                        // Add Trend Line
                        val lineDataSet = LineDataSet(lineEntries, "Trend").apply {
                            color = ContextCompat.getColor(requireContext(), R.color.destructive)
                            setCircleColor(Color.RED)
                            lineWidth = 2.5f
                            circleRadius = 3f
                            setDrawCircleHole(false)
                            mode = LineDataSet.Mode.CUBIC_BEZIER // Smooth curves
                            setDrawValues(false)
                        }
                        combinedData.setData(LineData(lineDataSet))

                        binding.salesCombinedChart.xAxis.valueFormatter = IndexAxisValueFormatter(labels)
                        binding.salesCombinedChart.data = combinedData
                        binding.salesCombinedChart.invalidate()
                    }
                }
                is Resource.Error -> {
                    binding.progressBar.gone()
                    requireContext().showToast(result.message ?: "Error")
                }
            }
        }
    }
     */

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

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}