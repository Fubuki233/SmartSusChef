import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';
import { format, parseISO } from 'date-fns';
import { PieChartIcon } from 'lucide-react';

interface DistributionPieChartProps {
  date: string;
}

// Global 10-Color Pie Chart Palette
const PIE_COLORS = [
  '#4F6F52', // 1. Deep Forest Sage - Primary
  '#E67E22', // 2. Burnt Orange - Secondary
  '#8A9A5B', // 3. Muted Olive - Tertiary
  '#B4A373', // 4. Muted Gold - Neutral
  '#B16A17', // 5. Dark Orange - Accent
  '#DDAB68', // 6. Light Gold - Highlight
  '#359290', // 7. Teal - Cool Accent
  '#6CB4EE', // 8. Sky Blue - Light Blue
  '#8E7AB5', // 9. Lavender - Purple
];

const OTHERS_COLOR = '#898989'; // 10. Grey - Reserved for "Others"

export function DistributionPieChart({ date }: DistributionPieChartProps) {
  const { salesData, recipes } = useApp();

  const chartData = useMemo(() => {
    // Create a map of ONLY main dishes (exclude sub-recipes)
    const recipeMap = new Map(
      recipes
        .filter(r => !r.isSubRecipe)
        .map((r) => [r.id, r.name])
    );
    
    const distribution: { [key: string]: number } = {};

    salesData
      .filter((sale) => sale.date === date)
      .forEach((sale) => {
        // If the ID isn't in our Main Dish map, it will be undefined (filtered out)
        const recipeName = recipeMap.get(sale.recipeId);
        
        if (recipeName) {
            distribution[recipeName] = (distribution[recipeName] || 0) + sale.quantity;
        }
      });

    // Convert to array and sort by value descending
    const entries = Object.entries(distribution)
      .map(([name, value]) => ({ name, value }))
      .sort((a, b) => b.value - a.value);

    // If more than 9 items, group the rest into "Others"
    if (entries.length > 9) {
      const top9 = entries.slice(0, 9);
      const others = entries.slice(9);
      const othersTotal = others.reduce((sum, item) => sum + item.value, 0);
      
      return [...top9, { name: 'Others', value: othersTotal }];
    }

    return entries;
  }, [salesData, recipes, date]);

  const total = chartData.reduce((sum, item) => sum + item.value, 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <PieChartIcon className="w-5 h-5" />
          Sales Distribution
        </CardTitle>
        <CardDescription>
          {format(parseISO(date), 'd MMM yyyy')} · Total: {total} dishes
        </CardDescription>
      </CardHeader>
      <CardContent>
        {chartData.length > 0 ? (
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={chartData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="value"
              >
                {chartData.map((entry, index) => (
                  <Cell 
                    key={`cell-${index}`} 
                    fill={entry.name === 'Others' ? OTHERS_COLOR : PIE_COLORS[index % PIE_COLORS.length]} 
                  />
                ))}
              </Pie>
              <Tooltip />
              <Legend />
            </PieChart>
          </ResponsiveContainer>
        ) : (
          <div className="h-[300px] flex items-center justify-center text-gray-500">
            No sales data for this date
          </div>
        )}
      </CardContent>
    </Card>
  );
}