import React, { useState, useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/app/components/ui/dialog';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/app/components/ui/sheet';
import { Badge } from '@/app/components/ui/badge';
import { Trash2, Edit, History, AlertTriangle, Plus } from 'lucide-react';
import { toast } from 'sonner';
import { format, differenceInDays, subDays } from 'date-fns';
import { WastageData, EditHistory } from '@/app/types/index';
import { getRecipeUnit, calculateRecipeCarbon } from '@/app/utils/recipeCalculations';
import { getStandardizedQuantity } from '@/app/utils/unitConversion';

export function WastageManagement() {
  const { user, wastageData, ingredients, recipes, updateWastageData, deleteWastageData, addWastageData } = useApp();
  const [selectedType, setSelectedType] = useState<string>('all');
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isHistoryOpen, setIsHistoryOpen] = useState(false);
  const [editingData, setEditingData] = useState<WastageData | null>(null);
  const [newQuantity, setNewQuantity] = useState<string>('');
  const [selectedHistoryItem, setSelectedHistoryItem] = useState<WastageData | null>(null);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [deletingData, setDeletingData] = useState<WastageData | null>(null);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [newDate, setNewDate] = useState<string>(() => format(new Date(), 'yyyy-MM-dd'));
  const [newItemType, setNewItemType] = useState<'ingredient' | 'recipe'>('ingredient');
  const [newItemId, setNewItemId] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  // Calculate the allowed date range for creating new records (last 7 days)
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const sevenDaysAgo = new Date(today);
  sevenDaysAgo.setDate(today.getDate() - 7);
  const todayStr = format(today, 'yyyy-MM-dd');
  const sevenDaysAgoStr = format(sevenDaysAgo, 'yyyy-MM-dd');

  // --- LOGIC: Check both recipeId and ingredientId ---
  const getItemInfo = (recipeId?: string | null, ingredientId?: string | null) => {
    // 1. Try to find a Recipe/Sub-Recipe first
    if (recipeId) {
      const recipe = recipes.find(r => r.id === recipeId);
      if (recipe) {
        return {
          name: recipe.name,
          type: recipe.isSubRecipe ? 'Sub-Recipe' : 'Dish',
          unit: getRecipeUnit(recipe), // Use utility function
          badgeColor: recipe.isSubRecipe ? 'bg-[#E67E22]' : 'bg-[#3498DB]'
        };
      }
    }

    // 2. Fallback to Ingredient if no recipe found
    if (ingredientId) {
      const ingredient = ingredients.find(i => i.id === ingredientId);
      if (ingredient) {
        return {
          name: ingredient.name,
          type: 'Raw',
          unit: ingredient.unit,
          badgeColor: 'bg-[#95A5A6]'
        };
      }
    }

    // Fallback for corrupted or legacy "ghost" data
    return { name: 'Unknown Item', type: 'Unknown', unit: '-', badgeColor: 'bg-gray-400' };
  };

  const filteredData = useMemo(() => {
    const today = new Date();
    const thirtyDaysAgo = subDays(today, 30);

    let data = wastageData.filter((waste) => {
      const wasteDate = new Date(waste.date);
      return wasteDate >= thirtyDaysAgo && wasteDate <= today;
    });

    if (selectedType !== 'all') {
      data = data.filter((waste) => {
        // Pass both IDs to get accurate type for filtering
        const info = getItemInfo(waste.recipeId, waste.ingredientId);
        return info.type.toLowerCase() === selectedType.toLowerCase() ||
          (selectedType === 'Dish' && info.type === 'Dish') ||
          (selectedType === 'Sub-Recipe' && info.type === 'Sub-Recipe') ||
          (selectedType === 'Raw' && info.type === 'Raw');
      });
    }

    return data.sort((a, b) => b.date.localeCompare(a.date));
  }, [wastageData, selectedType, recipes, ingredients]);

  const stats = useMemo(() => {
    const ingredientMap = new Map(ingredients.map((i) => [i.id, i]));
    const recipeMap = new Map(recipes.map((r) => [r.id, r]));

    const totals = filteredData.reduce((acc, waste) => {
      acc.quantity += waste.quantity;

      // Carbon footprint calculation
      if (waste.recipeId) {
        // Use accurate recipe carbon calculation
        const carbonPerUnit = calculateRecipeCarbon(waste.recipeId, recipeMap, ingredientMap);
        acc.carbon += carbonPerUnit * waste.quantity;
      } else if (waste.ingredientId) {
        // Ingredient carbon calculation
        const ingredient = ingredientMap.get(waste.ingredientId);
        if (ingredient) {
          const standardQty = getStandardizedQuantity(waste.quantity, ingredient.unit);
          acc.carbon += standardQty * ingredient.carbonFootprint;
        }
      }

      return acc;
    }, { quantity: 0, carbon: 0 });

    return totals;
  }, [filteredData, ingredients, recipes]);

  const canEdit = (dateStr: string): boolean => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const dataDate = new Date(dateStr);
    dataDate.setHours(0, 0, 0, 0);
    const daysDiff = differenceInDays(today, dataDate);
    return daysDiff <= 7;
  };

  const handleOpenEditDialog = (data: WastageData) => {
    if (!canEdit(data.date)) {
      toast.error('Cannot edit data older than 7 days');
      return;
    }
    setEditingData(data);
    setNewQuantity(data.quantity.toString());
    setIsEditDialogOpen(true);
  };

  const handleCloseEditDialog = () => {
    setIsEditDialogOpen(false);
    setEditingData(null);
    setNewQuantity('');
  };

  const handleSubmitEdit = async () => {
    if (!editingData) return;

    const quantity = parseFloat(newQuantity);
    if (isNaN(quantity) || quantity < 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    setIsSubmitting(true);
    try {
      await updateWastageData(editingData.id, {
        quantity,
      });

      toast.success('Wastage data updated successfully');
      handleCloseEditDialog();
    } catch (error) {
      toast.error('Failed to update wastage data');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleViewHistory = (data: WastageData) => {
    if (!data.editHistory || data.editHistory.length === 0) {
      toast.info('No edit history available for this record');
      return;
    }
    setSelectedHistoryItem(data);
    setIsHistoryOpen(true);
  };

  const handleDeleteRecord = async () => {
    if (!deletingData) return;

    setIsDeleting(true);
    try {
      await deleteWastageData(deletingData.id);

      toast.success('Wastage record deleted successfully');

      setIsDeleteDialogOpen(false);
      setIsEditDialogOpen(false);
      setEditingData(null);
      setDeletingData(null);

    } catch (error) {
      console.error('Failed to delete wastage data:', error);
      toast.error('Failed to delete wastage record');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleCreateRecord = async () => {
    if (!newDate || !newItemId || !newQuantity) {
      toast.error('Please fill in all required fields');
      return;
    }

    const quantity = parseFloat(newQuantity);
    if (isNaN(quantity) || quantity < 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    setIsSubmitting(true);
    try {
      // Check for duplicate record
      const existingRecord = wastageData.find(
        item => item.date === newDate &&
          ((newItemType === 'ingredient' && item.ingredientId === newItemId) ||
            (newItemType === 'recipe' && item.recipeId === newItemId))
      );

      if (existingRecord) {
        const itemInfo = getItemInfo(
          newItemType === 'recipe' ? newItemId : null,
          newItemType === 'ingredient' ? newItemId : null
        );
        toast.error(`A record already exists for ${format(new Date(newDate), 'd MMM yyyy')} and ${itemInfo.name}`);
        return;
      }

      // Add new wastage record
      await addWastageData({
        date: newDate,
        ingredientId: newItemType === 'ingredient' ? newItemId : undefined,
        recipeId: newItemType === 'recipe' ? newItemId : undefined,
        quantity: quantity,
      });

      toast.success('New wastage record added successfully');
      handleCloseCreateDialog();
    } catch (error) {
      console.error('Failed to create wastage data:', error);
      toast.error('Failed to add new wastage record');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCloseCreateDialog = () => {
    setIsCreateDialogOpen(false);
    setNewDate(format(new Date(), 'yyyy-MM-dd'));
    setNewItemType('ingredient');
    setNewItemId('');
    setNewQuantity('');
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold flex items-center gap-2 text-[#1A1C18] md:text-3xl lg:text-3xl">
            <Trash2 className="w-6 h-6 text-[#E74C3C]" />
            Wastage Data Management
          </h1>
          <p className="text-gray-600 mt-1">Audit log and data controls for store management</p>
        </div>
        <Button
          onClick={() => setIsCreateDialogOpen(true)}
          className="bg-[#4F6F52] hover:bg-[#3D563F] text-white flex items-center gap-2 rounded-[32px] px-6"
        >
          <Plus className="w-4 h-4" />
          Add New Record
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="border-[#81A263]/20 rounded-[8px]">
          <CardHeader>
            <CardTitle className="text-lg">Total Records</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-[#1A1C18]">{filteredData.length}</div>
            <p className="text-sm text-gray-600 mt-1">Last 30 days</p>
          </CardContent>
        </Card>

        <Card className="border-[#81A263]/20 rounded-[8px]">
          <CardHeader>
            <CardTitle className="text-lg">Total Wastage</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-[#1A1C18]">{stats.quantity.toFixed(1)}</div>
            <p className="text-sm text-gray-600 mt-1">mixed units</p>
          </CardContent>
        </Card>

        <Card className="border-[#E74C3C]/30 bg-red-50/30 rounded-[8px]">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <span>Carbon Footprint</span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-[#E74C3C]">{stats.carbon.toFixed(2)}</div>
            <p className="text-sm text-gray-600 mt-1">kg CO₂</p>
          </CardContent>
        </Card>
      </div>

      <Card className="border-[#81A263]/20 rounded-[8px]">
        <CardHeader>
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <CardTitle>Wastage Records</CardTitle>
              <CardDescription>Last 30 days. Only data from the last 7 days can be edited.</CardDescription>
            </div>
            <div className="w-full md:w-64">
              <Label className="mb-1 block text-sm font-medium">Filter by Item Type:</Label>
              <Select value={selectedType} onValueChange={setSelectedType}>
                <SelectTrigger className="rounded-[8px]">
                  <SelectValue placeholder="All Item Types" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Items</SelectItem>
                  <SelectItem value="Dish">Main Dishes</SelectItem>
                  <SelectItem value="Sub-Recipe">Sub-Recipes</SelectItem>
                  <SelectItem value="Raw">Raw Ingredients</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {filteredData.length > 0 ? (
            <div className="border rounded-[8px] overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow className="bg-gray-50/50">
                    <TableHead>Date</TableHead>
                    <TableHead>Item Name</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead>Unit</TableHead>
                    <TableHead>Quantity</TableHead>
                    <TableHead className="text-center">Editable</TableHead>
                    <TableHead className="text-right">Last Edit (UTC)</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredData.map((waste) => {
                    const info = getItemInfo(waste.recipeId, waste.ingredientId);
                    const isEditable = canEdit(waste.date);
                    return (
                      <TableRow key={waste.id} className="hover:bg-gray-50/50">
                        <TableCell>{format(new Date(waste.date), 'd MMM yyyy')}</TableCell>
                        <TableCell className="font-medium">{info.name}</TableCell>
                        <TableCell>
                          <Badge className={`${info.badgeColor} text-white border-none shadow-none`}>
                            {info.type}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-gray-500">{info.unit}</TableCell>
                        <TableCell className="font-mono">{waste.quantity.toFixed(2)}</TableCell>
                        <TableCell className="text-center">
                          {isEditable ? (
                            <span className="text-[10px] bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-bold uppercase">Yes</span>
                          ) : (
                            <span className="text-[10px] bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full font-bold uppercase">No</span>
                          )}
                        </TableCell>
                        <TableCell className="text-right">
                          {waste.updatedAt ? (
                            <span className="text-sm text-gray-700">
                              {format(new Date(waste.updatedAt), 'd MMM yyyy, h:mm a')}
                            </span>
                          ) : (
                            <span className="text-gray-400 text-sm px-4">-</span>
                          )}
                        </TableCell>
                        <TableCell className="text-right">
                          <Button variant="ghost" size="sm" onClick={() => handleOpenEditDialog(waste)} disabled={!isEditable} className="gap-1 text-[#4F6F52] hover:text-[#3D563F] hover:bg-gray-100">
                            <Edit className="w-4 h-4" /> Edit
                          </Button>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          ) : (
            <div className="py-12 text-center text-gray-500">No wastage data available for this filter.</div>
          )}
        </CardContent>
      </Card>

      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent className="max-w-md rounded-[12px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-amber-500" />
              Edit Wastage Data
            </DialogTitle>
          </DialogHeader>
          {editingData && (
            <div className="space-y-4 pt-2">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label className="text-xs text-gray-500 uppercase tracking-wider">Date</Label>
                  <p className="font-medium">{format(new Date(editingData.date), 'd MMM yyyy')}</p>
                </div>
                <div className="space-y-1.5">
                  <Label className="text-xs text-gray-500 uppercase tracking-wider">Item Name</Label>
                  <p className="font-medium">{getItemInfo(editingData.recipeId, editingData.ingredientId).name}</p>
                </div>
              </div>
              <div className="space-y-2">
                <Label className="text-xs text-gray-500 uppercase tracking-wider">Current Quantity</Label>
                <div className="bg-gray-50 p-2 rounded border text-gray-600">
                  {editingData.quantity} {getItemInfo(editingData.recipeId, editingData.ingredientId).unit}
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="new-quantity" className="text-sm font-semibold">New Quantity *</Label>
                <Input id="new-quantity" type="number" step="0.1" value={newQuantity} onChange={(e) => setNewQuantity(e.target.value)} className="rounded-[8px]" />
              </div>
              <div className="flex justify-between pt-4">
                <Button
                  variant="destructive"
                  onClick={() => {
                    setDeletingData(editingData);
                    setIsDeleteDialogOpen(true);
                  }}
                  disabled={isSubmitting}
                  className="bg-red-600 hover:bg-red-700 rounded-[32px] px-6"
                >
                  Delete Record
                </Button>
                <div className="flex gap-2">
                  <Button variant="outline" onClick={handleCloseEditDialog} disabled={isSubmitting} className="rounded-[32px] px-6">Cancel</Button>
                  <Button onClick={handleSubmitEdit} disabled={isSubmitting} className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-6">Update Record</Button>
                </div>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <DialogContent className="max-w-md rounded-[12px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-red-600">
              <AlertTriangle className="w-5 h-5" />
              Confirm Deletion
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <p className="text-gray-700">
                Are you sure you want to delete this wastage record?
              </p>
              {deletingData && (
                <div className="bg-red-50 border border-red-200 rounded-lg p-3">
                  <div className="grid grid-cols-2 gap-2 text-sm">
                    <div className="text-gray-600">Date:</div>
                    <div className="font-medium">
                      {format(new Date(deletingData.date), 'd MMM yyyy')}
                    </div>
                    <div className="text-gray-600">Item:</div>
                    <div className="font-medium">
                      {getItemInfo(deletingData.recipeId, deletingData.ingredientId).name}
                    </div>
                    <div className="text-gray-600">Type:</div>
                    <div className="font-medium">
                      {getItemInfo(deletingData.recipeId, deletingData.ingredientId).type}
                    </div>
                    <div className="text-gray-600">Quantity:</div>
                    <div className="font-medium">
                      {deletingData.quantity.toFixed(2)} {getItemInfo(deletingData.recipeId, deletingData.ingredientId).unit}
                    </div>
                  </div>
                </div>
              )}
              <p className="text-sm text-red-600 font-medium">
                Warning: This action cannot be undone.
              </p>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button
                variant="outline"
                onClick={() => setIsDeleteDialogOpen(false)}
                disabled={isDeleting}
                className="rounded-[32px] px-6 hover:bg-gray-100"
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                onClick={handleDeleteRecord}
                disabled={isDeleting}
                className="bg-red-600 hover:bg-red-700 rounded-[32px] px-6"
              >
                Yes, Delete Record
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Create New Record Dialog */}
      <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
        <DialogContent className="max-w-md rounded-[12px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-[#4F6F52]">
              <Plus className="w-5 h-5" />
              Add New Wastage Record
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="new-date">Date *</Label>
              <Input
                id="new-date"
                type="date"
                value={newDate}
                onChange={(e) => setNewDate(e.target.value)}
                min={sevenDaysAgoStr}
                max={format(new Date(), 'yyyy-MM-dd')}
                className="rounded-[8px]"
              />
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <AlertTriangle className="w-4 h-4" />
                <span>You can only add records for the last 7 days ({format(sevenDaysAgo, 'd MMM yyyy')} to {format(today, 'd MMM yyyy')})</span>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="item-type">Item Type *</Label>
              <Select value={newItemType} onValueChange={(value: 'ingredient' | 'recipe') => {
                setNewItemType(value);
                setNewItemId('');
              }}>
                <SelectTrigger id="item-type" className="rounded-[8px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ingredient">Raw Ingredient</SelectItem>
                  <SelectItem value="recipe">Recipe / Sub-Recipe</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="new-item">{newItemType === 'ingredient' ? 'Ingredient' : 'Recipe'} *</Label>
              <Select value={newItemId} onValueChange={setNewItemId}>
                <SelectTrigger id="new-item" className="rounded-[8px]">
                  <SelectValue placeholder={`Select a ${newItemType}...`} />
                </SelectTrigger>
                <SelectContent>
                  {newItemType === 'ingredient' ? (
                    ingredients
                      .sort((a, b) => a.name.localeCompare(b.name))
                      .map(ingredient => (
                        <SelectItem key={ingredient.id} value={ingredient.id}>
                          {ingredient.name}
                        </SelectItem>
                      ))
                  ) : (
                    recipes
                      .sort((a, b) => a.name.localeCompare(b.name))
                      .map(recipe => (
                        <SelectItem key={recipe.id} value={recipe.id}>
                          {recipe.name} {recipe.isSubRecipe ? '(Sub-Recipe)' : '(Dish)'}
                        </SelectItem>
                      ))
                  )}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="new-quantity-create">Quantity *</Label>
              <Input
                id="new-quantity-create"
                type="number"
                min="0"
                step="0.1"
                value={newQuantity}
                onChange={(e) => setNewQuantity(e.target.value)}
                placeholder="Enter quantity"
                className="rounded-[8px]"
              />
              {newItemId && (
                <p className="text-xs text-gray-500">
                  Unit: {getItemInfo(
                    newItemType === 'recipe' ? newItemId : null,
                    newItemType === 'ingredient' ? newItemId : null
                  ).unit}
                </p>
              )}
            </div>

            <div className="flex justify-end gap-2 pt-4">
              <Button variant="outline" onClick={handleCloseCreateDialog} disabled={isSubmitting} className="rounded-[32px] px-6">
                Cancel
              </Button>
              <Button
                onClick={handleCreateRecord}
                className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-6"
                disabled={!newDate || !newItemId || !newQuantity || isSubmitting}
              >
                Save Record
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <Sheet open={isHistoryOpen} onOpenChange={setIsHistoryOpen}>
        <SheetContent className="w-full sm:max-w-md md:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle className="flex items-center gap-2">
              <History className="w-5 h-5 text-[#4F6F52]" /> Audit Trail: Edit History
            </SheetTitle>
          </SheetHeader>
          {selectedHistoryItem && (
            <div className="mt-6 space-y-4">
              <div className="bg-gray-50 rounded-[8px] p-4 border space-y-2">
                <div className="flex justify-between items-center border-b pb-2">
                  <span className="text-sm text-gray-600">Item:</span>
                  <span className="font-bold text-[#1A1C18]">{getItemInfo(selectedHistoryItem.recipeId, selectedHistoryItem.ingredientId).name}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-gray-600">Current Quantity:</span>
                  <span className="font-mono bg-white px-2 py-0.5 rounded border">{selectedHistoryItem.quantity} {getItemInfo(selectedHistoryItem.recipeId, selectedHistoryItem.ingredientId).unit}</span>
                </div>
              </div>
              <div className="space-y-3">
                <h4 className="font-semibold text-sm text-gray-500 uppercase tracking-widest">Change Log</h4>
                {selectedHistoryItem.editHistory?.slice().reverse().map((entry, index) => (
                  <div key={index} className="border rounded-[8px] p-4 space-y-3 bg-white shadow-sm">
                    <div className="flex justify-between items-start">
                      <div>
                        <p className="font-bold text-[#1A1C18]">{entry.editedBy}</p>
                        <p className="text-xs text-gray-400">{format(new Date(entry.timestamp), 'd MMM yyyy, h:mm a')}</p>
                      </div>
                      <div className="text-right">
                        <div className="flex items-center gap-1 text-sm">
                          <span className="text-gray-400 line-through">{entry.previousValue}</span>
                          <span className="text-gray-400">→</span>
                          <span className="font-bold text-[#4F6F52]">{entry.newValue}</span>
                        </div>
                      </div>
                    </div>
                    <div className="bg-gray-50 rounded p-3 text-sm text-gray-700 italic border-l-4 border-[#4F6F52]">
                      "{entry.reason}"
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </SheetContent>
      </Sheet>
    </div>
  );
}