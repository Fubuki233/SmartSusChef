package com.smartsuschef.mobile.data.models

import java.util.Date

data class SalesData(
    val id: String, // Guid (.NET) --> String in Kotlin
    val storeId: Int,
    val date: String, // DateTime (.NET) -> String (ISO format) in Kotlin
    val recipeId: String,
    val quantity: Int,
    val createdBy: String,  // To record the User who entered the data
    val createdAt: String, // DateTime (.NET) -> String (ISO format) in Kotlin
    val recipeName: String? = null,

    // Edit history tracking (for audit trail)
    val editHistory: List<EditHistory>? = null

)