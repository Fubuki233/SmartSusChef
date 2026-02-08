package com.smartsuschef.mobile.util

object Constants {
    // API Configuration
    // const val BASE_URL = "http://oversea.zyh111.icu:234/api/"

    // For local development (uncomment when testing locally)
    // const val BASE_URL = "http://192.168.1.100:5000/api/"

    // For Android Emulator
    const val BASE_URL = "http://10.0.2.2:5000/api/"

    // Network Timeouts (seconds)
    const val CONNECT_TIMEOUT = 30L
    const val READ_TIMEOUT = 30L
    const val WRITE_TIMEOUT = 30L

    // DataStore Keys
    const val SHARED_PREFS_FILE_NAME = "smartsuschef_prefs"
    const val KEY_AUTH_TOKEN = "auth_token"
    const val KEY_USERNAME = "username"
    const val KEY_USER_ROLE = "user_role"
    const val KEY_TOKEN_EXPIRY = "token_expiry"

    // User Roles
    const val ROLE_EMPLOYEE = "Employee"
    const val ROLE_MANAGER = "Manager"

    // Date Formats
    const val DATE_FORMAT_API = "yyyy-MM-dd"              // For API: 2026-01-28
    const val DATE_FORMAT_DISPLAY = "dd MMM yyyy"         // For UI: 28 Jan 2026
    const val DATETIME_FORMAT_API = "yyyy-MM-dd'T'HH:mm:ss" // For API timestamps

    // Data Restrictions (Employee vs Manager)
    const val EMPLOYEE_DATA_DAYS = 7    // Employee can only see last 7 days
    const val MANAGER_DATA_DAYS = 30    // Manager can see last 30 days

    // Session Timeout (minutes)
    const val SESSION_TIMEOUT_MINUTES = 30
}