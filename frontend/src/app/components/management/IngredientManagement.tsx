import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogTrigger } from '@/app/components/ui/dialog';
import { Plus, Edit, Trash2, Package } from 'lucide-react';
import { toast } from 'sonner';
import { Ingredient } from '@/app/types';

export function IngredientManagement() {
  const { ingredients, addIngredient, updateIngredient, deleteIngredient } = useApp();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingIngredient, setEditingIngredient] = useState<Ingredient | null>(null);
  const [name, setName] = useState('');
  const [unit, setUnit] = useState('');
  const [carbonFootprint, setCarbonFootprint] = useState('');

  const handleOpenDialog = (ingredient?: Ingredient) => {
    if (ingredient) {
      setEditingIngredient(ingredient);
      setName(ingredient.name);
      setUnit(ingredient.unit);
      setCarbonFootprint(ingredient.carbonFootprint.toString());
    } else {
      setEditingIngredient(null);
      setName('');
      setUnit('');
      setCarbonFootprint('');
    }
    setIsDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setIsDialogOpen(false);
    setEditingIngredient(null);
    setName('');
    setUnit('');
    setCarbonFootprint('');
  };

  const handleSubmit = () => {
    if (!name.trim() || !unit.trim() || !carbonFootprint) {
      toast.error('Please fill in all required fields');
      return;
    }

    const carbon = parseFloat(carbonFootprint);
    if (isNaN(carbon) || carbon < 0) {
      toast.error('Carbon footprint must be a positive number');
      return;
    }

    const ingredientData = {
      name: name.trim(),
      unit: unit.trim(),
      carbonFootprint: carbon,
    };

    if (editingIngredient) {
      updateIngredient(editingIngredient.id, ingredientData);
      toast.success('Ingredient updated successfully');
    } else {
      addIngredient(ingredientData);
      toast.success('Ingredient added successfully');
    }

    handleCloseDialog();
  };

  const handleDelete = (id: string, name: string) => {
    if (confirm(`Are you sure you want to delete "${name}"?`)) {
      deleteIngredient(id);
      toast.success('Ingredient deleted successfully');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold flex items-center gap-2">
            <Package className="w-6 h-6 text-[#4F6F52]" />
            Ingredient Management
          </h1>
          <p className="text-gray-600 mt-1">Master data for <span className="font-bold text-[#4F6F52]">{useApp().storeSettings.storeName}</span></p>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button onClick={() => handleOpenDialog()} className="gap-2">
              <Plus className="w-4 h-4" />
              Add Ingredient
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>
                {editingIngredient ? 'Edit Ingredient' : 'Add New Ingredient'}
              </DialogTitle>
              <DialogDescription>
                {editingIngredient ? 'Edit the details of the ingredient' : 'Add a new ingredient to the system'}
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="ingredient-name">Ingredient Name</Label>
                <Input
                  id="ingredient-name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g., Chicken Breast"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="ingredient-unit">Unit</Label>
                <Input
                  id="ingredient-unit"
                  value={unit}
                  onChange={(e) => setUnit(e.target.value)}
                  placeholder="e.g., kg, g, L, ml"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="carbon-footprint">
                  Carbon Footprint (kg CO₂ per unit)
                </Label>
                <Input
                  id="carbon-footprint"
                  type="number"
                  step="0.1"
                  min="0"
                  value={carbonFootprint}
                  onChange={(e) => setCarbonFootprint(e.target.value)}
                  placeholder="e.g., 6.9"
                />
              </div>

              <div className="flex justify-end gap-2 pt-4">
                <Button variant="outline" onClick={handleCloseDialog}>
                  Cancel
                </Button>
                <Button onClick={handleSubmit}>
                  {editingIngredient ? 'Update Ingredient' : 'Add Ingredient'}
                </Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Ingredients</CardTitle>
          <CardDescription>
            {ingredients.length} ingredient{ingredients.length !== 1 ? 's' : ''} in the system
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="border rounded-lg">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Unit</TableHead>
                  <TableHead>Carbon Footprint (kg CO₂)</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {ingredients.map((ingredient) => (
                  <TableRow key={ingredient.id}>
                    <TableCell className="font-medium">{ingredient.name}</TableCell>
                    <TableCell>{ingredient.unit}</TableCell>
                    <TableCell>{ingredient.carbonFootprint.toFixed(1)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleOpenDialog(ingredient)}
                        >
                          <Edit className="w-4 h-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDelete(ingredient.id, ingredient.name)}
                        >
                          <Trash2 className="w-4 h-4 text-red-600" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}