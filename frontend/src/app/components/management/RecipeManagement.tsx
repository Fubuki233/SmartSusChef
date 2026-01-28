import React, { useState, useMemo } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogTrigger } from '@/app/components/ui/dialog';
import { Select, SelectContent, SelectGroup, SelectItem, SelectLabel, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { Checkbox } from '@/app/components/ui/checkbox';
import { Plus, Edit, Trash2, ChefHat } from 'lucide-react';
import { toast } from 'sonner';
import { Recipe, RecipeIngredient } from '@/app/types/index';

// Type for ingredient row - can be either ingredient or sub-recipe
type IngredientRowType = 'ingredient' | 'subrecipe';

interface IngredientRow {
  type: IngredientRowType;
  ingredientId?: string;
  childRecipeId?: string;
  quantity: number;
}

export function RecipeManagement() {
  const { recipes, ingredients, addRecipe, updateRecipe, deleteRecipe } = useApp();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingRecipe, setEditingRecipe] = useState<Recipe | null>(null);
  const [recipeName, setRecipeName] = useState('');
  const [isSubRecipe, setIsSubRecipe] = useState(false);
  const [ingredientRows, setIngredientRows] = useState<IngredientRow[]>([
    { type: 'ingredient', ingredientId: '', quantity: 0 },
  ]);

  // Get sub-recipes that can be used as ingredients (exclude current editing recipe)
  const availableSubRecipes = useMemo(() => {
    return recipes.filter(r => r.isSubRecipe && r.id !== editingRecipe?.id);
  }, [recipes, editingRecipe]);

  const handleOpenDialog = (recipe?: Recipe) => {
    if (recipe) {
      setEditingRecipe(recipe);
      setRecipeName(recipe.name);
      setIsSubRecipe(recipe.isSubRecipe || false);
      // Convert recipe ingredients to ingredient rows
      const rows: IngredientRow[] = recipe.ingredients.map(ri => ({
        type: ri.childRecipeId ? 'subrecipe' : 'ingredient',
        ingredientId: ri.ingredientId || '',
        childRecipeId: ri.childRecipeId || '',
        quantity: ri.quantity,
      }));
      setIngredientRows(rows.length > 0 ? rows : [{ type: 'ingredient', ingredientId: '', quantity: 0 }]);
    } else {
      setEditingRecipe(null);
      setRecipeName('');
      setIsSubRecipe(false);
      setIngredientRows([{ type: 'ingredient', ingredientId: '', quantity: 0 }]);
    }
    setIsDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setIsDialogOpen(false);
    setEditingRecipe(null);
    setRecipeName('');
    setIsSubRecipe(false);
    setIngredientRows([{ type: 'ingredient', ingredientId: '', quantity: 0 }]);
  };

  const handleAddIngredientRow = () => {
    setIngredientRows([...ingredientRows, { type: 'ingredient', ingredientId: '', quantity: 0 }]);
  };

  const handleRemoveIngredientRow = (index: number) => {
    setIngredientRows(ingredientRows.filter((_, i) => i !== index));
  };

  const handleRowTypeChange = (index: number, type: IngredientRowType) => {
    const updated = [...ingredientRows];
    updated[index] = {
      type,
      ingredientId: type === 'ingredient' ? '' : undefined,
      childRecipeId: type === 'subrecipe' ? '' : undefined,
      quantity: updated[index].quantity,
    };
    setIngredientRows(updated);
  };

  const handleRowValueChange = (index: number, value: string) => {
    const updated = [...ingredientRows];
    if (updated[index].type === 'ingredient') {
      updated[index].ingredientId = value;
      updated[index].childRecipeId = undefined;
    } else {
      updated[index].childRecipeId = value;
      updated[index].ingredientId = undefined;
    }
    setIngredientRows(updated);
  };

  const handleQuantityChange = (index: number, value: string) => {
    const updated = [...ingredientRows];
    updated[index].quantity = parseFloat(value) || 0;
    setIngredientRows(updated);
  };

  const handleSubmit = () => {
    if (!recipeName.trim()) {
      toast.error('Please enter a recipe name');
      return;
    }

    // Convert ingredient rows to recipe ingredients
    const validIngredients: RecipeIngredient[] = ingredientRows
      .filter(row => {
        if (row.type === 'ingredient') {
          return row.ingredientId && row.quantity > 0;
        } else {
          return row.childRecipeId && row.quantity > 0;
        }
      })
      .map(row => ({
        ingredientId: row.type === 'ingredient' ? row.ingredientId : undefined,
        childRecipeId: row.type === 'subrecipe' ? row.childRecipeId : undefined,
        quantity: row.quantity,
      }));

    if (validIngredients.length === 0) {
      toast.error('Please add at least one ingredient or sub-recipe with quantity > 0');
      return;
    }

    const recipeData = {
      name: recipeName,
      isSubRecipe,
      ingredients: validIngredients,
    };

    if (editingRecipe) {
      updateRecipe(editingRecipe.id, recipeData);
      toast.success('Recipe updated successfully');
    } else {
      addRecipe(recipeData);
      toast.success('Recipe added successfully');
    }

    handleCloseDialog();
  };

  const handleDelete = (id: string, name: string) => {
    if (confirm(`Are you sure you want to delete "${name}"?`)) {
      deleteRecipe(id);
      toast.success('Recipe deleted successfully');
    }
  };

  const getIngredientName = (id?: string) => {
    if (!id) return 'Unknown';
    return ingredients.find((i) => i.id === id)?.name || 'Unknown';
  };

  const getIngredientUnit = (id?: string) => {
    if (!id) return '';
    return ingredients.find((i) => i.id === id)?.unit || '';
  };

  const getSubRecipeName = (id?: string) => {
    if (!id) return 'Unknown';
    return recipes.find((r) => r.id === id)?.name || 'Unknown';
  };

  // Get display name for an ingredient row
  const getIngredientDisplayName = (ri: RecipeIngredient) => {
    if (ri.childRecipeId) {
      return `[Sub] ${getSubRecipeName(ri.childRecipeId)}`;
    }
    return getIngredientName(ri.ingredientId);
  };

  // Get unit for display
  const getDisplayUnit = (ri: RecipeIngredient) => {
    if (ri.childRecipeId) {
      return 'portion';
    }
    return getIngredientUnit(ri.ingredientId);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold flex items-center gap-2 text-[#1A1C18] md:text-3xl lg:text-3xl">
            <ChefHat className="w-6 h-6 text-[#4F6F52]" />
            Recipe Management
          </h1>
          <p className="text-gray-600 mt-1">
            Managing recipes for <span className="font-bold text-[#4F6F52]">{useApp().storeSettings.storeName}</span>
          </p>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button onClick={() => handleOpenDialog()} className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-6 gap-2">
              <Plus className="w-4 h-4" />
              Add Recipe
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-[90vw] w-fit min-w-[600px] max-h-[90vh] overflow-y-auto rounded-[8px]">
            <DialogHeader>
              <DialogTitle>{editingRecipe ? 'Edit Recipe' : 'Add New Recipe'}</DialogTitle>
              <DialogDescription>
                Add raw ingredients or sub-recipes to build your recipe.
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-6 py-4">
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="recipe-name">Recipe Name</Label>
                  <Input
                    id="recipe-name"
                    placeholder="Enter recipe name"
                    value={recipeName}
                    onChange={(e) => setRecipeName(e.target.value)}
                    className="rounded-[8px]"
                  />
                </div>

                <div className="flex items-center space-x-2">
                  <Checkbox
                    id="is-sub-recipe"
                    checked={isSubRecipe}
                    onCheckedChange={(checked) => setIsSubRecipe(checked as boolean)}
                  />
                  <Label htmlFor="is-sub-recipe">Set as Sub-Recipe (can be used as ingredient in other recipes)</Label>
                </div>
              </div>

              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <Label className="text-lg font-semibold">Ingredients / Sub-Recipes</Label>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={handleAddIngredientRow}
                    className="gap-2 border-[#4F6F52] text-[#4F6F52] hover:bg-[#4F6F52]/10 rounded-[8px]"
                  >
                    <Plus className="w-4 h-4" />
                    Add Item
                  </Button>
                </div>

                <div className="space-y-3">
                  {ingredientRows.map((row, index) => (
                    <div key={index} className="flex gap-3 items-end p-3 border rounded-[8px] bg-gray-50/50">
                      {/* Type selector */}
                      <div className="w-36 space-y-2">
                        <Label>Type</Label>
                        <Select
                          value={row.type}
                          onValueChange={(value) => handleRowTypeChange(index, value as IngredientRowType)}
                        >
                          <SelectTrigger className="rounded-[8px] bg-white">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="ingredient">Ingredient</SelectItem>
                            <SelectItem value="subrecipe">Sub-Recipe</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>

                      {/* Item selector */}
                      <div className="flex-1 space-y-2">
                        <Label>{row.type === 'ingredient' ? 'Raw Ingredient' : 'Sub-Recipe'}</Label>
                        <Select
                          value={row.type === 'ingredient' ? (row.ingredientId || '') : (row.childRecipeId || '')}
                          onValueChange={(value) => handleRowValueChange(index, value)}
                        >
                          <SelectTrigger className="rounded-[8px] bg-white">
                            <SelectValue placeholder={row.type === 'ingredient' ? 'Select Ingredient' : 'Select Sub-Recipe'} />
                          </SelectTrigger>
                          <SelectContent>
                            {row.type === 'ingredient' ? (
                              <SelectGroup>
                                <SelectLabel>Available Ingredients</SelectLabel>
                                {ingredients.map((ing) => (
                                  <SelectItem key={ing.id} value={ing.id}>
                                    {ing.name} ({ing.unit})
                                  </SelectItem>
                                ))}
                              </SelectGroup>
                            ) : (
                              <SelectGroup>
                                <SelectLabel>Available Sub-Recipes</SelectLabel>
                                {availableSubRecipes.length > 0 ? (
                                  availableSubRecipes.map((recipe) => (
                                    <SelectItem key={recipe.id} value={recipe.id}>
                                      {recipe.name}
                                    </SelectItem>
                                  ))
                                ) : (
                                  <div className="px-2 py-1.5 text-sm text-gray-500">
                                    No sub-recipes available
                                  </div>
                                )}
                              </SelectGroup>
                            )}
                          </SelectContent>
                        </Select>
                      </div>

                      <div className="w-32 space-y-2">
                        <Label>{row.type === 'ingredient' ? 'Quantity' : 'Portions'}</Label>
                        <Input
                          type="number"
                          step="0.01"
                          min="0"
                          placeholder="0.00"
                          value={row.quantity || ''}
                          onChange={(e) => handleQuantityChange(index, e.target.value)}
                          className="rounded-[8px] bg-white"
                        />
                      </div>

                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => handleRemoveIngredientRow(index)}
                        disabled={ingredientRows.length === 1}
                        className="text-red-500 hover:text-red-700 hover:bg-red-50 h-10 w-10"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button variant="outline" onClick={handleCloseDialog} className="rounded-[8px]">
                Cancel
              </Button>
              <Button onClick={handleSubmit} className="bg-[#4F6F52] hover:bg-[#3D563F] text-white rounded-[32px] px-6">
                {editingRecipe ? 'Update Recipe' : 'Add Recipe'}
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <Card className="border-[#81A263]/20 rounded-[8px] overflow-hidden">
        <CardHeader className="bg-gray-50/50">
          <CardTitle>Recipe & Sub-Recipe List</CardTitle>
          <CardDescription>
            Recipes can contain raw ingredients and/or sub-recipes.
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[30%]">Name</TableHead>
                  <TableHead className="w-[15%]">Type</TableHead>
                  <TableHead className="w-[40%]">Ingredients / Sub-Recipes</TableHead>
                  <TableHead className="text-right w-[15%]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recipes.map((recipe) => (
                  <TableRow key={recipe.id}>
                    <TableCell className="font-medium text-[#1A1C18]">{recipe.name}</TableCell>
                    <TableCell>
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${recipe.isSubRecipe ? 'bg-orange-100 text-orange-700' : 'bg-blue-100 text-blue-700'
                        }`}>
                        {recipe.isSubRecipe ? 'Sub-Recipe' : 'Main Dish'}
                      </span>
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {recipe.ingredients.map((ri, idx) => (
                          <span
                            key={idx}
                            className={`text-xs px-2 py-1 rounded-md ${ri.childRecipeId
                              ? 'bg-orange-50 text-orange-700 border border-orange-200'
                              : 'bg-gray-100 text-gray-600'
                              }`}
                          >
                            {getIngredientDisplayName(ri)}: {ri.quantity}{getDisplayUnit(ri)}
                          </span>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-1">
                        <Button variant="ghost" size="icon" onClick={() => handleOpenDialog(recipe)} className="h-8 w-8">
                          <Edit className="w-4 h-4 text-gray-500" />
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => handleDelete(recipe.id, recipe.name)} className="h-8 w-8">
                          <Trash2 className="w-4 h-4 text-[#E74C3C]" />
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