package com.smartsuschef.mobile.data.models

import java.util.Date

data class WastageData(
    val id: String,
    val storeId: Int,
    val date: String,                  // DateTime → String (ISO format)
    val ingredientId: String?,         // Nullable (can be null if recipeId is set)
    val recipeId: String?,             // Nullable (can be null if ingredientId is set)
    val quantity: Double,              // decimal → Double
    val createdAt: String,
    val updatedAt: String
)