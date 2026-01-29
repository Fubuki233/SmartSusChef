import React, { useState, useMemo, useEffect } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/app/components/ui/dialog';
import { Badge } from '@/app/components/ui/badge';
import { format, parseISO } from 'date-fns';
import { Plus, Edit, Trash2, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';

type CategoryType = 'Main Dish' | 'Sub-Recipe' | 'Raw Ingredient' | '';

export function WastageInputForm() {
  const { ingredients, recipes, addWastageData, updateWastageData, deleteWastageData, wastageData } = useApp();
  const [selectedCategory, setSelectedCategory] = useState<CategoryType>('');
  const [selectedItemId, setSelectedItemId] = useState<string>('');
  const [quantity, setQuantity] = useState<string>('');
  const [editingId, setEditingId] = useState<string | null>(null);
  
  const [showWarning, setShowWarning] = useState(false);
  const [duplicateEntryId, setDuplicateEntryId] = useState<string | null>(null);
  
  const today = new Date();
  const todayStr = format(today, 'yyyy-MM-dd');

  const categorizedItems = useMemo(() => {
    const mainDishes = recipes.filter(r => !r.isSubRecipe).sort((a, b) => a.name.localeCompare(b.name));
    const subRecipes = recipes.filter(r => r.isSubRecipe).sort((a, b) => a.name.localeCompare(b.name));
    const rawIngredients = [...ingredients].sort((a, b) => a.name.localeCompare(b.name));

    return {
      'Main Dish': mainDishes,
      'Sub-Recipe': subRecipes,
      'Raw Ingredient': rawIngredients
    };
  }, [recipes, ingredients]);

  const filteredItems = useMemo(() => {
    if (!selectedCategory) return [];
    return categorizedItems[selectedCategory as keyof typeof categorizedItems] || [];
  }, [selectedCategory, categorizedItems]);

  const getItemData = (id: string) => {
    const recipe = recipes.find(r => r.id === id);
    if (recipe) {
      return {
        name: recipe.name,
        type: recipe.isSubRecipe ? 'Sub-Recipe' : 'Main Dish',
        unit: recipe.isSubRecipe ? 'L' : 'plate',
        badgeColor: recipe.isSubRecipe ? 'bg-[#E67E22]' : 'bg-[#3498DB]'
      };
    }
    const ingredient = ingredients.find(i => i.id === id);
    if (ingredient) {
      return {
        name: ingredient.name,
        type: 'Raw Ingredient',
        unit: ingredient.unit,
        badgeColor: 'bg-[#95A5A6]'
      };
    }
    return { name: 'Unknown', type: 'Unknown', unit: '', badgeColor: 'bg-gray-400' };
  };

  // Determine the unit for the currently selected item to display in the input box
  const currentUnit = useMemo(() => {
    if (!selectedItemId) return '';
    return getItemData(selectedItemId).unit;
  }, [selectedItemId, recipes, ingredients]);

  const handleCategoryChange = (val: string) => {
    setSelectedCategory(val as CategoryType);
    setSelectedItemId('');
  };

  const handleItemSelect = (val: string) => {
    setSelectedItemId(val);
    
    if (!editingId) {
      const existingEntry = recentEntries.find(entry => entry.itemId === val);
      if (existingEntry) {
        setDuplicateEntryId(existingEntry.id);
        setShowWarning(true);
      }
    }
  };

  const handleSubmit = async () => {
    if (!selectedItemId) {
      toast.error('Please select an item');
      return;
    }

    const qty = parseFloat(quantity);
    if (!qty || qty <= 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    const isRecipe = selectedCategory === 'Main Dish' || selectedCategory === 'Sub-Recipe';
    
    const payload = {
      date: todayStr,
      quantity: qty,
      recipeId: isRecipe ? selectedItemId : undefined,
      ingredientId: !isRecipe ? selectedItemId : undefined,
    };

    try {
      if (editingId) {
        await updateWastageData(editingId, payload);
        toast.success('Wastage data updated successfully!');
        setEditingId(null);
      } else {
        await addWastageData(payload);
        toast.success('Wastage data saved successfully!');
      }

      setSelectedCategory('');
      setSelectedItemId('');
      setQuantity('');
    } catch (error) {
      toast.error('Failed to save wastage data');
    }
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
    
    // Explicitly check which ID exists to determine category
    if (entry.recipeId) {
      const recipe = recipes.find(r => r.id === entry.recipeId);
      if (recipe) {
        setSelectedCategory(recipe.isSubRecipe ? 'Sub-Recipe' : 'Main Dish');
        setTimeout(() => setSelectedItemId(entry.recipeId), 0);
      }
    } else if (entry.ingredientId) {
      setSelectedCategory('Raw Ingredient');
      setTimeout(() => setSelectedItemId(entry.ingredientId), 0);
    }
    
    setQuantity(entry.quantity.toString());
  };

  const handleDelete = async (id: string, itemName: string) => {
    if (confirm(`Are you sure you want to delete the entry for "${itemName}"?`)) {
      try {
        await deleteWastageData(id);
        toast.success('Entry deleted successfully');
      } catch (error) {
        toast.error('Failed to delete entry');
      }
    }
  };

  const handleCancel = () => {
    setEditingId(null);
    setSelectedCategory('');
    setSelectedItemId('');
    setQuantity('');
  };

  const recentEntries = useMemo(() => {
    return wastageData
      .filter(waste => waste.date === todayStr)
      .map(waste => {
        let item;
        
        // 1. Try to find a Recipe
        if (waste.recipeId) {
            const recipe = recipes.find(r => r.id === waste.recipeId);
            if (recipe) {
                item = {
                    name: recipe.name,
                    type: recipe.isSubRecipe ? 'Sub-Recipe' : 'Main Dish',
                    unit: recipe.isSubRecipe ? 'L' : 'plate',
                    badgeColor: recipe.isSubRecipe ? 'bg-[#E67E22]' : 'bg-[#3498DB]'
                };
            }
        } 
        
        // 2. Try to find an Ingredient
        if (!item && waste.ingredientId) {
            const ingredient = ingredients.find(i => i.id === waste.ingredientId);
            if (ingredient) {
                item = {
                    name: ingredient.name,
                    type: 'Raw Ingredient',
                    unit: ingredient.unit,
                    badgeColor: 'bg-[#95A5A6]'
                };
            }
        }

        if (!item) {
            item = { name: 'Unknown Item', type: 'Unknown', unit: '-', badgeColor: 'bg-gray-400' };
        }

        return {
          id: waste.id,
          itemId: waste.recipeId || waste.ingredientId, 
          recipeId: waste.recipeId,
          ingredientId: waste.ingredientId,
          itemName: item.name,
          type: item.type,
          badgeColor: item.badgeColor,
          quantity: waste.quantity,
          unit: item.unit,
          createdAt: waste.createdAt,
          modifiedAt: waste.modifiedAt,
        };
      })
      .sort((a, b) => {
        const timeA = new Date(a.modifiedAt || a.createdAt || 0).getTime();
        const timeB = new Date(b.modifiedAt || b.createdAt || 0).getTime();
        return timeB - timeA;
      });
  }, [wastageData, recipes, ingredients, todayStr]);

  const selectedItemName = selectedItemId ? getItemData(selectedItemId).name : 'this item';

  return (
    <>
      <Card className="rounded-[8px]">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-[#1A1C18]">
            <Plus className="w-5 h-5 text-[#4F6F52]" />
            {editingId ? 'Edit Entry' : 'Input Data'}
          </CardTitle>
          <CardDescription>
            {editingId ? 'Update wastage entry' : `Enter today's wastage data`} - {format(today, 'd MMM yyyy')}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Step 1: Select Category */}
            <div className="space-y-2">
              <Label htmlFor="category-select" className="text-sm font-semibold">Step 1: Select Category</Label>
              <Select value={selectedCategory} onValueChange={handleCategoryChange}>
                <SelectTrigger 
                  id="category-select"
                  className="rounded-[8px] border-gray-300 focus:ring-[#4F6F52] focus:border-[#4F6F52]"
                >
                  <SelectValue placeholder="Choose Category..." />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Main Dish">Main Dish</SelectItem>
                  <SelectItem value="Sub-Recipe">Sub-Recipe</SelectItem>
                  <SelectItem value="Raw Ingredient">Raw Ingredient</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Step 2: Select Item */}
            <div className="space-y-2">
              <Label htmlFor="item-select" className="text-sm font-semibold">Step 2: Select Item</Label>
              <Select 
                value={selectedItemId} 
                onValueChange={handleItemSelect}
                disabled={!selectedCategory}
              >
                <SelectTrigger 
                  id="item-select"
                  className={`rounded-[8px] border-gray-300 focus:ring-[#4F6F52] focus:border-[#4F6F52] ${!selectedCategory ? 'bg-gray-50' : ''}`}
                >
                  <SelectValue placeholder={selectedCategory ? "Choose an item..." : "Select category first"} />
                </SelectTrigger>
                <SelectContent className="max-h-[300px]">
                  {filteredItems.map((item: any) => {
                    // UI Enhancement 1: Show Unit in Dropdown
                    let displayUnit = '';
                    if (selectedCategory === 'Raw Ingredient') displayUnit = item.unit;
                    else if (selectedCategory === 'Sub-Recipe') displayUnit = 'L';
                    else displayUnit = 'plate';

                    return (
                      <SelectItem key={item.id} value={item.id}>
                        {item.name} <span className="text-gray-400">({displayUnit})</span>
                      </SelectItem>
                    );
                  })}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Third Field: Quantity */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <Label htmlFor="quantity-input" className="text-sm font-semibold">
                Quantity Wasted
              </Label>
              {/* UI Enhancement 2: Unit Suffix Inside Input */}
              <div className="relative">
                <Input
                  id="quantity-input"
                  type="number"
                  min="0"
                  step="0.1"
                  placeholder="0.00"
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                  className="rounded-[8px] border-gray-300 focus:ring-[#4F6F52] focus:border-[#4F6F52] pr-12"
                />
                <div className="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
                  <span className="text-gray-500 text-sm font-medium">
                    {currentUnit || '-'}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <div className="flex gap-3 pt-2">
            <Button 
              onClick={handleSubmit} 
              className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-8 h-11 transition-all"
            >
              {editingId ? 'Update Entry' : 'Save Entry'}
            </Button>
            {editingId && (
              <Button onClick={handleCancel} variant="outline" className="rounded-[32px] px-8 h-11">
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
              You have already entered wastage data for <span className="font-semibold">{selectedItemName}</span>. Do you want to overwrite the existing quantity?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4 gap-2">
            <Button 
              variant="outline" 
              onClick={() => {
                setShowWarning(false);
                setSelectedItemId('');
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

      {recentEntries.length > 0 && (
        <Card className="rounded-[8px]">
          <CardHeader>
            <CardTitle className="text-lg">Recent Entries</CardTitle>
            <CardDescription>Wastage data entered today</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="rounded-[8px] overflow-hidden border">
              <Table>
                <TableHeader className="bg-gray-50">
                  <TableRow>
                    <TableHead>Item Name</TableHead>
                    <TableHead>Category</TableHead>
                    <TableHead className="text-right">Quantity</TableHead>
                    <TableHead>Unit</TableHead>
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
                        <TableCell className="font-medium">{entry.itemName}</TableCell>
                        <TableCell>
                          <Badge className={`${entry.badgeColor} text-white hover:${entry.badgeColor} border-none`}>
                            {entry.type}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-right font-mono">{entry.quantity}</TableCell>
                        <TableCell className="text-muted-foreground">{entry.unit}</TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {lastModified ? format(parseISO(lastModified), 'd MMM, h:mm a') : '-'}
                          {wasModified && <span className="text-[10px] bg-gray-100 px-1 py-0.5 rounded ml-1 uppercase font-bold">Edited</span>}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleEdit(entry)}
                              className="h-8 w-8 hover:bg-gray-100"
                            >
                              <Edit className="w-4 h-4 text-gray-500" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleDelete(entry.id, entry.itemName)}
                              className="h-8 w-8 hover:bg-red-50"
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