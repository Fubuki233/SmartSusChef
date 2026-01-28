import React, { useState, useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/app/components/ui/dialog';
import { format, parseISO } from 'date-fns';
import { Plus, Edit, Trash2, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';

export function SalesInputForm() {
  const { recipes, addSalesData, updateSalesData, deleteSalesData, salesData } = useApp();
  const [selectedRecipe, setSelectedRecipe] = useState<string>('');
  const [quantity, setQuantity] = useState<string>('');
  const [editingId, setEditingId] = useState<string | null>(null);
  
  // Warning modal state
  const [showWarning, setShowWarning] = useState(false);
  const [duplicateEntryId, setDuplicateEntryId] = useState<string | null>(null);
  
  const today = new Date();
  const todayStr = format(today, 'yyyy-MM-dd');

  // Filter only main dishes and sort alphabetically
  const mainDishes = useMemo(() => {
    return recipes
      .filter(r => !r.isSubRecipe)
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [recipes]);

  // Get today's sales entries for Recent Entries table
  const recentEntries = useMemo(() => {
    const todaysData = salesData
      .filter(sale => sale.date === todayStr)
      .map(sale => {
        const recipe = recipes.find(r => r.id === sale.recipeId);
        return {
          id: sale.id,
          recipeId: sale.recipeId,
          dishName: recipe?.name || 'Unknown',
          quantity: sale.quantity,
          createdAt: sale.createdAt,
          modifiedAt: sale.modifiedAt,
        };
      })
      .sort((a, b) => {
        const timeA = new Date(a.modifiedAt || a.createdAt || 0).getTime();
        const timeB = new Date(b.modifiedAt || b.createdAt || 0).getTime();
        return timeB - timeA;
      });
    
    return todaysData;
  }, [salesData, recipes, todayStr]);

  const handleRecipeSelect = (value: string) => {
    setSelectedRecipe(value);
    
    // Check for duplicate entry if not in edit mode
    if (!editingId) {
      const existingEntry = recentEntries.find(entry => entry.recipeId === value);
      if (existingEntry) {
        setDuplicateEntryId(existingEntry.id);
        setShowWarning(true);
      }
    }
  };

  const handleSubmit = () => {
    if (!selectedRecipe) {
      toast.error('Please select a dish');
      return;
    }

    const qty = parseInt(quantity);
    if (!qty || qty <= 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    if (editingId) {
      // Update existing entry
      updateSalesData(editingId, {
        date: todayStr,
        recipeId: selectedRecipe,
        quantity: qty,
      });
      toast.success('Sales data updated successfully!');
      setEditingId(null);
    } else {
      // Add new entry
      addSalesData({
        date: todayStr,
        recipeId: selectedRecipe,
        quantity: qty,
      });
      toast.success('Sales data saved successfully!');
    }

    setSelectedRecipe('');
    setQuantity('');
  };

  const handleOverwrite = () => {
    if (duplicateEntryId) {
      setEditingId(duplicateEntryId);
      const entry = recentEntries.find(e => e.id === duplicateEntryId);
      if (entry) {
        setQuantity(entry.quantity.toString());
      }
      setShowWarning(false);
      setDuplicateEntryId(null);
    }
  };

  const handleEdit = (entry: any) => {
    setEditingId(entry.id);
    setSelectedRecipe(entry.recipeId);
    setQuantity(entry.quantity.toString());
  };

  const handleDelete = (id: string, dishName: string) => {
    if (confirm(`Are you sure you want to delete the entry for "${dishName}"?`)) {
      deleteSalesData(id);
      toast.success('Entry deleted successfully');
    }
  };

  const handleCancel = () => {
    setEditingId(null);
    setSelectedRecipe('');
    setQuantity('');
  };

  const selectedDishName = recipes.find(r => r.id === selectedRecipe)?.name || 'this dish';

  return (
    <>
      <Card className="rounded-[8px]">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Plus className="w-5 h-5" />
            {editingId ? 'Edit Entry' : 'Input Data'}
          </CardTitle>
          <CardDescription>
            {editingId ? 'Update sales entry' : `Enter today's sales data`} - {format(today, 'd MMM yyyy')}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="dish-select">Select Dish</Label>
              <Select value={selectedRecipe} onValueChange={handleRecipeSelect}>
                <SelectTrigger 
                  id="dish-select"
                  // --- FIX APPLIED: Replaced 'outline' classes with standard 'border' classes ---
                  className="rounded-[8px] border border-gray-300 focus:ring-[#4F6F52] focus:border-[#4F6F52]"
                >
                  <SelectValue placeholder="Choose a dish..." />
                </SelectTrigger>
                <SelectContent>
                  {mainDishes.map((recipe) => (
                    <SelectItem key={recipe.id} value={recipe.id}>
                      {recipe.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="quantity-input">Quantity Sold</Label>
              <Input
                id="quantity-input"
                type="number"
                min="0"
                placeholder="Enter quantity"
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                className="rounded-[8px] border-gray-300 focus:ring-[#4F6F52] focus:border-[#4F6F52]"
              />
            </div>
          </div>

          <div className="flex gap-2">
            <Button 
              onClick={handleSubmit} 
              className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-6"
            >
              {editingId ? 'Update Sales Data' : 'Save Sales Data'}
            </Button>
            {editingId && (
              <Button onClick={handleCancel} variant="outline" className="rounded-[32px] px-6">
                Cancel
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Duplicate Entry Warning Modal */}
      <Dialog open={showWarning} onOpenChange={setShowWarning}>
        <DialogContent className="max-w-md rounded-[8px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-[#E74C3C]" />
              Entry Already Exists
            </DialogTitle>
            <DialogDescription className="pt-2 text-base">
              You have already entered sales data for <span className="font-semibold">{selectedDishName}</span>. Do you want to overwrite the existing quantity?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 gap-2">
            <Button 
              variant="outline" 
              onClick={() => {
                setShowWarning(false);
                setSelectedRecipe('');
              }}
              className="rounded-[8px]"
            >
              Cancel
            </Button>
            <Button 
              onClick={handleOverwrite} 
              className="bg-[#E74C3C] hover:bg-[#C0392B] text-white rounded-[8px]"
            >
              Overwrite Entry
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Recent Entries Section */}
      {recentEntries.length > 0 && (
        <Card className="rounded-[8px]">
          <CardHeader>
            <CardTitle>Recent Entries</CardTitle>
            <CardDescription>Sales data entered today</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="rounded-[8px] overflow-hidden border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Dish Name</TableHead>
                    <TableHead>Quantity</TableHead>
                    <TableHead>Last Modified</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {recentEntries.map((entry) => {
                    const lastModified = entry.modifiedAt || entry.createdAt;
                    const wasModified = entry.modifiedAt && entry.modifiedAt !== entry.createdAt;
                    
                    return (
                      <TableRow key={entry.id}>
                        <TableCell className="font-medium">{entry.dishName}</TableCell>
                        <TableCell>{entry.quantity}</TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {lastModified ? format(parseISO(lastModified), 'd MMM yyyy, h:mm a') : '-'}
                          {wasModified && <span className="text-xs text-gray-500 ml-1">(edited)</span>}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleEdit(entry)}
                              className="h-8 w-8"
                            >
                              <Edit className="w-4 h-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleDelete(entry.id, entry.dishName)}
                              className="h-8 w-8"
                            >
                              <Trash2 className="w-4 h-4 text-[#E74C3C]" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      )}
    </>
  );
}