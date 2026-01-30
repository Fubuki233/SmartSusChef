package com.smartsuschef.mobile.ui.sales

import android.graphics.Color
import android.os.Bundle
import android.view.View
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import com.github.mikephil.charting.charts.LineChart
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.LineData
import com.github.mikephil.charting.data.LineDataSet
import com.github.mikephil.charting.formatter.IndexAxisValueFormatter
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.FragmentSalesOverviewBinding
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.showToast
import dagger.hilt.android.AndroidEntryPoint
import java.text.SimpleDateFormat
import java.util.Locale

@AndroidEntryPoint
class SalesOverviewFragment : Fragment(R.layout.fragment_sales_overview) {

    private var _binding: FragmentSalesOverviewBinding? = null
    private val binding get() = _binding!!
    private val viewModel: SalesViewModel by viewModels()
    private lateinit var lineChart: LineChart

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentSalesOverviewBinding.bind(view)

        setupChart()
        observeViewModel()
    }

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

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}