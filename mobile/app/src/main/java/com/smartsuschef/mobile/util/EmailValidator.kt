package com.smartsuschef.mobile.util
import android.util.Patterns
import javax.inject.Inject
class EmailValidator @Inject constructor() {
    fun isValid(email: String): Boolean {
        return Patterns.EMAIL_ADDRESS.matcher(email).matches()
    }
}