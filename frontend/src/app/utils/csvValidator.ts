import { CSVRow, CSVValidationError, CSVValidationResult } from '@/app/types/csv';
import { Recipe } from '@/app/types';

const REQUIRED_COLUMNS = ['Date', 'Dish_Name', 'Quantity_Sold'];

/**
 * Supported date format definitions.
 * label: display text, value: .NET format string sent to backend,
 * regex: JS validation regex, example: sample date string,
 * parse: function to parse a date string into a Date object
 */
export interface DateFormatOption {
  label: string;
  value: string;        // .NET format string (sent to backend)
  regex: RegExp;
  example: string;
  parse: (s: string) => Date | null;
}

export const DATE_FORMATS: DateFormatOption[] = [
  {
    label: 'M/D/YY  (e.g. 5/1/25)',
    value: 'M/d/yy',
    regex: /^\d{1,2}\/\d{1,2}\/\d{2}$/,
    example: '5/1/25',
    parse: (s) => {
      const p = s.split('/');
      return new Date(2000 + +p[2], +p[0] - 1, +p[1]);
    },
  },
  {
    label: 'M/D/YYYY  (e.g. 5/1/2025)',
    value: 'M/d/yyyy',
    regex: /^\d{1,2}\/\d{1,2}\/\d{4}$/,
    example: '5/1/2025',
    parse: (s) => {
      const p = s.split('/');
      return new Date(+p[2], +p[0] - 1, +p[1]);
    },
  },
  {
    label: 'YYYY-MM-DD  (e.g. 2025-05-01)',
    value: 'yyyy-MM-dd',
    regex: /^\d{4}-\d{2}-\d{2}$/,
    example: '2025-05-01',
    parse: (s) => {
      const p = s.split('-');
      return new Date(+p[0], +p[1] - 1, +p[2]);
    },
  },
  {
    label: 'D/M/YY  (e.g. 1/5/25)',
    value: 'd/M/yy',
    regex: /^\d{1,2}\/\d{1,2}\/\d{2}$/,
    example: '1/5/25',
    parse: (s) => {
      const p = s.split('/');
      return new Date(2000 + +p[2], +p[1] - 1, +p[0]);
    },
  },
  {
    label: 'DD/MM/YYYY  (e.g. 01/05/2025)',
    value: 'dd/MM/yyyy',
    regex: /^\d{1,2}\/\d{1,2}\/\d{4}$/,
    example: '01/05/2025',
    parse: (s) => {
      const p = s.split('/');
      return new Date(+p[2], +p[1] - 1, +p[0]);
    },
  },
  {
    label: 'DD-MM-YYYY  (e.g. 01-05-2025)',
    value: 'dd-MM-yyyy',
    regex: /^\d{1,2}-\d{1,2}-\d{4}$/,
    example: '01-05-2025',
    parse: (s) => {
      const p = s.split('-');
      return new Date(+p[2], +p[1] - 1, +p[0]);
    },
  },
  {
    label: 'YYYY/MM/DD  (e.g. 2025/05/01)',
    value: 'yyyy/MM/dd',
    regex: /^\d{4}\/\d{2}\/\d{2}$/,
    example: '2025/05/01',
    parse: (s) => {
      const p = s.split('/');
      return new Date(+p[0], +p[1] - 1, +p[2]);
    },
  },
];

/**
 * Validates CSV data against SmartSus Chef requirements
 * Implements strict validation with auto-correction where possible
 */
export class CSVValidator {
  private recipes: Recipe[];
  private errors: CSVValidationError[] = [];
  private warnings: CSVValidationError[] = [];
  private dateFormat: DateFormatOption;

  constructor(recipes: Recipe[], dateFormatValue?: string) {
    this.recipes = recipes;
    this.dateFormat = DATE_FORMATS.find(f => f.value === dateFormatValue) || DATE_FORMATS[0];
  }

  /**
   * Main validation function
   */
  validate(rows: any[]): CSVValidationResult {
    this.errors = [];
    this.warnings = [];
    const validRows: any[] = [];

    // Validate headers
    if (rows.length === 0) {
      this.errors.push({
        row: 0,
        column: 'File',
        value: '',
        error: 'CSV file is empty',
      });
      return this.buildResult(validRows, rows.length);
    }

    const headers = Object.keys(rows[0]);
    const missingColumns = REQUIRED_COLUMNS.filter(col => !headers.includes(col));

    if (missingColumns.length > 0) {
      this.errors.push({
        row: 0,
        column: 'Headers',
        value: headers.join(', '),
        error: `Missing required columns: ${missingColumns.join(', ')}`,
        suggestion: 'Download the sample template to see the correct format',
      });
      return this.buildResult(validRows, rows.length);
    }

    // Validate each row
    rows.forEach((row, index) => {
      const rowNumber = index + 2; // +2 because index 0 is headers and spreadsheets start at 1
      const validatedRow = this.validateRow(row, rowNumber);

      if (validatedRow) {
        validRows.push(validatedRow);
      }
    });

    // Check for high volume failures
    if (this.errors.length > 50) {
      // Replace all errors with a single high-volume error
      const errorSummary = this.generateErrorSummary();
      this.errors = [{
        row: 0,
        column: 'File',
        value: `${this.errors.length} errors`,
        error: 'Massive data mismatch detected',
        suggestion: errorSummary,
      }];
    }

    return this.buildResult(validRows, rows.length);
  }

  /**
   * Validate a single row
   */
  private validateRow(row: any, rowNumber: number): any | null {
    let hasErrors = false;
    const validated: any = {};

    // Validate Date
    const dateValidation = this.validateDate(row.Date, rowNumber);
    if (dateValidation.error) {
      this.errors.push(dateValidation.error);
      hasErrors = true;
    } else {
      validated.date = dateValidation.value;
    }

    // Validate Dish Name (non-blocking — new dishes will be auto-created on import)
    const dishValidation = this.validateDishName(row.Dish_Name, rowNumber);
    if (dishValidation.error) {
      this.errors.push(dishValidation.error);
      hasErrors = true;
    } else {
      validated.dishName = row.Dish_Name.trim();
      // If matched an existing recipe, store recipeId for duplicate checking
      if (dishValidation.value) {
        validated.recipeId = dishValidation.value;
      }
    }

    // Validate Quantity (auto-correct format)
    const quantityValidation = this.validateNumber(
      row.Quantity_Sold,
      'Quantity_Sold',
      rowNumber,
      { min: 0, isInteger: true }
    );
    if (quantityValidation.error) {
      this.errors.push(quantityValidation.error);
      hasErrors = true;
    } else {
      validated.quantity = quantityValidation.value;
    }

    return hasErrors ? null : validated;
  }

  /**
   * Validate date format dynamically based on selected dateFormat
   */
  private validateDate(value: string, row: number): { value?: string; error?: CSVValidationError } {
    if (!value || value.trim() === '') {
      return {
        error: {
          row,
          column: 'Date',
          value,
          error: 'Date is required',
          suggestion: `Use format: ${this.dateFormat.label}`,
        },
      };
    }

    const trimmed = value.trim();
    if (!this.dateFormat.regex.test(trimmed)) {
      return {
        error: {
          row,
          column: 'Date',
          value,
          error: 'Invalid date format',
          suggestion: `Expected: ${this.dateFormat.label}`,
        },
      };
    }

    // Parse and validate it's a real calendar date
    const date = this.dateFormat.parse(trimmed);
    if (!date || isNaN(date.getTime())) {
      return {
        error: {
          row,
          column: 'Date',
          value,
          error: 'Invalid date',
          suggestion: 'Date does not exist in the calendar',
        },
      };
    }

    // Cross-check parsed month/day (guards against overflow like 2/30)
    const parts = trimmed.replace(/-/g, '/').split('/');
    const fmt = this.dateFormat.value;
    let expectedMonth: number;
    let expectedDay: number;
    if (fmt.startsWith('yyyy')) {
      expectedMonth = +parts[1];
      expectedDay = +parts[2];
    } else if (fmt.startsWith('d') || fmt.startsWith('D')) {
      expectedDay = +parts[0];
      expectedMonth = +parts[1];
    } else {
      expectedMonth = +parts[0];
      expectedDay = +parts[1];
    }
    if (date.getMonth() + 1 !== expectedMonth || date.getDate() !== expectedDay) {
      return {
        error: {
          row,
          column: 'Date',
          value,
          error: 'Invalid date',
          suggestion: 'Date does not exist in the calendar',
        },
      };
    }

    return { value: trimmed };
  }

  /**
   * Validate dish name — empty is an error, unknown dishes generate a warning
   * (they will be auto-created on import)
   */
  private validateDishName(value: string, row: number): { value?: string; error?: CSVValidationError } {
    if (!value || value.trim() === '') {
      return {
        error: {
          row,
          column: 'Dish_Name',
          value,
          error: 'Dish name is required',
        },
      };
    }

    const trimmedValue = value.trim();
    const recipe = this.recipes.find(r => r.name.toLowerCase() === trimmedValue.toLowerCase());

    if (!recipe) {
      // Not an error — dish will be auto-created on import
      this.warnings.push({
        row,
        column: 'Dish_Name',
        value,
        error: `New dish "${trimmedValue}" will be auto-created on import`,
      });
      return { value: undefined }; // no recipeId yet
    }

    return { value: recipe.id };
  }

  /**
   * Find closest matching recipe name using Levenshtein distance
   */
  private findClosestRecipe(target: string): Recipe | null {
    if (this.recipes.length === 0) return null;

    let closest = this.recipes[0];
    let minDistance = this.levenshteinDistance(target.toLowerCase(), closest.name.toLowerCase());

    this.recipes.forEach(recipe => {
      const distance = this.levenshteinDistance(target.toLowerCase(), recipe.name.toLowerCase());
      if (distance < minDistance) {
        minDistance = distance;
        closest = recipe;
      }
    });

    // Only suggest if similarity is reasonable (distance < 5)
    return minDistance < 5 ? closest : null;
  }

  /**
   * Calculate Levenshtein distance for fuzzy matching
   */
  private levenshteinDistance(a: string, b: string): number {
    const matrix: number[][] = [];

    for (let i = 0; i <= b.length; i++) {
      matrix[i] = [i];
    }

    for (let j = 0; j <= a.length; j++) {
      matrix[0][j] = j;
    }

    for (let i = 1; i <= b.length; i++) {
      for (let j = 1; j <= a.length; j++) {
        if (b.charAt(i - 1) === a.charAt(j - 1)) {
          matrix[i][j] = matrix[i - 1][j - 1];
        } else {
          matrix[i][j] = Math.min(
            matrix[i - 1][j - 1] + 1,
            matrix[i][j - 1] + 1,
            matrix[i - 1][j] + 1
          );
        }
      }
    }

    return matrix[b.length][a.length];
  }

  /**
   * Validate and auto-correct number fields
   */
  private validateNumber(
    value: string,
    column: string,
    row: number,
    options: { min?: number; max?: number; isInteger?: boolean } = {}
  ): { value?: number; error?: CSVValidationError; warning?: CSVValidationError } {
    if (value === undefined || value === null || value.trim() === '') {
      return {
        error: {
          row,
          column,
          value,
          error: `${column} is required`,
        },
      };
    }

    // Try to parse the number
    const parsed = parseFloat(value.toString().replace(/[^0-9.-]/g, ''));

    if (isNaN(parsed)) {
      return {
        error: {
          row,
          column,
          value,
          error: `Invalid number format`,
          suggestion: 'Expected a numeric value',
        },
      };
    }

    // Check constraints
    if (options.min !== undefined && parsed < options.min) {
      return {
        error: {
          row,
          column,
          value,
          error: `Value must be at least ${options.min}`,
        },
      };
    }

    if (options.max !== undefined && parsed > options.max) {
      return {
        error: {
          row,
          column,
          value,
          error: `Value must be at most ${options.max}`,
        },
      };
    }

    if (options.isInteger && !Number.isInteger(parsed)) {
      return {
        error: {
          row,
          column,
          value,
          error: `Value must be a whole number`,
        },
      };
    }

    return { value: parsed };
  }

  /**
   * Validate and auto-correct currency format
   * Silently strips "S$", "$", and other currency symbols
   */
  private validateCurrency(
    value: string,
    column: string,
    row: number
  ): { value?: number; error?: CSVValidationError; warning?: CSVValidationError } {
    if (value === undefined || value === null || value.trim() === '') {
      return {
        error: {
          row,
          column,
          value,
          error: `${column} is required`,
        },
      };
    }

    // Auto-correct: strip currency symbols and whitespace
    const cleaned = value.toString()
      .replace(/S\$/g, '')
      .replace(/\$/g, '')
      .replace(/,/g, '')
      .trim();

    const parsed = parseFloat(cleaned);

    if (isNaN(parsed)) {
      return {
        error: {
          row,
          column,
          value,
          error: `Invalid currency format`,
          suggestion: 'Expected format: 5.00 or S$ 5.00',
        },
      };
    }

    // Format to 2 decimal places
    const formatted = parseFloat(parsed.toFixed(2));

    // Add warning if format was corrected
    let warning: CSVValidationError | undefined;
    if (value !== formatted.toFixed(2)) {
      warning = {
        row,
        column,
        value,
        error: `Auto-corrected to ${formatted.toFixed(2)}`,
      };
    }

    return { value: formatted, warning };
  }

  /**
   * Generate error summary for high-volume failures
   */
  private generateErrorSummary(): string {
    const errorTypes: { [key: string]: number } = {};

    this.errors.forEach(error => {
      const key = error.error;
      errorTypes[key] = (errorTypes[key] || 0) + 1;
    });

    const summaryLines = Object.entries(errorTypes)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 10)
      .map(([error, count]) => `${error}: ${count} occurrences`);

    return `Download Error Log to see all issues:\n\n${summaryLines.join('\n')}`;
  }

  /**
   * Build validation result
   */
  private buildResult(validRows: any[], totalRows: number): CSVValidationResult {
    return {
      isValid: this.errors.length === 0,
      errors: this.errors,
      warnings: this.warnings,
      validRows,
      totalRows,
    };
  }

  /**
   * Generate error log for download
   */
  static generateErrorLog(errors: CSVValidationError[]): string {
    const lines = ['SmartSus Chef - CSV Import Error Log', '='.repeat(60), ''];

    errors.forEach((error, index) => {
      lines.push(`Error ${index + 1}:`);
      lines.push(`  Row: ${error.row}`);
      lines.push(`  Column: ${error.column}`);
      lines.push(`  Value: "${error.value}"`);
      lines.push(`  Issue: ${error.error}`);
      if (error.suggestion) {
        lines.push(`  Suggestion: ${error.suggestion}`);
      }
      lines.push('');
    });

    lines.push('=' + '='.repeat(60));
    lines.push(`Total Errors: ${errors.length}`);
    lines.push('');
    lines.push('Common Solutions:');
    lines.push('1. Ensure date format matches the selected format');
    lines.push('2. Quantity must be a whole number >= 0');
    lines.push('3. Dish name cannot be empty');
    lines.push('4. Download the sample template for correct format');

    return lines.join('\n');
  }

  /**
   * Generate sample CSV template
   */
  static generateSampleCSV(dateFormatValue?: string): string {
    const fmt = DATE_FORMATS.find(f => f.value === dateFormatValue) || DATE_FORMATS[0];
    const headers = REQUIRED_COLUMNS.join(',');
    // Generate sample dates using the selected format's example pattern
    const ex = fmt.example;
    const sampleRows = [
      `${ex},Laksa,85`,
      `${ex},Hainanese Chicken Rice,120`,
      `${ex},Chicken Salad,45`,
    ];

    return [headers, ...sampleRows].join('\n');
  }
}
