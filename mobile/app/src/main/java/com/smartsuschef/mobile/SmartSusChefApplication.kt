package com.smartsuschef.mobile
import android.app.Application
import dagger.hilt.android.HiltAndroidApp

/**
 * SmartSusChef Application Class
 *
 * This is the entry point of the app.
 * The @HiltAndroidApp annotation triggers Hilt's code generation and sets up:
 * - The application-level dependency container
 * - All Hilt modules (like NetworkModule)
 * - Dependency injection throughout the app
 */
@HiltAndroidApp
class SmartSusChefApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        // Application initialization code here (if needed)
        // Hilt is automatically initialized by the @HiltAndroidApp annotation
    }
}