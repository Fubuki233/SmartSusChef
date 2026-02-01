package com.smartsuschef.mobile.ui.datainput

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.smartsuschef.mobile.databinding.ItemRecentEntryBinding
import com.smartsuschef.mobile.ui.datainput.RecentEntry

/**
 * Adapter to display today's submitted sales or wastage entries.
 */
class RecentEntriesAdapter : ListAdapter<RecentEntry, RecentEntriesAdapter.RecentViewHolder>(DiffCallback) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): RecentViewHolder {
        val binding = ItemRecentEntryBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return RecentViewHolder(binding)
    }

    override fun onBindViewHolder(holder: RecentViewHolder, position: Int) {
        holder.bind(getItem(position))
    }

    class RecentViewHolder(private val binding: ItemRecentEntryBinding) :
        RecyclerView.ViewHolder(binding.root) {

        fun bind(item: RecentEntry) {
            binding.tvItemName.text = item.name
            // Displays quantity and unit (e.g., "95 plate" or "1.74 kg")
            binding.tvQuantity.text = "${item.quantity} ${item.unit}"
        }
    }

    companion object DiffCallback : DiffUtil.ItemCallback<RecentEntry>() {
        override fun areItemsTheSame(oldItem: RecentEntry, newItem: RecentEntry) =
            oldItem.name == newItem.name

        override fun areContentsTheSame(oldItem: RecentEntry, newItem: RecentEntry) =
            oldItem == newItem
    }
}