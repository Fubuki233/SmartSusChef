import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { format, addDays } from 'date-fns';
import { formatShortDate } from '@/app/utils/dateFormat';
import { Package } from 'lucide-react';
import { convertUnit } from '@/app/utils/unitConversion';

export function PredictionDetail() {
  const { forecastData, recipes, ingredients } = useApp();

  const predictionData = useMemo(() => {
    const ingredientTotals: { [ingredientId: string]: { [date: string]: number } } = {};
    const ingredientMap = new Map(ingredients.map((i) => [i.id, i]));
    const recipeMap = new Map(recipes.map((r) => [r.id, r]));

    console.log('[PredictionDetail] Total forecast data:', forecastData.length);
    console.log('[PredictionDetail] Recipes:', recipes.length, 'Ingredients:', ingredients.length);

    // Show all unique dates in forecast data
    const uniqueDates = [...new Set(forecastData.map(f => f.date))].sort();
    console.log('[PredictionDetail] Available forecast dates:', uniqueDates);

    forecastData.forEach(forecast => {
      const recipe = recipeMap.get(forecast.recipeId);
      if (!recipe) return;

      const fQty = forecast.quantity || (forecast as any).predictedQuantity || 0;

      // --- SUB-RECIPE EXPLOSION LOGIC ---
      recipe.ingredients.forEach((component) => {
        let quantityToAdd = 0;
        let targetId = '';

        // Case 1: Raw Ingredient
        if (component.ingredientId) {
          targetId = component.ingredientId;
          quantityToAdd = component.quantity * fQty;

          if (targetId) {
            const date = forecast.date;
            if (!ingredientTotals[targetId]) ingredientTotals[targetId] = {};
            if (!ingredientTotals[targetId][date]) ingredientTotals[targetId][date] = 0;
            ingredientTotals[targetId][date] += quantityToAdd;
          }
        }

        // Case 2: Sub-Recipe
        else if (component.childRecipeId) {
          const subRecipe = recipeMap.get(component.childRecipeId);
          if (subRecipe) {
            const totalSubWeight = subRecipe.ingredients.reduce((sum, i) => sum + i.quantity, 0);
            const amountUsed = component.quantity;

            if (totalSubWeight > 0) {
              subRecipe.ingredients.forEach(subComp => {
                if (subComp.ingredientId) {
                  const ratio = subComp.quantity / totalSubWeight;
                  const subQty = ratio * amountUsed * fQty;

                  if (!ingredientTotals[subComp.ingredientId]) ingredientTotals[subComp.ingredientId] = {};
                  if (!ingredientTotals[subComp.ingredientId][forecast.date]) ingredientTotals[subComp.ingredientId][forecast.date] = 0;
                  ingredientTotals[subComp.ingredientId][forecast.date] += subQty;
                }
              });
            }
          }
        }
      });
    });

    // --- ROW PROCESSING & UNIT CONVERSION ---
    const processedData: {
      ingredient: string;
      unit: string;
      predictions: { [key: string]: number };
    }[] = [];

    Object.entries(ingredientTotals).forEach(([ingredientId, predictions]) => {
      const ingredient = ingredientMap.get(ingredientId);
      if (ingredient) {
        // 1. Calculate max value to decide if we should upgrade unit
        const maxValue = Math.max(...Object.values(predictions));

        // Use the conversion utility to determine the best unit
        const sampleConversion = convertUnit(maxValue, ingredient.unit);
        const displayUnit = sampleConversion.unit;

        // 2. Convert all prediction values to the display unit
        const scaledPredictions: { [key: string]: number } = {};
        Object.entries(predictions).forEach(([date, val]) => {
          const converted = convertUnit(val, ingredient.unit);
          // Ensure we use the same unit across the entire row
          if (ingredient.unit === 'g' && displayUnit === 'kg') {
            scaledPredictions[date] = val / 1000;
          } else if (ingredient.unit === 'ml' && displayUnit === 'L') {
            scaledPredictions[date] = val / 1000;
          } else {
            scaledPredictions[date] = val;
          }
        });

        processedData.push({
          ingredient: ingredient.name,
          unit: displayUnit,
          predictions: scaledPredictions,
        });
      }
    });

    return processedData.sort((a, b) => a.ingredient.localeCompare(b.ingredient));
  }, [forecastData, recipes, ingredients]);

  const dates = useMemo(() => {
    const today = new Date();
    const dates = [];
    for (let i = 1; i <= 7; i++) {
      const date = addDays(today, i);
      dates.push(format(date, 'yyyy-MM-dd'));
    }
    return dates;
  }, []);

  return (
    <Card className="rounded-[8px]">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Package className="w-5 h-5 text-[#4F6F52]" />
          Ingredient Forecast Details
        </CardTitle>
        <CardDescription>
          Raw ingredient requirements for next 7 days (including sub-recipes)
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="border rounded-[8px] overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="sticky left-0 bg-white z-10">Ingredient</TableHead>
                <TableHead className="sticky left-0 bg-white z-10">Unit</TableHead>
                {dates.map((date) => (
                  <TableHead key={date} className="text-center min-w-[100px]">
                    {formatShortDate(date)}
                  </TableHead>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>
              {predictionData.map((item, index) => (
                <TableRow key={index}>
                  <TableCell className="font-medium sticky left-0 bg-white">
                    {item.ingredient}
                  </TableCell>
                  <TableCell className="sticky left-0 bg-white text-gray-500 text-sm">
                    {item.unit}
                  </TableCell>
                  {dates.map((date) => {
                    const value = item.predictions[date] || 0;
                    return (
                      <TableCell key={date} className="text-center font-mono text-sm">
                        {value > 0 ? value.toFixed(2) : '-'}
                      </TableCell>
                    );
                  })}
                </TableRow>
              ))}
              {predictionData.length === 0 && (
                <TableRow>
                  <TableCell colSpan={dates.length + 2} className="text-center py-8 text-gray-500">
                    No ingredient forecast data available.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </CardContent>
    </Card>
  );
}