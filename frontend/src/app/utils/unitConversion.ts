/**
 * Unit Conversion Utilities
 * Automatically converts small units (g, ml) to larger units (kg, L) when appropriate
 */

export interface ConvertedUnit {
  quantity: number;
  unit: string;
  displayText: string; // e.g., "2.50 kg"
}

/**
 * Converts a quantity and unit to the most appropriate display format
 * @param quantity - The numeric quantity
 * @param unit - The original unit (g, ml, kg, L, etc.)
 * @param threshold - The threshold for conversion (default: 1000)
 * @returns ConvertedUnit object with converted values
 */
export function convertUnit(
  quantity: number, 
  unit: string, 
  threshold: number = 1000
): ConvertedUnit {
  let displayQuantity = quantity;
  let displayUnit = unit;

  // Convert grams to kilograms if >= threshold
  if (unit === 'g' && quantity >= threshold) {
    displayQuantity = quantity / 1000;
    displayUnit = 'kg';
  }
  // Convert milliliters to liters if >= threshold
  else if (unit === 'ml' && quantity >= threshold) {
    displayQuantity = quantity / 1000;
    displayUnit = 'L';
  }

  return {
    quantity: displayQuantity,
    unit: displayUnit,
    displayText: `${displayQuantity.toFixed(2)} ${displayUnit}`
  };
}

/**
 * Formats a quantity with its unit for display
 * @param quantity - The numeric quantity
 * @param unit - The unit
 * @param decimalPlaces - Number of decimal places (default: 2)
 * @returns Formatted string like "2.50 kg"
 */
export function formatQuantity(
  quantity: number, 
  unit: string, 
  decimalPlaces: number = 2
): string {
  const converted = convertUnit(quantity, unit);
  return `${converted.quantity.toFixed(decimalPlaces)} ${converted.unit}`;
}

/**
 * Gets the standardized quantity in base units (kg or L)
 * Used for calculations like carbon footprint
 * @param quantity - The numeric quantity
 * @param unit - The unit
 * @returns Quantity in kg/L
 */
export function getStandardizedQuantity(quantity: number, unit: string): number {
  if (unit === 'g' || unit === 'ml') {
    return quantity / 1000;
  }
  return quantity; // Assumes kg, L, or 'plate' is already standard
}

/**
 * Converts a quantity from one unit to another
 * @param quantity - The numeric quantity
 * @param fromUnit - The original unit
 * @param toUnit - The target unit
 * @returns Converted quantity
 */
export function convertBetweenUnits(
  quantity: number,
  fromUnit: string,
  toUnit: string
): number {
  // Same unit, no conversion needed
  if (fromUnit === toUnit) return quantity;

  // Convert to base unit first (g -> kg or ml -> L)
  let baseQuantity = quantity;
  if (fromUnit === 'g' || fromUnit === 'ml') {
    baseQuantity = quantity / 1000;
  }

  // Convert from base unit to target unit
  if (toUnit === 'g' || toUnit === 'ml') {
    return baseQuantity * 1000;
  }

  return baseQuantity;
}