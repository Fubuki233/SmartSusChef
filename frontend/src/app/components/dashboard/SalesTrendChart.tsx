import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { format, subDays, parseISO } from 'date-fns';
import { TrendingUp } from 'lucide-react';

interface SalesTrendChartProps {
  dateRange: 'today' | '7days' | '30days' | '90days' | 'all' | 'custom';
  onDateRangeChange: (range: 'today' | '7days' | '30days' | '90days' | 'all' | 'custom') => void;
  maxDays?: number;
  onBarClick?: (date: string) => void;
  selectedDate?: string | null;
}

export function SalesTrendChart({
  dateRange,
  onDateRangeChange,
  maxDays,
  onBarClick,
  selectedDate,
}: SalesTrendChartProps) {
  const { salesData, recipes } = useApp();

  const chartData = useMemo(() => {
    const today = new Date();
    today.setHours(23, 59, 59, 999); // Set to end of today

    // Calculate daysToShow based on the selected range
    let daysToShow: number;
    if (dateRange === 'today') {
      daysToShow = 1;
    } else if (dateRange === '7days') {
      daysToShow = 7;
    } else if (dateRange === '30days') {
      daysToShow = 30;
    } else if (dateRange === '90days') {
      daysToShow = 90;
    } else if (dateRange === 'all') {
      // Find the earliest date in sales data
      if (salesData.length === 0) {
        daysToShow = 30;
      } else {
        const earliest = salesData.reduce((min, s) => s.date < min ? s.date : min, salesData[0].date);
        const earliestDate = new Date(earliest);
        earliestDate.setHours(0, 0, 0, 0);
        daysToShow = Math.max(1, Math.ceil((today.getTime() - earliestDate.getTime()) / (1000 * 60 * 60 * 24)) + 1);
      }
    } else {
      daysToShow = maxDays ? Math.min(maxDays, 30) : 30;
    }

    // Apply maxDays cap if set
    if (maxDays && dateRange !== 'all') {
      daysToShow = Math.min(daysToShow, maxDays);
    }

    const startDate = subDays(today, daysToShow - 1);
    startDate.setHours(0, 0, 0, 0); // Set to start of the first day

    // Filter to ensure we only count Main Dishes (exclude sub-recipes)
    const mainRecipeIds = new Set(recipes.filter(r => !r.isSubRecipe).map(r => r.id));

    const groupedByDate: { [key: string]: number } = {};

    salesData.forEach((sale) => {
      const saleDate = parseISO(sale.date);
      // Only count if it is a Main Dish
      if (saleDate >= startDate && saleDate <= today && mainRecipeIds.has(sale.recipeId)) {
        const dateKey = sale.date;
        if (!groupedByDate[dateKey]) {
          groupedByDate[dateKey] = 0;
        }
        groupedByDate[dateKey] += sale.quantity;
      }
    });

    const data = [];
    for (let i = daysToShow - 1; i >= 0; i--) {
      const date = subDays(today, i);
      const dateKey = format(date, 'yyyy-MM-dd');
      data.push({
        date: dateKey,
        displayDate: format(date, 'd MMM'),
        sales: groupedByDate[dateKey] || 0,
      });
    }

    return data;
  }, [salesData, recipes, dateRange, maxDays]);

  const averageSales = useMemo(() => {
    const total = chartData.reduce((sum, item) => sum + item.sales, 0);
    return chartData.length > 0 ? Math.round(total / chartData.length) : 0;
  }, [chartData]);

  const handleBarClick = (data: any) => {
    if (onBarClick && data && data.date) {
      onBarClick(data.date);
    }
  };

  // Custom bar shape to show selected state
  const CustomBar = (props: any) => {
    const { x, y, width, height, fill } = props;
    const isSelected = selectedDate === props.payload?.date;

    return (
      <g>
        <rect
          x={x}
          y={y}
          width={width}
          height={height}
          fill={isSelected ? '#3A4D39' : fill}
          rx={8}
          ry={8}
          className="cursor-pointer transition-all hover:opacity-80"
        />
        {isSelected && (
          <rect
            x={x}
            y={y}
            width={width}
            height={height}
            fill="none"
            stroke="#E74C3C"
            strokeWidth={3}
            rx={8}
            ry={8}
            pointerEvents="none"
          />
        )}
      </g>
    );
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="w-5 h-5" />
              Sales Trend
            </CardTitle>
            <CardDescription>
              Total dishes sold · Average: {averageSales} per day
              <span className="block mt-1 text-[#27AE60] font-medium">
                Click on any bar to view details
              </span>
            </CardDescription>
          </div>
          <Select value={dateRange} onValueChange={(value: any) => onDateRangeChange(value)}>
            <SelectTrigger className="w-36">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="today">Today</SelectItem>
              <SelectItem value="7days">Last 7 Days</SelectItem>
              {(!maxDays || maxDays > 7) && <SelectItem value="30days">Last 30 Days</SelectItem>}
              {(!maxDays || maxDays > 30) && <SelectItem value="90days">Last 90 Days</SelectItem>}
              {!maxDays && <SelectItem value="all">All Time</SelectItem>}
            </SelectContent>
          </Select>
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
            <Tooltip formatter={(value: number) => [value, 'Dishes Sold']} />
            <Legend />
            <Bar
              dataKey="sales"
              fill="#4F6F52"
              name="Total Sales"
              shape={<CustomBar />}
              onClick={handleBarClick}
            />
            <Line
              type="monotone"
              dataKey="sales"
              stroke="#E74C3C"
              strokeWidth={3}
              dot={{ fill: '#E74C3C', r: 4 }}
              name="Trend"
            />
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}