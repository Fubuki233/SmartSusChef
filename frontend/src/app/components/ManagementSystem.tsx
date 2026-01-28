import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Button } from '@/app/components/ui/button';
import { ArrowLeft, LogOut, ChefHat, Package, Upload, Trash2, Download, Users } from 'lucide-react';
import { RecipeManagement } from '@/app/components/management/RecipeManagement';
import { IngredientManagement } from '@/app/components/management/IngredientManagement';
import { ImportSalesData } from '@/app/components/management/ImportSalesData';
import { WastageManagement } from '@/app/components/management/WastageManagement';
import { ExportData } from '@/app/components/management/ExportData';
import { StaffManagement } from '@/app/components/management/StaffManagement';

interface ManagementSystemProps {
  onNavigateToDashboard: () => void;
}

type MenuSection = 'recipes' | 'ingredients' | 'import' | 'wastage' | 'export' | 'staff';

export function ManagementSystem({ onNavigateToDashboard }: ManagementSystemProps) {
  const { storeSettings } = useApp();
  const [activeSection, setActiveSection] = useState<MenuSection>('recipes');

  const menuItems = [
    { id: 'recipes' as MenuSection, label: 'Recipe Management', icon: ChefHat },
    { id: 'ingredients' as MenuSection, label: 'Ingredient Management', icon: Package },
    { id: 'import' as MenuSection, label: 'Import Sales Data', icon: Upload },
    { id: 'wastage' as MenuSection, label: 'Wastage Data Management', icon: Trash2 },
    { id: 'export' as MenuSection, label: 'Export Data', icon: Download },
    { id: 'staff' as MenuSection, label: 'Staff Management', icon: Users },
  ];

  return (
    <div className="min-h-[calc(100vh-64px)] flex">
      {/* Sidebar - Fully Sticky */}
      <aside className="w-64 bg-white border-r flex flex-col shadow-sm sticky top-16 h-[calc(100vh-64px)] overflow-y-auto">
        <div className="p-4 border-b bg-gray-50">
          <h2 className="font-bold text-sm text-[#4F6F52] uppercase tracking-wider">Management Module</h2>
          <p className="text-xs text-gray-500 font-medium">{storeSettings.storeName}</p>
        </div>
        
        {/* Sticky Navigation Buttons at Top */}
        <div className="p-4 border-b space-y-2 bg-white">
          <Button
            onClick={onNavigateToDashboard}
            variant="outline"
            className="w-full gap-2 rounded-[32px] border-[#4F6F52] text-[#4F6F52] hover:bg-[#4F6F52] hover:text-white"
          >
            <ArrowLeft className="w-4 h-4" />
            Exit Management
          </Button>
        </div>
        
        <nav className="flex-1 p-4">
          <ul className="space-y-2">
            {menuItems.map((item) => {
              const Icon = item.icon;
              return (
                <li key={item.id}>
                  <button
                    onClick={() => setActiveSection(item.id)}
                    className={`w-full flex items-center justify-start gap-3 px-4 py-3 rounded-[8px] transition-colors text-left ${
                      activeSection === item.id
                        ? 'bg-[#4F6F52] text-white font-medium shadow-sm'
                        : 'text-gray-700 hover:bg-gray-100'
                    }`}
                  >
                    <Icon className="w-5 h-5 flex-shrink-0" />
                    <span>{item.label}</span>
                  </button>
                </li>
              );
            })}
          </ul>
        </nav>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-auto">
        <div className="container mx-auto px-6 py-8 max-w-6xl">
          {activeSection === 'recipes' && <RecipeManagement />}
          {activeSection === 'ingredients' && <IngredientManagement />}
          {activeSection === 'import' && <ImportSalesData />}
          {activeSection === 'wastage' && <WastageManagement />}
          {activeSection === 'export' && <ExportData />}
          {activeSection === 'staff' && <StaffManagement />}
        </div>
      </main>
    </div>
  );
}