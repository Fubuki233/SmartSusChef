package com.smartsuschef.mobile.data.models

import java.util.Date

data class ForecastData(
    val id: String,
    val storeId: Int,
    val recipeId: String,
    val forecastDate: String,
    val predictedQuantity: Int,
    val createdAt: String,
    val updatedAt: String
)
