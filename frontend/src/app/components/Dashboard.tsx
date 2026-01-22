import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/app/components/ui/tabs';
import { Card, CardContent } from '@/app/components/ui/card';
import { SalesTrendChart } from '@/app/components/dashboard/SalesTrendChart';
import { DataInputForm } from '@/app/components/dashboard/DataInputForm';
import { DistributionPieChart } from '@/app/components/dashboard/DistributionPieChart';
import { IngredientTable } from '@/app/components/dashboard/IngredientTable';
import { CalendarWidget } from '@/app/components/dashboard/CalendarWidget';
import { WeatherWidget } from '@/app/components/dashboard/WeatherWidget';
import { PredictionSummary } from '@/app/components/dashboard/PredictionSummary';
import { PredictionDetail } from '@/app/components/dashboard/PredictionDetail';
import { WastageTrendChart } from '@/app/components/dashboard/WastageTrendChart';
import { WastageDistribution } from '@/app/components/dashboard/WastageDistribution';
import { Button } from '@/app/components/ui/button';
import { LogOut, Settings, BarChart3, RefreshCw } from 'lucide-react';

interface DashboardProps {
  onNavigateToManagement: () => void;
}

export function Dashboard({ onNavigateToManagement }: DashboardProps) {
  const { user, logout, refreshData, isLoading } = useApp();
  const isManager = user?.role === 'manager';
  const [isRefreshing, setIsRefreshing] = useState(false);

  const [selectedSalesDate, setSelectedSalesDate] = useState<string | null>(null);
  const [selectedWastageDate, setSelectedWastageDate] = useState<string | null>(null);
  const [dateRange, setDateRange] = useState<'today' | '7days' | 'custom'>('7days');

  const handleRefresh = async () => {
    setIsRefreshing(true);
    try {
      await refreshData();
    } finally {
      setIsRefreshing(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b sticky top-0 z-10">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="bg-green-600 p-2 rounded-lg">
                <BarChart3 className="w-6 h-6 text-white" />
              </div>
              <div>
                <h1 className="text-xl font-semibold">SmartSus Chef</h1>
                <p className="text-sm text-gray-600">
                  {user?.name} ({isManager ? 'Manager' : 'Employee'})
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="icon"
                onClick={handleRefresh}
                disabled={isRefreshing}
                title="Refresh data"
              >
                <RefreshCw className={`w-4 h-4 ${isRefreshing ? 'animate-spin' : ''}`} />
              </Button>
              {isManager && (
                <Button
                  variant="outline"
                  onClick={onNavigateToManagement}
                  className="gap-2"
                >
                  <Settings className="w-4 h-4" />
                  Management
                </Button>
              )}
              <Button variant="outline" onClick={logout} className="gap-2">
                <LogOut className="w-4 h-4" />
                Logout
              </Button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-6 max-w-7xl">
        <Tabs defaultValue="overview" className="space-y-6">
          <TabsList className="grid w-full grid-cols-4 lg:w-auto lg:inline-grid">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="sales">Sales</TabsTrigger>
            <TabsTrigger value="predictions">Predictions</TabsTrigger>
            <TabsTrigger value="wastage">Wastage</TabsTrigger>
          </TabsList>

          {/* Overview Tab */}
          <TabsContent value="overview" className="space-y-6">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              <div className="lg:col-span-2">
                <SalesTrendChart
                  dateRange={dateRange}
                  onDateRangeChange={setDateRange}
                  maxDays={isManager ? undefined : 7}
                  onBarClick={setSelectedSalesDate}
                  selectedDate={selectedSalesDate}
                />
              </div>
              <div className="space-y-4">
                <CalendarWidget />
                <WeatherWidget />
              </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {selectedSalesDate && (
                <>
                  <DistributionPieChart date={selectedSalesDate} />
                  <IngredientTable date={selectedSalesDate} />
                </>
              )}
              {!selectedSalesDate && (
                <div className="lg:col-span-2 text-center py-12 text-gray-500 bg-gray-50 rounded-lg border-2 border-dashed">
                  <p className="text-lg">👆 Click on any bar in the chart above to view detailed breakdown</p>
                  <p className="text-sm mt-2">See recipe distribution and ingredient requirements for any day</p>
                </div>
              )}
            </div>
          </TabsContent>

          {/* Sales Tab */}
          <TabsContent value="sales" className="space-y-6">
            <DataInputForm maxDaysBack={isManager ? undefined : 0} />

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              <div className="lg:col-span-2">
                <SalesTrendChart
                  dateRange={dateRange}
                  onDateRangeChange={setDateRange}
                  maxDays={isManager ? undefined : 7}
                  onBarClick={setSelectedSalesDate}
                  selectedDate={selectedSalesDate}
                />
              </div>
              <div>
                {selectedSalesDate ? (
                  <DistributionPieChart date={selectedSalesDate} />
                ) : (
                  <Card className="h-full flex items-center justify-center">
                    <CardContent className="text-center text-gray-500 py-8">
                      <p>Click on a bar to view</p>
                      <p className="text-sm mt-1">recipe distribution</p>
                    </CardContent>
                  </Card>
                )}
              </div>
            </div>

            {selectedSalesDate && (
              <IngredientTable date={selectedSalesDate} />
            )}
          </TabsContent>

          {/* Predictions Tab */}
          <TabsContent value="predictions" className="space-y-6">
            {isManager && <PredictionSummary />}
            <PredictionDetail />
          </TabsContent>

          {/* Wastage Tab */}
          <TabsContent value="wastage" className="space-y-6">
            <WastageTrendChart
              dateRange={dateRange}
              onDateRangeChange={setDateRange}
              maxDays={isManager ? undefined : 7}
              onBarClick={setSelectedWastageDate}
            />

            {selectedWastageDate && (
              <WastageDistribution date={selectedWastageDate} />
            )}
          </TabsContent>
        </Tabs>
      </main>
    </div>
  );
}