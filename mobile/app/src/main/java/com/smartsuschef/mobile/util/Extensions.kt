package com.smartsuschef.mobile.util

import android.content.Context
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

// String Extensions
fun String.isValidEmail(): Boolean {
    return android.util.Patterns.EMAIL_ADDRESS.matcher(this).matches()
}

fun String.isValidPassword(): Boolean {
    // At least 6 characters
    return this.length >= 6
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
 * // Validation:
 * if (!email.isValidEmail()) {
 *     showToast("Invalid email")
 * }
 */