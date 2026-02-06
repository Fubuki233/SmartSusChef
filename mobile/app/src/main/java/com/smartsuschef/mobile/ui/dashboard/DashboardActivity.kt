package com.smartsuschef.mobile.ui.dashboard

import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import androidx.activity.viewModels
import android.content.Intent
import androidx.appcompat.app.AppCompatActivity
import android.view.View
import android.text.TextUtils

import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.ActivityDashboardBinding
import com.smartsuschef.mobile.ui.auth.LoginActivity
import com.smartsuschef.mobile.ui.settings.SettingsActivity
import com.smartsuschef.mobile.util.visible
import com.smartsuschef.mobile.util.gone
import com.smartsuschef.mobile.util.showToast

import dagger.hilt.android.AndroidEntryPoint
import androidx.navigation.NavController
import androidx.navigation.fragment.NavHostFragment
import androidx.navigation.ui.setupWithNavController

@AndroidEntryPoint
class DashboardActivity : AppCompatActivity() {
    private lateinit var binding: ActivityDashboardBinding
    private val viewModel: DashboardViewModel by viewModels()
    private lateinit var navController: NavController

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityDashboardBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setSupportActionBar(binding.toolbar)
        setupNavigation()
        observeUserInfo()
    }

    private fun setupNavigation() {
        val navHostFragment = supportFragmentManager.findFragmentById(R.id.nav_host_fragment) as NavHostFragment
        navController = navHostFragment.navController
        binding.bottomNav.setupWithNavController(navController)

        // links the toolbar to NavController so the title changes and back arrow appears automatically
        androidx.navigation.ui.NavigationUI.setupActionBarWithNavController(this, navController)

        navController.addOnDestinationChangedListener { _, destination, _ ->
            // Define main fragments (tabs in bottom navigation)
            val mainFragments = setOf(
                R.id.nav_sales,
                R.id.nav_forecast,
                R.id.nav_wastage,
                R.id.nav_input
            )

            // When we are on the main tabs, show the store info and hide back arrow
            if (destination.id in mainFragments) {
                // Hide back arrow on main fragments
                supportActionBar?.setDisplayHomeAsUpEnabled(false)

                val name = viewModel.username.value ?: "User"
                val role = viewModel.userRole.value ?: "Employee"
                supportActionBar?.subtitle = "$name | ${role.lowercase().replaceFirstChar { it.uppercase() }}"

                val storeName = viewModel.storeName.value ?: "SmartSus Chef"
                val location = viewModel.outletLocation.value ?: ""
                supportActionBar?.title = if (location.isNotEmpty()) "$storeName | $location" else storeName

                // Apply text styling for better fit
                applyToolbarTextStyling()
            } else {
                // Show back arrow on detail fragments
                supportActionBar?.setDisplayHomeAsUpEnabled(true)

                // Remove the subtitle on detail fragments so the title looks clean
                supportActionBar?.subtitle = null
            }
        }
    }

    /**
     * Apply text styling to toolbar to prevent truncation
     * - Smaller text size for title and subtitle
     * - Enable ellipsize with marquee effect
     */
    private fun applyToolbarTextStyling() {
        binding.toolbar.apply {
            // Reduce title text size to 16sp (default is 20sp)
            setTitleTextAppearance(context, R.style.ToolbarTitleTextAppearance)

            // Reduce subtitle text size to 12sp (default is 14sp)
            setSubtitleTextAppearance(context, R.style.ToolbarSubtitleTextAppearance)
        }
    }

    override fun onSupportNavigateUp(): Boolean {
        return navController.navigateUp() || super.onSupportNavigateUp()
    }

    private fun observeUserInfo() {
        // Observe loading state to show/hide progress bar
        viewModel.isLoading.observe(this) { loading ->
            if (loading) binding.progressBar.visible() else binding.progressBar.gone()
        }

        // Observe user details
        viewModel.username.observe(this) { name ->
            val role = viewModel.userRole.value ?: "Employee"
            supportActionBar?.subtitle = "$name | ${role.lowercase().replaceFirstChar { it.uppercase() }}"
            applyToolbarTextStyling()
        }

        // Observe store details
        viewModel.storeName.observe(this) { sName ->
            val location = viewModel.outletLocation.value ?: ""
            supportActionBar?.title = if (location.isNotEmpty()) "$sName | $location" else sName
            applyToolbarTextStyling()
        }
    }

    override fun onCreateOptionsMenu(menu: Menu?): Boolean {
        menuInflater.inflate(R.menu.dashboard_menu, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            R.id.action_logout -> {
                performLogout()
                true
            }
            R.id.action_settings -> {
                navigateToSettings()
                true
            }

            else -> super.onOptionsItemSelected(item)
        }
    }

    private fun performLogout() {
        viewModel.logout()
        // Navigate back to Login and clear all previous screens
        val intent = Intent(this, LoginActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }

    private fun navigateToSettings() {
        val intent = Intent(this, SettingsActivity::class.java)
        startActivity(intent)
    }
}