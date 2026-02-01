package com.smartsuschef.mobile.ui.dashboard

import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import androidx.activity.viewModels
import android.content.Intent
import androidx.appcompat.app.AppCompatActivity
import android.view.View

import com.smartsuschef.mobile.R
import com.smartsuschef.mobile.databinding.ActivityDashboardBinding
import com.smartsuschef.mobile.ui.auth.LoginActivity
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
            // When we are on the main tabs, show the store info
            if (destination.id == R.id.nav_sales || destination.id == R.id.nav_forecast ||
                destination.id == R.id.nav_wastage || destination.id == R.id.nav_input) {

                val name = viewModel.username.value ?: "User"
                val role = viewModel.userRole.value ?: "Employee"
                supportActionBar?.subtitle = "$name | ${role.lowercase().replaceFirstChar { it.uppercase() }}"

                val storeName = viewModel.storeName.value ?: "SmartSus Chef"
                val location = viewModel.outletLocation.value ?: ""
                supportActionBar?.title = if (location.isNotEmpty()) "$storeName | $location" else storeName
            } else {
                // Remove the subtitle on SalesDtail so the title in this Fragment looks clean
                supportActionBar?.subtitle = null
            }
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
        }

        // Observe store details
        viewModel.storeName.observe(this) { sName ->
            val location = viewModel.outletLocation.value ?: ""
            supportActionBar?.title = if (location.isNotEmpty()) "$sName | $location" else sName
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
                // navigateToSettings()
                showToast("To be directed to Settings: Coming Soon")
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
        // val intent = Intent(this, SettingsActivity::class.java)
        // startActivity(intent)
    }
}