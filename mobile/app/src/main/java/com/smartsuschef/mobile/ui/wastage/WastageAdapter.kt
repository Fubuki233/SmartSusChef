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
        val type: TextView = view.findViewById(R.id.tvWastedItemType)
        val quantity: TextView = view.findViewById(R.id.tvWastedQuantity)
        val co2: TextView = view.findViewById(R.id.tvWastedCo2)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_wastage_row, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = wastedItems[position]
        holder.name.text = item.name
        holder.type.text = "Type: ${item.type}"
        holder.quantity.text = "Quantity: ${item.quantity} ${item.unit}"
        holder.co2.text = "CO2: %.2f kg".format(item.carbonFootprint)
    }

    override fun getItemCount() = wastedItems.size

    fun updateData(newItems: List<WastageBreakdownItem>) {
        wastedItems = newItems
        notifyDataSetChanged()
    }
}
