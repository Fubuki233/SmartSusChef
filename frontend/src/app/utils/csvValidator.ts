import { CSVRow, CSVValidationError, CSVValidationResult } from '@/app/types/csv';
import { Recipe } from '@/app/types';

const REQUIRED_COLUMNS = ['Date', 'Dish_Name', 'Quantity_Sold', 'Total_Revenue_SGD', 'Unit_Cost_SGD'];

/**
 * Validates CSV data against SmartSus Chef requirements
 * Implements strict validation with auto-correction where possible
 */
export class CSVValidator {
  private recipes: Recipe[];
  private errors: CSVValidationError[] = [];
  private warnings: CSVValidationError[] = [];

  constructor(recipes: Recipe[]) {
    this.recipes = recipes;
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

    // Validate Dish Name (STRICT - must exist in recipes)
    const dishValidation = this.validateDishName(row.Dish_Name, rowNumber);
    if (dishValidation.error) {
      this.errors.push(dishValidation.error);
      hasErrors = true;
    } else {
      validated.recipeId = dishValidation.value;
      validated.recipeName = row.Dish_Name;
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

    // Validate Total Revenue (auto-correct currency format)
    const revenueValidation = this.validateCurrency(
      row.Total_Revenue_SGD,
      'Total_Revenue_SGD',
      rowNumber
    );
    if (revenueValidation.error) {
      this.errors.push(revenueValidation.error);
      hasErrors = true;
    } else if (revenueValidation.warning) {
      this.warnings.push(revenueValidation.warning);
    }
    validated.revenue = revenueValidation.value;

    // Validate Unit Cost (auto-correct currency format)
    const costValidation = this.validateCurrency(
      row.Unit_Cost_SGD,
      'Unit_Cost_SGD',
      rowNumber
    );
    if (costValidation.error) {
      this.errors.push(costValidation.error);
      hasErrors = true;
    } else if (costValidation.warning) {
      this.warnings.push(costValidation.warning);
    }
    validated.unitCost = costValidation.value;

    return hasErrors ? null : validated;
  }

  /**
   * Validate date format (YYYY-MM-DD)
   */
  private validateDate(value: string, row: number): { value?: string; error?: CSVValidationError } {
    if (!value || value.trim() === '') {
      return {
        error: {
          row,
          column: 'Date',
          value,
          error: 'Date is required',
          suggestion: 'Use format: YYYY-MM-DD (e.g., 2026-01-22)',
        },
      };
    }

    const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
    if (!dateRegex.test(value)) {
      return {
        error: {
          row,
          column: 'Date',
          value,
          error: 'Invalid date format',
          suggestion: 'Expected: YYYY-MM-DD (e.g., 2026-01-22)',
        },
      };
    }

    // Validate it's a real date
    const date = new Date(value);
    if (isNaN(date.getTime())) {
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

    return { value };
  }

  /**
   * Validate dish name exists in recipe database
   * STRICT - No auto-creation allowed
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
      // Find closest match for suggestion
      const suggestion = this.findClosestRecipe(trimmedValue);
      return {
        error: {
          row,
          column: 'Dish_Name',
          value,
          error: `Dish "${trimmedValue}" not found in recipe database`,
          suggestion: suggestion 
            ? `Did you mean "${suggestion.name}"? Or add this dish to Recipe Management first.`
            : 'Please add this dish to Recipe Management before importing.',
        },
      };
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
    const lines = ['SmartSus Chef - CSV Import Error Log', '=' .repeat(60), ''];

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
    lines.push('1. Ensure all dish names exist in Recipe Management');
    lines.push('2. Use date format: YYYY-MM-DD');
    lines.push('3. Currency values should be numeric (S$ prefix is optional)');
    lines.push('4. Download the sample template for correct format');

    return lines.join('\n');
  }

  /**
   * Generate sample CSV template
   */
  static generateSampleCSV(): string {
    const headers = REQUIRED_COLUMNS.join(',');
    const sampleRows = [
      '2026-01-20,Laksa,85,510.00,6.00',
      '2026-01-20,Hainanese Chicken Rice,120,660.00,5.50',
      '2026-01-21,Laksa,70,420.00,6.00',
      '2026-01-21,Hainanese Chicken Rice,95,522.50,5.50',
    ];

    return [headers, ...sampleRows].join('\n');
  }
}
