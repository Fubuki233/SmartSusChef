package com.smartsuschef.mobile.data.models

import java.util.Date

data class WastageData(
    val id: String,
    val storeId: Int,
    val date: String,                  // DateTime → String (ISO format)
    val ingredientId: String?,         // Nullable (can be null if recipeId is set)
    val recipeId: String?,             // Nullable (can be null if ingredientId is set)
    val quantity: Double,              // decimal → Double
    val unit: String,
    val carbonFootprint: Double?,
    val createdBy: String,              // To record the User who entered the data
    val createdAt: String,

    // Optional: Backend might include these for convenience
    val ingredientName: String? = null,
    val recipeName: String? = null
    val type: String, // if Backend can do the computation

    // Edit history tracking (for audit trail)
    val editHistory: List<EditHistory>? = null
)