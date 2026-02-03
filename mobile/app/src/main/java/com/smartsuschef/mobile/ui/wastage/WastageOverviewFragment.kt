package com.smartsuschef.mobile.ui.wastage

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
import com.smartsuschef.mobile.databinding.FragmentWastageOverviewBinding
import com.smartsuschef.mobile.util.*
import dagger.hilt.android.AndroidEntryPoint
import java.text.SimpleDateFormat
import java.util.Locale

@AndroidEntryPoint
class WastageOverviewFragment : Fragment(R.layout.fragment_wastage_overview) {

    private var _binding: FragmentWastageOverviewBinding? = null
    private val binding get() = _binding!!
    private val viewModel: WastageViewModel by viewModels()

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentWastageOverviewBinding.bind(view)

        setupCombinedChart()
        observeViewModel()
        observeFilter()
        binding.tvDateContext.setOnClickListener { v -> showFilterMenu(v) }
    }

    private fun setupCombinedChart() {
        binding.wastageCombinedChart.apply {
            description.isEnabled = false
            setDrawGridBackground(false)
            setExtraOffsets(10f, 10f, 10f, 20f)

            setDrawOrder(arrayOf(CombinedChart.DrawOrder.BAR, CombinedChart.DrawOrder.LINE))

            xAxis.apply {
                position = XAxis.XAxisPosition.BOTTOM
                setDrawGridLines(false)
                granularity = 1f
                isGranularityEnabled = true
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                labelRotationAngle = -45f
                textSize = 8f
                setAvoidFirstLastClipping(false)
            }

            axisLeft.apply {
                setDrawGridLines(true)
                gridColor = ContextCompat.getColor(requireContext(), R.color.border_grey)
                textColor = ContextCompat.getColor(requireContext(), R.color.muted_text)
                axisLineColor = Color.TRANSPARENT
                axisMinimum = 0f
                setSpaceBottom(15f)
            }
            axisRight.isEnabled = false
            legend.isEnabled = true

            setOnChartValueSelectedListener(object : OnChartValueSelectedListener {
                override fun onValueSelected(e: Entry?, h: Highlight?) {
                    val selectedDateIndex = e?.x?.toInt() ?: return
                    val fullDate = (viewModel.wastageTrend.value as? Resource.Success)?.data?.getOrNull(selectedDateIndex)?.date

                    if (fullDate != null) {
                        val action = WastageOverviewFragmentDirections.actionNavWastageToWastageDetailFragment(date = fullDate)
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
        viewModel.wastageTrend.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Loading -> binding.progressBar.visible()
                is Resource.Success -> {
                    binding.progressBar.gone()
                    val wastageData = result.data ?: emptyList()

                    if (wastageData.isNotEmpty()) {
                        updateChart(wastageData)
                    } else {
                        binding.wastageCombinedChart.clear()
                        requireContext().showToast("No wastage data available")
                    }
                }
                is Resource.Error -> {
                    binding.progressBar.gone()
                    binding.wastageCombinedChart.clear()
                    requireContext().showToast(result.message ?: "Error loading wastage data")
                }
            }
        }
    }

    private fun updateChart(wastageData: List<WastageTrendItem>) {
        val avgWastage = wastageData.map { it.totalQuantity }.average()
        binding.tvWastageSubtitle.text = "Total wastage · Average: %.2f kg per day".format(avgWastage)

        val barEntries = mutableListOf<BarEntry>()
        val lineEntries = mutableListOf<Entry>()
        val labels = mutableListOf<String>()

        val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
        val outputFormat = SimpleDateFormat("d MMM", Locale.getDefault())

        wastageData.forEachIndexed { index, item ->
            barEntries.add(BarEntry(index.toFloat(), item.totalQuantity.toFloat()))
            lineEntries.add(Entry(index.toFloat(), item.totalQuantity.toFloat()))

            val formattedLabel = try {
                inputFormat.parse(item.date)?.let { outputFormat.format(it) } ?: item.date
            } catch (e: Exception) {
                item.date
            }
            labels.add(formattedLabel)
        }

        binding.wastageCombinedChart.xAxis.apply {
            valueFormatter = IndexAxisValueFormatter(labels)
            axisMinimum = -0.5f
            axisMaximum = (wastageData.size - 1).toFloat() + 0.5f
            setLabelCount(wastageData.size, false)
        }

        val combinedData = CombinedData()

        val barDataSet = BarDataSet(barEntries, "Daily Wastage (kg)").apply {
            color = ContextCompat.getColor(requireContext(), R.color.destructive)
            setDrawValues(false)
        }

        val barData = BarData(barDataSet)
        barData.barWidth = if (wastageData.size == 1) 0.5f else 0.8f
        combinedData.setData(barData)

        if (wastageData.size > 1) {
            val lineDataSet = LineDataSet(lineEntries, "Trend").apply {
                color = ContextCompat.getColor(requireContext(), R.color.primary)
                setCircleColor(ContextCompat.getColor(requireContext(), R.color.primary))
                lineWidth = 2.5f
                circleRadius = 4f
                setDrawCircleHole(false)
                mode = LineDataSet.Mode.CUBIC_BEZIER
                setDrawValues(false)
            }
            combinedData.setData(LineData(lineDataSet))
        }

        binding.wastageCombinedChart.data = combinedData
        binding.wastageCombinedChart.notifyDataSetChanged()
        binding.wastageCombinedChart.invalidate()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }

    private fun showFilterMenu(view: View) {
        val popup = PopupMenu(requireContext(), view)
        popup.menuInflater.inflate(R.menu.date_filter_menu, popup.menu)

        popup.setOnMenuItemClickListener { item ->
            when (item.itemId) {
                R.id.filter_today -> {
                    viewModel.setFilter(WastageFilter.TODAY)
                    true
                }
                R.id.filter_7_days -> {
                    viewModel.setFilter(WastageFilter.LAST_7_DAYS)
                    true
                }
                else -> false
            }
        }
        popup.show()
    }

    private fun observeFilter() {
        viewModel.currentFilter.observe(viewLifecycleOwner) { filter ->
            binding.tvDateContext.text = if (filter == WastageFilter.TODAY) "Today" else "Last 7 Days"
        }
    }
}
