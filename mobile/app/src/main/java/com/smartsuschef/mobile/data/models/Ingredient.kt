package com.smartsuschef.mobile.data.models

import java.util.Date

data class Ingredient(
    val id: String, // Guid (.NET) --> String in Kotlin
    val name: String,
    val storeId: Int,
    val unit: String,
    val carbonFootprint: Double, // decimal (.NET) --> double in Kotlin
    val createdAt: String, // DateTime (.NET) -> String (ISO format) in Kotlin
    val updatedAt: String
)
