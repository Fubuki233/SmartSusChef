package com.smartsuschef.mobile.data.models

import java.util.Date

data class Recipe(
    val id: String, // Guid (.NET) --> String in Kotlin
    val storeId: Int,
    val name: String,
    val isSubRecipe: Boolean,
    val isSellable: Boolean,
    val createdAt: String, // DateTime (.NET) -> String (ISO format) in Kotlin
    val updatedAt: String,
    // To connect the two models - Recipe and Ingredient
    val recipeIngredients: List<RecipeIngredient> = emptyList()
)
