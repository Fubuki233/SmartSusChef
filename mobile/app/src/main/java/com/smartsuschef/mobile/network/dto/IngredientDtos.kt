package com.smartsuschef.mobile.network.dto

import com.google.gson.annotations.SerializedName

/**
 * Ingredient DTO
 * Maps to: IngredientDto(string Id, string Name, string Unit, decimal CarbonFootprint)
 */
data class IngredientDto(
    @SerializedName("id")
    val id: String,

    @SerializedName("name")
    val name: String,

    @SerializedName("unit")
    val unit: String,

    @SerializedName("carbonFootprint")
    val carbonFootprint: Double // decimal in C#
)

/**
 * Create Ingredient Request
 * Maps to: CreateIngredientRequest(string Name, string Unit, decimal CarbonFootprint)
 */
data class CreateIngredientRequest(
    @SerializedName("name")
    val name: String,

    @SerializedName("unit")
    val unit: String,

    @SerializedName("carbonFootprint")
    val carbonFootprint: Double // decimal in C#
)

/**
 * Update Ingredient Request
 * Maps to: UpdateIngredientRequest(string Name, string Unit, decimal CarbonFootprint)
 */
data class UpdateIngredientRequest(
    @SerializedName("name")
    val name: String,

    @SerializedName("unit")
    val unit: String,

    @SerializedName("carbonFootprint")
    val carbonFootprint: Double // decimal in C#
)