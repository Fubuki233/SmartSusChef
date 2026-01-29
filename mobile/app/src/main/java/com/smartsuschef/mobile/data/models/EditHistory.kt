package com.smartsuschef.mobile.data.models

import java.util.Date

/*
* Audit Trail for Edited Sales and Wastage Data
 */

data class EditHistory(
    val updatedAt: String,
    val editedBy: String,
    val reason: String,
    val previousValue: Double,
    val newValue: Double
)
