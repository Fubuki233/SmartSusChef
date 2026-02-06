import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { format, addDays } from 'date-fns';
import { TrendingUp } from 'lucide-react';

// Global 10-Color Pie Chart Palette (for stacked bars)
const CHART_COLORS = [
  '#4F6F52', // 1. Deep Forest Sage - Primary
  '#E67E22', // 2. Burnt Orange - Secondary
  '#8A9A5B', // 3. Muted Olive - Tertiary
  '#B4A373', // 4. Muted Gold - Neutral
  '#B16A17', // 5. Dark Orange - Accent
  '#DDAB68', // 6. Light Gold - Highlight
  '#359290', // 7. Teal - Cool Accent
  '#6CB4EE', // 8. Sky Blue - Light Blue
  '#8E7AB5', // 9. Lavender - Purple
  '#898989', // 10. Grey - Others
];

export function DishesForecast() {
  const { forecastData, recipes } = useApp();

  // Filter only main dishes for the forecast
  const mainRecipes = useMemo(() => {
    return recipes.filter(r => !r.isSubRecipe);
  }, [recipes]);

  const chartData = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0); // Normalize to start of day
    const data: any[] = [];

    console.log('[DishesForecast] Total forecast data:', forecastData.length);
    console.log('[DishesForecast] Main recipes:', mainRecipes.length);
    console.log('[DishesForecast] Today:', format(today, 'yyyy-MM-dd'));
    const uniqueDates = [...new Set(forecastData.map(f => f.date))].sort();
    console.log('[DishesForecast] Available forecast dates:', uniqueDates);
    console.log('[DishesForecast] Date range expected: ',
      format(addDays(today, 1), 'yyyy-MM-dd'), 'to', format(addDays(today, 7), 'yyyy-MM-dd'));

    // Get next 7 days of forecast (starting from tomorrow)
    for (let i = 1; i <= 7; i++) {
      const date = addDays(today, i);
      const dateKey = format(date, 'yyyy-MM-dd');
      const dayOfWeek = format(date, 'EEE');

      // Aggregate forecast by recipe for this date
      const recipeForecasts: { [key: string]: number } = {};

      const dayForecasts = forecastData.filter((f) => f.date === dateKey);
      console.log(`[DishesForecast] ${dateKey}: ${dayForecasts.length} forecasts`);

      dayForecasts.forEach((forecast) => {
        const recipe = mainRecipes.find(r => r.id === forecast.recipeId);
        if (recipe) {
          const qty = forecast.quantity || (forecast as any).predictedQuantity || 0;
          recipeForecasts[recipe.name] = (recipeForecasts[recipe.name] || 0) + qty;
        }
      });

      const dayData: any = {
        date: dateKey,
        displayDate: format(date, 'd MMM'),
        day: dayOfWeek,
      };

      // Add each main recipe as a separate key
      mainRecipes.forEach(recipe => {
        dayData[recipe.name] = recipeForecasts[recipe.name] || 0;
      });

      data.push(dayData);
    }

    return data;
  }, [forecastData, mainRecipes]);

  const totalForWeek = chartData.reduce((sum, day) => {
    let dayTotal = 0;
    mainRecipes.forEach(recipe => {
      dayTotal += day[recipe.name] || 0;
    });
    return sum + dayTotal;
  }, 0);
  const averagePerDay = totalForWeek / 7;

  return (
    <Card className="rounded-[8px]">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <TrendingUp className="w-5 h-5 text-[#4F6F52]" />
          Dishes Forecast
        </CardTitle>
        <CardDescription>
          AI-predicted main dishes breakdown for the next 7 days · Avg: {averagePerDay.toFixed(0)} dishes/day
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={400}>
          <BarChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
            <XAxis
              dataKey="displayDate"
              tick={{ fontSize: 12 }}
              angle={-45}
              textAnchor="end"
              height={60}
            />
            <YAxis
              label={{ value: 'Total Dishes', angle: -90, position: 'insideLeft' }}
              tick={{ fontSize: 12 }}
            />
            <Tooltip
              content={({ active, payload }) => {
                if (active && payload && payload.length) {
                  const data = payload[0].payload;
                  let total = 0;
                  return (
                    <div className="bg-white p-3 border rounded-lg shadow-lg max-w-xs">
                      <p className="font-semibold text-sm">{data.day}</p>
                      <p className="text-xs text-gray-600 mb-2">{data.displayDate}</p>
                      <div className="space-y-1">
                        {payload.map((entry: any, index: number) => {
                          const value = entry.value || 0;
                          total += value;
                          if (value > 0) {
                            return (
                              <div key={index} className="flex items-center justify-between gap-4 text-xs">
                                <div className="flex items-center gap-2">
                                  <div
                                    className="w-3 h-3 rounded-sm"
                                    style={{ backgroundColor: entry.fill }}
                                  />
                                  <span>{entry.name}</span>
                                </div>
                                <span className="font-semibold">{value}</span>
                              </div>
                            );
                          }
                          return null;
                        })}
                      </div>
                      <div className="mt-2 pt-2 border-t">
                        <p className="text-sm font-semibold text-[#4F6F52]">
                          Total: {total} dishes
                        </p>
                      </div>
                    </div>
                  );
                }
                return null;
              }}
            />
            <Legend wrapperStyle={{ fontSize: '12px' }} />
            {mainRecipes.map((recipe, index) => (
              <Bar
                key={recipe.id}
                dataKey={recipe.name}
                stackId="dishes"
                fill={CHART_COLORS[index % CHART_COLORS.length]}
                radius={index === mainRecipes.length - 1 ? [4, 4, 0, 0] : [0, 0, 0, 0]}
              />
            ))}
          </BarChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}