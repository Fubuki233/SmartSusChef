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
        if (recipe) {
          recipe.ingredients.forEach((recipeIngredient) => {
            const quantity = recipeIngredient.quantity * sale.quantity;
            totals[recipeIngredient.ingredientId] =
              (totals[recipeIngredient.ingredientId] || 0) + quantity;
          });
        }
      });

    return Object.entries(totals)
      .map(([ingredientId, quantity]) => {
        const ingredient = ingredientMap.get(ingredientId);
        return {
          name: ingredient?.name || 'Unknown',
          quantity: quantity.toFixed(2),
          unit: ingredient?.unit || '',
        };
      })
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
          Required ingredients for {format(parseISO(date), 'PPP')}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {ingredientData.length > 0 ? (
          <div className="border rounded-lg">
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
                    <TableCell className="text-right">{item.quantity}</TableCell>
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
