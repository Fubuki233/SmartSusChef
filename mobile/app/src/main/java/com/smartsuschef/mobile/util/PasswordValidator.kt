package com.smartsuschef.mobile.util

/**
 * Password Validator
 * Validates passwords according to backend security requirements.
 *
 * Current Requirements (Update as needed):
 * - Minimum 12 characters
 * - At least 1 uppercase letter
 * - At least 1 lowercase letter
 * - At least 1 number
 * - At least 1 special character (@$!%*?&)
 */
object PasswordValidator {

    // Password validation configuration
    private const val MIN_LENGTH = 12
    private const val MAX_LENGTH = 36
    private const val SPECIAL_CHARS = "@$!%*?&#^()-_=+[]{}|;:',.<>/~`"

    // Result of password validation
    data class ValidationResult(
        val isValid: Boolean,
        val hasMinLength: Boolean = true,
        val hasMaxLength: Boolean = true,
        val hasUpperCase: Boolean = true,
        val hasLowerCase: Boolean = true,
        val hasNumber: Boolean = true,
        val hasSpecialChar: Boolean = true,
        val errorMessage: String? = null
    )

    // Validates a password and returns detailed results
    fun validate(password: String): ValidationResult {
        val hasMinLength = password.length >= MIN_LENGTH
        val hasMaxLength = password.length <= MAX_LENGTH
        val hasUpperCase = password.any { it.isUpperCase() }
        val hasLowerCase = password.any { it.isLowerCase() }
        val hasNumber = password.any { it.isDigit() }
        val hasSpecialChar = password.any { SPECIAL_CHARS.contains(it) }

        val isValid = hasMinLength && hasMaxLength && hasUpperCase &&
                hasLowerCase && hasNumber && hasSpecialChar

        val errorMessage = when {
            !hasMinLength -> "Password must be at least $MIN_LENGTH characters"
            !hasMaxLength -> "Password must not exceed $MAX_LENGTH characters"
            !hasUpperCase -> "Password must contain at least one uppercase letter"
            !hasLowerCase -> "Password must contain at least one lowercase letter"
            !hasNumber -> "Password must contain at least one number"
            !hasSpecialChar -> "Password must contain at least one special character ($SPECIAL_CHARS)"
            else -> null
        }

        return ValidationResult(
            isValid = isValid,
            hasMinLength = hasMinLength,
            hasMaxLength = hasMaxLength,
            hasUpperCase = hasUpperCase,
            hasLowerCase = hasLowerCase,
            hasNumber = hasNumber,
            hasSpecialChar = hasSpecialChar,
            errorMessage = errorMessage
        )
    }

    // Quick validation - returns true if password is valid
    fun isValid(password: String): Boolean {
        return validate(password).isValid
    }

    // Gets the first error message for a password
    fun getErrorMessage(password: String): String? {
        return validate(password).errorMessage
    }

    // Gets all password requirements as a list of strings
    fun getRequirements(): List<String> {
        return listOf(
            "At least $MIN_LENGTH characters",
            "At least one uppercase letter (A-Z)",
            "At least one lowercase letter (a-z)",
            "At least one number (0-9)",
            "At least one special character ($SPECIAL_CHARS)"
        )
    }

    // Gets requirements with check status for a given password, for real-time UI validation
    fun getRequirementsWithStatus(password: String): List<Pair<String, Boolean>> {
        val result = validate(password)
        return listOf(
            "At least $MIN_LENGTH characters" to result.hasMinLength,
            "At least one uppercase letter (A-Z)" to result.hasUpperCase,
            "At least one lowercase letter (a-z)" to result.hasLowerCase,
            "At least one number (0-9)" to result.hasNumber,
            "At least one special character" to result.hasSpecialChar
        )
    }
}