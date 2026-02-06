import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/app/components/ui/dialog';
import { Upload, Download, AlertCircle, CheckCircle, ArrowRight, ChefHat, FileText, AlertTriangle, CalendarDays } from 'lucide-react';
import { toast } from 'sonner';
import { parse } from 'papaparse';
import { format } from 'date-fns';
import { CSVValidator, DATE_FORMATS } from '@/app/utils/csvValidator';
import { CSVValidationError } from '@/app/types/csv';
import { SalesData } from '@/app/types';
import { salesApi } from '@/app/services/api';

interface CSVRow {
  Date: string;
  Dish_Name: string;
  Quantity_Sold: string;
}

export function ImportSalesData() {
  const { recipes, salesData, addSalesData, importSalesData } = useApp();
  const [isDragging, setIsDragging] = useState(false);
  const [csvData, setCsvData] = useState<CSVRow[]>([]);
  const [errors, setErrors] = useState<CSVValidationError[]>([]);
  const [showPreview, setShowPreview] = useState(false);
  const [duplicates, setDuplicates] = useState<{ date: string; dish: string; rows: number[] }[]>([]);
  const [isImporting, setIsImporting] = useState(false);
  const [dateFormat, setDateFormat] = useState(DATE_FORMATS[0].value);

  // Add after the existing state declarations
  const [overwriteConfirmData, setOverwriteConfirmData] = useState<{
    isOpen: boolean;
    duplicates: Array<{
      date: string;
      dishName: string;
      existingQuantity: number;
      newQuantity: number;
    }>;
    importData: Array<{ date: string; recipeId: string; quantity: number }>;
  }>({
    isOpen: false,
    duplicates: [],
    importData: [],
  });

  // Check which records in the imported data will be overwritten
  const checkForExistingDuplicates = (
    importData: Array<{ date: string; recipeId: string; quantity: number }>
  ): Array<{
    date: string;
    dishName: string;
    existingQuantity: number;
    newQuantity: number;
  }> => {
    const duplicates: Array<{
      date: string;
      dishName: string;
      existingQuantity: number;
      newQuantity: number;
    }> = [];

    // Create a mapping of existing data for quick lookup
    const existingMap = new Map<string, { quantity: number; recipeName: string }>();

    salesData.forEach(item => {
      const key = `${item.date}|${item.recipeId}`;
      const recipe = recipes.find(r => r.id === item.recipeId);
      existingMap.set(key, {
        quantity: item.quantity,
        recipeName: recipe?.name || 'Unknown',
      });
    });

    // Check which records in the imported data will be overwritten
    importData.forEach(item => {
      const key = `${item.date}|${item.recipeId}`;
      const existing = existingMap.get(key);

      if (existing) {
        const recipe = recipes.find(r => r.id === item.recipeId);
        duplicates.push({
          date: item.date,
          dishName: recipe?.name || 'Unknown',
          existingQuantity: existing.quantity,
          newQuantity: item.quantity,
        });
      }
    });

    return duplicates;
  };

  const handleConfirmOverwrite = async () => {
    try {
      const salesDataToImport: SalesData[] = overwriteConfirmData.importData.map((sale, index) => ({
        id: `import-${Date.now()}-${index}`,
        date: sale.date,
        recipeId: sale.recipeId,
        quantity: sale.quantity,
        createdAt: new Date().toISOString(),
        modifiedAt: new Date().toISOString(),
        editHistory: []
      }));
      await importSalesData(salesDataToImport);
      toast.success(`Successfully imported ${overwriteConfirmData.importData.length} sales records`);

      // Turn off all states
      setOverwriteConfirmData({
        isOpen: false,
        duplicates: [],
        importData: [],
      });
      setCsvData([]);
      setErrors([]);
      setShowPreview(false);
    } catch (error: any) {
      toast.error('Failed to import sales data');
    }
  };

  const handleCancelOverwrite = () => {
    setOverwriteConfirmData({
      isOpen: false,
      duplicates: [],
      importData: [],
    });
  };

  // Find duplicate rows (same date, same dish) in the CSV file
  const findDuplicatesInCSV = (data: CSVRow[]): { date: string; dish: string; rows: number[] }[] => {
    const duplicates: { date: string; dish: string; rows: number[] }[] = [];
    const seen = new Map<string, number[]>(); // key: date|dish, value: row number array

    data.forEach((row, index) => {
      const key = `${row.Date}|${row.Dish_Name}`;
      if (!seen.has(key)) {
        seen.set(key, [index + 2]);
      } else {
        seen.get(key)?.push(index + 2);
      }
    });

    // Find entries with duplicates
    seen.forEach((rows, key) => {
      if (rows.length > 1) {
        const [date, dish] = key.split('|');
        duplicates.push({ date, dish, rows });
      }
    });

    return duplicates;
  };

  // Handling duplicate rows: Keep the last row and delete the others
  const handleMergeDuplicates = () => {
    if (!csvData.length || !duplicates.length) return;

    const mergedData: CSVRow[] = [];
    const seen = new Map<string, CSVRow>();

    // Reverse traversal, keeping only the last one
    for (let i = csvData.length - 1; i >= 0; i--) {
      const row = csvData[i];
      const key = `${row.Date}|${row.Dish_Name}`;

      if (!seen.has(key)) {
        seen.set(key, row);
      }
    }

    // Convert back to array
    seen.forEach(row => mergedData.push(row));

    setCsvData(mergedData);
    setDuplicates([]);
    setShowPreview(true);
    toast.success(`Merged ${duplicates.length} duplicate entries. Keeping last record for each duplicate.`);
  };

  const handleImportOnlyNew = async (newRecords: Array<{ date: string; recipeId: string; quantity: number }>) => {
    if (newRecords.length === 0) {
      toast.info('No new records to import');
      setOverwriteConfirmData({
        isOpen: false,
        duplicates: [],
        importData: [],
      });
      return;
    }

    try {
      const salesDataToImport: SalesData[] = newRecords.map((record, index) => ({
        id: `import-${Date.now()}-${index}`,
        date: record.date,
        recipeId: record.recipeId,
        quantity: record.quantity,
        createdAt: new Date().toISOString(),
        modifiedAt: new Date().toISOString(),
        editHistory: []
      }));

      await importSalesData(salesDataToImport);
      toast.success(`Successfully imported ${newRecords.length} new records (skipped ${overwriteConfirmData.duplicates.length} duplicates)`);

      setOverwriteConfirmData({
        isOpen: false,
        duplicates: [],
        importData: [],
      });
      setCsvData([]);
      setErrors([]);
      setShowPreview(false);
    } catch (error: any) {
      toast.error('Failed to import sales data');
    }
  };

  const handleDownloadTemplate = () => {
    const fmt = DATE_FORMATS.find(f => f.value === dateFormat) || DATE_FORMATS[0];
    const template = [
      ['Date', 'Dish_Name', 'Quantity_Sold'],
      [fmt.example, 'Laksa', '85'],
      [fmt.example, 'Hainanese Chicken Rice', '120'],
      [fmt.example, 'Chicken Salad', '45'],
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

          // Check for duplicate rows within the CSV file
          const duplicates = findDuplicatesInCSV(results.data);
          if (duplicates.length > 0) {
            // Display duplicate line warning
            setDuplicates(duplicates);
            setCsvData(results.data);
            setShowPreview(false);
            toast.error(`Found ${duplicates.length} duplicate entries in CSV file`);
            return;
          }

          // Use CSV Validator
          const validator = new CSVValidator(recipes, dateFormat);
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
        error: (error: Error) => {
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

  // Add import logic to the handleImport function
  const handleImport = async () => {
    if (errors.length > 0) {
      toast.error('Please fix all errors before importing');
      return;
    }

    // Check for duplicates within the imported data
    const seenInImport = new Set<string>();
    const duplicatesInImport: string[] = [];

    csvData.forEach((row, index) => {
      const key = `${row.Date}|${row.Dish_Name}`.toLowerCase();

      if (seenInImport.has(key)) {
        duplicatesInImport.push(`Row ${index + 2}: ${row.Dish_Name} on ${row.Date}`);
      } else {
        seenInImport.add(key);
      }
    });

    if (duplicatesInImport.length > 0) {
      toast.error(`Found ${duplicatesInImport.length} duplicates in import file. Please fix them first.`);
      return;
    }

    const salesToImport = csvData.map(row => ({
      date: row.Date,
      dishName: row.Dish_Name.trim(),
      quantity: parseInt(row.Quantity_Sold, 10),
    }));

    setIsImporting(true);
    try {
      const result = await salesApi.importByName({ salesData: salesToImport, dateFormat });

      let msg = `Successfully imported ${result.imported} sales records`;
      if (result.newDishesCreated > 0) {
        msg += `. Auto-created ${result.newDishesCreated} new dish(es): ${result.newDishes.join(', ')}`;
      }
      toast.success(msg);

      // Refresh app data
      setCsvData([]);
      setErrors([]);
      setShowPreview(false);

      // Trigger a page reload to refresh recipes + sales data
      window.location.reload();
    } catch (error: any) {
      if (error.message?.includes('duplicate')) {
        toast.error('Duplicate records detected. The system prevents duplicate entries for the same date and recipe.');
      } else {
        toast.error(`Failed to import sales data: ${error.message}`);
      }
    } finally {
      setIsImporting(false);
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

      {/* Date Format & Template */}
      <Card className="border-[#4F6F52]/20">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CalendarDays className="w-5 h-5 text-[#4F6F52]" />
            Step 1: Select Date Format & Download Template
          </CardTitle>
          <CardDescription>
            Choose the date format used in your CSV file, then download the template
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Date Format Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Date Format in CSV</label>
            <select
              value={dateFormat}
              title="Select date format for CSV import"
              onChange={(e) => {
                setDateFormat(e.target.value);
                // Reset validation when format changes
                if (csvData.length > 0) {
                  setCsvData([]);
                  setErrors([]);
                  setShowPreview(false);
                  toast.info('Date format changed. Please re-upload your CSV file.');
                }
              }}
              className="w-full max-w-md border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#4F6F52] focus:border-[#4F6F52] bg-white"
            >
              {DATE_FORMATS.map((fmt) => (
                <option key={fmt.value} value={fmt.value}>
                  {fmt.label}
                </option>
              ))}
            </select>
          </div>

          {/* Template Download */}
          <Button onClick={handleDownloadTemplate} variant="outline" className="gap-2">
            <Download className="w-4 h-4" />
            Download Sample Template (.csv)
          </Button>
          <div className="p-4 bg-[#E6EFE0] rounded-lg border border-[#4F6F52]/20">
            <p className="text-sm font-medium text-[#1A1C18] mb-2">Required Columns:</p>
            <ul className="text-sm text-gray-700 space-y-1">
              <li>• <strong>Date</strong>: {DATE_FORMATS.find(f => f.value === dateFormat)?.label || dateFormat}</li>
              <li>• <strong>Dish_Name</strong>: Dish name (new dishes will be auto-created)</li>
              <li>• <strong>Quantity_Sold</strong>: Number of dishes sold</li>
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
            className={`border-2 border-dashed rounded-lg p-12 text-center transition-colors ${isDragging
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
              Please fix the errors below before importing.
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

      {/* Duplicate Entries Display */}
      {duplicates.length > 0 && (
        <Card className="border-[#E74C3C]">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-[#E74C3C]">
              <AlertTriangle className="w-5 h-5" />
              Duplicate Entries Found
            </CardTitle>
            <CardDescription>
              Found {duplicates.length} duplicate entries in CSV file (same date and dish).
              We recommend merging duplicates before importing.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="border rounded-lg overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-[#E74C3C]/10">
                      <TableHead>Date</TableHead>
                      <TableHead>Dish Name</TableHead>
                      <TableHead>Rows in CSV</TableHead>
                      <TableHead>Action</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {duplicates.map((dup, index) => (
                      <TableRow key={index} className="bg-[#FDEDEC]">
                        <TableCell>{dup.date}</TableCell>
                        <TableCell className="font-medium">{dup.dish}</TableCell>
                        <TableCell>{dup.rows.join(', ')}</TableCell>
                        <TableCell>
                          <span className="text-[#E74C3C] font-medium">Will keep last record</span>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>

              <div className="flex gap-2">
                <Button
                  onClick={handleMergeDuplicates}
                  className="bg-[#E74C3C] hover:bg-[#C0392B]"
                >
                  Merge Duplicates (Keep Last Record)
                </Button>
                <Button
                  variant="outline"
                  onClick={() => {
                    setDuplicates([]);
                    setCsvData([]);
                    setShowPreview(false);
                  }}
                >
                  Cancel Upload
                </Button>
              </div>

              <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
                <div className="flex items-start gap-2">
                  <AlertTriangle className="w-4 h-4 text-yellow-600 mt-0.5" />
                  <div className="text-sm text-yellow-800">
                    <p className="font-medium">Note:</p>
                    <p>Duplicate entries (same date and dish) will be merged. The last record in the CSV file will be kept.</p>
                    <p className="mt-1">If you import this data, existing database records with the same date and dish will be overwritten.</p>
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Overwrite Confirmation Dialog */}
      <Dialog open={overwriteConfirmData.isOpen} onOpenChange={(open) => {
        if (!open) handleCancelOverwrite();
      }}>
        <DialogContent className="max-w-[95vw] lg:max-w-[1200px] max-h-[85vh] overflow-y-auto p-4 sm:p-6">
          <DialogHeader className="px-2 sm:px-0">
            <DialogTitle className="flex items-center gap-2 text-amber-600 text-lg sm:text-xl">
              <AlertTriangle className="w-5 h-5 sm:w-6 sm:h-6" />
              Overwrite Existing Records
            </DialogTitle>
            <DialogDescription className="pt-1 text-sm sm:text-base">
              The following records already exist in the database and will be overwritten.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 px-2 sm:px-0">
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 sm:p-4">
              <div className="flex items-start gap-2 sm:gap-3">
                <AlertTriangle className="w-5 h-5 text-amber-600 mt-0.5 flex-shrink-0" />
                <div className="text-sm sm:text-base text-amber-800">
                  <p className="font-medium">Warning: Overwriting existing data</p>
                  <p>
                    {overwriteConfirmData.duplicates.length} record(s) will overwrite existing data.
                    The new quantities will replace the existing ones.
                  </p>
                </div>
              </div>
            </div>

            {overwriteConfirmData.duplicates.length > 0 && (
              <div className="border rounded-lg overflow-hidden">
                <div className="overflow-x-auto">
                  <Table className="min-w-[800px] sm:min-w-full">
                    <TableHeader>
                      <TableRow className="bg-gray-50">
                        <TableHead className="whitespace-nowrap w-32 px-3 py-3">Date</TableHead>
                        <TableHead className="whitespace-nowrap min-w-40 px-3 py-3">Recipe</TableHead>
                        <TableHead className="whitespace-nowrap w-40 px-3 py-3 text-right">Existing Quantity</TableHead>
                        <TableHead className="whitespace-nowrap w-40 px-3 py-3 text-right">New Quantity</TableHead>
                        <TableHead className="whitespace-nowrap w-40 px-3 py-3 text-right">Change</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {overwriteConfirmData.duplicates.map((dup, index) => {
                        const change = dup.newQuantity - dup.existingQuantity;
                        const changePercent = dup.existingQuantity > 0
                          ? ((change / dup.existingQuantity) * 100).toFixed(1)
                          : '∞';

                        return (
                          <TableRow key={index} className="hover:bg-amber-50/50">
                            <TableCell className="font-medium whitespace-nowrap px-3 py-3">
                              {format(new Date(dup.date), 'd MMM yyyy')}
                            </TableCell>
                            <TableCell className="whitespace-nowrap px-3 py-3 truncate max-w-[200px]" title={dup.dishName}>
                              {dup.dishName}
                            </TableCell>
                            <TableCell className="text-right whitespace-nowrap px-3 py-3">
                              {dup.existingQuantity} dishes
                            </TableCell>
                            <TableCell className="text-right whitespace-nowrap px-3 py-3">
                              {dup.newQuantity} dishes
                            </TableCell>
                            <TableCell className="text-right whitespace-nowrap px-3 py-3">
                              <span className={`font-medium ${change > 0
                                ? 'text-green-600'
                                : change < 0
                                  ? 'text-red-600'
                                  : 'text-gray-600'
                                }`}>
                                {change > 0 ? '+' : ''}{change} ({changePercent}%)
                              </span>
                            </TableCell>
                          </TableRow>
                        );
                      })}
                    </TableBody>
                  </Table>
                </div>
              </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="bg-gray-50 rounded-lg p-4">
                <p className="text-sm font-medium text-gray-700 mb-3">Import Summary</p>
                <div className="space-y-2 text-sm text-gray-600">
                  <div className="flex justify-between items-center py-1 border-b border-gray-200">
                    <span>Total records to import:</span>
                    <span className="font-medium text-lg">{overwriteConfirmData.importData.length}</span>
                  </div>
                  <div className="flex justify-between items-center py-1 border-b border-gray-200">
                    <span>New records:</span>
                    <span className="font-medium text-lg">
                      {overwriteConfirmData.importData.length - overwriteConfirmData.duplicates.length}
                    </span>
                  </div>
                  <div className="flex justify-between items-center py-1">
                    <span>Records to overwrite:</span>
                    <span className="font-medium text-lg text-amber-600">
                      {overwriteConfirmData.duplicates.length}
                    </span>
                  </div>
                </div>
              </div>

              <div className="bg-blue-50 rounded-lg p-4">
                <p className="text-sm font-medium text-blue-700 mb-3">How this works</p>
                <ul className="text-sm text-blue-600 space-y-2">
                  <li className="flex items-start gap-2">
                    <span className="bg-blue-100 rounded-full w-5 h-5 flex items-center justify-center text-xs mt-0.5">1</span>
                    <span>New records will be added to the database</span>
                  </li>
                  <li className="flex items-start gap-2">
                    <span className="bg-blue-100 rounded-full w-5 h-5 flex items-center justify-center text-xs mt-0.5">2</span>
                    <span>Existing records with the same date and recipe will be overwritten</span>
                  </li>
                  <li className="flex items-start gap-2">
                    <span className="bg-blue-100 rounded-full w-5 h-5 flex items-center justify-center text-xs mt-0.5">3</span>
                    <span>Edit history for overwritten records will be preserved</span>
                  </li>
                </ul>
              </div>
            </div>

            <div className="pt-6 border-t">
              <div className="flex flex-col md:flex-row justify-between gap-4">
                <Button
                  variant="outline"
                  onClick={handleCancelOverwrite}
                  className="hover:bg-gray-100 w-full md:w-auto order-2 md:order-1 py-3 text-base"
                >
                  Cancel Import
                </Button>
                <div className="flex flex-col sm:flex-row gap-3 w-full md:w-auto order-1 md:order-2">
                  <Button
                    variant="outline"
                    onClick={() => {
                      const newRecords = overwriteConfirmData.importData.filter(item => {
                        const existing = salesData.find(s =>
                          s.date === item.date && s.recipeId === item.recipeId
                        );
                        return !existing;
                      });

                      handleImportOnlyNew(newRecords);
                    }}
                    className="text-blue-600 border-blue-300 hover:bg-blue-50 w-full sm:w-auto py-3 text-base"
                  >
                    Import Only New
                  </Button>
                  <Button
                    onClick={handleConfirmOverwrite}
                    className="bg-amber-600 hover:bg-amber-700 w-full sm:w-auto py-3 text-base"
                  >
                    Overwrite & Import All
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </DialogContent>
      </Dialog>

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
                <Button variant="outline" onClick={handleCancel} disabled={isImporting}>
                  Cancel
                </Button>
                <Button onClick={handleImport} disabled={isImporting} className="gap-2 bg-[#4F6F52] hover:bg-[#3A4D39]">
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
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {csvData.slice(0, 10).map((row, index) => (
                    <TableRow key={index}>
                      <TableCell className="font-mono">{row.Date}</TableCell>
                      <TableCell className="font-medium">{row.Dish_Name}</TableCell>
                      <TableCell>{row.Quantity_Sold}</TableCell>
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