import { useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend, BarChart } from 'recharts';
import { format, subDays } from 'date-fns';
import { Target, TrendingUp, TrendingDown } from 'lucide-react';

export function PredictionAccuracy() {
    const { forecastData, salesData, recipes } = useApp();

    const chartData = useMemo(() => {
        const data = [];
        const today = new Date();
        today.setHours(0, 0, 0, 0); // Normalize to start of day

        // Filter main recipes once for efficiency
        const mainRecipeIds = new Set(recipes.filter(r => !r.isSubRecipe).map(r => r.id));

        console.log('[PredictionAccuracy] Total forecast data:', forecastData.length);
        console.log('[PredictionAccuracy] Total sales data:', salesData.length);
        console.log('[PredictionAccuracy] Today:', format(today, 'yyyy-MM-dd'));

        // Get last 7 days including today's actual sales and predictions
        for (let i = 6; i >= 0; i--) {
            const date = subDays(today, i);
            const dateKey = format(date, 'yyyy-MM-dd');
            const dayOfWeek = format(date, 'EEE');

            const actualSales = salesData
                .filter((s) => s.date === dateKey && mainRecipeIds.has(s.recipeId))
                .reduce((sum, s) => sum + s.quantity, 0);

            const dayForecasts = forecastData.filter((f) => f.date === dateKey && mainRecipeIds.has(f.recipeId));
            const predicted = dayForecasts.reduce((sum, f) => sum + (f.quantity || (f as any).predictedQuantity || 0), 0);

            console.log(`[PredictionAccuracy] ${dateKey}: actual=${actualSales}, predicted=${predicted}, forecasts=${dayForecasts.length}`);

            data.push({
                date: dateKey,
                day: dayOfWeek,
                displayDate: format(date, 'd MMM'),
                actual: actualSales,
                predicted: predicted,
            });
        }

        console.log('[PredictionAccuracy] Chart data:', data);
        return data;
    }, [forecastData, salesData, recipes]);

    const totalActual = chartData.reduce((sum, item) => sum + item.actual, 0);
    const totalPredicted = chartData.reduce((sum, item) => sum + item.predicted, 0);
    const accuracy = totalPredicted > 0
        ? (1 - Math.abs(totalActual - totalPredicted) / totalPredicted) * 100
        : 0;
    const difference = totalActual - totalPredicted;

    return (
        <Card className="rounded-[8px]">
            <CardHeader>
                <div className="flex items-start justify-between">
                    <div>
                        <CardTitle className="flex items-center gap-2">
                            <Target className="w-5 h-5 text-[#3498DB]" />
                            Prediction Accuracy
                        </CardTitle>
                        <CardDescription>
                            Last 7 days (including today) actual sales vs predicted sales comparison
                        </CardDescription>
                    </div>
                    <div className="text-right">
                        <div className="text-2xl font-bold">{accuracy.toFixed(1)}%</div>
                        <div className={`text-sm flex items-center gap-1 justify-end ${difference >= 0 ? 'text-[#27AE60]' : 'text-[#E67E22]'
                            }`}>
                            {difference >= 0 ? (
                                <TrendingUp className="w-4 h-4" />
                            ) : (
                                <TrendingDown className="w-4 h-4" />
                            )}
                            {Math.abs(difference)} dishes {difference >= 0 ? 'above' : 'below'} prediction
                        </div>
                    </div>
                </div>
            </CardHeader>
            <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                    <BarChart data={chartData}>
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
                            dataKey="predicted"
                            fill="#B4A373"
                            name="Predicted"
                            radius={[8, 8, 0, 0]}
                        />
                        <Bar
                            dataKey="actual"
                            fill="#4F6F52"
                            name="Actual Sales"
                            radius={[8, 8, 0, 0]}
                        />
                    </BarChart>
                </ResponsiveContainer>
            </CardContent>
        </Card>
    );
}
