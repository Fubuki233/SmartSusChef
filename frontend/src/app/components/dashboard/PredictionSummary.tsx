import { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend, ComposedChart, Line } from 'recharts';
import { format, addDays } from 'date-fns';
import { Sparkles } from 'lucide-react';

export function PredictionSummary() {
  const { forecastData, recipes } = useApp();

  const chartData = useMemo(() => {
    const data = [];
    const today = new Date();
    today.setHours(0, 0, 0, 0); // Normalize to start of day

    // Filter main recipes once for efficiency
    const mainRecipeIds = new Set(recipes.filter(r => !r.isSubRecipe).map(r => r.id));

    console.log('[PredictionSummary] Total forecast data:', forecastData.length);
    console.log('[PredictionSummary] Main recipe IDs:', Array.from(mainRecipeIds));
    console.log('[PredictionSummary] Today:', format(today, 'yyyy-MM-dd'));

    // Show all unique dates in forecast data
    const uniqueDates = [...new Set(forecastData.map(f => f.date))].sort();
    console.log('[PredictionSummary] Available forecast dates:', uniqueDates);
    console.log('[PredictionSummary] Date range expected: ',
      format(addDays(today, 1), 'yyyy-MM-dd'), 'to', format(addDays(today, 7), 'yyyy-MM-dd'));

    // Get next 7 days forecast
    for (let i = 1; i <= 7; i++) {
      const date = addDays(today, i);
      const dateKey = format(date, 'yyyy-MM-dd');
      const dayOfWeek = format(date, 'EEE');

      const dayForecasts = forecastData.filter((f) => f.date === dateKey && mainRecipeIds.has(f.recipeId));
      const forecast = dayForecasts.reduce((sum, f) => sum + (f.quantity || (f as any).predictedQuantity || 0), 0);

      console.log(`[PredictionSummary] ${dateKey}: ${dayForecasts.length} forecasts, total: ${forecast}`);

      data.push({
        date: dateKey,
        day: dayOfWeek,
        displayDate: format(date, 'd MMM'),
        forecast,
      });
    }

    console.log('[PredictionSummary] Chart data:', data);
    return data;
  }, [forecastData, recipes]);

  const totalForecast = chartData.reduce((sum, item) => sum + item.forecast, 0);

  return (
    <Card className="rounded-[8px]">
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-[#8E7AB5]" />
              Prediction Summary
            </CardTitle>
            <CardDescription>
              Forecasted main dish sales for next 7 days
            </CardDescription>
          </div>
          <div className="text-right">
            <div className="text-2xl font-bold">{totalForecast}</div>
            <div className="text-sm text-gray-500">
              Total Dishes
            </div>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <ComposedChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="displayDate"
              tick={{ fontSize: 12 }}
              angle={-45}
              textAnchor="end"
              height={60}
            />
            <YAxis tick={{ fontSize: 12 }} />
            <Tooltip formatter={(value: number) => [value, 'Dishes']} />
            <Legend />
            <Bar
              dataKey="forecast"
              fill="#B4A373"
              name="Forecast"
              radius={[8, 8, 0, 0]}
            />
            <Line
              type="monotone"
              dataKey="forecast"
              stroke="#E74C3C"
              strokeWidth={3}
              dot={{ fill: '#E74C3C', r: 4 }}
              name="Forecast Trend"
            />
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}