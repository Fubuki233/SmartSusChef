package com.smartsuschef.mobile.ui.settings

import android.os.Bundle
import android.view.MenuItem
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.isVisible
import androidx.core.widget.addTextChangedListener
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.ActivitySettingsBinding
import com.smartsuschef.mobile.util.showToast
import com.smartsuschef.mobile.util.PasswordValidator
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class SettingsActivity : AppCompatActivity() {

    private lateinit var binding: ActivitySettingsBinding
    private val viewModel: SettingsViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupToolbar()
        setupPasswordValidation()
        setupObservers()
        setupClickListeners()
    }

    private fun setupToolbar() {
        setSupportActionBar(binding.toolbar)
        supportActionBar?.apply {
            setDisplayHomeAsUpEnabled(true)
            setDisplayShowHomeEnabled(true)
            title = "Settings"
            subtitle = "Manage your profile and account security"
        }
    }

    /**
     * Setup real-time password validation
     * Shows errors as user types in the new password field
     */
    private fun setupPasswordValidation() {
        // Real-time validation for new password
        binding.tilNewPassword.editText?.addTextChangedListener { text ->
            val password = text?.toString() ?: ""

            // Don't show errors for empty field (until user starts typing)
            if (password.isEmpty()) {
                binding.tilNewPassword.error = null
                binding.tilNewPassword.isErrorEnabled = false
                return@addTextChangedListener
            }

            // Show validation errors as user types
            val result = PasswordValidator.validate(password)
            binding.tilNewPassword.error = result.errorMessage
            binding.tilNewPassword.isErrorEnabled = result.errorMessage != null
        }

        // Real-time validation for confirm password (check if matches)
        binding.tilConfirmPassword.editText?.addTextChangedListener { text ->
            val confirmPassword = text?.toString() ?: ""
            val newPassword = binding.tilNewPassword.editText?.text?.toString() ?: ""

            // Don't show errors for empty field
            if (confirmPassword.isEmpty()) {
                binding.tilConfirmPassword.error = null
                binding.tilConfirmPassword.isErrorEnabled = false
                return@addTextChangedListener
            }

            // Check if passwords match
            if (confirmPassword != newPassword) {
                binding.tilConfirmPassword.error = "Passwords do not match"
                binding.tilConfirmPassword.isErrorEnabled = true
            } else {
                binding.tilConfirmPassword.error = null
                binding.tilConfirmPassword.isErrorEnabled = false
            }
        }
    }

    private fun setupObservers() {
        // Observe current user to populate profile fields
        viewModel.currentUser.observe(this) { user ->
            user?.let {
                binding.etFullName.setText(it.name)
                binding.etEmail.setText(it.email)
            }
        }

        // Observe profile loading state
        viewModel.isLoadingProfile.observe(this) { loading ->
            binding.btnSaveProfile.isEnabled = !loading
            binding.progressBarProfile.isVisible = loading
        }

        // Observe password loading state
        viewModel.isLoadingPassword.observe(this) { loading ->
            binding.btnUpdatePassword.isEnabled = !loading
            binding.progressBarPassword.isVisible = loading
        }

        // Observe profile update results
        viewModel.profileUpdateResult.observe(this) { message ->
            message?.let {
                showToast(it)
                viewModel.clearProfileResult()
            }
        }

        // Observe password update results
        viewModel.passwordUpdateResult.observe(this) { message ->
            message?.let {
                showToast(it)
                if (it.contains("successfully", ignoreCase = true)) {
                    clearPasswordFields()
                }
                viewModel.clearPasswordResult()
            }
        }
    }

    private fun setupClickListeners() {
        // Update Password button
        binding.btnUpdatePassword.setOnClickListener {
            val currentPassword = binding.etCurrentPassword.text.toString()
            val newPassword = binding.etNewPassword.text.toString()
            val confirmPassword = binding.etConfirmPassword.text.toString()

            viewModel.changePassword(currentPassword, newPassword, confirmPassword)
        }

        // Save Profile button
        binding.btnSaveProfile.setOnClickListener {
            val name = binding.etFullName.text.toString()
            val email = binding.etEmail.text.toString()

            viewModel.updateProfile(name, email)
        }
    }

    private fun clearPasswordFields() {
        binding.etCurrentPassword.text?.clear()
        binding.etNewPassword.text?.clear()
        binding.etConfirmPassword.text?.clear()

        // Clear any error states
        binding.tilCurrentPassword.error = null
        binding.tilCurrentPassword.isErrorEnabled = false
        binding.tilNewPassword.error = null
        binding.tilNewPassword.isErrorEnabled = false
        binding.tilConfirmPassword.error = null
        binding.tilConfirmPassword.isErrorEnabled = false
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            android.R.id.home -> {
                finish()
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }
}