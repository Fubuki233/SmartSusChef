package com.smartsuschef.mobile.data.models

import java.util.Date
data class Store(
    val id: Int,
    val companyName: String,
    val uen: String,                   // Unique Entity Number (Singapore)
    val storeName: String,
    val outletLocation: String,

    // Store Details
    val openingDate: String,        // DateTime -> String (ISO format)
    val latitude: Double,
    val longitude: Double,
    val address: String?,
    val contactNumber: String,

    val isActive: Boolean,
    val createdAt: String,
    val updatedAt: String
)
