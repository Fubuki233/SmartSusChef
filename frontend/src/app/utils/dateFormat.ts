import { format as dateFnsFormat, parseISO } from 'date-fns';

/**
 * Singapore Date Format Utilities
 * Standardized date formats across the application
 */

/**
 * Format: "18 Jan 2026" (DD Mth YYYY)
 * Standard display format for Singapore
 */
export function formatSingaporeDate(date: Date | string): string {
  const dateObj = typeof date === 'string' ? parseISO(date) : date;
  return dateFnsFormat(dateObj, 'd MMM yyyy');
}

/**
 * Format: "18 Jan 2026, 2:30 PM" (DD Mth YYYY, h:mm a)
 * Full timestamp with time
 */
export function formatSingaporeDateTime(date: Date | string): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  return dateFnsFormat(dateObj, 'd MMM yyyy, h:mm a');
}

/**
 * Format: "18 Jan" (DD Mth)
 * Short date for charts and compact displays
 */
export function formatShortDate(date: Date | string): string {
  const dateObj = typeof date === 'string' ? parseISO(date) : date;
  return dateFnsFormat(dateObj, 'd MMM');
}

/**
 * Format: "Mon, 18 Jan 2026" (EEE, DD Mth YYYY)
 * Date with day of week
 */
export function formatDateWithDay(date: Date | string): string {
  const dateObj = typeof date === 'string' ? parseISO(date) : date;
  return dateFnsFormat(dateObj, 'EEE, d MMM yyyy');
}

/**
 * Format: "18 Jan 2026, 14:30" (DD Mth YYYY, HH:mm)
 * 24-hour format for logs/history
 */
export function formatDateTimeLog(date: Date | string): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  return dateFnsFormat(dateObj, 'd MMM yyyy, HH:mm');
}
