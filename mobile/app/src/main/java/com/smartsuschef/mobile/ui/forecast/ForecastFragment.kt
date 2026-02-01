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

@AndroidEntryPoint
class ForecastFragment : Fragment(R.layout.fragment_forecast) {

    private var _binding: FragmentForecastBinding? = null
    private val binding get() = _binding!!
    private val viewModel: ForecastViewModel by viewModels()
    private lateinit var forecastAdapter: ForecastAdapter

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        _binding = FragmentForecastBinding.bind(view)

        setupRecyclerView()
        observeViewModel()
    }

    private fun setupRecyclerView() {
        forecastAdapter = ForecastAdapter()
        binding.rvDishForecast.apply {
            adapter = forecastAdapter
            layoutManager = LinearLayoutManager(requireContext())
            setHasFixedSize(true)
        }
    }

    private fun observeViewModel() {
        viewModel.dishForecasts.observe(viewLifecycleOwner) { result ->
            when(result) {
                is Resource.Success -> {
                    binding.progressBar.isVisible = false
                    val dailyForecasts = result.data ?: emptyList()
                    
                    // Flatten the list for the adapter
                    val adapterList = mutableListOf<ForecastListItem>()
                    dailyForecasts.forEach { day ->
                        adapterList.add(ForecastListItem.DateHeader(day.date))
                        day.dishes.forEach { dish ->
                            adapterList.add(ForecastListItem.DishItem(dish))
                        }
                    }
                    forecastAdapter.submitList(adapterList)
                }
                is Resource.Error -> {
                    binding.progressBar.isVisible = false
                    requireContext().showToast("Error: ${result.message}")
                }
                is Resource.Loading -> {
                    binding.progressBar.isVisible = true
                }
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}