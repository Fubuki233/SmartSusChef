import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/app/components/ui/tabs';
import { Download, FileSpreadsheet, FileText, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';
import { jsPDF } from 'jspdf';

export function ExportData() {
  const { exportData, salesData, wastageData, forecastData } = useApp();
  const [lastExport, setLastExport] = useState<string | null>(null);

  const handleExportCSV = (type: 'sales' | 'wastage' | 'forecast') => {
    exportData(type);
    setLastExport(`csv-${type}`);
    toast.success(`${type.charAt(0).toUpperCase() + type.slice(1)} data exported as CSV successfully`);
  };

  const handleExportReport = (reportType: 'sales-trend' | 'predictions' | 'wastage-trend') => {
    const reportNames = {
      'sales-trend': 'Sales Trend Report',
      'predictions': 'Predictions Report',
      'wastage-trend': 'Wastage Trend Report',
    };
    const doc = new jsPDF();
    const title = reportNames[reportType];

    doc.setFontSize(16);
    doc.text(title, 14, 20);
    doc.setFontSize(10);
    doc.text(`Generated: ${new Date().toLocaleString()}`, 14, 28);

    const lines: string[] = [];
    if (reportType === 'sales-trend') {
      lines.push(`Total sales records: ${salesData.length}`);
    }
    if (reportType === 'predictions') {
      lines.push(`Forecast records: ${forecastData.length}`);
    }
    if (reportType === 'wastage-trend') {
      lines.push(`Wastage records: ${wastageData.length}`);
    }

    let y = 40;
    lines.forEach((line) => {
      doc.text(line, 14, y);
      y += 8;
    });

    const filename = `${reportType}_report.pdf`;
    doc.save(filename);
    setLastExport(`pdf-${reportType}`);
    toast.success(`${title} exported as PDF successfully`);
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

  const pdfReportOptions = [
    {
      type: 'sales-trend' as const,
      title: 'Sales Trend Report',
      description: 'Comprehensive sales analysis with charts and insights',
      color: 'text-[#4F6F52]',
      bgColor: 'bg-[#E6EFE0]',
      borderColor: 'border-[#4F6F52]',
    },
    {
      type: 'predictions' as const,
      title: 'Predictions Report',
      description: 'AI forecast analysis for dishes and ingredients',
      color: 'text-[#8E7AB5]',
      bgColor: 'bg-purple-50',
      borderColor: 'border-[#8E7AB5]',
    },
    {
      type: 'wastage-trend' as const,
      title: 'Wastage Trend Report',
      description: 'Wastage patterns and carbon footprint analysis',
      color: 'text-[#E67E22]',
      bgColor: 'bg-orange-50',
      borderColor: 'border-[#E67E22]',
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold flex items-center gap-2">
          <Download className="w-6 h-6" />
          Export Data
        </h1>
        <p className="text-gray-600 mt-1">Export raw data as CSV or generate comprehensive PDF reports</p>
      </div>

      <Tabs defaultValue="csv" className="space-y-6">
        <TabsList className="grid w-full max-w-md grid-cols-2">
          <TabsTrigger value="csv">CSV Export</TabsTrigger>
          <TabsTrigger value="pdf">PDF Reports</TabsTrigger>
        </TabsList>

        {/* CSV Export Tab */}
        <TabsContent value="csv" className="space-y-6">
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
        </TabsContent>

        {/* PDF Reports Tab */}
        <TabsContent value="pdf" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {pdfReportOptions.map((option) => (
              <Card
                key={option.type}
                className={`${lastExport === `pdf-${option.type}` ? `border-2 ${option.borderColor}` : ''}`}
              >
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className={`${option.bgColor} p-3 rounded-lg`}>
                      <FileText className={`w-6 h-6 ${option.color}`} />
                    </div>
                    {lastExport === `pdf-${option.type}` && (
                      <CheckCircle2 className="w-5 h-5 text-green-600" />
                    )}
                  </div>
                  <CardTitle className="text-lg mt-4">{option.title}</CardTitle>
                  <CardDescription>{option.description}</CardDescription>
                </CardHeader>
                <CardContent>
                  <Button
                    onClick={() => handleExportReport(option.type)}
                    className="w-full gap-2 bg-[#4F6F52] hover:bg-[#3D563F] text-white"
                  >
                    <Download className="w-4 h-4" />
                    Export PDF Report
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">PDF Report Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div>
                  <h4 className="font-medium mb-2">Sales Trend Report</h4>
                  <ul className="text-sm text-gray-600 space-y-1">
                    <li>• Daily/Weekly/Monthly trends</li>
                    <li>• Top-performing dishes</li>
                    <li>• Revenue analysis</li>
                    <li>• Visual charts and graphs</li>
                  </ul>
                </div>
                <div>
                  <h4 className="font-medium mb-2">Predictions Report</h4>
                  <ul className="text-sm text-gray-600 space-y-1">
                    <li>• 7-day forecast breakdown</li>
                    <li>• Dish-level predictions</li>
                    <li>• Ingredient requirements</li>
                    <li>• Confidence intervals</li>
                  </ul>
                </div>
                <div>
                  <h4 className="font-medium mb-2">Wastage Trend Report</h4>
                  <ul className="text-sm text-gray-600 space-y-1">
                    <li>• Wastage patterns over time</li>
                    <li>• Ingredient-level breakdown</li>
                    <li>• Carbon footprint analysis</li>
                    <li>• Cost impact assessment</li>
                  </ul>
                </div>
              </div>

              <div className="border-t pt-4">
                <h4 className="font-medium mb-2">Report Features</h4>
                <ul className="text-sm text-gray-600 space-y-1">
                  <li>• Professional PDF format ready for printing or sharing</li>
                  <li>• Includes SmartSus Chef branding and report metadata</li>
                  <li>• Data visualizations with charts and tables</li>
                  <li>• Executive summary with key insights and recommendations</li>
                </ul>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
