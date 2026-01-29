package com.smartsuschef.mobile.network.dto

import com.google.gson.annotations.SerializedName

/**
 * Store DTO
 * Maps to: StoreDto in StoreDtos.cs
 * Used to display store info in app (e.g., "Aunty May's Cafe | Orchard Central")
 */
data class StoreDto(
    @SerializedName("id")
    val id: Int,

    @SerializedName("companyName")
    val companyName: String,

    @SerializedName("uen")
    val uen: String, // Unique Entity Number (Singapore)

    @SerializedName("storeName")
    val storeName: String,

    @SerializedName("outletLocation")
    val outletLocation: String,

    @SerializedName("contactNumber")
    val contactNumber: String,

    @SerializedName("openingDate")
    val openingDate: String, // DateTime as ISO string

    @SerializedName("latitude")
    val latitude: Double, // decimal in C#

    @SerializedName("longitude")
    val longitude: Double, // decimal in C#

    @SerializedName("address")
    val address: String? = null,

    @SerializedName("isActive")
    val isActive: Boolean,

    @SerializedName("createdAt")
    val createdAt: String, // DateTime as ISO string

    @SerializedName("updatedAt")
    val updatedAt: String // DateTime as ISO string
)