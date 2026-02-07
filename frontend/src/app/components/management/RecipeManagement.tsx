import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogTrigger, DialogFooter } from '@/app/components/ui/dialog';
import { Select, SelectContent, SelectGroup, SelectItem, SelectLabel, SelectTrigger, SelectValue, SelectSeparator } from '@/app/components/ui/select';
import { Checkbox } from '@/app/components/ui/checkbox';
import { Badge } from '@/app/components/ui/badge';
import { Plus, Edit, Trash2, ChefHat, Utensils, Wheat, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';
import { Recipe } from '@/app/types/index';

// Helper type for the form state
interface FormRow {
  type: 'ingredient' | 'recipe';
  id: string;
  quantity: string | number;
}

export function RecipeManagement() {
  const { recipes, ingredients, addRecipe, updateRecipe, deleteRecipe, storeSettings, salesData, wastageData } = useApp();

  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingRecipe, setEditingRecipe] = useState<Recipe | null>(null);
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [deletingRecipe, setDeletingRecipe] = useState<{ id: string; name: string; salesCount: number; wastageCount: number } | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isRecipeUsageDialogOpen, setIsRecipeUsageDialogOpen] = useState(false);
  const [recipeInUse, setRecipeInUse] = useState<{ id: string; name: string; usedInRecipes: string[] } | null>(null);

  interface RecipeFormState {
    name: string;
    isSubRecipe: boolean;
    formRows: FormRow[];
  }

  const initialFormState: RecipeFormState = {
    name: '',
    isSubRecipe: false,
    formRows: [{ type: 'ingredient', id: '', quantity: '' }],
  };

  const [recipeForm, setRecipeForm] = useState<RecipeFormState>(initialFormState);
  const { name: recipeName, isSubRecipe, formRows } = recipeForm;


  // --- Handlers ---

  const handleOpenDialog = (recipe?: Recipe) => {
    if (recipe) {
      setEditingRecipe(recipe);
      const mappedRows: FormRow[] = recipe.ingredients.map(comp => ({
        type: comp.childRecipeId ? 'recipe' : 'ingredient',
        id: comp.childRecipeId || comp.ingredientId || '',
        quantity: comp.quantity.toString()
      }));
      setRecipeForm({
        name: recipe.name,
        isSubRecipe: recipe.isSubRecipe || false,
        formRows: mappedRows.length > 0 ? mappedRows : [{ type: 'ingredient', id: '', quantity: '' }]
      });
    } else {
      setEditingRecipe(null);
      setRecipeForm(initialFormState);
    }
    setIsDialogOpen(true);
  };

  const handleSubRecipeToggle = (checked: boolean) => {
    setRecipeForm(prev => {
      let newRows = prev.formRows;
      if (checked) {
        newRows = prev.formRows.map(row => {
          if (row.type === 'recipe') {
            return { type: 'ingredient' as const, id: '', quantity: row.quantity };
          }
          return row;
        });
      }
      return { ...prev, isSubRecipe: checked, formRows: newRows };
    });
  };

  const handleAddRow = () => {
    setRecipeForm(prev => ({
      ...prev,
      formRows: [...prev.formRows, { type: 'ingredient', id: '', quantity: '' }]
    }));
  };

  const handleRemoveRow = (index: number) => {
    setRecipeForm(prev => ({
      ...prev,
      formRows: prev.formRows.filter((_, i) => i !== index)
    }));
  };

  const handleQuantityChange = (index: number, value: string) => {
    const updatedRows = [...formRows];
    updatedRows[index].quantity = value;
    setRecipeForm(prev => ({ ...prev, formRows: updatedRows }));
  };

  const handleItemSelection = (index: number, value: string) => {
    const [type, id] = value.split('|');
    const updatedRows = [...formRows];
    updatedRows[index] = {
      ...updatedRows[index],
      type: type as 'ingredient' | 'recipe',
      id: id
    };
    setRecipeForm(prev => ({ ...prev, formRows: updatedRows }));
  };

  const handleSubmit = async () => {
    if (!recipeName.trim()) {
      toast.error('Please enter a recipe name');
      return;
    }

    // Check if any selected component has quantity <= 0
    const invalidComponents = formRows.filter(r => {
      if (!r.id) return false;
      const qty = typeof r.quantity === 'string' ? parseFloat(r.quantity) : r.quantity;
      return isNaN(qty) || qty <= 0;
    });
    if (invalidComponents.length > 0) {
      toast.error('QTY cannot be 0 or negative. Please enter a valid quantity.');
      return;
    }

    const validComponents = formRows.filter(r => {
      if (!r.id) return false;
      const qty = typeof r.quantity === 'string' ? parseFloat(r.quantity) : r.quantity;
      return !isNaN(qty) && qty > 0;
    });

    if (validComponents.length === 0) {
      toast.error('Please add at least one component with quantity > 0');
      return;
    }

    const formattedIngredients = validComponents.map(row => ({
      ingredientId: row.type === 'ingredient' ? row.id : undefined,
      childRecipeId: row.type === 'recipe' ? row.id : undefined,
      quantity: typeof row.quantity === 'string' ? parseFloat(row.quantity) : row.quantity
    }));

    const recipeData = {
      name: recipeName,
      isSubRecipe,
      ingredients: formattedIngredients,
    };

    setIsSubmitting(true);
    try {
      if (editingRecipe) {
        await updateRecipe(editingRecipe.id, recipeData);
        toast.success('Recipe updated successfully');
      } else {
        await addRecipe(recipeData);
        toast.success('Recipe added successfully');
      }
      setIsDialogOpen(false);
    } catch (error) {
      toast.error('Failed to save recipe');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenDeleteDialog = (id: string, name: string) => {
    // First check if this is a sub-recipe used in other recipes
    const recipeToDelete = recipes.find(r => r.id === id);
    if (recipeToDelete?.isSubRecipe) {
      const recipesUsingSubRecipe = recipes.filter(recipe =>
        recipe.ingredients.some(ing => ing.childRecipeId === id)
      );

      if (recipesUsingSubRecipe.length > 0) {
        // Show recipe usage dialog instead of delete dialog
        setRecipeInUse({
          id,
          name,
          usedInRecipes: recipesUsingSubRecipe.map(r => r.name)
        });
        setIsRecipeUsageDialogOpen(true);
        return;
      }
    }

    // Check if recipe exists in Sales Data or Wastage Data
    const salesCount = salesData.filter(sale => sale.recipeId === id).length;
    const wastageCount = wastageData.filter(waste => waste.recipeId === id).length;

    setDeletingRecipe({ id, name, salesCount, wastageCount });
    setIsDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!deletingRecipe) return;

    const { salesCount, wastageCount } = deletingRecipe;
    const hasRelatedData = salesCount > 0 || wastageCount > 0;

    // If no related data, just delete
    if (!hasRelatedData) {
      setIsDeleting(true);
      try {
        await deleteRecipe(deletingRecipe.id, false);
        toast.success('Recipe deleted successfully');
        setIsDeleteDialogOpen(false);
        setDeletingRecipe(null);
      } catch (error) {
        toast.error('Failed to delete recipe');
      } finally {
        setIsDeleting(false);
      }
      return;
    }

    // If related data exists, delete with cascade
    setIsDeleting(true);
    try {
      await deleteRecipe(deletingRecipe.id, true);
      toast.success(`Recipe and ${salesCount + wastageCount} related records deleted successfully`);
      setIsDeleteDialogOpen(false);
      setDeletingRecipe(null);
    } catch (error) {
      toast.error('Failed to delete recipe and related data');
    } finally {
      setIsDeleting(false);
    }
  };

  const getComponentName = (item: any) => {
    if (item.childRecipeId) {
      return recipes.find(r => r.id === item.childRecipeId)?.name || 'Unknown Recipe';
    }
    return ingredients.find(i => i.id === item.ingredientId)?.name || 'Unknown Ingredient';
  };

  const getComponentUnit = (item: any) => {
    if (item.childRecipeId) {
      const recipe = recipes.find(r => r.id === item.childRecipeId);
      return recipe?.unit || 'portion'; // Use recipe's unit or default to portion
    }
    return ingredients.find(i => i.id === item.ingredientId)?.unit || '';
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold flex items-center gap-2 text-[#1A1C18] md:text-3xl lg:text-3xl">
            <ChefHat className="w-7 h-7 text-[#4F6F52]" />
            Recipe Management
          </h1>
          <p className="text-gray-600 mt-1">
            Manage main dishes and sub-recipes
          </p>
        </div>
        <Button onClick={() => handleOpenDialog()} className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-6 gap-2">
          <Plus className="w-4 h-4" />
          Add Recipe
        </Button>
      </div>

      {/* Main Content Card */}
      <Card className="rounded-[12px] shadow-sm overflow-hidden bg-white border border-gray-200">
        <CardHeader>
          <CardTitle>All Recipes</CardTitle>
          <CardDescription>
            {recipes.length} recipe{recipes.length !== 1 ? 's' : ''} in the system
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="border rounded-lg">
            <Table>
              <TableHeader>
                <TableRow className="bg-gray-50/50">
                  <TableHead className="w-[30%]">Recipe Name</TableHead>
                  <TableHead className="w-[15%]">Type</TableHead>
                  <TableHead className="w-[40%]">Components</TableHead>
                  <TableHead className="text-right w-[15%]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recipes.map((recipe) => (
                  <TableRow key={recipe.id} className="hover:bg-gray-50/50">
                    <TableCell className="font-medium text-[#1A1C18]">
                      {recipe.name}
                    </TableCell>
                    <TableCell>
                      <Badge className={`rounded-[4px] px-3 py-1 font-medium text-white border-none shadow-none ${recipe.isSubRecipe
                        ? 'bg-[#F59E0B]' // Amber/Orange for Sub-Recipe
                        : 'bg-[#0EA5E9]' // Sky Blue for Main Dish 
                        }`}
                      >
                        {recipe.isSubRecipe ? 'Sub-Recipe' : 'Main Dish'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-col gap-1.5">
                        {recipe.ingredients.map((comp, idx) => (
                          <div key={idx} className="flex items-center gap-2 text-sm text-gray-600">
                            {comp.childRecipeId ? (
                              <Utensils className="w-3 h-3 text-orange-500" />
                            ) : (
                              <Wheat className="w-3 h-3 text-[#4F6F52]" />
                            )}
                            <span className="font-medium">
                              {getComponentName(comp)}
                            </span>
                            <span className="text-gray-400 text-xs">
                              — {comp.quantity} {getComponentUnit(comp)}
                            </span>
                          </div>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button variant="ghost" size="icon" onClick={() => handleOpenDialog(recipe)} className="hover:bg-gray-100">
                          <Edit className="w-4 h-4" />
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => handleOpenDeleteDialog(recipe.id, recipe.name)} className="hover:bg-gray-100">
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

      {/* --- ADD/EDIT DIALOG --- */}
      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="max-w-2xl rounded-[12px] border-none shadow-2xl p-0 overflow-hidden">
          <DialogHeader className="p-6 pb-2">
            <DialogTitle className="text-2xl text-[#1A1C18]">
              {editingRecipe ? 'Edit Recipe' : 'Add New Recipe'}
            </DialogTitle>
            <DialogDescription>
              Build your recipe by adding raw ingredients or existing sub-recipes.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-6 px-6 py-2 max-h-[500px] overflow-y-auto">
            {/* 1. Recipe Name & Checkbox */}
            <div className="space-y-3">
              <div className="space-y-2">
                <Label htmlFor="recipe-name" className="text-sm font-bold text-gray-700">Recipe Name</Label>
                <Input
                  id="recipe-name"
                  placeholder="e.g. Mala Xiang Guo"
                  value={recipeName}
                  onChange={(e) => setRecipeForm(prev => ({ ...prev, name: e.target.value }))}
                  className="rounded-[8px] border-gray-300 h-10"
                />
              </div>

              <div className="flex items-center space-x-2">
                <Checkbox
                  id="is-sub-recipe"
                  checked={isSubRecipe}
                  onCheckedChange={(checked) => handleSubRecipeToggle(checked as boolean)}
                  className="data-[state=checked]:bg-[#4F6F52] border-gray-400 rounded-[4px]"
                />
                <Label htmlFor="is-sub-recipe" className="text-sm font-medium cursor-pointer text-gray-700">
                  Set as Sub-Recipe (e.g. Sauce, Stock)
                </Label>
              </div>

            </div>

            {/* 2. Recipe Components Section */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label className="text-sm font-bold text-[#1A1C18]">Recipe Components</Label>
                <Button
                  type="button"
                  onClick={handleAddRow}
                  variant="ghost"
                  size="sm"
                  className="text-[#4F6F52] hover:text-[#3D563F] hover:bg-[#4F6F52]/10 h-8 px-2 font-medium"
                >
                  <Plus className="w-4 h-4 mr-1.5" /> Add Component
                </Button>
              </div>

              {/* Minimal Header */}
              {formRows.length > 0 && (
                <div className="grid grid-cols-12 gap-3 px-1 mb-1">
                  <div className="col-span-8 text-xs font-bold text-gray-500 uppercase tracking-wider">Item Name</div>
                  <div className="col-span-3 text-xs font-bold text-gray-500 uppercase tracking-wider">Qty</div>
                  <div className="col-span-1"></div>
                </div>
              )}

              {/* Component Rows */}
              <div className="space-y-2">
                {formRows.map((row, index) => (
                  <div key={index} className="grid grid-cols-12 gap-3 items-center">

                    {/* Item Name Dropdown */}
                    <div className="col-span-8">
                      <Select
                        value={row.id ? `${row.type}|${row.id}` : ''}
                        onValueChange={(val) => handleItemSelection(index, val)}
                      >
                        <SelectTrigger className="bg-white rounded-[8px] h-10 border-gray-300 focus:ring-[#4F6F52]">
                          <SelectValue placeholder="Select Ingredient..." />
                        </SelectTrigger>
                        <SelectContent className="max-h-[200px]" position="popper" sideOffset={5}>
                          {!isSubRecipe && (
                            <SelectGroup>
                              <SelectLabel className="text-xs font-bold text-[#4F6F52] px-2 py-1.5 bg-gray-50 uppercase tracking-wider sticky top-0 z-10">Sub-Recipes</SelectLabel>
                              {recipes
                                .filter(r => r.isSubRecipe && r.id !== editingRecipe?.id)
                                .map((r) => (
                                  <SelectItem key={r.id} value={`recipe|${r.id}`} className="pl-4">
                                    {r.name}
                                  </SelectItem>
                                ))}
                            </SelectGroup>
                          )}

                          {!isSubRecipe && <SelectSeparator className="my-1" />}

                          <SelectGroup>
                            <SelectLabel className="text-xs font-bold text-[#4F6F52] px-2 py-1.5 bg-gray-50 uppercase tracking-wider sticky top-0 z-10">Raw Ingredients</SelectLabel>
                            {ingredients.map((ing) => (
                              <SelectItem key={ing.id} value={`ingredient|${ing.id}`} className="pl-4">
                                {ing.name} <span className="text-gray-400 text-xs ml-1">({ing.unit})</span>
                              </SelectItem>
                            ))}
                          </SelectGroup>
                        </SelectContent>
                      </Select>
                    </div>

                    {/* Quantity Input */}
                    <div className="col-span-3">
                      <Input
                        type="number"
                        step="0.01"
                        value={row.quantity || ''}
                        onChange={(e) => handleQuantityChange(index, e.target.value)}
                        className="bg-white rounded-[8px] h-10 border-gray-300 focus-visible:ring-[#4F6F52]"
                        placeholder="0.00"
                      />
                    </div>

                    {/* Delete Action */}
                    <div className="col-span-1 flex justify-end">
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => handleRemoveRow(index)}
                        disabled={formRows.length === 1}
                        className="text-gray-400 hover:text-red-600 hover:bg-red-50 h-9 w-9 rounded-full transition-colors"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <DialogFooter className="p-6 pt-4 bg-gray-50 border-t border-gray-100 gap-3">
            <Button variant="outline" onClick={() => setIsDialogOpen(false)} disabled={isSubmitting} className="rounded-[32px] px-6 border-gray-300 h-10 font-medium hover:bg-gray-100">
              Cancel
            </Button>
            <Button onClick={handleSubmit} disabled={isSubmitting} className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-8 h-10 font-medium shadow-sm transition-all hover:shadow-md">
              {editingRecipe ? 'Save Changes' : 'Create Recipe'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
        <DialogContent className="max-w-md rounded-[12px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-red-600">
              <Trash2 className="w-5 h-5" />
              Confirm Deletion
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              {deletingRecipe && (
                <>
                  {deletingRecipe.salesCount > 0 || deletingRecipe.wastageCount > 0 ? (
                    <>
                      <p className="text-gray-700">
                        This recipe has related data records. Deleting it will also remove:
                      </p>
                      <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 space-y-2">
                        <div className="font-medium text-amber-900">{deletingRecipe.name}</div>
                        <div className="space-y-1 text-sm">
                          {deletingRecipe.salesCount > 0 && (
                            <div className="text-gray-700">
                              • <span className="font-semibold text-amber-700">{deletingRecipe.salesCount}</span> Sales Data record{deletingRecipe.salesCount > 1 ? 's' : ''}
                            </div>
                          )}
                          {deletingRecipe.wastageCount > 0 && (
                            <div className="text-gray-700">
                              • <span className="font-semibold text-amber-700">{deletingRecipe.wastageCount}</span> Wastage Data record{deletingRecipe.wastageCount > 1 ? 's' : ''}
                            </div>
                          )}
                        </div>
                      </div>
                      <p className="text-sm text-red-600 font-medium">
                        ⚠️ Warning: This will permanently delete the recipe and all {deletingRecipe.salesCount + deletingRecipe.wastageCount} related data records. This action cannot be undone.
                      </p>
                    </>
                  ) : (
                    <>
                      <p className="text-gray-700">
                        Are you sure you want to delete this recipe?
                      </p>
                      <div className="bg-red-50 border border-red-200 rounded-lg p-3">
                        <div className="text-sm">
                          <div className="text-gray-600">Recipe Name:</div>
                          <div className="font-medium text-gray-900 mt-1">
                            {deletingRecipe.name}
                          </div>
                        </div>
                      </div>
                      <p className="text-sm text-red-600 font-medium">
                        Warning: This action cannot be undone.
                      </p>
                    </>
                  )}
                </>
              )}
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
                onClick={handleDeleteConfirm}
                disabled={isDeleting}
                className="bg-red-600 hover:bg-red-700 rounded-[32px] px-6"
              >
                Yes, Delete Recipe
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Recipe Usage Warning Dialog */}
      <Dialog open={isRecipeUsageDialogOpen} onOpenChange={setIsRecipeUsageDialogOpen}>
        <DialogContent className="max-w-md rounded-[12px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-amber-600">
              <AlertTriangle className="w-5 h-5" />
              Cannot Delete Sub-Recipe
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            {recipeInUse && (
              <>
                <p className="text-gray-700">
                  This sub-recipe <span className="font-semibold">{recipeInUse.name}</span> is currently used in the following main dish{recipeInUse.usedInRecipes.length > 1 ? 'es' : ''}:
                </p>
                <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 max-h-60 overflow-y-auto">
                  <ul className="space-y-2">
                    {recipeInUse.usedInRecipes.map((recipeName, index) => (
                      <li key={index} className="flex items-start gap-2">
                        <span className="text-amber-600 mt-0.5">•</span>
                        <span className="text-gray-900 font-medium">{recipeName}</span>
                      </li>
                    ))}
                  </ul>
                </div>
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
                  <p className="text-sm text-blue-900">
                    <strong>Note:</strong> You need to remove this sub-recipe from {recipeInUse.usedInRecipes.length > 1 ? 'these dishes' : 'this dish'} before you can delete it.
                  </p>
                </div>
              </>
            )}
            <div className="flex justify-end gap-2 pt-2">
              <Button
                variant="outline"
                onClick={() => {
                  setIsRecipeUsageDialogOpen(false);
                  setRecipeInUse(null);
                }}
                className="rounded-[32px] px-6 hover:bg-gray-100"
              >
                OK, I Understand
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}