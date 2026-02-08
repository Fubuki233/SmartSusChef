import java.util.Properties
import java.io.FileInputStream

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    id("com.google.dagger.hilt.android")
    alias(libs.plugins.ksp)
    id("androidx.navigation.safeargs.kotlin")
    id("kotlin-parcelize")
}

// Load properties from local.properties file, if it exists
val localProperties = Properties()
val localPropertiesFile = rootProject.file("local.properties")
if (localPropertiesFile.exists()) {
    localProperties.load(FileInputStream(localPropertiesFile))
}

android {
    namespace = "com.smartsuschef.mobile"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.smartsuschef.mobile"
        minSdk = 29
        targetSdk = 35
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        multiDexEnabled = true
    }

    buildTypes {
        debug {
            // Read the base URL from local.properties, with a fallback to the Android emulator default
            val baseUrl = localProperties.getProperty("local.base.url", "http://10.0.2.2:5001/api/")
            buildConfigField("String", "BASE_URL", "\"$baseUrl\"")
        }
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
            // Your production URL
            buildConfigField("String", "BASE_URL", "\"https://smartsuschef.com/api/\"")
        }
    }

    buildFeatures {
        buildConfig = true
        dataBinding = true
        viewBinding = true
    }

    dataBinding {
        enable = true
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
        isCoreLibraryDesugaringEnabled = true
    }
    kotlinOptions {
        jvmTarget = "17"
    }

    ksp {
        arg("dagger.hilt.internal.useAggregatingRootProcessor", "false")
    }

    testOptions {
        unitTests.isIncludeAndroidResources = true
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.material)
    implementation(libs.androidx.activity)
    implementation(libs.androidx.constraintlayout)
    implementation(libs.androidx.legacy.support.v4)
    implementation(libs.androidx.lifecycle.livedata.ktx)
    implementation(libs.androidx.lifecycle.viewmodel.ktx)
    implementation(libs.androidx.fragment.ktx)
    testImplementation(libs.junit)
    testImplementation(libs.mockito.core)
    testImplementation(libs.mockito.kotlin)
    testImplementation(libs.coroutines.test)
    testImplementation(libs.core.testing)
    testImplementation(libs.datastore.preferences)
    testImplementation(libs.datastore.preferences.core)
    testImplementation("io.mockk:mockk:1.13.17") // Added for Mockk
    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.coroutines.test) // Add this for instrumentation coroutine testing
    // Networking (For .NET Backend & Python ML)
    implementation(libs.retrofit.core)
    implementation(libs.retrofit.gson)
    implementation(libs.okhttp.logging)
    // Navigation (For fragments)
    implementation(libs.navigation.fragment)
    implementation(libs.navigation.ui)
    // Hilt (Dependency Injection)
    implementation(libs.hilt.android)
    ksp(libs.hilt.compiler)
    // Add Hilt testing dependencies
    testImplementation("com.google.dagger:hilt-android-testing:2.51.1")
    androidTestImplementation("com.google.dagger:hilt-android-testing:2.51.1")
    // Use kspTest and kspAndroidTest for the Hilt compiler for test sources
    kspTest("com.google.dagger:hilt-android-compiler:2.51.1")
    kspAndroidTest("com.google.dagger:hilt-android-compiler:2.51.1")
    // Storage (For TokenManager)
    implementation(libs.datastore.preferences)
    implementation("androidx.security:security-crypto:1.1.0-alpha06")
    // UI Components (For charts)
    implementation(libs.mp.android.chart)
    // MultiDex
    implementation("androidx.multidex:multidex:2.0.1")
    coreLibraryDesugaring("com.android.tools:desugar_jdk_libs:2.0.4")

}