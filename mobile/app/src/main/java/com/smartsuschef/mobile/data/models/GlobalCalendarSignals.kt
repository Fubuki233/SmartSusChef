package com.smartsuschef.mobile.data.models

import java.util.Date
data class GlobalCalendarSignals(
    val date: String,
    val isHoliday: Boolean,
    val holidayName: String,
    val isSchoolHoliday: Boolean,
    val rainMm: Double,
    val weatherDesc: String
)
