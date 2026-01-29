package com.smartsuschef.mobile.network.dto

import com.google.gson.annotations.SerializedName

/**
 * Forecast DTO
 * Maps to: ForecastDto(string Date, string RecipeId, string RecipeName, int Quantity, List<ForecastIngredientDto> Ingredients)
 */
data class ForecastDto(
    @SerializedName("date")
    val date: String,

    @SerializedName("recipeId")
    val recipeId: String,

    @SerializedName("recipeName")
    val recipeName: String,

    @SerializedName("quantity")
    val quantity: Int,

    @SerializedName("ingredients")
    val ingredients: List<ForecastIngredientDto>
)

/**
 * Forecast Ingredient DTO
 * Maps to: ForecastIngredientDto
 */
data class ForecastIngredientDto(
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
 * Forecast Summary DTO
 * Maps to: ForecastSummaryDto(string Date, int TotalQuantity, decimal ChangePercentage)
 */
data class ForecastSummaryDto(
    @SerializedName("date")
    val date: String,

    @SerializedName("totalQuantity")
    val totalQuantity: Int,

    @SerializedName("changePercentage")
    val changePercentage: Double // decimal in C#
)

/**
 * Weather DTO
 * Maps to: WeatherDto
 */
data class WeatherDto(
    @SerializedName("temperature")
    val temperature: Double, // decimal in C#

    @SerializedName("condition")
    val condition: String,

    @SerializedName("humidity")
    val humidity: Int,

    @SerializedName("description")
    val description: String
)

/**
 * Holiday DTO
 * Maps to: HolidayDto
 */
data class HolidayDto(
    @SerializedName("date")
    val date: String,

    @SerializedName("name")
    val name: String
)

/**
 * Weather Forecast DTO
 * Maps to: WeatherForecastDto
 */
data class WeatherForecastDto(
    @SerializedName("date")
    val date: String,

    @SerializedName("temperatureMax")
    val temperatureMax: Double? = null,

    @SerializedName("temperatureMin")
    val temperatureMin: Double? = null,

    @SerializedName("rainMm")
    val rainMm: Double, // decimal in C#

    @SerializedName("weatherCode")
    val weatherCode: Int,

    @SerializedName("weatherDescription")
    val weatherDescription: String
)

/**
 * Calendar Day DTO
 * Maps to: CalendarDayDto - includes weather and holiday status
 */
data class CalendarDayDto(
    @SerializedName("date")
    val date: String,

    @SerializedName("isHoliday")
    val isHoliday: Boolean,

    @SerializedName("holidayName")
    val holidayName: String? = null,

    @SerializedName("isSchoolHoliday")
    val isSchoolHoliday: Boolean,

    @SerializedName("isWeekend")
    val isWeekend: Boolean,

    @SerializedName("weather")
    val weather: WeatherForecastDto? = null
)

/**
 * Tomorrow Forecast DTO
 * Maps to: TomorrowForecastDto - combined forecast and calendar response
 */
data class TomorrowForecastDto(
    @SerializedName("date")
    val date: String,

    @SerializedName("calendar")
    val calendar: CalendarDayDto,

    @SerializedName("weather")
    val weather: WeatherForecastDto? = null
)