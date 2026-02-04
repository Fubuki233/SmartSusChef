package com.smartsuschef.mobile.ui.wastage

import android.graphics.Color
import android.os.Bundle
import android.view.View
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.navigation.fragment.navArgs
import androidx.recyclerview.widget.LinearLayoutManager
import com.github.mikephil.charting.data.PieData
import com.github.mikephil.charting.data.PieDataSet
import com.github.mikephil.charting.data.PieEntry
import com.github.mikephil.charting.formatter.PercentFormatter
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.FragmentWastageDetailBinding
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class WastageDetailFragment : Fragment(R.layout.fragment_wastage_detail) {
    private var _binding: FragmentWastageDetailBinding? = null
    private val binding get() = _binding!!
    private val viewModel: WastageViewModel by viewModels()
    private val args: WastageDetailFragmentArgs by navArgs()
    private lateinit var wastageAdapter: WastageAdapter

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentWastageDetailBinding.bind(view)

        setupUI()
        setupRecyclerView()
        args.itemBreakdown?.let {
            viewModel.setWastageBreakdown(it.toList())
        }
        observeData()
    }

    private fun setupUI() {
        binding.tvDetailTitle.text = "Wastage for ${args.date}"
    }

    private fun setupPieChart(data: List<WastageBreakdownItem>) {
        val breakdown = data.groupBy { it.type }
            .mapValues { entry -> entry.value.sumOf { it.carbonFootprint } }

        val entries = breakdown.map { (type, carbonFootprint) ->
            PieEntry(carbonFootprint.toFloat(), type)
        }

        val dataSet = PieDataSet(entries, "").apply {
            colors = listOf(
                ContextCompat.getColor(requireContext(), R.color.chart_1),
                ContextCompat.getColor(requireContext(), R.color.chart_2),
                ContextCompat.getColor(requireContext(), R.color.chart_3)
            )
            valueTextColor = Color.WHITE
            valueTextSize = 12f
        }

        val pieData = PieData(dataSet).apply {
            setValueFormatter(PercentFormatter(binding.pieChartWastageBreakdown))
        }

        binding.pieChartWastageBreakdown.apply {
            this.data = pieData
            description.isEnabled = false
            isDrawHoleEnabled = true
            holeRadius = 40f
            transparentCircleRadius = 45f
            setUsePercentValues(true)
            setEntryLabelColor(Color.WHITE)
            setEntryLabelTextSize(12f)
            animateY(1000)
            invalidate()
        }
    }

    private fun setupRecyclerView() {
        wastageAdapter = WastageAdapter(emptyList())
        binding.rvWastedItems.layoutManager = LinearLayoutManager(requireContext())
        binding.rvWastedItems.adapter = wastageAdapter
    }

    private fun observeData() {
        viewModel.wastageBreakdown.observe(viewLifecycleOwner) { result ->
            when (result) {
                is Resource.Success -> {
                    val wastageData = result.data ?: emptyList()
                    wastageAdapter.updateData(wastageData)
                    setupPieChart(wastageData)
                }
                // Handle loading and error states if needed
                else -> {}
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
