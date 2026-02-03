package com.smartsuschef.mobile.ui.wastage

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.smartsuschef.mobile.R

class WastageAdapter(private var wastedItems: List<WastageBreakdownItem>) :
    RecyclerView.Adapter<WastageAdapter.ViewHolder>() {

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val name: TextView = view.findViewById(R.id.tvWastedItemName)
        val quantity: TextView = view.findViewById(R.id.tvWastedQuantity)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_wastage_row, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = wastedItems[position]
        holder.name.text = item.name
        holder.quantity.text = "${item.quantity} ${item.unit}"
    }

    override fun getItemCount() = wastedItems.size

    fun updateData(newItems: List<WastageBreakdownItem>) {
        wastedItems = newItems
        notifyDataSetChanged()
    }
}
