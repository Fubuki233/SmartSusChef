import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';
import { format, parseISO } from 'date-fns';
import { PieChartIcon } from 'lucide-react';

interface DistributionPieChartProps {
  date: string;
}

const COLORS = ['#16a34a', '#0ea5e9', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

export function DistributionPieChart({ date }: DistributionPieChartProps) {
  const { salesData, recipes } = useApp();

  const chartData = useMemo(() => {
    const recipeMap = new Map(recipes.map((r) => [r.id, r.name]));
    const distribution: { [key: string]: number } = {};

    salesData
      .filter((sale) => sale.date === date)
      .forEach((sale) => {
        const recipeName = recipeMap.get(sale.recipeId) || 'Unknown';
        distribution[recipeName] = (distribution[recipeName] || 0) + sale.quantity;
      });

    return Object.entries(distribution).map(([name, value]) => ({
      name,
      value,
    }));
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
          {format(parseISO(date), 'PPP')} · Total: {total} dishes
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
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
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
