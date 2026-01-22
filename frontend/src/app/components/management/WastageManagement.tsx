import React, { useState, useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Trash2, Edit, Save, X } from 'lucide-react';
import { toast } from 'sonner';
import { format, subDays } from 'date-fns';

export function WastageManagement() {
  const { wastageData, ingredients, updateWastageData, deleteWastageData } = useApp();
  const [selectedIngredient, setSelectedIngredient] = useState<string>('all');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editValue, setEditValue] = useState<string>('');
  const [isSaving, setIsSaving] = useState(false);

  const filteredData = useMemo(() => {
    const today = new Date();
    const thirtyDaysAgo = subDays(today, 30);

    let data = wastageData.filter((waste) => {
      const wasteDate = new Date(waste.date);
      return wasteDate >= thirtyDaysAgo && wasteDate <= today;
    });

    if (selectedIngredient !== 'all') {
      data = data.filter((waste) => waste.ingredientId === selectedIngredient);
    }

    return data.sort((a, b) => b.date.localeCompare(a.date));
  }, [wastageData, selectedIngredient]);

  const totalWastage = useMemo(() => {
    return filteredData.reduce((sum, waste) => sum + waste.quantity, 0);
  }, [filteredData]);

  const totalCarbon = useMemo(() => {
    const ingredientMap = new Map(ingredients.map((i) => [i.id, i]));
    return filteredData.reduce((sum, waste) => {
      const ingredient = ingredientMap.get(waste.ingredientId);
      if (ingredient) {
        return sum + waste.quantity * ingredient.carbonFootprint;
      }
      return sum;
    }, 0);
  }, [filteredData, ingredients]);

  const getIngredientName = (id: string) => {
    return ingredients.find((i) => i.id === id)?.name || 'Unknown';
  };

  const getIngredientUnit = (id: string) => {
    return ingredients.find((i) => i.id === id)?.unit || '';
  };

  const handleEdit = (id: string, currentValue: number) => {
    setEditingId(id);
    setEditValue(currentValue.toString());
  };

  const handleSave = async (id: string) => {
    const newQuantity = parseFloat(editValue);
    if (isNaN(newQuantity) || newQuantity < 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    setIsSaving(true);
    try {
      await updateWastageData(id, { quantity: newQuantity });
      toast.success('Wastage data updated');
      setEditingId(null);
      setEditValue('');
    } catch (error) {
      toast.error('Failed to update wastage data');
    } finally {
      setIsSaving(false);
    }
  };

  const handleCancel = () => {
    setEditingId(null);
    setEditValue('');
  };

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this wastage record?')) {
      try {
        await deleteWastageData(id);
        toast.success('Wastage record deleted');
      } catch (error) {
        toast.error('Failed to delete wastage record');
      }
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold flex items-center gap-2">
          <Trash2 className="w-6 h-6" />
          Wastage Management
        </h1>
        <p className="text-gray-600 mt-1">View and edit wastage data by ingredient</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Total Records</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{filteredData.length}</div>
            <p className="text-sm text-gray-600 mt-1">Last 30 days</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Total Wastage</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{totalWastage.toFixed(2)}</div>
            <p className="text-sm text-gray-600 mt-1">kg</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Carbon Footprint</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold">{totalCarbon.toFixed(2)}</div>
            <p className="text-sm text-gray-600 mt-1">kg CO₂</p>
          </CardContent>
        </Card>
      </div>

      {/* Filter and Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Wastage Records</CardTitle>
              <CardDescription>Last 30 days of wastage data</CardDescription>
            </div>
            <div className="w-64">
              <Label>Filter by Ingredient:</Label>
              <Select value={selectedIngredient} onValueChange={setSelectedIngredient}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Ingredients</SelectItem>
                  {ingredients.map((ingredient) => (
                    <SelectItem key={ingredient.id} value={ingredient.id}>
                      {ingredient.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {filteredData.length > 0 ? (
            <div className="border rounded-lg">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Date</TableHead>
                    <TableHead>Ingredient</TableHead>
                    <TableHead>Unit</TableHead>
                    <TableHead>Quantity</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredData.map((waste) => (
                    <TableRow key={waste.id}>
                      <TableCell>
                        {format(new Date(waste.date), 'MMM dd, yyyy')}
                      </TableCell>
                      <TableCell className="font-medium">
                        {getIngredientName(waste.ingredientId)}
                      </TableCell>
                      <TableCell>{getIngredientUnit(waste.ingredientId)}</TableCell>
                      <TableCell>
                        {editingId === waste.id ? (
                          <Input
                            type="number"
                            step="0.01"
                            min="0"
                            value={editValue}
                            onChange={(e) => setEditValue(e.target.value)}
                            className="w-24"
                          />
                        ) : (
                          waste.quantity.toFixed(2)
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        {editingId === waste.id ? (
                          <div className="flex items-center justify-end gap-2">
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleSave(waste.id)}
                            >
                              <Save className="w-4 h-4 text-green-600" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={handleCancel}
                            >
                              <X className="w-4 h-4" />
                            </Button>
                          </div>
                        ) : (
                          <div className="flex items-center justify-end gap-2">
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleEdit(waste.id, waste.quantity)}
                            >
                              <Edit className="w-4 h-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleDelete(waste.id)}
                            >
                              <Trash2 className="w-4 h-4 text-red-600" />
                            </Button>
                          </div>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          ) : (
            <div className="py-12 text-center text-gray-500">
              No wastage data available
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
