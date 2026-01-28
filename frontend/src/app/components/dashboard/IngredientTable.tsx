import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { format, parseISO } from 'date-fns';
import { List } from 'lucide-react';

interface IngredientTableProps {
  date: string;
}

export function IngredientTable({ date }: IngredientTableProps) {
  const { salesData, recipes, ingredients } = useApp();

  const ingredientData = useMemo(() => {
    const ingredientMap = new Map(ingredients.map((i) => [i.id, i]));
    const recipeMap = new Map(recipes.map((r) => [r.id, r]));
    const totals: { [key: string]: number } = {};

    salesData
      .filter((sale) => sale.date === date)
      .forEach((sale) => {
        const recipe = recipeMap.get(sale.recipeId);
        if (!recipe) return;

        // --- SUB-RECIPE EXPLOSION LOGIC ---
        recipe.ingredients.forEach((component) => {
          
          // Case 1: Direct Raw Ingredient
          if (component.ingredientId) {
            const quantity = component.quantity * sale.quantity;
            totals[component.ingredientId] = (totals[component.ingredientId] || 0) + quantity;
          } 
          
          // Case 2: Sub-Recipe (e.g. Mala Sauce in Mala Chicken)
          else if (component.childRecipeId) {
            const subRecipe = recipeMap.get(component.childRecipeId);
            if (subRecipe) {
              // Calculate total weight of the sub-recipe to determine ratios
              const totalSubWeight = subRecipe.ingredients.reduce((sum, i) => sum + i.quantity, 0);
              const amountUsed = component.quantity; // Amount of sauce used in this dish

              if (totalSubWeight > 0) {
                // Break down the sub-recipe into its raw ingredients
                subRecipe.ingredients.forEach((subComp) => {
                  if (subComp.ingredientId) {
                    const ratio = subComp.quantity / totalSubWeight;
                    const finalQty = ratio * amountUsed * sale.quantity;
                    totals[subComp.ingredientId] = (totals[subComp.ingredientId] || 0) + finalQty;
                  }
                });
              }
            }
          }
        });
      });

    return Object.entries(totals)
      .map(([ingredientId, quantity]) => {
        const ingredient = ingredientMap.get(ingredientId);
        
        // --- NEW: Unit Conversion Logic (g->kg, ml->L) ---
        let displayUnit = ingredient?.unit || '';
        let displayQuantity = quantity;

        // Convert if >= 1000
        if (displayUnit === 'g' && quantity >= 1000) {
            displayUnit = 'kg';
            displayQuantity = quantity / 1000;
        } else if (displayUnit === 'ml' && quantity >= 1000) {
            displayUnit = 'L';
            displayQuantity = quantity / 1000;
        }

        return {
          name: ingredient?.name || 'Unknown',
          quantity: displayQuantity.toFixed(2), // Pure number, scaled
          unit: displayUnit, // Updated unit (e.g. "kg")
        };
      })
      .filter(item => parseFloat(item.quantity) > 0)
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [salesData, recipes, ingredients, date]);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <List className="w-5 h-5" />
          Ingredient Breakdown
        </CardTitle>
        <CardDescription>
          Required ingredients for {format(parseISO(date), 'd MMM yyyy')}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {ingredientData.length > 0 ? (
          <div className="border rounded-lg overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ingredient</TableHead>
                  <TableHead>Unit</TableHead>
                  <TableHead className="text-right">Quantity</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {ingredientData.map((item, index) => (
                  <TableRow key={index}>
                    <TableCell className="font-medium">{item.name}</TableCell>
                    <TableCell>{item.unit}</TableCell>
                    <TableCell className="text-right font-mono">{item.quantity}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        ) : (
          <div className="py-8 text-center text-gray-500">
            No ingredient data available for this date
          </div>
        )}
      </CardContent>
    </Card>
  );
}