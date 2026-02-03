package com.smartsuschef.mobile.ui.datainput

import android.os.Parcelable
import kotlinx.parcelize.Parcelize

@Parcelize
data class RecentEntry(
    val id: String,
    val name: String,
    val quantity: Double,
    val unit: String,
    val date: String, // format "yyyy-MM-dd"
    val time: String, // format "HH:mm"
    val isSales: Boolean,
    val isEditing: Boolean = false // Added for UI state management in adapter
) : Parcelable
