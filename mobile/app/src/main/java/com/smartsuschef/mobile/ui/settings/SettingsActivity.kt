package com.smartsuschef.mobile.ui.settings

import android.os.Bundle
import android.view.MenuItem
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.isVisible
import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.ActivitySettingsBinding
import com.smartsuschef.mobile.util.showToast
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