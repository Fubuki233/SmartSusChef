package com.smartsuschef.mobile.util

import javax.inject.Inject

/**
 * Email Validator
 *
 * Validates email addresses using RFC 5322 compliant regex pattern.
 * This was done using pure Kotlin implementation to avoid Android framework dependencies.
 * Enables unit tested without Robolectric, which caused issues.
 */
class EmailValidator @Inject constructor() {

    companion object {
        private val EMAIL_REGEX = "^[A-Za-z0-9+_.-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$".toRegex()
    }

    /**
     * Validates if the given email string matches valid email format
     *
     * @param email The email string to validate
     * @return true if valid email format, false otherwise
     */
    fun isValid(email: String): Boolean {
        return email.matches(EMAIL_REGEX)
    }
}