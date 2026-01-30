package com.smartsuschef.mobile.ui.forecast

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.smartsuschef.mobile.databinding.ItemDateHeaderBinding
import com.smartsuschef.mobile.databinding.ItemDishForecastBinding
import java.text.SimpleDateFormat
import java.util.Locale

// Sealed class to represent the items in our RecyclerView
sealed class ForecastListItem {
    data class DateHeader(val date: String) : ForecastListItem()
    data class DishItem(val dishForecast: DishForecast) : ForecastListItem()
}

class ForecastAdapter : ListAdapter<ForecastListItem, RecyclerView.ViewHolder>(DiffCallback) {

    override fun getItemViewType(position: Int): Int {
        return when (getItem(position)) {
            is ForecastListItem.DateHeader -> VIEW_TYPE_HEADER
            is ForecastListItem.DishItem -> VIEW_TYPE_DISH
        }
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): RecyclerView.ViewHolder {
        val inflater = LayoutInflater.from(parent.context)
        return when (viewType) {
            VIEW_TYPE_HEADER -> {
                val binding = ItemDateHeaderBinding.inflate(inflater, parent, false)
                DateViewHolder(binding)
            }
            else -> {
                val binding = ItemDishForecastBinding.inflate(inflater, parent, false)
                DishViewHolder(binding)
            }
        }
    }

    override fun onBindViewHolder(holder: RecyclerView.ViewHolder, position: Int) {
        when (val item = getItem(position)) {
            is ForecastListItem.DateHeader -> (holder as DateViewHolder).bind(item)
            is ForecastListItem.DishItem -> (holder as DishViewHolder).bind(item)
        }
    }

    class DateViewHolder(private val binding: ItemDateHeaderBinding) : RecyclerView.ViewHolder(binding.root) {
        fun bind(header: ForecastListItem.DateHeader) {
            // Reformat date for display if needed, e.g., "January 30, 2026"
            val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
            val outputFormat = SimpleDateFormat("MMMM d, yyyy", Locale.getDefault())
            val date = inputFormat.parse(header.date)
            binding.tvDateHeader.text = date?.let { outputFormat.format(it) } ?: header.date
        }
    }

    class DishViewHolder(private val binding: ItemDishForecastBinding) : RecyclerView.ViewHolder(binding.root) {
        fun bind(item: ForecastListItem.DishItem) {
            binding.tvDishName.text = item.dishForecast.name
            binding.tvPredictedQty.text = "Prep: ${item.dishForecast.predictedSales} units"
        }
    }

    companion object DiffCallback : DiffUtil.ItemCallback<ForecastListItem>() {
        private const val VIEW_TYPE_HEADER = 0
        private const val VIEW_TYPE_DISH = 1

        override fun areItemsTheSame(oldItem: ForecastListItem, newItem: ForecastListItem): Boolean {
            return when {
                oldItem is ForecastListItem.DateHeader && newItem is ForecastListItem.DateHeader -> oldItem.date == newItem.date
                oldItem is ForecastListItem.DishItem && newItem is ForecastListItem.DishItem -> oldItem.dishForecast.name == newItem.dishForecast.name
                else -> false
            }
        }

        override fun areContentsTheSame(oldItem: ForecastListItem, newItem: ForecastListItem): Boolean {
            return oldItem == newItem
        }
    }
}