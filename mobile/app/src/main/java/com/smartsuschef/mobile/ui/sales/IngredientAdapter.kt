package com.smartsuschef.mobile.ui.sales

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.smartsuschef.mobile.R

class IngredientAdapter(private var ingredients: List<IngredientRequirement>) :
    RecyclerView.Adapter<IngredientAdapter.ViewHolder>() {

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val name: TextView = view.findViewById(R.id.tvIngredientName)
        val quantity: TextView = view.findViewById(R.id.tvQuantity)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_ingredient_row, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = ingredients[position]
        holder.name.text = item.name
        holder.quantity.text = "${item.quantity} ${item.unit}"
    }

    override fun getItemCount() = ingredients.size

    fun updateData(newItems: List<IngredientRequirement>) {
        ingredients = newItems
        notifyDataSetChanged()
    }
}