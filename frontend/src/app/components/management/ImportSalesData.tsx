import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Upload, Download, AlertCircle, CheckCircle, ArrowRight, ChefHat, FileText } from 'lucide-react';
import { toast } from 'sonner';
import { parse } from 'papaparse';
import { CSVValidator } from '@/app/utils/csvValidator';
import { CSVValidationError } from '@/app/types/csv';

interface CSVRow {
  Date: string;
  Dish_Name: string;
  Quantity_Sold: string;
  Total_Revenue_SGD?: string;
  Unit_Cost_SGD?: string;
}

export function ImportSalesData() {
  const { recipes, addSalesData } = useApp();
  const [isDragging, setIsDragging] = useState(false);
  const [csvData, setCsvData] = useState<CSVRow[]>([]);
  const [errors, setErrors] = useState<CSVValidationError[]>([]);
  const [showPreview, setShowPreview] = useState(false);

  const handleDownloadTemplate = () => {
    const template = [
      ['Date', 'Dish_Name', 'Quantity_Sold', 'Total_Revenue_SGD', 'Unit_Cost_SGD'],
      ['2026-01-20', 'Laksa', '85', '510.00', '6.00'],
      ['2026-01-20', 'Hainanese Chicken Rice', '120', '660.00', '5.50'],
      ['2026-01-21', 'Laksa', '70', '420.00', '6.00'],
      ['2026-01-21', 'Chicken Salad', '45', '360.00', '8.00'],
    ];

    const csv = template.map(row => row.join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'smartsus_sales_template.csv';
    a.click();
    URL.revokeObjectURL(url);
    toast.success('Template downloaded successfully');
  };

  const handleFileUpload = (file: File) => {
    if (!file.name.endsWith('.csv')) {
      toast.error('Please upload a CSV file');
      return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result as string;
      
      parse<CSVRow>(text, {
        header: true,
        skipEmptyLines: true,
        complete: (results) => {
          if (results.data.length === 0) {
            toast.error('CSV file is empty');
            return;
          }

          // Use CSV Validator
          const validator = new CSVValidator(recipes);
          const validationResult = validator.validate(results.data);
          
          if (validationResult.errors.length > 50) {
            // High volume failure - download error log
            const errorLog = CSVValidator.generateErrorLog(validationResult.errors);
            
            const blob = new Blob([errorLog], { type: 'text/plain' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = 'smartsus_error_log.txt';
            a.click();
            URL.revokeObjectURL(url);

            toast.error(`Massive data mismatch detected. ${validationResult.errors.length}+ errors found. Error log downloaded.`);
            setErrors(validationResult.errors.slice(0, 50)); // Show first 50
            setCsvData([]);
            setShowPreview(false);
            return;
          }

          if (validationResult.errors.length > 0) {
            setErrors(validationResult.errors);
            setCsvData(results.data);
            setShowPreview(true);
            toast.error(`Upload failed: ${validationResult.errors.length} issue${validationResult.errors.length > 1 ? 's' : ''} detected`);
          } else {
            setCsvData(results.data);
            setErrors([]);
            setShowPreview(true);
            
            if (validationResult.warnings.length > 0) {
              toast.success(`CSV validated successfully! ${validationResult.warnings.length} value(s) auto-corrected.`);
            } else {
              toast.success('CSV validated successfully! Review data before importing.');
            }
          }
        },
        error: (error) => {
          toast.error(`Failed to parse CSV: ${error.message}`);
        },
      });
    };

    reader.readAsText(file);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    
    const file = e.dataTransfer.files[0];
    if (file) {
      handleFileUpload(file);
    }
  };

  const handleImport = async () => {
    if (errors.length > 0) {
      toast.error('Please fix all errors before importing');
      return;
    }

    const recipeMap = new Map(recipes.map(r => [r.name.toLowerCase(), r.id]));
    const salesToImport: { date: string; recipeId: string; quantity: number }[] = [];

    csvData.forEach((row) => {
      const recipeId = recipeMap.get(row.Dish_Name.trim().toLowerCase());
      if (recipeId) {
        const quantity = parseFloat(row.Quantity_Sold);
        salesToImport.push({
          date: row.Date,
          recipeId,
          quantity,
        });
      }
    });

    try {
      // Import all sales data
      for (const sale of salesToImport) {
        await addSalesData(sale);
      }
      toast.success(`Successfully imported ${salesToImport.length} sales records`);
      setCsvData([]);
      setErrors([]);
      setShowPreview(false);
    } catch (error) {
      toast.error('Failed to import sales data');
    }
  };

  const handleCancel = () => {
    setCsvData([]);
    setErrors([]);
    setShowPreview(false);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold flex items-center gap-2 text-[#1A1C18] md:text-3xl lg:text-3xl">
          <Upload className="w-6 h-6 text-[#4F6F52]" />
          Import Sales Data
        </h1>
        <p className="text-gray-600 mt-1">Upload CSV file from your POS system</p>
      </div>

      {/* Template Download */}
      <Card className="border-[#4F6F52]/20">
        <CardHeader>
          <CardTitle>Step 1: Download Template</CardTitle>
          <CardDescription>
            Use our standard template to ensure your data is formatted correctly
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button onClick={handleDownloadTemplate} variant="outline" className="gap-2">
            <Download className="w-4 h-4" />
            Download Sample Template (.csv)
          </Button>
          <div className="mt-4 p-4 bg-[#E6EFE0] rounded-lg border border-[#4F6F52]/20">
            <p className="text-sm font-medium text-[#1A1C18] mb-2">Required Columns:</p>
            <ul className="text-sm text-gray-700 space-y-1">
              <li>• <strong>Date</strong>: YYYY-MM-DD format (e.g., 2026-01-20)</li>
              <li>• <strong>Dish_Name</strong>: Must match exactly with recipes in your database</li>
              <li>• <strong>Quantity_Sold</strong>: Number of dishes sold</li>
              <li>• <strong>Total_Revenue_SGD</strong>: Optional (will auto-correct "S$ 5" to "5.00")</li>
              <li>• <strong>Unit_Cost_SGD</strong>: Optional</li>
            </ul>
          </div>
        </CardContent>
      </Card>

      {/* Upload Zone */}
      <Card className="border-[#4F6F52]/20">
        <CardHeader>
          <CardTitle>Step 2: Upload CSV File</CardTitle>
          <CardDescription>Drag and drop or click to browse</CardDescription>
        </CardHeader>
        <CardContent>
          <div
            onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={handleDrop}
            className={`border-2 border-dashed rounded-lg p-12 text-center transition-colors ${
              isDragging
                ? 'border-[#4F6F52] bg-[#E6EFE0]'
                : 'border-gray-300 hover:border-[#4F6F52] hover:bg-gray-50'
            }`}
          >
            <Upload className="w-12 h-12 mx-auto mb-4 text-gray-400" />
            <p className="text-lg font-medium text-gray-700 mb-2">
              Drop your CSV file here
            </p>
            <p className="text-sm text-gray-500 mb-4">or</p>
            <Button
              onClick={() => document.getElementById('file-upload')?.click()}
              className="bg-[#4F6F52] hover:bg-[#3A4D39]"
            >
              Browse Files
            </Button>
            <input
              id="file-upload"
              type="file"
              accept=".csv"
              className="hidden"
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file) handleFileUpload(file);
              }}
            />
          </div>
        </CardContent>
      </Card>

      {/* Error Display */}
      {errors.length > 0 && (
        <Card className="border-[#E67E22]">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-[#E67E22]">
              <AlertCircle className="w-5 h-5" />
              Upload Failed: {errors.length} issue{errors.length > 1 ? 's' : ''} detected
            </CardTitle>
            <CardDescription>
              Please fix the errors below before importing. Missing dishes must be added to Recipe Management first.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow className="bg-[#E67E22]/10">
                    <TableHead>Row</TableHead>
                    <TableHead>Column</TableHead>
                    <TableHead>Value</TableHead>
                    <TableHead>Issue</TableHead>
                    <TableHead>Suggestion</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {errors.slice(0, 10).map((error, index) => (
                    <TableRow key={index} className="bg-[#FEF5E7]">
                      <TableCell className="font-mono">{error.row}</TableCell>
                      <TableCell className="font-medium">{error.column}</TableCell>
                      <TableCell className="font-mono text-sm">{error.value}</TableCell>
                      <TableCell className="text-[#E67E22]">{error.error}</TableCell>
                      <TableCell className="text-sm text-gray-600">{error.suggestion || '-'}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            {errors.length > 10 && (
              <p className="text-sm text-gray-600 mt-4 text-center">
                Showing first 10 of {errors.length} errors
              </p>
            )}
            <div className="flex items-center gap-4 mt-4">
              <Button
                variant="outline"
                onClick={() => {
                  const recipeManagementUrl = '#recipes';
                  window.location.hash = 'recipes';
                  toast.info('Navigate to Recipe Management to add missing dishes');
                }}
                className="gap-2"
              >
                <ChefHat className="w-4 h-4" />
                Go to Recipe Management
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Preview Table */}
      {showPreview && csvData.length > 0 && errors.length === 0 && (
        <Card className="border-[#4F6F52]/20">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="flex items-center gap-2 text-[#4F6F52]">
                  <CheckCircle className="w-5 h-5" />
                  Data Preview - Ready to Import
                </CardTitle>
                <CardDescription>
                  {csvData.length} record{csvData.length > 1 ? 's' : ''} validated successfully
                </CardDescription>
              </div>
              <div className="flex gap-2">
                <Button variant="outline" onClick={handleCancel}>
                  Cancel
                </Button>
                <Button onClick={handleImport} className="gap-2 bg-[#4F6F52] hover:bg-[#3A4D39]">
                  <ArrowRight className="w-4 h-4" />
                  Import Data
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow className="bg-[#E6EFE0]">
                    <TableHead>Date</TableHead>
                    <TableHead>Dish Name</TableHead>
                    <TableHead>Quantity</TableHead>
                    <TableHead>Revenue (S$)</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {csvData.slice(0, 10).map((row, index) => (
                    <TableRow key={index}>
                      <TableCell className="font-mono">{row.Date}</TableCell>
                      <TableCell className="font-medium">{row.Dish_Name}</TableCell>
                      <TableCell>{row.Quantity_Sold}</TableCell>
                      <TableCell>{row.Total_Revenue_SGD || '-'}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            {csvData.length > 10 && (
              <p className="text-sm text-gray-600 mt-4 text-center">
                Showing first 10 of {csvData.length} records
              </p>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}