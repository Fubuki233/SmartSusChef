import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { format, subDays, parseISO } from 'date-fns';
import { Trash2, Leaf } from 'lucide-react';

interface WastageTrendChartProps {
  dateRange: 'today' | '7days' | 'custom';
  onDateRangeChange: (range: 'today' | '7days' | 'custom') => void;
  maxDays?: number;
  onBarClick?: (date: string) => void;
}

export function WastageTrendChart({
  dateRange,
  onDateRangeChange,
  maxDays = 30,
  onBarClick,
}: WastageTrendChartProps) {
  const { wastageData, ingredients } = useApp();

  const { chartData, totalCarbonFootprint } = useMemo(() => {
    const today = new Date();
    const daysToShow = dateRange === 'today' ? 1 : Math.min(maxDays, dateRange === '7days' ? 7 : 30);
    const startDate = subDays(today, daysToShow - 1);

    const ingredientMap = new Map(ingredients.map((i) => [i.id, { ...i }]));
    const groupedByDate: { [key: string]: { quantity: number; carbon: number } } = {};

    wastageData.forEach((waste) => {
      const wasteDate = parseISO(waste.date);
      if (wasteDate >= startDate && wasteDate <= today) {
        const dateKey = waste.date;
        const ingredient = ingredientMap.get(waste.ingredientId);
        
        if (!groupedByDate[dateKey]) {
          groupedByDate[dateKey] = { quantity: 0, carbon: 0 };
        }
        
        groupedByDate[dateKey].quantity += waste.quantity;
        if (ingredient) {
          groupedByDate[dateKey].carbon += waste.quantity * ingredient.carbonFootprint;
        }
      }
    });

    const data = [];
    let totalCarbon = 0;
    
    for (let i = daysToShow - 1; i >= 0; i--) {
      const date = subDays(today, i);
      const dateKey = format(date, 'yyyy-MM-dd');
      const dayData = groupedByDate[dateKey] || { quantity: 0, carbon: 0 };
      totalCarbon += dayData.carbon;
      
      data.push({
        date: dateKey,
        displayDate: format(date, 'MMM dd'),
        wastage: parseFloat(dayData.quantity.toFixed(2)),
        carbon: parseFloat(dayData.carbon.toFixed(2)),
      });
    }

    return { chartData: data, totalCarbonFootprint: totalCarbon };
  }, [wastageData, ingredients, dateRange, maxDays]);

  const handleBarClick = (data: any) => {
    if (onBarClick && data && data.date) {
      onBarClick(data.date);
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Trash2 className="w-5 h-5 text-orange-600" />
              Wastage Trend
            </CardTitle>
            <CardDescription className="flex items-center gap-2 mt-2">
              <Leaf className="w-4 h-4 text-green-600" />
              Total Carbon Footprint: {totalCarbonFootprint.toFixed(2)} kg CO₂
            </CardDescription>
          </div>
          <Select value={dateRange} onValueChange={(value: any) => onDateRangeChange(value)}>
            <SelectTrigger className="w-32">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="today">Today</SelectItem>
              <SelectItem value="7days">Last 7 Days</SelectItem>
              {maxDays > 7 && <SelectItem value="custom">Last 30 Days</SelectItem>}
            </SelectContent>
          </Select>
        </div>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <ComposedChart data={chartData} onClick={handleBarClick}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis
              dataKey="displayDate"
              tick={{ fontSize: 12 }}
              angle={-45}
              textAnchor="end"
              height={60}
            />
            <YAxis yAxisId="left" tick={{ fontSize: 12 }} />
            <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 12 }} />
            <Tooltip />
            <Legend />
            <Bar
              yAxisId="left"
              dataKey="wastage"
              fill="#f59e0b"
              name="Wastage (kg)"
              radius={[8, 8, 0, 0]}
              cursor="pointer"
            />
            <Line
              yAxisId="right"
              type="monotone"
              dataKey="carbon"
              stroke="#ef4444"
              strokeWidth={2}
              dot={{ fill: '#ef4444', r: 4 }}
              name="Carbon (kg CO₂)"
            />
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}
