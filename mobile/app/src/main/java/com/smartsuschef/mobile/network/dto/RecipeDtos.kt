package com.smartsuschef.mobile.network.dto

import com.google.gson.annotations.SerializedName

/**
 * Recipe DTO
 * Maps to: RecipeDto(string Id, string Name, bool IsSellable, bool IsSubRecipe, List<RecipeIngredientDto> Ingredients)
 */
data class RecipeDto(
    @SerializedName("id")
    val id: String,

    @SerializedName("name")
    val name: String,

    @SerializedName("isSellable")
    val isSellable: Boolean,

    @SerializedName("isSubRecipe")
    val isSubRecipe: Boolean,

    @SerializedName("ingredients")
    val ingredients: List<RecipeIngredientDto>,

    @SerializedName("createdAt")
    val createdAt: String,

    @SerializedName("updatedAt")
    val updatedAt: String
)

/**
 * Recipe Ingredient DTO
 * Maps to: RecipeIngredientDto with IngredientId OR ChildRecipeId
 */
data class RecipeIngredientDto(
    @SerializedName("ingredientId")
    val ingredientId: String? = null,

    @SerializedName("childRecipeId")
    val childRecipeId: String? = null,

    @SerializedName("displayName")
    val displayName: String, // Either Ingredient Name or Recipe Name

    @SerializedName("unit")
    val unit: String,

    @SerializedName("quantity")
    val quantity: Double // decimal in C#
)

/**
 * Create Recipe Request
 * Maps to: CreateRecipeRequest
 */
data class CreateRecipeRequest(
    @SerializedName("name")
    val name: String,

    @SerializedName("isSellable")
    val isSellable: Boolean,

    @SerializedName("isSubRecipe")
    val isSubRecipe: Boolean,

    @SerializedName("ingredients")
    val ingredients: List<CreateRecipeIngredientRequest>
)

/**
 * Create Recipe Ingredient Request
 * Either IngredientId or ChildRecipeId must be provided
 */
data class CreateRecipeIngredientRequest(
    @SerializedName("ingredientId")
    val ingredientId: String? = null,

    @SerializedName("childRecipeId")
    val childRecipeId: String? = null,

    @SerializedName("quantity")
    val quantity: Double // decimal in C#
)

/**
 * Update Recipe Request
 * Maps to: UpdateRecipeRequest
 */
data class UpdateRecipeRequest(
    @SerializedName("name")
    val name: String,

    @SerializedName("isSellable")
    val isSellable: Boolean,

    @SerializedName("isSubRecipe")
    val isSubRecipe: Boolean,

    @SerializedName("ingredients")
    val ingredients: List<CreateRecipeIngredientRequest>
)