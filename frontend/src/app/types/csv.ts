// CSV Import types and interfaces

export interface CSVRow {
  Date: string;
  Dish_Name: string;
  Quantity_Sold: string;
}

export interface CSVValidationError {
  row: number;
  column: string;
  value: string;
  error: string;
  suggestion?: string;
}

export interface CSVValidationResult {
  isValid: boolean;
  errors: CSVValidationError[];
  warnings: CSVValidationError[];
  validRows: any[];
  totalRows: number;
}

export interface CSVImportSummary {
  imported: number;
  failed: number;
  warnings: number;
  errors: CSVValidationError[];
}
