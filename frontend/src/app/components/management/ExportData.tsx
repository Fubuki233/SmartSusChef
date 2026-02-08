import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Download, FileSpreadsheet, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';

export function ExportData() {
  const { exportData, salesData, wastageData, forecastData } = useApp();
  const [lastExport, setLastExport] = useState<string | null>(null);
  const [isExporting, setIsExporting] = useState<string | null>(null);

  const handleExportCSV = async (type: 'sales' | 'wastage' | 'forecast') => {
    setIsExporting(`csv-${type}`);
    try {
      await exportData(type);
      setLastExport(`csv-${type}`);
      toast.success(`${type.charAt(0).toUpperCase() + type.slice(1)} data exported as CSV successfully`);
    } finally {
      setIsExporting(null);
    }
  };

  const csvExportOptions = [
    {
      type: 'sales' as const,
      title: 'Sales Data',
      description: 'Export all sales records with dates and quantities',
      count: salesData.length,
      color: 'text-green-600',
      bgColor: 'bg-green-50',
      borderColor: 'border-green-200',
    },
    {
      type: 'wastage' as const,
      title: 'Wastage Data',
      description: 'Export wastage records with ingredient details',
      count: wastageData.length,
      color: 'text-orange-600',
      bgColor: 'bg-orange-50',
      borderColor: 'border-orange-200',
    },
    {
      type: 'forecast' as const,
      title: 'Forecast Data',
      description: 'Export predicted sales for upcoming days',
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
        <p className="text-gray-600 mt-1">Export raw data as CSV files</p>
      </div>

      <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {csvExportOptions.map((option) => (
              <Card
                key={option.type}
                className={`${lastExport === `csv-${option.type}` ? `border-2 ${option.borderColor}` : ''}`}
              >
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className={`${option.bgColor} p-3 rounded-lg`}>
                      <FileSpreadsheet className={`w-6 h-6 ${option.color}`} />
                    </div>
                    {lastExport === `csv-${option.type}` && (
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
                    onClick={() => handleExportCSV(option.type)}
                    disabled={isExporting === `csv-${option.type}`}
                    className="w-full gap-2 bg-[#4F6F52] hover:bg-[#3D563F] text-white"
                  >
                    <Download className="w-4 h-4" />
                    Export as CSV
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">CSV Export Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <h4 className="font-medium mb-2">File Format</h4>
                  <p className="text-sm text-gray-600">
                    All data is exported in CSV (Comma-Separated Values) format, compatible with Excel,
                    Google Sheets, and other spreadsheet applications.
                  </p>
                </div>
                <div>
                  <h4 className="font-medium mb-2">Data Contents</h4>
                  <ul className="text-sm text-gray-600 space-y-1">
                    <li>• Sales: Date, Recipe Name, Quantity Sold</li>
                    <li>• Wastage: Date, Ingredient Name, Quantity Wasted, Unit</li>
                    <li>• Forecast: Date, Recipe Name, Predicted Quantity</li>
                  </ul>
                </div>
              </div>
            </CardContent>
          </Card>
      </div>
    </div>
  );
}
