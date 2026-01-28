import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Badge } from '@/app/components/ui/badge';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';
import { format, parseISO } from 'date-fns';
import { PieChartIcon, List, Leaf } from 'lucide-react';

interface WastageDistributionProps {
  date: string;
}

const CATEGORY_COLORS = {
  'Main Dishes': '#3498DB',    // Blue
  'Sub-Recipes': '#E67E22',    // Orange
  'Raw Ingredients': '#95A5A6' // Gray
};

export function WastageDistribution({ date }: WastageDistributionProps) {
  const { wastageData, ingredients, recipes } = useApp();

  const { pieData, tableData } = useMemo(() => {
    const ingredientMap = new Map(ingredients.map((i) => [i.id, i]));
    const recipeMap = new Map(recipes.map((r) => [r.id, r]));

    const categoryTotals: { [key: string]: number } = {
      'Main Dishes': 0,
      'Sub-Recipes': 0,
      'Raw Ingredients': 0
    };

    const itemDetails: Array<{ 
      name: string; 
      type: 'Dish' | 'Sub-Recipe' | 'Raw'; 
      unit: string; 
      quantity: number; 
      carbon: number;
      badgeColor: string;
    }> = [];

    // --- HELPER: Convert Quantity to Standard Unit (kg/L) ---
    const getStandardizedQuantity = (qty: number, unit: string) => {
      if (unit === 'g' || unit === 'ml') {
        return qty / 1000;
      }
      return qty; // Assumes kg, L, or 'plate' is already standard
    };

    wastageData
      .filter((waste) => waste.date === date)
      .forEach((waste) => {
        let itemProcessed = false;
        let calculatedCarbon = 0;

        // --- Check for Recipe ID ---
        if (waste.recipeId) {
          const recipe = recipeMap.get(waste.recipeId);
          if (recipe) {
            const type = recipe.isSubRecipe ? 'Sub-Recipe' : 'Dish';
            const category = recipe.isSubRecipe ? 'Sub-Recipes' : 'Main Dishes';
            const unit = recipe.isSubRecipe ? 'L' : 'plate';
            const badgeColor = recipe.isSubRecipe ? 'bg-[#E67E22]' : 'bg-[#3498DB]';
            
            // Mock carbon calculation for dishes (0.5 multiplier per plate/unit)
            // For sub-recipes (L), we should also ideally check ingredients, but using 0.5 as proxy for now
            calculatedCarbon = waste.quantity * 0.5;
            
            categoryTotals[category] += calculatedCarbon;
            
            itemDetails.push({
              name: recipe.name,
              type: type,
              unit: unit,
              quantity: waste.quantity,
              carbon: calculatedCarbon,
              badgeColor
            });
            itemProcessed = true;
          }
        } 
        
        // --- Check for Ingredient ID ---
        if (!itemProcessed && waste.ingredientId) {
          const ingredient = ingredientMap.get(waste.ingredientId);
          if (ingredient) {
            // FIX: Convert grams to kg before multiplying by Carbon Factor
            const standardQty = getStandardizedQuantity(waste.quantity, ingredient.unit);
            calculatedCarbon = standardQty * ingredient.carbonFootprint;
            
            categoryTotals['Raw Ingredients'] += calculatedCarbon;
            
            itemDetails.push({
              name: ingredient.name,
              type: 'Raw',
              unit: ingredient.unit,
              quantity: waste.quantity,
              carbon: calculatedCarbon,
              badgeColor: 'bg-[#95A5A6]'
            });
          }
        }
      });

    const pieChartData = Object.entries(categoryTotals)
      .map(([name, value]) => ({ name, value }))
      .filter(item => item.value > 0);

    return {
      pieData: pieChartData,
      tableData: itemDetails.sort((a, b) => b.carbon - a.carbon),
    };
  }, [wastageData, ingredients, recipes, date]);

  const totalCarbonFootprint = tableData.reduce((sum, item) => sum + item.carbon, 0);

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {/* Pie Chart - Wastage Sources (By Carbon) */}
      <Card className="rounded-[8px]">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PieChartIcon className="w-5 h-5" />
            Wastage Impact (by CO₂)
          </CardTitle>
          <CardDescription>
            Carbon footprint breakdown - {format(parseISO(date), 'd MMM yyyy')}
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
                  label={({ name, percent }) => `${(percent * 100).toFixed(0)}%`}
                  outerRadius={80}
                  dataKey="value"
                >
                  {pieData.map((entry, index) => (
                    <Cell 
                      key={`cell-${index}`} 
                      fill={CATEGORY_COLORS[entry.name as keyof typeof CATEGORY_COLORS] || '#8884d8'} 
                    />
                  ))}
                </Pie>
                <Tooltip formatter={(value: number) => [`${value.toFixed(2)} kg`, 'CO₂ Emission']} />
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

      {/* Table - Top Wasted Items */}
      <Card className="rounded-[8px]">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <List className="w-5 h-5" />
            Top Wasted Items
          </CardTitle>
          <CardDescription className="flex items-center gap-2">
            <Leaf className="w-3 h-3 text-[#4F6F52]" />
            Total CO₂: {totalCarbonFootprint.toFixed(2)} kg
          </CardDescription>
        </CardHeader>
        <CardContent>
          {tableData.length > 0 ? (
            <div className="border rounded-[8px] overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Item Name</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead className="text-right">Quantity</TableHead>
                    <TableHead className="text-right">CO₂ (kg)</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {tableData.map((item, index) => (
                    <TableRow key={index}>
                      <TableCell className="font-medium">{item.name}</TableCell>
                      <TableCell>
                        <Badge className={`${item.badgeColor} text-white border-none`}>
                          {item.type}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">{item.quantity.toFixed(2)} {item.unit}</TableCell>
                      <TableCell className="text-right font-medium text-[#E74C3C]">{item.carbon.toFixed(2)}</TableCell>
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