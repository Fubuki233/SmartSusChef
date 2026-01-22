import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { format, subDays, parseISO } from 'date-fns';
import { TrendingUp } from 'lucide-react';

interface SalesTrendChartProps {
  dateRange: 'today' | '7days' | 'custom';
  onDateRangeChange: (range: 'today' | '7days' | 'custom') => void;
  maxDays?: number;
  onBarClick?: (date: string) => void;
  selectedDate?: string | null;
}

export function SalesTrendChart({
  dateRange,
  onDateRangeChange,
  maxDays = 30,
  onBarClick,
  selectedDate,
}: SalesTrendChartProps) {
  const { salesData, recipes } = useApp();

  const chartData = useMemo(() => {
    const today = new Date();
    const daysToShow = dateRange === 'today' ? 1 : Math.min(maxDays, dateRange === '7days' ? 7 : 30);
    const startDate = subDays(today, daysToShow - 1);

    const groupedByDate: { [key: string]: number } = {};

    salesData.forEach((sale) => {
      const saleDate = parseISO(sale.date);
      if (saleDate >= startDate && saleDate <= today) {
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
        displayDate: format(date, 'MMM dd'),
        sales: groupedByDate[dateKey] || 0,
      });
    }

    return data;
  }, [salesData, dateRange, maxDays]);

  const averageSales = useMemo(() => {
    const total = chartData.reduce((sum, item) => sum + item.sales, 0);
    return Math.round(total / chartData.length);
  }, [chartData]);

  const handleBarClick = (data: any) => {
    if (onBarClick && data && data.date) {
      onBarClick(data.date);
    }
  };

  // Custom bar shape to show selected state
  const CustomBar = (props: any) => {
    const { fill, x, y, width, height, payload } = props;
    const isSelected = selectedDate === payload.date;
    
    return (
      <g>
        <rect
          x={x}
          y={y}
          width={width}
          height={height}
          fill={isSelected ? '#15803d' : fill}
          rx={8}
          ry={8}
          opacity={isSelected ? 1 : 0.8}
          style={{ cursor: 'pointer' }}
        />
        {isSelected && (
          <rect
            x={x}
            y={y}
            width={width}
            height={height}
            fill="none"
            stroke="#15803d"
            strokeWidth={3}
            rx={8}
            ry={8}
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
              <span className="block mt-1 text-green-600 font-medium">
                Click on any bar to view details
              </span>
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
              dataKey="sales"
              fill="#16a34a"
              name="Total Sales"
              shape={<CustomBar />}
              onClick={handleBarClick}
            />
            <Line
              type="monotone"
              dataKey="sales"
              stroke="#0ea5e9"
              strokeWidth={2}
              dot={{ fill: '#0ea5e9', r: 4 }}
              name="Trend"
            />
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}