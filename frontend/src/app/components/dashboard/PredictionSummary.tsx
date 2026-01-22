import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { format, parseISO, addDays, subDays } from 'date-fns';
import { Sparkles, TrendingUp, TrendingDown } from 'lucide-react';

export function PredictionSummary() {
  const { forecastData, salesData } = useApp();

  const chartData = useMemo(() => {
    const data = [];
    const today = new Date();

    // Get last week's data for comparison
    const lastWeekTotal: { [key: string]: number } = {};
    for (let i = 7; i >= 1; i--) {
      const date = subDays(today, i);
      const dateKey = format(date, 'yyyy-MM-dd');
      const dayOfWeek = format(date, 'EEE');
      
      const dailySales = salesData
        .filter((s) => s.date === dateKey)
        .reduce((sum, s) => sum + s.quantity, 0);
      
      if (!lastWeekTotal[dayOfWeek]) {
        lastWeekTotal[dayOfWeek] = 0;
      }
      lastWeekTotal[dayOfWeek] += dailySales;
    }

    // Get next 7 days forecast
    for (let i = 1; i <= 7; i++) {
      const date = addDays(today, i);
      const dateKey = format(date, 'yyyy-MM-dd');
      const dayOfWeek = format(date, 'EEE');

      const forecast = forecastData
        .filter((f) => f.date === dateKey)
        .reduce((sum, f) => sum + f.quantity, 0);

      data.push({
        date: dateKey,
        day: dayOfWeek,
        displayDate: format(date, 'MMM dd'),
        forecast,
        lastWeek: lastWeekTotal[dayOfWeek] || 0,
      });
    }

    return data;
  }, [forecastData, salesData]);

  const totalForecast = chartData.reduce((sum, item) => sum + item.forecast, 0);
  const totalLastWeek = chartData.reduce((sum, item) => sum + item.lastWeek, 0);
  const percentChange = totalLastWeek > 0
    ? ((totalForecast - totalLastWeek) / totalLastWeek) * 100
    : 0;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-purple-600" />
              Prediction Summary
            </CardTitle>
            <CardDescription>
              Forecasted sales for next 7 days
            </CardDescription>
          </div>
          <div className="text-right">
            <div className="text-2xl font-bold">{totalForecast}</div>
            <div className={`text-sm flex items-center gap-1 ${
              percentChange >= 0 ? 'text-green-600' : 'text-red-600'
            }`}>
              {percentChange >= 0 ? (
                <TrendingUp className="w-4 h-4" />
              ) : (
                <TrendingDown className="w-4 h-4" />
              )}
              {Math.abs(percentChange).toFixed(1)}% vs last week
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
            <Tooltip />
            <Legend />
            <Bar
              dataKey="forecast"
              fill="#9333ea"
              name="Forecast"
              radius={[8, 8, 0, 0]}
            />
            <Bar
              dataKey="lastWeek"
              fill="#94a3b8"
              name="Last Week"
              radius={[8, 8, 0, 0]}
            />
            <Line
              type="monotone"
              dataKey="forecast"
              stroke="#7c3aed"
              strokeWidth={2}
              dot={{ fill: '#7c3aed', r: 4 }}
              name="Forecast Trend"
            />
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}
