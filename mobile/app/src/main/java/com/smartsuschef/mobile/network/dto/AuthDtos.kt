package com.smartsuschef.mobile.network.dto

import com.google.gson.annotations.SerializedName

/**
 * Login Request DTO
 * Maps to: LoginRequest in AuthDtos.cs
 */
data class LoginRequest(
    @SerializedName("username")
    val username: String,

    @SerializedName("password")
    val password: String
)

/**
 * Login Response DTO
 * Maps to: LoginResponse in AuthDtos.cs
 */
data class LoginResponse(
    @SerializedName("token")
    val token: String,

    @SerializedName("user")
    val user: UserDto,

    @SerializedName("storeSetupRequired")
    val storeSetupRequired: Boolean
)

/**
 * User DTO
 * Maps to: UserDto in AuthDtos.cs
 */
data class UserDto(
    @SerializedName("id")
    val id: String,

    @SerializedName("username")
    val username: String,

    @SerializedName("name")
    val name: String,

    @SerializedName("email")
    val email: String,

    @SerializedName("role")
    val role: String, // "manager" or "employee"

    @SerializedName("status")
    val status: String // "Active" or "Inactive"
)

/**
 * Update Profile Request
 * For users to update their own name and email
 * Username cannot be changed
 */
data class UpdateProfileRequest(
    @SerializedName("name")
    val name: String,

    @SerializedName("email")
    val email: String
)

/**
 * Change Password Request
 * For users to change their own password
 */
data class ChangePasswordRequest(
    @SerializedName("currentPassword")
    val currentPassword: String,

    @SerializedName("newPassword")
    val newPassword: String
)

/**
 * Password Reset Request
 * For "Forgot Password" feature on login screen
 */
data class PasswordResetRequest(
    @SerializedName("email")
    val email: String
)