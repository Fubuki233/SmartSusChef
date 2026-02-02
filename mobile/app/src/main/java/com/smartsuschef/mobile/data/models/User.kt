package com.smartsuschef.mobile.data.models

import com.google.gson.annotations.SerializedName
import java.util.Date

data class User(
    val id: String,
    val storeId: Int,
    val username: String,
    val email: String,
    val name: String,
    val role: UserRole,
    val createdAt: String,
    val updatedAt: String
)

enum class UserRole {
    @SerializedName("employee") Employee, // Explicitly match backend lowercase
    @SerializedName("manager") Manager;

    companion object {
        fun fromString(role: String): UserRole {
            return when (role.lowercase()) {
                "employee" -> Employee
                "manager" -> Manager
                else -> throw IllegalArgumentException("Invalid user role: $role")
            }
        }
    }
}
