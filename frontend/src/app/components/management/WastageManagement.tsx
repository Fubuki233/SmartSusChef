import React, { useState, useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Textarea } from '@/app/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/app/components/ui/dialog';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/app/components/ui/sheet';
import { Badge } from '@/app/components/ui/badge';
import { Trash2, Edit, History, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';
import { format, differenceInDays, subDays } from 'date-fns';
import { WastageData, EditHistory } from '@/app/types/index';

export function WastageManagement() {
  const { user, wastageData, ingredients, recipes, updateWastageData } = useApp();
  const [selectedType, setSelectedType] = useState<string>('all');
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isHistoryOpen, setIsHistoryOpen] = useState(false);
  const [editingData, setEditingData] = useState<WastageData | null>(null);
  const [newQuantity, setNewQuantity] = useState<string>('');
  const [editReason, setEditReason] = useState('');
  const [selectedHistoryItem, setSelectedHistoryItem] = useState<WastageData | null>(null);

  // --- LOGIC FIX: Check both recipeId and ingredientId ---
  const getItemInfo = (recipeId?: string | null, ingredientId?: string | null) => {
    // 1. Try to find a Recipe/Sub-Recipe first
    if (recipeId) {
      const recipe = recipes.find(r => r.id === recipeId);
      if (recipe) {
        return {
          name: recipe.name,
          type: recipe.isSubRecipe ? 'Sub-Recipe' : 'Dish',
          unit: recipe.isSubRecipe ? 'L' : 'plate',
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
    const totals = filteredData.reduce((acc, waste) => {
      acc.quantity += waste.quantity;
      
      // Carbon footprint calculation
      if (waste.ingredientId) {
        const ingredient = ingredientMap.get(waste.ingredientId);
        if (ingredient) {
          acc.carbon += waste.quantity * ingredient.carbonFootprint;
        }
      } else {
        // For dishes/sub-recipes, using a mock value (can be expanded to aggregate recipe ingredients)
        acc.carbon += waste.quantity * 0.5; 
      }
      return acc;
    }, { quantity: 0, carbon: 0 });
    return totals;
  }, [filteredData, ingredients]);

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
    setEditReason('');
    setIsEditDialogOpen(true);
  };

  const handleCloseEditDialog = () => {
    setIsEditDialogOpen(false);
    setEditingData(null);
    setNewQuantity('');
    setEditReason('');
  };

  const handleSubmitEdit = () => {
    if (!editingData) return;

    const quantity = parseFloat(newQuantity);
    if (isNaN(quantity) || quantity < 0) {
      toast.error('Please enter a valid quantity');
      return;
    }

    if (!editReason.trim()) {
      toast.error('Please provide a reason for editing this historical data');
      return;
    }

    const historyEntry: EditHistory = {
      timestamp: new Date().toISOString(),
      editedBy: user?.name || 'Unknown User',
      reason: editReason.trim(),
      previousValue: editingData.quantity,
      newValue: quantity,
    };

    const updatedHistory = [...(editingData.editHistory || []), historyEntry];
    
    updateWastageData(editingData.id, {
      quantity,
      editHistory: updatedHistory,
    });

    toast.success('Wastage data updated successfully');
    handleCloseEditDialog();
  };

  const handleViewHistory = (data: WastageData) => {
    if (!data.editHistory || data.editHistory.length === 0) {
      toast.info('No edit history available for this record');
      return;
    }
    setSelectedHistoryItem(data);
    setIsHistoryOpen(true);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold flex items-center gap-2 text-[#1A1C18] md:text-3xl lg:text-3xl">
          <Trash2 className="w-6 h-6 text-[#E74C3C]" />
          Wastage Data Management
        </h1>
        <p className="text-gray-600 mt-1">Audit log and data controls for <span className="font-bold text-[#4F6F52]">{useApp().storeSettings.storeName}</span></p>
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
                    <TableHead className="text-right">History</TableHead>
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
                          {waste.editHistory && waste.editHistory.length > 0 ? (
                            <Button variant="ghost" size="sm" onClick={() => handleViewHistory(waste)} className="gap-1 text-[#4F6F52] hover:text-[#3D563F] hover:bg-gray-100">
                              <History className="w-4 h-4" />
                              {waste.editHistory.length}
                            </Button>
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
              <div className="bg-amber-50 border border-amber-200 rounded-[8px] p-3 text-sm">
                <p className="text-amber-800 font-semibold flex items-center gap-1">⚠️ Audit Notice</p>
                <p className="text-amber-700 mt-1">This change will be logged for audit purposes. Please provide a detailed reason.</p>
              </div>
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
              <div className="space-y-2">
                <Label htmlFor="reason" className="text-sm font-semibold">Reason for Edit *</Label>
                <Textarea id="reason" placeholder="e.g., Wrong quantity entered by staff..." value={editReason} onChange={(e) => setEditReason(e.target.value)} rows={3} className="rounded-[8px] resize-none" />
              </div>
              <DialogFooter className="gap-2 sm:gap-0">
                <Button variant="outline" onClick={handleCloseEditDialog} className="rounded-[32px] px-6">Cancel</Button>
                <Button onClick={handleSubmitEdit} className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-6">Update Record</Button>
              </DialogFooter>
            </div>
          )}
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