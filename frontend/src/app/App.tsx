import React, { useState } from 'react';
import { AppProvider, useApp } from '@/app/context/AppContext';
import { LoginPage } from '@/app/components/LoginPage';
import { RegisterPage } from '@/app/components/RegisterPage';
import { StoreSetupPage } from '@/app/components/StoreSetupPage';
import { Dashboard } from '@/app/components/Dashboard';
import { ManagementSystem } from '@/app/components/ManagementSystem';
import { StoreSettings } from '@/app/components/management/StoreSettings';
import { Header } from '@/app/components/Header';
import { Toaster } from 'sonner';

type View = 'dashboard' | 'management' | 'settings';
type AuthView = 'login' | 'register';

function MainContent() {
  const context = useApp();

  if (!context) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#F9FBF7]">
        <div className="text-center p-8 bg-white rounded-lg shadow-sm border">
          <p className="text-gray-600 animate-pulse">Initializing SmartSus Chef...</p>
        </div>
      </div>
    );
  }

  const { user, storeSetupRequired, completeStoreSetup } = context;
  const [currentView, setCurrentView] = useState<View>('dashboard');
  const [authView, setAuthView] = useState<AuthView>('login');

  // Not logged in - show login or register
  if (!user) {
    if (authView === 'register') {
      return (
        <>
          <RegisterPage
            onRegisterSuccess={() => {
              // After registration, user will need to set up store
              // The context will automatically set storeSetupRequired to true
            }}
            onBackToLogin={() => setAuthView('login')}
          />
          <Toaster position="top-right" />
        </>
      );
    }
    return (
      <>
        <LoginPage onNavigateToRegister={() => setAuthView('register')} />
        <Toaster position="top-right" />
      </>
    );
  }

  // Logged in but store setup required
  if (storeSetupRequired) {
    return (
      <>
        <StoreSetupPage onSetupComplete={completeStoreSetup} />
        <Toaster position="top-right" />
      </>
    );
  }

  // Fully logged in with store set up
  return (
    <div className="min-h-screen bg-[#F9FBF7]">
      <Header
        onNavigateToSettings={() => setCurrentView('settings')}
        showSettingsLink={true}
      />

      <main>
        {currentView === 'dashboard' && (
          <Dashboard onNavigateToManagement={() => setCurrentView('management')} />
        )}

        {currentView === 'management' && (
          <ManagementSystem onNavigateToDashboard={() => setCurrentView('dashboard')} />
        )}

        {currentView === 'settings' && (
          <div className="container mx-auto px-6 py-8">
            <StoreSettings onBack={() => setCurrentView('dashboard')} />
          </div>
        )}
      </main>

      <Toaster position="top-right" />
    </div>
  );
}

// Ensure AppProvider is NOT null before using it
export default function App() {
  if (typeof AppProvider === 'undefined') {
    return <div>Error: AppProvider is undefined. Check imports.</div>;
  }

  return (
    <AppProvider>
      <MainContent />
    </AppProvider>
  );
}
