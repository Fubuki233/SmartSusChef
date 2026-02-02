import React, { useState, useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/app/components/ui/dialog';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/app/components/ui/sheet';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { DollarSign, Edit, History, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';
import { SalesData, EditHistory } from '@/app/types';
import { format, differenceInDays } from 'date-fns';

export function SalesManagement() {
  const { user, salesData, recipes, updateSalesData } = useApp();
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isHistoryOpen, setIsHistoryOpen] = useState(false);
  const [editingData, setEditingData] = useState<SalesData | null>(null);
  const [newQuantity, setNewQuantity] = useState<string>('');
  const [selectedHistoryItem, setSelectedHistoryItem] = useState<SalesData | null>(null);
  const [dateFilter, setDateFilter] = useState('all');

  // Filter sales data by date range
  const filteredSalesData = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    return salesData.filter(item => {
      const itemDate = new Date(item.date);
      itemDate.setHours(0, 0, 0, 0);
      const daysDiff = differenceInDays(today, itemDate);

      if (dateFilter === '7days') return daysDiff <= 7;
      if (dateFilter === '30days') return daysDiff <= 30;
      return true;
    }).sort((a, b) => b.date.localeCompare(a.date));
  }, [salesData, dateFilter]);

  const getRecipeName = (id: string) => {
    return recipes.find((r) => r.id === id)?.name || 'Unknown Recipe';
  };

  const canEdit = (dateStr: string): boolean => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const dataDate = new Date(dateStr);
    dataDate.setHours(0, 0, 0, 0);
    const daysDiff = differenceInDays(today, dataDate);
    return daysDiff <= 7;
  };

  const handleOpenEditDialog = (data: SalesData) => {
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

    if (quantity === editingData.quantity) {
      toast.error('New quantity must be different from current quantity');
      return;
    }

    try {
      // Update sales data with new quantity
      await updateSalesData(editingData.id, {
        quantity,
      });

      toast.success('Sales data updated successfully');
      handleCloseEditDialog();
    } catch (error) {
      toast.error('Failed to update sales data');
    }
  };

  const handleViewHistory = (data: SalesData) => {
    if (!data.editHistory || data.editHistory.length === 0) {
      toast.info('No edit history available for this record');
      return;
    }
    setSelectedHistoryItem(data);
    setIsHistoryOpen(true);
  };

  // Group sales data by date
  const groupedData = useMemo(() => {
    const grouped: Record<string, SalesData[]> = {};
    filteredSalesData.forEach(item => {
      if (!grouped[item.date]) {
        grouped[item.date] = [];
      }
      grouped[item.date].push(item);
    });
    return grouped;
  }, [filteredSalesData]);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold flex items-center gap-2 text-[#1A1C18] md:text-3xl lg:text-3xl">
            <DollarSign className="w-6 h-6 text-[#81A263]" />
            Sales Data Management
          </h1>
          <p className="text-gray-600 mt-1">View and edit sales data with audit trail</p>
        </div>
        <div className="flex items-center gap-2">
          <Label className="text-sm text-gray-600">Filter:</Label>
          <Select value={dateFilter} onValueChange={setDateFilter}>
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Time</SelectItem>
              <SelectItem value="7days">Last 7 Days</SelectItem>
              <SelectItem value="30days">Last 30 Days</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <Card className="border-[#81A263]/20">
        <CardHeader>
          <CardTitle>Sales Records</CardTitle>
          <CardDescription>
            {filteredSalesData.length} record{filteredSalesData.length !== 1 ? 's' : ''} found.
            Only data from the last 7 days can be edited.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-6">
            {Object.entries(groupedData).map(([date, items]) => {
              const isEditable = canEdit(date);
              const totalQuantity = items.reduce((sum, item) => sum + item.quantity, 0);

              return (
                <div key={date} className="space-y-2">
                  <div className="flex items-center justify-between bg-gray-50 px-4 py-2 rounded-lg">
                    <div>
                      <h3 className="font-medium text-[#333333]">
                        {format(new Date(date), 'EEE, d MMM yyyy')}
                      </h3>
                      <p className="text-sm text-gray-600">Total: {totalQuantity} dishes</p>
                    </div>
                    {!isEditable && (
                      <div className="flex items-center gap-2 text-amber-600 text-sm">
                        <AlertTriangle className="w-4 h-4" />
                        <span>Read-only (older than 7 days)</span>
                      </div>
                    )}
                  </div>

                  <div className="border rounded-lg overflow-hidden">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Recipe</TableHead>
                          <TableHead className="text-right">Quantity</TableHead>
                          <TableHead className="text-right">Edit History</TableHead>
                          <TableHead className="text-right">Actions</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {items.map((item) => (
                          <TableRow key={item.id}>
                            <TableCell className="font-medium">
                              {getRecipeName(item.recipeId)}
                            </TableCell>
                            <TableCell className="text-right">
                              {item.quantity} dishes
                            </TableCell>
                            <TableCell className="text-right">
                              {item.modifiedAt ? (
                                <div className="text-sm text-gray-600">
                                  {format(new Date(item.modifiedAt), 'MMM d, yyyy HH:mm')}
                                </div>
                              ) : (
                                <span className="text-gray-400 text-sm">No edits</span>
                              )}
                            </TableCell>
                            <TableCell className="text-right">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleOpenEditDialog(item)}
                                disabled={!isEditable}
                                className="gap-1"
                              >
                                <Edit className="w-4 h-4" />
                                Edit
                              </Button>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                </div>
              );
            })}

            {Object.keys(groupedData).length === 0 && (
              <div className="text-center py-12 text-gray-500">
                <p>No sales data found for the selected filter</p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Edit Dialog */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-amber-500" />
              Edit Sales Data
            </DialogTitle>
          </DialogHeader>
          {editingData && (
            <div className="space-y-4">
              <div className="space-y-2">
                <Label>Date</Label>
                <Input value={format(new Date(editingData.date), 'd MMM yyyy')} disabled />
              </div>

              <div className="space-y-2">
                <Label>Recipe</Label>
                <Input value={getRecipeName(editingData.recipeId)} disabled />
              </div>

              <div className="space-y-2">
                <Label>Current Quantity</Label>
                <Input value={`${editingData.quantity} dishes`} disabled />
              </div>

              <div className="space-y-2">
                <Label htmlFor="new-quantity">New Quantity *</Label>
                <Input
                  id="new-quantity"
                  type="number"
                  step="1"
                  min="0"
                  value={newQuantity}
                  onChange={(e) => setNewQuantity(e.target.value)}
                  placeholder="Enter new quantity"
                />
              </div>

              <div className="flex justify-end gap-2 pt-4">
                <Button variant="outline" onClick={handleCloseEditDialog}>
                  Cancel
                </Button>
                <Button
                  onClick={handleSubmitEdit}
                  className="bg-[#81A263] hover:bg-[#6b9a4d]"
                >
                  Update Record
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* History Sheet */}
      <Sheet open={isHistoryOpen} onOpenChange={setIsHistoryOpen}>
        <SheetContent className="w-[500px] sm:w-[600px] overflow-y-auto">
          <SheetHeader>
            <SheetTitle className="flex items-center gap-2">
              <History className="w-5 h-5 text-[#81A263]" />
              Edit History
            </SheetTitle>
          </SheetHeader>
          {selectedHistoryItem && (
            <div className="mt-6 space-y-4">
              <div className="bg-gray-50 rounded-lg p-4 space-y-2">
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600">Date:</span>
                  <span className="font-medium">
                    {format(new Date(selectedHistoryItem.date), 'd MMM yyyy')}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600">Recipe:</span>
                  <span className="font-medium">
                    {getRecipeName(selectedHistoryItem.recipeId)}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600">Current Quantity:</span>
                  <span className="font-medium">{selectedHistoryItem.quantity} dishes</span>
                </div>
              </div>

              <div className="space-y-3">
                <h4 className="font-medium text-[#333333]">
                  Change History ({selectedHistoryItem.editHistory?.length || 0} edits)
                </h4>
                {selectedHistoryItem.editHistory?.map((entry, index) => (
                  <div key={index} className="border rounded-lg p-4 space-y-2">
                    <div className="flex justify-between items-start">
                      <div>
                        <p className="font-medium text-[#333333]">{entry.editedBy}</p>
                        <p className="text-xs text-gray-500">
                          {format(new Date(entry.timestamp), 'd MMM yyyy HH:mm')}
                        </p>
                      </div>
                      <div className="text-right">
                        <p className="text-sm text-gray-600">
                          <span className="line-through">{entry.previousValue}</span>
                          {' → '}
                          <span className="font-medium text-[#81A263]">{entry.newValue}</span>
                        </p>
                      </div>
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