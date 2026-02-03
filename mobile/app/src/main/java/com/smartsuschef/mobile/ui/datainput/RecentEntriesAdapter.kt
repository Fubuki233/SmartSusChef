package com.smartsuschef.mobile.ui.datainput

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.smartsuschef.mobile.databinding.ItemRecentEntryBinding

interface RecentEntryActions {
    fun onEditClick(entry: RecentEntry)
    fun onDeleteClick(entry: RecentEntry)
}

/**
 * Adapter to display today's submitted sales or wastage entries.
 */
class RecentEntriesAdapter(private val actions: RecentEntryActions) : ListAdapter<RecentEntry, RecentEntriesAdapter.RecentViewHolder>(DiffCallback) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): RecentViewHolder {
        val binding = ItemRecentEntryBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return RecentViewHolder(binding)
    }

    override fun onBindViewHolder(holder: RecentViewHolder, position: Int) {
        holder.bind(getItem(position), actions)
    }

    class RecentViewHolder(private val binding: ItemRecentEntryBinding) :
        RecyclerView.ViewHolder(binding.root) {

        fun bind(item: RecentEntry, actions: RecentEntryActions) {
            binding.tvItemName.text = item.name
            binding.tvQuantity.text = "${item.quantity} ${item.unit}"
            binding.tvEntryTime.text = item.time // Display only the time
            // Assuming your layout has these buttons
            binding.btnEdit.setOnClickListener { actions.onEditClick(item) }
            binding.btnDelete.setOnClickListener { actions.onDeleteClick(item) }
        }
    }

    companion object DiffCallback : DiffUtil.ItemCallback<RecentEntry>() {
        override fun areItemsTheSame(oldItem: RecentEntry, newItem: RecentEntry) =
            oldItem.id == newItem.id // Compare by ID now

        override fun areContentsTheSame(oldItem: RecentEntry, newItem: RecentEntry) =
            oldItem == newItem
    }
}