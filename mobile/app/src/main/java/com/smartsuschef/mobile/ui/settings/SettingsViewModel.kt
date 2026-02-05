package com.smartsuschef.mobile.ui.settings

import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.smartsuschef.mobile.data.repository.AuthRepository
import com.smartsuschef.mobile.data.repository.UsersRepository
import com.smartsuschef.mobile.network.dto.ChangePasswordRequest
import com.smartsuschef.mobile.network.dto.UpdateProfileRequest
import com.smartsuschef.mobile.network.dto.UserDto
import com.smartsuschef.mobile.util.Resource
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class SettingsViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val usersRepository: UsersRepository
) : ViewModel() {

    // Current user data
    private val _currentUser = MutableLiveData<UserDto?>()
    val currentUser: LiveData<UserDto?> = _currentUser

    // Loading states
    private val _isLoadingProfile = MutableLiveData<Boolean>()
    val isLoadingProfile: LiveData<Boolean> = _isLoadingProfile

    private val _isLoadingPassword = MutableLiveData<Boolean>()
    val isLoadingPassword: LiveData<Boolean> = _isLoadingPassword

    // Result messages
    private val _profileUpdateResult = MutableLiveData<String?>()
    val profileUpdateResult: LiveData<String?> = _profileUpdateResult

    private val _passwordUpdateResult = MutableLiveData<String?>()
    val passwordUpdateResult: LiveData<String?> = _passwordUpdateResult

    init {
        loadCurrentUser()
    }

    /**
     * Load current user information
     */
    private fun loadCurrentUser() {
        viewModelScope.launch {
            _isLoadingProfile.value = true
            when (val result = usersRepository.getCurrentUser()) {
                is Resource.Success -> {
                    _currentUser.value = result.data
                    _isLoadingProfile.value = false
                }
                is Resource.Error -> {
                    _profileUpdateResult.value = "Failed to load user info: ${result.message}"
                    _isLoadingProfile.value = false
                }
                is Resource.Loading -> {
                    _isLoadingProfile.value = true
                }
            }
        }
    }

    /**
     * Update user profile (name and email)
     */
    fun updateProfile(name: String, email: String) {
        // Validation
        if (name.isBlank()) {
            _profileUpdateResult.value = "Name cannot be empty"
            return
        }
        if (!isValidEmail(email)) {
            _profileUpdateResult.value = "Please enter a valid email address"
            return
        }

        viewModelScope.launch {
            _isLoadingProfile.value = true
            val request = UpdateProfileRequest(name = name.trim(), email = email.trim())

            when (val result = usersRepository.updateUser(request)) {
                is Resource.Success -> {
                    _currentUser.value = result.data
                    _profileUpdateResult.value = "Profile updated successfully"
                    _isLoadingProfile.value = false
                }
                is Resource.Error -> {
                    _profileUpdateResult.value = "Failed to update profile: ${result.message}"
                    _isLoadingProfile.value = false
                }
                is Resource.Loading -> {
                    _isLoadingProfile.value = true
                }
            }
        }
    }

    /**
     * Change password
     */
    fun changePassword(currentPassword: String, newPassword: String, confirmPassword: String) {
        // Validation
        if (currentPassword.isBlank()) {
            _passwordUpdateResult.value = "Current password is required"
            return
        }
        if (newPassword.isBlank()) {
            _passwordUpdateResult.value = "New password is required"
            return
        }
        if (newPassword.length < 6) {
            _passwordUpdateResult.value = "Password must be at least 6 characters"
            return
        }
        if (newPassword != confirmPassword) {
            _passwordUpdateResult.value = "Passwords do not match"
            return
        }
        if (currentPassword == newPassword) {
            _passwordUpdateResult.value = "New password must be different from current password"
            return
        }

        viewModelScope.launch {
            _isLoadingPassword.value = true
            val request = ChangePasswordRequest(
                currentPassword = currentPassword,
                newPassword = newPassword
            )

            when (val result = authRepository.changePassword(request)) {
                is Resource.Success -> {
                    _passwordUpdateResult.value = "Password changed successfully"
                    _isLoadingPassword.value = false
                }
                is Resource.Error -> {
                    _passwordUpdateResult.value = "Failed to change password: ${result.message}"
                    _isLoadingPassword.value = false
                }
                is Resource.Loading -> {
                    _isLoadingPassword.value = true
                }
            }
        }
    }

    /**
     * Clear result messages after they've been shown
     */
    fun clearProfileResult() {
        _profileUpdateResult.value = null
    }

    fun clearPasswordResult() {
        _passwordUpdateResult.value = null
    }

    /**
     * Email validation helper
     */
    private fun isValidEmail(email: String): Boolean {
        return android.util.Patterns.EMAIL_ADDRESS.matcher(email).matches()
    }
}