import React, { useState } from "react";
import { useApp } from "@/app/context/AppContext";
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/app/components/ui/tabs";
import { SalesTrendChart } from "@/app/components/dashboard/SalesTrendChart";
import { SalesInputForm } from "@/app/components/dashboard/SalesInputForm";
import { WastageInputForm } from "@/app/components/dashboard/WastageInputForm";
import { DistributionPieChart } from "@/app/components/dashboard/DistributionPieChart";
import { IngredientTable } from "@/app/components/dashboard/IngredientTable";
import { CalendarWidget } from "@/app/components/dashboard/CalendarWidget";
import { WeatherWidget } from "@/app/components/dashboard/WeatherWidget";
import { PredictionSummary } from "@/app/components/dashboard/PredictionSummary";
import { PredictionAccuracy } from "@/app/components/dashboard/PredictionAccuracy";
import { PredictionDetail } from "@/app/components/dashboard/PredictionDetail";
import { DishesForecast } from "@/app/components/dashboard/DishesForecast";
import { WastageTrendChart } from "@/app/components/dashboard/WastageTrendChart";
import { WastageDistribution } from "@/app/components/dashboard/WastageDistribution";
import { Button } from "@/app/components/ui/button";
import { Settings } from "lucide-react";

interface DashboardProps {
  onNavigateToManagement: () => void;
}

export function Dashboard({
  onNavigateToManagement,
}: DashboardProps) {
  const context = useApp();
  if (!context) return null;
  const { user, storeSettings } = context;
  const isManager = user?.role === "manager";

  const [selectedSalesDate, setSelectedSalesDate] = useState<
    string | null
  >(null);
  const [selectedWastageDate, setSelectedWastageDate] =
    useState<string | null>(null);
  const [dateRange, setDateRange] = useState<
    "today" | "7days" | "custom"
  >("7days");

  return (
    <div className="min-h-screen">
      <main className="container mx-auto px-4 py-6 max-w-7xl">
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold text-[#1A1C18]">
              Dashboard Overview
            </h1>
          </div>

          {isManager && (
            <Button
              onClick={onNavigateToManagement}
              className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-6 gap-2"
            >
              <Settings className="w-4 h-4" />
              Manage Store
            </Button>
          )}
        </div>

        <Tabs defaultValue="overview" className="space-y-6">
          <TabsList
            className={`grid w-full ${isManager ? "grid-cols-3" : "grid-cols-4"} lg:w-auto lg:inline-grid`}
          >
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="predictions">
              Predictions
            </TabsTrigger>
            <TabsTrigger value="wastage">Wastage</TabsTrigger>
            {!isManager && (
              <TabsTrigger value="data-input">
                Data Input
              </TabsTrigger>
            )}
          </TabsList>

          {/* Overview Tab - FIXED PADDING & CONSISTENCY */}
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

            {/* Content below follows the same spacing as other tabs */}
            {selectedSalesDate ? (
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <DistributionPieChart
                  date={selectedSalesDate}
                />
                <IngredientTable date={selectedSalesDate} />
              </div>
            ) : (
              <div className="text-center py-12 text-gray-500 bg-gray-50 rounded-lg border-2 border-dashed">
                <p className="text-lg">
                  👆 Click on any bar in the chart above to view
                  detailed breakdown
                </p>
                <p className="text-sm mt-2">
                  See recipe distribution and ingredient
                  requirements for any day
                </p>
              </div>
            )}
          </TabsContent>

          {/* Predictions Tab */}
          <TabsContent
            value="predictions"
            className="space-y-6"
          >
            <PredictionSummary />
            <DishesForecast />
            <PredictionDetail />
            <PredictionAccuracy />
          </TabsContent>

          {/* Wastage Tab */}
          <TabsContent value="wastage" className="space-y-6">
            <WastageTrendChart
              dateRange={dateRange}
              onDateRangeChange={setDateRange}
              maxDays={isManager ? undefined : 7}
              onBarClick={setSelectedWastageDate}
              selectedDate={selectedWastageDate}
            />

            {selectedWastageDate ? (
              <WastageDistribution date={selectedWastageDate} />
            ) : (
              <div className="text-center py-12 text-gray-500 bg-gray-50 rounded-lg border-2 border-dashed">
                <p className="text-lg">
                  👆 Click on any bar in the chart above to view
                  wastage details
                </p>
                <p className="text-sm mt-2">
                  See carbon footprint breakdown and top wasted
                  items for any day
                </p>
              </div>
            )}
          </TabsContent>

          {/* Data Input Tab */}
          {!isManager && (
            <TabsContent
              value="data-input"
              className="space-y-6"
            >
              <Tabs defaultValue="sales" className="space-y-6">
                <TabsList className="grid w-full grid-cols-2 lg:w-auto lg:inline-grid">
                  <TabsTrigger value="sales">Sales</TabsTrigger>
                  <TabsTrigger value="wastage">
                    Wastage
                  </TabsTrigger>
                </TabsList>

                <TabsContent
                  value="sales"
                  className="space-y-6"
                >
                  <SalesInputForm />
                </TabsContent>

                <TabsContent
                  value="wastage"
                  className="space-y-6"
                >
                  <WastageInputForm />
                </TabsContent>
              </Tabs>
            </TabsContent>
          )}
        </Tabs>
      </main>
    </div>
  );
}