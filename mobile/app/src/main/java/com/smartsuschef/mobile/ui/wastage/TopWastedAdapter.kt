package com.smartsuschef.mobile.ui.wastage

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.smartsuschef.mobile.databinding.ItemTopWastedBinding

class TopWastedAdapter : ListAdapter<ImpactData, TopWastedAdapter.WastedViewHolder>(DiffCallback) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): WastedViewHolder {
        val binding = ItemTopWastedBinding.inflate(LayoutInflater.from(parent.context), parent, false)
        return WastedViewHolder(binding)
    }

    override fun onBindViewHolder(holder: WastedViewHolder, position: Int) {
        holder.bind(getItem(position))
    }

    class WastedViewHolder(private val binding: ItemTopWastedBinding) : RecyclerView.ViewHolder(binding.root) {
        fun bind(item: ImpactData) {
            binding.tvItemName.text = item.category
            binding.tvCarbonImpact.text = String.format("%.2f kg CO₂", item.carbonValue)

            // Hide fields not available in ImpactData
            binding.tvItemType.visibility = View.GONE
            binding.tvQuantity.visibility = View.GONE
        }
    }

    companion object DiffCallback : DiffUtil.ItemCallback<ImpactData>() {
        override fun areItemsTheSame(oldItem: ImpactData, newItem: ImpactData) =
            oldItem.category == newItem.category

        override fun areContentsTheSame(oldItem: ImpactData, newItem: ImpactData) =
            oldItem == newItem
    }
}