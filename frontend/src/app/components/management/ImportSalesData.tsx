import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Upload, FileText, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';
import { SalesData } from '@/app/types';

export function ImportSalesData() {
  const { importSalesData, recipes } = useApp();
  const [importing, setImporting] = useState(false);
  const [importedCount, setImportedCount] = useState(0);

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setImporting(true);

    try {
      const text = await file.text();
      let data: any[];

      if (file.name.endsWith('.json')) {
        data = JSON.parse(text);
      } else if (file.name.endsWith('.csv')) {
        data = parseCSV(text);
      } else {
        throw new Error('Unsupported file format. Please use JSON or CSV.');
      }

      // Validate and import data
      const salesData: SalesData[] = data
        .map((item, index) => {
          if (!item.date || !item.recipeId || !item.quantity) {
            console.warn(`Skipping invalid row ${index + 1}`);
            return null;
          }
          return {
            id: `import-${Date.now()}-${index}`,
            date: item.date,
            recipeId: item.recipeId,
            quantity: parseInt(item.quantity),
          };
        })
        .filter((item): item is SalesData => item !== null);

      if (salesData.length === 0) {
        throw new Error('No valid sales data found in file');
      }

      await importSalesData(salesData);
      setImportedCount(salesData.length);
      toast.success(`Successfully imported ${salesData.length} sales records`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to import file');
    } finally {
      setImporting(false);
      // Reset the input
      event.target.value = '';
    }
  };

  const parseCSV = (text: string): any[] => {
    const lines = text.trim().split('\n');
    const headers = lines[0].split(',').map((h) => h.trim());

    return lines.slice(1).map((line) => {
      const values = line.split(',').map((v) => v.trim());
      const obj: any = {};
      headers.forEach((header, index) => {
        obj[header] = values[index];
      });
      return obj;
    });
  };

  const handleDownloadTemplate = () => {
    const template = [
      { date: '2026-01-15', recipeId: '1', quantity: 45 },
      { date: '2026-01-15', recipeId: '2', quantity: 30 },
      { date: '2026-01-15', recipeId: '3', quantity: 25 },
    ];

    const blob = new Blob([JSON.stringify(template, null, 2)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'sales-data-template.json';
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleDownloadCSVTemplate = () => {
    const csv = [
      'date,recipeId,quantity',
      '2026-01-15,1,45',
      '2026-01-15,2,30',
      '2026-01-15,3,25',
    ].join('\n');

    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'sales-data-template.csv';
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold flex items-center gap-2">
          <Upload className="w-6 h-6" />
          Import Sales Data
        </h1>
        <p className="text-gray-600 mt-1">Upload sales data from CSV or JSON files</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Upload Section */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Upload File</CardTitle>
            <CardDescription>
              Import sales data from a JSON or CSV file
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center">
              <Upload className="w-12 h-12 mx-auto text-gray-400 mb-3" />
              <p className="text-sm text-gray-600 mb-4">
                Drag and drop your file here or click to browse
              </p>
              <label htmlFor="file-upload">
                <Button asChild disabled={importing}>
                  <span className="cursor-pointer">
                    {importing ? 'Importing...' : 'Select File'}
                  </span>
                </Button>
              </label>
              <input
                id="file-upload"
                type="file"
                accept=".json,.csv"
                onChange={handleFileUpload}
                className="hidden"
              />
            </div>

            {importedCount > 0 && (
              <div className="flex items-center gap-2 p-3 bg-green-50 border border-green-200 rounded-lg">
                <CheckCircle2 className="w-5 h-5 text-green-600" />
                <p className="text-sm text-green-800">
                  Last import: {importedCount} records
                </p>
              </div>
            )}

            <div className="text-xs text-gray-500 space-y-1">
              <p>Supported formats:</p>
              <ul className="list-disc list-inside ml-2">
                <li>JSON (.json)</li>
                <li>CSV (.csv)</li>
              </ul>
            </div>
          </CardContent>
        </Card>

        {/* Template Section */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Download Template</CardTitle>
            <CardDescription>
              Get a template file to format your data correctly
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-3">
              <Button
                onClick={handleDownloadTemplate}
                variant="outline"
                className="w-full gap-2"
              >
                <FileText className="w-4 h-4" />
                Download JSON Template
              </Button>
              <Button
                onClick={handleDownloadCSVTemplate}
                variant="outline"
                className="w-full gap-2"
              >
                <FileText className="w-4 h-4" />
                Download CSV Template
              </Button>
            </div>

            <div className="border rounded-lg p-4 bg-gray-50">
              <h4 className="font-medium text-sm mb-2">File Format:</h4>
              <div className="text-xs text-gray-600 space-y-1">
                <p><strong>date:</strong> YYYY-MM-DD format</p>
                <p><strong>recipeId:</strong> Recipe ID from system</p>
                <p><strong>quantity:</strong> Number of dishes sold</p>
              </div>
            </div>

            <div className="border rounded-lg p-4 bg-blue-50">
              <h4 className="font-medium text-sm mb-2">Available Recipe IDs:</h4>
              <div className="text-xs text-gray-600 space-y-1">
                {recipes.map((recipe) => (
                  <p key={recipe.id}>
                    <strong>{recipe.id}:</strong> {recipe.name}
                  </p>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Example Section */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Example Data</CardTitle>
          <CardDescription>Sample format for your import file</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <h4 className="font-medium text-sm mb-2">JSON Format:</h4>
              <pre className="bg-gray-900 text-gray-100 p-4 rounded-lg text-xs overflow-x-auto">
                {`[
  {
    "date": "2026-01-15",
    "recipeId": "1",
    "quantity": 45
  },
  {
    "date": "2026-01-15",
    "recipeId": "2",
    "quantity": 30
  }
]`}
              </pre>
            </div>
            <div>
              <h4 className="font-medium text-sm mb-2">CSV Format:</h4>
              <pre className="bg-gray-900 text-gray-100 p-4 rounded-lg text-xs overflow-x-auto">
                {`date,recipeId,quantity
2026-01-15,1,45
2026-01-15,2,30
2026-01-15,3,25`}
              </pre>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
