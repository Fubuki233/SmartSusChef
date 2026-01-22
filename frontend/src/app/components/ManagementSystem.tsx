import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Button } from '@/app/components/ui/button';
import { ArrowLeft, LogOut, ChefHat, Package, Upload, Trash2, Download } from 'lucide-react';
import { RecipeManagement } from '@/app/components/management/RecipeManagement';
import { IngredientManagement } from '@/app/components/management/IngredientManagement';
import { ImportSalesData } from '@/app/components/management/ImportSalesData';
import { WastageManagement } from '@/app/components/management/WastageManagement';
import { ExportData } from '@/app/components/management/ExportData';

interface ManagementSystemProps {
  onNavigateToDashboard: () => void;
}

type MenuSection = 'recipes' | 'ingredients' | 'import' | 'wastage' | 'export';

export function ManagementSystem({ onNavigateToDashboard }: ManagementSystemProps) {
  const { user, logout } = useApp();
  const [activeSection, setActiveSection] = useState<MenuSection>('recipes');

  const menuItems = [
    { id: 'recipes' as MenuSection, label: 'Recipe Management', icon: ChefHat },
    { id: 'ingredients' as MenuSection, label: 'Ingredient Management', icon: Package },
    { id: 'import' as MenuSection, label: 'Import Sales Data', icon: Upload },
    { id: 'wastage' as MenuSection, label: 'Wastage Management', icon: Trash2 },
    { id: 'export' as MenuSection, label: 'Export Data', icon: Download },
  ];

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Sidebar */}
      <aside className="w-64 bg-white border-r flex flex-col">
        <div className="p-4 border-b">
          <h2 className="font-semibold text-lg">Management System</h2>
          <p className="text-sm text-gray-600">{user?.name}</p>
        </div>
        
        <nav className="flex-1 p-4">
          <ul className="space-y-2">
            {menuItems.map((item) => {
              const Icon = item.icon;
              return (
                <li key={item.id}>
                  <button
                    onClick={() => setActiveSection(item.id)}
                    className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                      activeSection === item.id
                        ? 'bg-green-50 text-green-700 font-medium'
                        : 'text-gray-700 hover:bg-gray-100'
                    }`}
                  >
                    <Icon className="w-5 h-5" />
                    {item.label}
                  </button>
                </li>
              );
            })}
          </ul>
        </nav>

        <div className="p-4 border-t space-y-2">
          <Button
            onClick={onNavigateToDashboard}
            variant="outline"
            className="w-full gap-2"
          >
            <ArrowLeft className="w-4 h-4" />
            Back to Dashboard
          </Button>
          <Button onClick={logout} variant="outline" className="w-full gap-2">
            <LogOut className="w-4 h-4" />
            Logout
          </Button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto">
        <div className="container mx-auto px-6 py-8 max-w-6xl">
          {activeSection === 'recipes' && <RecipeManagement />}
          {activeSection === 'ingredients' && <IngredientManagement />}
          {activeSection === 'import' && <ImportSalesData />}
          {activeSection === 'wastage' && <WastageManagement />}
          {activeSection === 'export' && <ExportData />}
        </div>
      </main>
    </div>
  );
}
