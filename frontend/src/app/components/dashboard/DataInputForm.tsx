import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Calendar } from '@/app/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/app/components/ui/popover';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/app/components/ui/tabs';
import { format } from 'date-fns';
import { CalendarIcon, Plus } from 'lucide-react';
import { toast } from 'sonner';

interface DataInputFormProps {
  maxDaysBack?: number; // undefined means no limit
}

export function DataInputForm({ maxDaysBack = 0 }: DataInputFormProps) {
  const { recipes, ingredients, addSalesData, addWastageData } = useApp();
  const [selectedDate, setSelectedDate] = useState<Date>(new Date());
  const [salesInputs, setSalesInputs] = useState<{ [key: string]: string }>({});
  const [wastageInputs, setWastageInputs] = useState<{ [key: string]: string }>({});
  const [isSavingSales, setIsSavingSales] = useState(false);
  const [isSavingWastage, setIsSavingWastage] = useState(false);

  const handleSalesSubmit = async () => {
    let hasData = false;
    const promises: Promise<void>[] = [];

    recipes.forEach((recipe) => {
      const quantity = parseInt(salesInputs[recipe.id] || '0');
      if (quantity > 0) {
        promises.push(addSalesData({
          date: format(selectedDate, 'yyyy-MM-dd'),
          recipeId: recipe.id,
          quantity,
        }));
        hasData = true;
      }
    });

    if (!hasData) {
      toast.error('Please enter at least one sales value');
      return;
    }

    setIsSavingSales(true);
    try {
      await Promise.all(promises);
      toast.success('Sales data saved successfully!');
      setSalesInputs({});
    } catch (error) {
      toast.error('Failed to save sales data. Please try again.');
    } finally {
      setIsSavingSales(false);
    }
  };

  const handleWastageSubmit = async () => {
    let hasData = false;
    const promises: Promise<void>[] = [];

    ingredients.forEach((ingredient) => {
      const quantity = parseFloat(wastageInputs[ingredient.id] || '0');
      if (quantity > 0) {
        promises.push(addWastageData({
          date: format(selectedDate, 'yyyy-MM-dd'),
          ingredientId: ingredient.id,
          quantity,
        }));
        hasData = true;
      }
    });

    if (!hasData) {
      toast.error('Please enter at least one wastage value');
      return;
    }

    setIsSavingWastage(true);
    try {
      await Promise.all(promises);
      toast.success('Wastage data saved successfully!');
      setWastageInputs({});
    } catch (error) {
      toast.error('Failed to save wastage data. Please try again.');
    } finally {
      setIsSavingWastage(false);
    }
  };

  const disabledDates = (date: Date) => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (maxDaysBack === undefined) {
      // Manager: can select any date up to today
      return date > today;
    } else if (maxDaysBack === 0) {
      // Employee: only today
      return date.toDateString() !== today.toDateString();
    } else {
      // Other restrictions
      const minDate = new Date(today);
      minDate.setDate(minDate.getDate() - maxDaysBack);
      return date < minDate || date > today;
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Plus className="w-5 h-5" />
          Input Data
        </CardTitle>
        <CardDescription>
          {maxDaysBack === 0
            ? "Enter today's sales and wastage data"
            : 'Enter sales and wastage data for any date'}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center gap-4">
          <Label>Date:</Label>
          <Popover>
            <PopoverTrigger asChild>
              <Button variant="outline" className="gap-2">
                <CalendarIcon className="w-4 h-4" />
                {format(selectedDate, 'PPP')}
              </Button>
            </PopoverTrigger>
            <PopoverContent className="w-auto p-0" align="start">
              <Calendar
                mode="single"
                selected={selectedDate}
                onSelect={(date) => date && setSelectedDate(date)}
                disabled={disabledDates}
                initialFocus
              />
            </PopoverContent>
          </Popover>
        </div>

        <Tabs defaultValue="sales">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="sales">Sales Data</TabsTrigger>
            <TabsTrigger value="wastage">Wastage Data</TabsTrigger>
          </TabsList>

          <TabsContent value="sales" className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {recipes.map((recipe) => (
                <div key={recipe.id} className="space-y-2">
                  <Label htmlFor={`sales-${recipe.id}`}>{recipe.name}</Label>
                  <Input
                    id={`sales-${recipe.id}`}
                    type="number"
                    min="0"
                    placeholder="Quantity sold"
                    value={salesInputs[recipe.id] || ''}
                    onChange={(e) =>
                      setSalesInputs({ ...salesInputs, [recipe.id]: e.target.value })
                    }
                  />
                </div>
              ))}
            </div>
            <Button
              onClick={handleSalesSubmit}
              className="bg-green-600 hover:bg-green-700"
              disabled={isSavingSales}
            >
              {isSavingSales ? 'Saving...' : 'Save Sales Data'}
            </Button>
          </TabsContent>

          <TabsContent value="wastage" className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {ingredients.map((ingredient) => (
                <div key={ingredient.id} className="space-y-2">
                  <Label htmlFor={`waste-${ingredient.id}`}>
                    {ingredient.name} ({ingredient.unit})
                  </Label>
                  <Input
                    id={`waste-${ingredient.id}`}
                    type="number"
                    min="0"
                    step="0.1"
                    placeholder="Quantity wasted"
                    value={wastageInputs[ingredient.id] || ''}
                    onChange={(e) =>
                      setWastageInputs({ ...wastageInputs, [ingredient.id]: e.target.value })
                    }
                  />
                </div>
              ))}
            </div>
            <Button
              onClick={handleWastageSubmit}
              className="bg-orange-600 hover:bg-orange-700"
              disabled={isSavingWastage}
            >
              {isSavingWastage ? 'Saving...' : 'Save Wastage Data'}
            </Button>
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  );
}
