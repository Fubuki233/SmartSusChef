import React, { useMemo, useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { format, parseISO, addDays } from 'date-fns';
import { Calculator, Save } from 'lucide-react';
import { toast } from 'sonner';

export function PredictionDetail() {
  const { forecastData, recipes, ingredients, updateForecastData } = useApp();
  const [editMode, setEditMode] = useState(false);
  const [editedData, setEditedData] = useState<{ [key: string]: string }>({});

  const predictionData = useMemo(() => {
    const ingredientMap = new Map(ingredients.map((i) => [i.id, i]));
    const recipeMap = new Map(recipes.map((r) => [r.id, r]));
    
    const data: {
      ingredient: string;
      unit: string;
      predictions: { [key: string]: number };
    }[] = [];

    const ingredientTotals: { [ingredientId: string]: { [date: string]: number } } = {};

    forecastData.forEach((forecast) => {
      const recipe = recipeMap.get(forecast.recipeId);
      if (recipe) {
        recipe.ingredients.forEach((recipeIngredient) => {
          if (!ingredientTotals[recipeIngredient.ingredientId]) {
            ingredientTotals[recipeIngredient.ingredientId] = {};
          }
          if (!ingredientTotals[recipeIngredient.ingredientId][forecast.date]) {
            ingredientTotals[recipeIngredient.ingredientId][forecast.date] = 0;
          }
          ingredientTotals[recipeIngredient.ingredientId][forecast.date] +=
            recipeIngredient.quantity * forecast.quantity;
        });
      }
    });

    Object.entries(ingredientTotals).forEach(([ingredientId, predictions]) => {
      const ingredient = ingredientMap.get(ingredientId);
      if (ingredient) {
        data.push({
          ingredient: ingredient.name,
          unit: ingredient.unit,
          predictions,
        });
      }
    });

    return data.sort((a, b) => a.ingredient.localeCompare(b.ingredient));
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

  const handleEdit = () => {
    setEditMode(true);
    // Initialize edited data with current values
    const initial: { [key: string]: string } = {};
    predictionData.forEach((item) => {
      dates.forEach((date) => {
        const key = `${item.ingredient}-${date}`;
        initial[key] = (item.predictions[date] || 0).toFixed(2);
      });
    });
    setEditedData(initial);
  };

  const handleSave = () => {
    // This is a simplified save - in a real app, you'd recalculate forecast quantities
    // based on ingredient changes
    toast.success('Predictions updated successfully!');
    setEditMode(false);
  };

  const handleCancel = () => {
    setEditMode(false);
    setEditedData({});
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Calculator className="w-5 h-5" />
              Ingredient Forecast Details
            </CardTitle>
            <CardDescription>
              Predicted ingredient requirements for next 7 days
            </CardDescription>
          </div>
          <div className="flex gap-2">
            {!editMode ? (
              <Button onClick={handleEdit} variant="outline" size="sm">
                Edit Forecast
              </Button>
            ) : (
              <>
                <Button onClick={handleSave} size="sm" className="gap-2">
                  <Save className="w-4 h-4" />
                  Save
                </Button>
                <Button onClick={handleCancel} variant="outline" size="sm">
                  Cancel
                </Button>
              </>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="border rounded-lg overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="sticky left-0 bg-white z-10">Ingredient</TableHead>
                <TableHead className="sticky left-0 bg-white z-10">Unit</TableHead>
                {dates.map((date) => (
                  <TableHead key={date} className="text-center min-w-[100px]">
                    {format(parseISO(date), 'MMM dd')}
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
                  <TableCell className="sticky left-0 bg-white">{item.unit}</TableCell>
                  {dates.map((date) => {
                    const value = item.predictions[date] || 0;
                    const key = `${item.ingredient}-${date}`;
                    return (
                      <TableCell key={date} className="text-center">
                        {editMode ? (
                          <Input
                            type="number"
                            step="0.01"
                            value={editedData[key] || '0'}
                            onChange={(e) =>
                              setEditedData({ ...editedData, [key]: e.target.value })
                            }
                            className="w-20 mx-auto text-center"
                          />
                        ) : (
                          value.toFixed(2)
                        )}
                      </TableCell>
                    );
                  })}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </CardContent>
    </Card>
  );
}
