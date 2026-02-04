package com.smartsuschef.mobile.ui.forecast

import android.content.Context
import android.widget.TextView
import com.github.mikephil.charting.components.MarkerView
import com.github.mikephil.charting.data.BarEntry
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.highlight.Highlight
import com.github.mikephil.charting.utils.MPPointF
import com.smartsuschef.mobile.R

/**
 * Enhanced Marker View for displaying detailed data on chart tap
 * Supports:
 * - Simple values (Prediction Summary)
 * - Stacked bar breakdown (Dishes Forecast)
 * - Grouped bar comparison (Predicted vs Actual)
 */
class ForecastMarkerView(context: Context, layoutResource: Int) : MarkerView(context, layoutResource) {

    private val tvDate: TextView = findViewById(R.id.tvMarkerDate)
    private val tvQuantity: TextView = findViewById(R.id.tvMarkerQuantity)
    private val tvDetails: TextView = findViewById(R.id.tvMarkerDetails)

    // Store dish names for stacked bars
    var dishNames: Array<String>? = null

    // Store date labels for display
    var labels: List<String>? = null

    // Store chart type for proper formatting
    var chartType: ChartType = ChartType.SIMPLE

    enum class ChartType {
        SIMPLE,           // Prediction Summary (single value)
        STACKED,          // Dishes Forecast (breakdown)
        COMPARISON        // Comparison chart (predicted vs actual)
    }

    override fun refreshContent(e: Entry?, highlight: Highlight?) {
        if (e == null) return

        // Set date if available
        val index = e.x.toInt()
        val dateLabel = labels?.getOrNull(index) ?: ""
        tvDate.text = dateLabel
        tvDate.visibility = if (dateLabel.isNotEmpty()) VISIBLE else GONE

        when (chartType) {
            ChartType.SIMPLE -> {
                // Prediction Summary Chart
                val quantity = e.y.toInt()
                tvQuantity.text = "Dishes: $quantity"
                tvDetails.visibility = GONE
            }

            ChartType.STACKED -> {
                // Dishes Forecast Chart - Show breakdown
                if (e is BarEntry && e.yVals != null) {
                    val total = e.yVals.sum().toInt()
                    tvQuantity.text = "Total: $total dishes"

                    // Build breakdown string
                    val breakdown = StringBuilder()
                    e.yVals.forEachIndexed { index, value ->
                        val dishName = dishNames?.getOrNull(index) ?: "Dish ${index + 1}"
                        breakdown.append("$dishName: ${value.toInt()}\n")
                    }

                    tvDetails.text = breakdown.toString().trim()
                    tvDetails.visibility = VISIBLE
                } else {
                    tvQuantity.text = "Dishes: ${e.y.toInt()}"
                    tvDetails.visibility = GONE
                }
            }

            ChartType.COMPARISON -> {
                // Comparison Chart - Show both predicted and actual
                // The data should be passed via Entry.data
                if (e.data is ComparisonData) {
                    val data = e.data as ComparisonData
                    tvQuantity.text = "Predicted: ${data.predicted}"
                    tvDetails.text = "Actual: ${data.actual}"
                    tvDetails.visibility = VISIBLE
                } else {
                    // Fallback if data not provided
                    tvQuantity.text = "Dishes: ${e.y.toInt()}"
                    tvDetails.visibility = GONE
                }
            }
        }

        super.refreshContent(e, highlight)
    }

    override fun getOffset(): MPPointF {
        // Center the marker above the selected value
        return MPPointF((-(width / 2)).toFloat(), (-height).toFloat())
    }

    /**
     * Data class for comparison chart
     */
    data class ComparisonData(
        val predicted: Int,
        val actual: Int
    )
}