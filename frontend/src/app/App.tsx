import React, { useState } from 'react';
import { AppProvider, useApp } from '@/app/context/AppContext';
import { LoginPage } from '@/app/components/LoginPage';
import { Dashboard } from '@/app/components/Dashboard';
import { ManagementSystem } from '@/app/components/ManagementSystem';
import { Toaster } from '@/app/components/ui/sonner';

type View = 'dashboard' | 'management';

function AppContent() {
  const { user } = useApp();
  const [currentView, setCurrentView] = useState<View>('dashboard');

  if (!user) {
    return <LoginPage />;
  }

  return (
    <>
      {currentView === 'dashboard' ? (
        <Dashboard onNavigateToManagement={() => setCurrentView('management')} />
      ) : (
        <ManagementSystem onNavigateToDashboard={() => setCurrentView('dashboard')} />
      )}
      <Toaster position="top-right" />
    </>
  );
}

export default function App() {
  return (
    <AppProvider>
      <AppContent />
    </AppProvider>
  );
}
