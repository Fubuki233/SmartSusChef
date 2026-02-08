package com.smartsuschef.mobile.network.dto

import com.google.gson.annotations.SerializedName

/**
 * Sales Data DTO
 * Maps to: SalesDataDto in SalesDtos.cs
 */
data class SalesDataDto(
    @SerializedName("id")
    val id: String,

    @SerializedName("date")
    val date: String,

    @SerializedName("recipeId")
    val recipeId: String,

    @SerializedName("recipeName")
    val recipeName: String,

    @SerializedName("quantity")
    val quantity: Int,

    @SerializedName("createdAt")
    val createdAt: String,

    @SerializedName("updatedAt")
    val updatedAt: String
)

/**
 * Create Sales Data Request
 * Maps to: CreateSalesDataRequest in SalesDtos.cs
 */
data class CreateSalesDataRequest(
    @SerializedName("date")
    val date: String,

    @SerializedName("recipeId")
    val recipeId: String,

    @SerializedName("quantity")
    val quantity: Int
)

/**
 * Update Sales Data Request
 * Maps to: UpdateSalesDataRequest in SalesDtos.cs
 */
data class UpdateSalesDataRequest(
    @SerializedName("quantity")
    val quantity: Int
)

/**
 * Sales Trend DTO (for charts/dashboard)
 * Maps to: SalesTrendDto in SalesDtos.cs
 */
data class SalesTrendDto(
    @SerializedName("date")
    val date: String,

    @SerializedName("totalQuantity")
    val totalQuantity: Int,

    @SerializedName("recipeBreakdown")
    val recipeBreakdown: List<RecipeSalesDto>
)

/**
 * Recipe Sales DTO (nested in SalesTrendDto)
 * Maps to: RecipeSalesDto in SalesDtos.cs
 */
data class RecipeSalesDto(
    @SerializedName("recipeId")
    val recipeId: String,

    @SerializedName("recipeName")
    val recipeName: String,

    @SerializedName("quantity")
    val quantity: Int
)

/**
 * Ingredient Usage DTO (derived from sales)
 * Maps to: IngredientUsageDto in SalesDtos.cs
 */
data class IngredientUsageDto(
    @SerializedName("ingredientId")
    val ingredientId: String,

    @SerializedName("ingredientName")
    val ingredientName: String,

    @SerializedName("unit")
    val unit: String,

    @SerializedName("quantity")
    val quantity: Double // decimal in C#
)

/**
 * Import Sales Data Request (bulk import)
 * Maps to: ImportSalesDataRequest in SalesDtos.cs
 */
data class ImportSalesDataRequest(
    @SerializedName("salesData")
    val salesData: List<CreateSalesDataRequest>
)

/**
 * Sales with Signals DTO (includes weather/calendar data)
 * Maps to: SalesWithSignalsDto in SalesWithSignalsDto.cs
 */
data class SalesWithSignalsDto(
    @SerializedName("date")
    val date: String,

    @SerializedName("totalQuantity")
    val totalQuantity: Int,

    @SerializedName("isHoliday")
    val isHoliday: Boolean,

    @SerializedName("holidayName")
    val holidayName: String,

    @SerializedName("rainMm")
    val rainMm: Double,

    @SerializedName("weatherDesc")
    val weatherDesc: String,

    @SerializedName("recipes")
    val recipes: List<RecipeSalesDto>
)