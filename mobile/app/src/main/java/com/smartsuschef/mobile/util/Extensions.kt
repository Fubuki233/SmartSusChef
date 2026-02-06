package com.smartsuschef.mobile.util

import android.content.Context
import android.text.Editable
import android.text.TextWatcher
import com.google.android.material.textfield.TextInputLayout
import android.view.View
import android.widget.Toast
import com.google.android.material.snackbar.Snackbar

// Context Extensions
fun Context.showToast(message: String, duration: Int = Toast.LENGTH_SHORT) {
    Toast.makeText(this, message, duration).show()
}

// View Extensions
fun View.visible() {
    visibility = View.VISIBLE
}

fun View.gone() {
    visibility = View.GONE
}

fun View.invisible() {
    visibility = View.INVISIBLE
}

fun View.showSnackbar(message: String, duration: Int = Snackbar.LENGTH_SHORT) {
    Snackbar.make(this, message, duration).show()
}

/**
 * Extension functions for form validation for text input
 */

// Sets error message on TextInputLayout
fun TextInputLayout.setError(message: String?) {
    error = message
    isErrorEnabled = message != null
}

// Clears error from TextInputLayout
fun TextInputLayout.clearError() {
    error = null
    isErrorEnabled = false
}

// Gets the text from the TextInputLayout's EditText
fun TextInputLayout.getText(): String {
    return editText?.text?.toString() ?: ""
}

// Adds a text change listener that validates on each change
// Returns the TextWatcher so it can be removed if needed

fun TextInputLayout.addValidationWatcher(
    validator: (String) -> String?
): TextWatcher {
    val textWatcher = object : TextWatcher {
        override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
        override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
        override fun afterTextChanged(s: Editable?) {
            val text = s?.toString() ?: ""
            val errorMessage = validator(text)
            setError(errorMessage)
        }
    }
    editText?.addTextChangedListener(textWatcher)
    return textWatcher
}

// Validates password field using PasswordValidator
// Shows first error encountered
fun TextInputLayout.validatePassword(): Boolean {
    val password = getText()
    val result = PasswordValidator.validate(password)
    setError(result.errorMessage)
    return result.isValid
}

// Validates that two password fields match
fun TextInputLayout.validatePasswordMatch(otherField: TextInputLayout): Boolean {
    val password1 = getText()
    val password2 = otherField.getText()

    return if (password1 != password2) {
        setError("Passwords do not match")
        false
    } else {
        clearError()
        true
    }
}

// Validates that field is not empty
fun TextInputLayout.validateNotEmpty(fieldName: String = "This field"): Boolean {
    val text = getText()
    return if (text.isBlank()) {
        setError("$fieldName is required")
        false
    } else {
        clearError()
        true
    }
}

// Validates email format using pure Kotlin regex (no Android framework dependencies)
// Uses RFC 5322 compliant pattern for email validation
fun TextInputLayout.validateEmail(): Boolean {
    val email = getText()

    // RFC 5322 compliant email regex (simplified version)
    // Matches: user@domain.com, user.name@domain.co.uk, user+tag@domain.com
    val emailRegex = "^[A-Za-z0-9+_.-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$".toRegex()

    return when {
        email.isBlank() -> {
            setError("Email is required")
            false
        }
        !email.matches(emailRegex) -> {
            setError("Please enter a valid email address")
            false
        }
        else -> {
            clearError()
            true
        }
    }
}

/**
 * Usage Examples:
 *
 * // In Activity/Fragment:
 * showToast("Login successful")
 * binding.root.showSnackbar("Error occurred")
 *
 * // View visibility:
 * binding.progressBar.visible()
 * binding.errorText.gone()
 *
 * // TextInputLayout validation in Activity/Fragment:
 * if (!binding.tilPassword.validatePassword()) {
 *     return // Error already shown
 * }
 *
 * if (!binding.tilConfirmPassword.validatePasswordMatch(binding.tilPassword)) {
 *     return // Error already shown
 * }
 *
 * if (!binding.tilEmail.validateEmail()) {
 *     return // Error already shown
 * }
 *
 * // For ViewModel validation, inject validators:
 * class MyViewModel @Inject constructor(
 *     private val emailValidator: EmailValidator
 * ) {
 *     private fun isValidEmail(email: String): Boolean {
 *         return emailValidator.isValid(email)  // Testable without Robolectric
 *     }
 * }
 */