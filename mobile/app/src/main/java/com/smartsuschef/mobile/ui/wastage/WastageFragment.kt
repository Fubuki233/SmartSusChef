package com.smartsuschef.mobile.ui.wastage

import android.graphics.Color
import android.os.Bundle
import android.view.View
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.recyclerview.widget.LinearLayoutManager
import com.github.mikephil.charting.charts.LineChart
import com.github.mikephil.charting.charts.PieChart
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.LineData
import com.github.mikephil.charting.data.LineDataSet
import com.github.mikephil.charting.data.PieData
import com.github.mikephil.charting.data.PieDataSet
import com.github.mikephil.charting.data.PieEntry
import com.github.mikephil.charting.formatter.IndexAxisValueFormatter
import com.github.mikephil.charting.formatter.PercentFormatter
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.FragmentWastageBinding
import com.smartsuschef.mobile.util.Resource
import com.smartsuschef.mobile.util.showToast
import dagger.hilt.android.AndroidEntryPoint
import java.text.SimpleDateFormat
import java.util.Locale

@AndroidEntryPoint
class WastageFragment : Fragment(R.layout.fragment_wastage) {

    private var _binding: FragmentWastageBinding? = null
    private val binding get() = _binding!!
    private val viewModel: WastageViewModel by viewModels()
    
    private lateinit var lineChart: LineChart
    private lateinit var pieChart: PieChart
    private lateinit var topWastedAdapter: TopWastedAdapter

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentWastageBinding.bind(view)

        setupLineChart()
        setupPieChart()
        setupRecyclerView()
        observeViewModel()
    }

    private fun setupLineChart() {
        lineChart = LineChart(requireContext()).apply {
            description.isEnabled = false
            legend.isEnabled = false
            setDrawGridBackground(false)
            setExtraOffsets(10f, 10f, 10f, 20f)
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
        binding.wastageChartContainer.addView(lineChart)
    }

    private fun setupPieChart() {
        pieChart = PieChart(requireContext()).apply {
            description.isEnabled = false
            legend.isEnabled = false
            isDrawHoleEnabled = true
            holeRadius = 58f
            transparentCircleRadius = 61f
            setUsePercentValues(true)
            setEntryLabelColor(Color.BLACK)
            setEntryLabelTextSize(12f)
        }
        binding.pieChartContainer.addView(pieChart)
    }

    private fun setupRecyclerView() {
        topWastedAdapter = TopWastedAdapter()
        binding.rvTopWasted.apply {
            adapter = topWastedAdapter
            layoutManager = LinearLayoutManager(requireContext())
            isNestedScrollingEnabled = false
        }
    }

    private fun observeViewModel() {
        viewModel.wastageTrend.observe(viewLifecycleOwner) { result ->
            if (result is Resource.Success) {
                val trendData = result.data ?: emptyList()
                updateLineChart(trendData)
            } else if (result is Resource.Error) {
                requireContext().showToast("Error loading wastage trend: ${result.message}")
            }
        }

        viewModel.impactDistribution.observe(viewLifecycleOwner) { impactData ->
            updatePieChart(impactData)
            topWastedAdapter.submitList(impactData)
        }
    }

    private fun updateLineChart(data: List<WastageTrendData>) {
        if (data.isEmpty()) return

        val entries = data.mapIndexed { index, item -> Entry(index.toFloat(), item.weightKg.toFloat()) }
        val labels = data.map {
            val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
            val outputFormat = SimpleDateFormat("d MMM", Locale.getDefault())
            inputFormat.parse(it.date)?.let { date -> outputFormat.format(date) } ?: ""
        }
        lineChart.xAxis.valueFormatter = IndexAxisValueFormatter(labels)

        val dataSet = LineDataSet(entries, "Wastage Trend (kg)").apply {
            color = ContextCompat.getColor(requireContext(), R.color.wastage_primary)
            setCircleColor(ContextCompat.getColor(requireContext(), R.color.wastage_primary))
            // ... other styling
        }
        lineChart.data = LineData(dataSet)
        lineChart.invalidate()
    }

    private fun updatePieChart(data: List<ImpactData>) {
        if (data.isEmpty()) return

        val entries = data.map { PieEntry(it.carbonValue.toFloat(), it.category) }
        val dataSet = PieDataSet(entries, "Wastage Impact").apply {
            colors = listOf(
                ContextCompat.getColor(requireContext(), R.color.destructive),
                ContextCompat.getColor(requireContext(), R.color.forecast_gold),
                ContextCompat.getColor(requireContext(), R.color.muted_text),
                ContextCompat.getColor(requireContext(), R.color.carbon_green)
            )
            valueTextColor = Color.BLACK
            valueTextSize = 12f
        }
        val pieData = PieData(dataSet).apply {
            setValueFormatter(PercentFormatter(pieChart))
        }
        pieChart.data = pieData
        pieChart.invalidate()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}