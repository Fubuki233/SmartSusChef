import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';
import { format, parseISO } from 'date-fns';
import { PieChartIcon, List } from 'lucide-react';

interface WastageDistributionProps {
  date: string;
}

const COLORS = ['#f59e0b', '#ef4444', '#ec4899', '#8b5cf6', '#0ea5e9', '#16a34a'];

export function WastageDistribution({ date }: WastageDistributionProps) {
  const { wastageData, ingredients, recipes, salesData } = useApp();

  const { pieData, tableData } = useMemo(() => {
    const ingredientMap = new Map(ingredients.map((i) => [i.id, i]));
    const recipeMap = new Map(recipes.map((r) => [r.id, r]));

    // Calculate wastage by recipe (based on ingredients used)
    const recipeWastage: { [key: string]: number } = {};
    const ingredientDetails: Array<{ name: string; unit: string; quantity: number; carbon: number }> = [];

    // Get wastage by ingredient
    wastageData
      .filter((waste) => waste.date === date)
      .forEach((waste) => {
        const ingredient = ingredientMap.get(waste.ingredientId);
        if (ingredient) {
          ingredientDetails.push({
            name: ingredient.name,
            unit: ingredient.unit,
            quantity: waste.quantity,
            carbon: waste.quantity * ingredient.carbonFootprint,
          });

          // Find which recipes use this ingredient
          recipes.forEach((recipe) => {
            const usesIngredient = recipe.ingredients.find(
              (ri) => ri.ingredientId === waste.ingredientId
            );
            if (usesIngredient) {
              if (!recipeWastage[recipe.name]) {
                recipeWastage[recipe.name] = 0;
              }
              recipeWastage[recipe.name] += waste.quantity;
            }
          });
        }
      });

    const pieChartData = Object.entries(recipeWastage).map(([name, value]) => ({
      name,
      value: parseFloat(value.toFixed(2)),
    }));

    return {
      pieData: pieChartData,
      tableData: ingredientDetails.sort((a, b) => a.name.localeCompare(b.name)),
    };
  }, [wastageData, ingredients, recipes, date]);

  const totalCarbonFootprint = tableData.reduce((sum, item) => sum + item.carbon, 0);

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {/* Pie Chart - Wastage by Recipe */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PieChartIcon className="w-5 h-5" />
            Wastage by Recipe
          </CardTitle>
          <CardDescription>
            {format(parseISO(date), 'PPP')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {pieData.length > 0 ? (
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {pieData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <div className="h-[300px] flex items-center justify-center text-gray-500">
              No wastage data for this date
            </div>
          )}
        </CardContent>
      </Card>

      {/* Table - Wastage by Ingredient */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <List className="w-5 h-5" />
            Wastage by Ingredient
          </CardTitle>
          <CardDescription>
            Total CO₂: {totalCarbonFootprint.toFixed(2)} kg
          </CardDescription>
        </CardHeader>
        <CardContent>
          {tableData.length > 0 ? (
            <div className="border rounded-lg">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Ingredient</TableHead>
                    <TableHead>Unit</TableHead>
                    <TableHead className="text-right">Quantity</TableHead>
                    <TableHead className="text-right">CO₂ (kg)</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {tableData.map((item, index) => (
                    <TableRow key={index}>
                      <TableCell className="font-medium">{item.name}</TableCell>
                      <TableCell>{item.unit}</TableCell>
                      <TableCell className="text-right">{item.quantity.toFixed(2)}</TableCell>
                      <TableCell className="text-right">{item.carbon.toFixed(2)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          ) : (
            <div className="py-8 text-center text-gray-500">
              No wastage data available for this date
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
