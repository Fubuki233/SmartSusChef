package com.smartsuschef.mobile.data.models

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
    Employee,
    Manager;

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
