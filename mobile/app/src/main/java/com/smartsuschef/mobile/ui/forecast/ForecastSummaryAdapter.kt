package com.smartsuschef.mobile.ui.forecast

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.smartsuschef.mobile.databinding.ItemIngredientForecastRowBinding
import com.smartsuschef.mobile.ui.forecast.IngredientForecast
import java.util.Locale

class ForecastSummaryAdapter(private var items: List<IngredientForecast>) :
    RecyclerView.Adapter<ForecastSummaryAdapter.ViewHolder>() {

    class ViewHolder(val binding: ItemIngredientForecastRowBinding) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val binding = ItemIngredientForecastRowBinding.inflate(
            LayoutInflater.from(parent.context), parent, false
        )
        return ViewHolder(binding)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        holder.binding.apply {
            tvIngredientName.text = "${item.name} (${item.unit})"

            // Map the 7 quantities to the 7 textviews
            val textViews = listOf(day1, day2, day3, day4, day5, day6, day7)
            item.totalQuantity.forEachIndexed { index, qty ->
                if (index < textViews.size) {
                    textViews[index].text = if (qty > 0) String.format("%.1f", qty) else "-"
                }
            }
        }
    }

    override fun getItemCount() = items.size

    fun updateData(newItems: List<IngredientForecast>) {
        items = newItems
        notifyDataSetChanged()
    }
}