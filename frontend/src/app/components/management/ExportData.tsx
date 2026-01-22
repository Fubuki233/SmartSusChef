import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Download, FileJson, FileText, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';

export function ExportData() {
  const { exportData, salesData, wastageData, forecastData } = useApp();
  const [lastExport, setLastExport] = useState<string | null>(null);

  const handleExport = (type: 'sales' | 'wastage' | 'forecast') => {
    exportData(type);
    setLastExport(type);
    toast.success(`${type.charAt(0).toUpperCase() + type.slice(1)} data exported successfully`);
  };

  const exportOptions = [
    {
      type: 'sales' as const,
      title: 'Sales Data',
      description: 'Export all sales records with dates and quantities',
      icon: FileJson,
      count: salesData.length,
      color: 'text-green-600',
      bgColor: 'bg-green-50',
      borderColor: 'border-green-200',
    },
    {
      type: 'wastage' as const,
      title: 'Wastage Data',
      description: 'Export wastage records with ingredient details',
      icon: FileText,
      count: wastageData.length,
      color: 'text-orange-600',
      bgColor: 'bg-orange-50',
      borderColor: 'border-orange-200',
    },
    {
      type: 'forecast' as const,
      title: 'Forecast Data',
      description: 'Export predicted sales for upcoming days',
      icon: FileJson,
      count: forecastData.length,
      color: 'text-purple-600',
      bgColor: 'bg-purple-50',
      borderColor: 'border-purple-200',
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold flex items-center gap-2">
          <Download className="w-6 h-6" />
          Export Data
        </h1>
        <p className="text-gray-600 mt-1">Download data in JSON format for backup or analysis</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {exportOptions.map((option) => {
          const Icon = option.icon;
          return (
            <Card
              key={option.type}
              className={`${lastExport === option.type ? option.borderColor : ''}`}
            >
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className={`${option.bgColor} p-3 rounded-lg`}>
                    <Icon className={`w-6 h-6 ${option.color}`} />
                  </div>
                  {lastExport === option.type && (
                    <CheckCircle2 className="w-5 h-5 text-green-600" />
                  )}
                </div>
                <CardTitle className="text-lg mt-4">{option.title}</CardTitle>
                <CardDescription>{option.description}</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-baseline gap-2">
                  <span className="text-3xl font-bold">{option.count}</span>
                  <span className="text-gray-600">records</span>
                </div>
                <Button
                  onClick={() => handleExport(option.type)}
                  className="w-full gap-2"
                  variant="outline"
                >
                  <Download className="w-4 h-4" />
                  Export as JSON
                </Button>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Information Section */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Export Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <h4 className="font-medium mb-2">File Format</h4>
              <p className="text-sm text-gray-600">
                All data is exported in JSON format, which can be easily imported into other
                systems or analyzed using spreadsheet software.
              </p>
            </div>
            <div>
              <h4 className="font-medium mb-2">Data Contents</h4>
              <ul className="text-sm text-gray-600 space-y-1">
                <li>• Sales: Date, Recipe ID, Quantity</li>
                <li>• Wastage: Date, Ingredient ID, Quantity</li>
                <li>• Forecast: Date, Recipe ID, Predicted Quantity</li>
              </ul>
            </div>
          </div>

          <div className="border-t pt-4">
            <h4 className="font-medium mb-2">Usage Tips</h4>
            <ul className="text-sm text-gray-600 space-y-1">
              <li>• Regular exports help maintain data backups</li>
              <li>• JSON files can be imported back using the Import feature</li>
              <li>• Use exported data for external reporting and analysis</li>
              <li>• Share forecast data with suppliers for better inventory planning</li>
            </ul>
          </div>
        </CardContent>
      </Card>

      {/* Example Output */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Example Export Format</CardTitle>
          <CardDescription>Sample JSON structure for sales data</CardDescription>
        </CardHeader>
        <CardContent>
          <pre className="bg-gray-900 text-gray-100 p-4 rounded-lg text-xs overflow-x-auto">
{`[
  {
    "id": "sales-2026-01-15-1",
    "date": "2026-01-15",
    "recipeId": "1",
    "quantity": 45
  },
  {
    "id": "sales-2026-01-15-2",
    "date": "2026-01-15",
    "recipeId": "2",
    "quantity": 30
  }
]`}
          </pre>
        </CardContent>
      </Card>
    </div>
  );
}
