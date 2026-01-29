import React, { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { format, subDays, parseISO } from 'date-fns';
import { Trash2, Leaf } from 'lucide-react';
import { getStandardizedQuantity } from '@/app/utils/unitConversion';
import { calculateRecipeCarbon, calculateRecipeWeight } from '@/app/utils/recipeCalculations';

interface WastageTrendChartProps {
  dateRange: 'today' | '7days' | 'custom';
  onDateRangeChange: (range: 'today' | '7days' | 'custom') => void;
  maxDays?: number;
  onBarClick?: (date: string) => void;
  selectedDate?: string | null;
}

export function WastageTrendChart({
  dateRange,
  onDateRangeChange,
  maxDays = 30,
  onBarClick,
  selectedDate,
}: WastageTrendChartProps) {
  const { wastageData, ingredients, recipes } = useApp();

  const { chartData, totalCarbonFootprint } = useMemo(() => {
    const today = new Date();
    const daysToShow = dateRange === 'today' ? 1 : Math.min(maxDays, dateRange === '7days' ? 7 : 30);
    const startDate = subDays(today, daysToShow - 1);

    const ingredientMap = new Map(ingredients.map((i) => [i.id, { ...i }]));
    const recipeMap = new Map(recipes.map((r) => [r.id, r]));
    
    const groupedByDate: { [key: string]: { weightKg: number; carbon: number } } = {};

    wastageData.forEach((waste) => {
      const wasteDate = parseISO(waste.date);
      if (wasteDate >= startDate && wasteDate <= today) {
        const dateKey = waste.date;
        
        if (!groupedByDate[dateKey]) {
          groupedByDate[dateKey] = { weightKg: 0, carbon: 0 };
        }
        
        let weightInKg = 0;
        let carbonEmission = 0;

        if (waste.recipeId) {
          const portionWeight = calculateRecipeWeight(waste.recipeId, recipeMap, ingredientMap);
          const portionCarbon = calculateRecipeCarbon(waste.recipeId, recipeMap, ingredientMap);
          
          weightInKg = portionWeight * waste.quantity;
          carbonEmission = portionCarbon * waste.quantity;

        } else if (waste.ingredientId) {
          const ingredient = ingredientMap.get(waste.ingredientId);
          if (ingredient) {
            weightInKg = getStandardizedQuantity(waste.quantity, ingredient.unit);
            carbonEmission = weightInKg * ingredient.carbonFootprint;
          }
        }

        groupedByDate[dateKey].weightKg += weightInKg;
        groupedByDate[dateKey].carbon += carbonEmission;
      }
    });

    const data = [];
    let totalCarbon = 0;
    
    for (let i = daysToShow - 1; i >= 0; i--) {
      const date = subDays(today, i);
      const dateKey = format(date, 'yyyy-MM-dd');
      const dayData = groupedByDate[dateKey] || { weightKg: 0, carbon: 0 };
      totalCarbon += dayData.carbon;
      
      data.push({
        date: dateKey,
        displayDate: format(date, 'd MMM'),
        wastage: parseFloat(dayData.weightKg.toFixed(2)),
        carbon: parseFloat(dayData.carbon.toFixed(2)),
      });
    }

    return { chartData: data, totalCarbonFootprint: totalCarbon };
  }, [wastageData, ingredients, recipes, dateRange, maxDays]);

  const handleBarClick = (data: any) => {
    if (onBarClick && data && data.date) {
      onBarClick(data.date);
    }
  };

  // Custom Bar with Selection State & Hover Effects
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
          fill={isSelected ? '#D35400' : fill}
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
              <Trash2 className="w-5 h-5 text-[#E67E22]" />
              Wastage Trend
            </CardTitle>
            <CardDescription className="mt-2">
              <span className="flex items-center gap-2">
                <Leaf className="w-4 h-4 text-[#8A9A5B]" />
                Total Carbon Footprint: {totalCarbonFootprint.toFixed(2)} kg CO₂
              </span>
              <span className="block mt-1 text-[#E67E22] font-medium">
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
            <YAxis yAxisId="left" tick={{ fontSize: 12 }} label={{ value: 'Wastage (kg)', angle: -90, position: 'insideLeft' }}/>
            <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 12 }} label={{ value: 'Carbon (kg CO₂)', angle: 90, position: 'insideRight' }}/>
            <Tooltip />
            <Legend />
            <Bar
              yAxisId="left"
              dataKey="wastage"
              fill="#F39C12"
              name="Wastage (kg)"
              shape={<CustomBar />}
              onClick={handleBarClick}
            />
            <Line
              yAxisId="right"
              type="monotone"
              dataKey="carbon"
              stroke="#E74C3C"
              strokeWidth={3}
              dot={{ fill: '#E74C3C', r: 4 }}
              name="Carbon (kg CO₂)"
            />
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}