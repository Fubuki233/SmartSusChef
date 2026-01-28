package com.smartsuschef.mobile.util

import java.text.SimpleDateFormat
import java.util.*

object DateUtils {

    // Get today's date in API format (yyyy-MM-dd)
    fun getTodayFormatted(): String {
        val formatter = SimpleDateFormat(Constants.DATE_FORMAT_API, Locale.getDefault())
        return formatter.format(Date())
    }

    // Get date N days ago in API format
    fun getDaysAgoFormatted(daysAgo: Int): String {
        val calendar = Calendar.getInstance()
        calendar.add(Calendar.DAY_OF_YEAR, -daysAgo)
        val formatter = SimpleDateFormat(Constants.DATE_FORMAT_API, Locale.getDefault())
        return formatter.format(calendar.time)
    }

    // Get date N days from now in API format
    fun getDaysFromNowFormatted(daysFromNow: Int): String {
        val calendar = Calendar.getInstance()
        calendar.add(Calendar.DAY_OF_YEAR, daysFromNow)
        val formatter = SimpleDateFormat(Constants.DATE_FORMAT_API, Locale.getDefault())
        return formatter.format(calendar.time)
    }

    // Convert API date string (yyyy-MM-dd) to display format (dd MMM yyyy)
    fun formatDateForDisplay(apiDateString: String): String {
        return try {
            val apiFormatter = SimpleDateFormat(Constants.DATE_FORMAT_API, Locale.getDefault())
            val displayFormatter = SimpleDateFormat(Constants.DATE_FORMAT_DISPLAY, Locale.getDefault())
            val date = apiFormatter.parse(apiDateString)
            displayFormatter.format(date!!)
        } catch (e: Exception) {
            apiDateString // Return as-is if parsing fails
        }
    }

    // Convert Date to API format string (yyyy-MM-dd)
    fun formatDateForApi(date: Date): String {
        val formatter = SimpleDateFormat(Constants.DATE_FORMAT_API, Locale.getDefault())
        return formatter.format(date)
    }

    // Get start date based on user role - Employee (last 7 days), Manager (last 30 days)
    fun getStartDateByRole(isEmployee: Boolean): String {
        val daysAgo = if (isEmployee) Constants.EMPLOYEE_DATA_DAYS else Constants.MANAGER_DATA_DAYS
        return getDaysAgoFormatted(daysAgo)
    }

    // Check if date string is today
    fun isToday(apiDateString: String): Boolean {
        return apiDateString == getTodayFormatted()
    }

    // Get current timestamp in API format
    fun getCurrentTimestamp(): String {
        val formatter = SimpleDateFormat(Constants.DATETIME_FORMAT_API, Locale.getDefault())
        return formatter.format(Date())
    }
}