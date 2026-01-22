import React, { useState } from 'react';
import { useApp } from '@/app/context/AppContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/app/components/ui/card';
import { Button } from '@/app/components/ui/button';
import { Input } from '@/app/components/ui/input';
import { Label } from '@/app/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/app/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/app/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/app/components/ui/select';
import { Plus, Edit, Trash2, ChefHat } from 'lucide-react';
import { toast } from 'sonner';
import { Recipe, RecipeIngredient } from '@/app/types';

export function RecipeManagement() {
  const { recipes, ingredients, addRecipe, updateRecipe, deleteRecipe } = useApp();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingRecipe, setEditingRecipe] = useState<Recipe | null>(null);
  const [recipeName, setRecipeName] = useState('');
  const [recipeIngredients, setRecipeIngredients] = useState<RecipeIngredient[]>([
    { ingredientId: '', quantity: 0 },
  ]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleOpenDialog = (recipe?: Recipe) => {
    if (recipe) {
      setEditingRecipe(recipe);
      setRecipeName(recipe.name);
      setRecipeIngredients([...recipe.ingredients]);
    } else {
      setEditingRecipe(null);
      setRecipeName('');
      setRecipeIngredients([{ ingredientId: '', quantity: 0 }]);
    }
    setIsDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setIsDialogOpen(false);
    setEditingRecipe(null);
    setRecipeName('');
    setRecipeIngredients([{ ingredientId: '', quantity: 0 }]);
  };

  const handleAddIngredient = () => {
    setRecipeIngredients([...recipeIngredients, { ingredientId: '', quantity: 0 }]);
  };

  const handleRemoveIngredient = (index: number) => {
    setRecipeIngredients(recipeIngredients.filter((_, i) => i !== index));
  };

  const handleIngredientChange = (
    index: number,
    field: 'ingredientId' | 'quantity',
    value: string | number
  ) => {
    const updated = [...recipeIngredients];
    if (field === 'ingredientId') {
      updated[index].ingredientId = value as string;
    } else {
      updated[index].quantity = parseFloat(value as string) || 0;
    }
    setRecipeIngredients(updated);
  };

  const handleSubmit = async () => {
    if (!recipeName.trim()) {
      toast.error('Please enter a recipe name');
      return;
    }

    const validIngredients = recipeIngredients.filter(
      (ri) => ri.ingredientId && ri.quantity > 0
    );

    if (validIngredients.length === 0) {
      toast.error('Please add at least one ingredient with quantity > 0');
      return;
    }

    setIsSubmitting(true);
    try {
      if (editingRecipe) {
        await updateRecipe(editingRecipe.id, {
          name: recipeName,
          ingredients: validIngredients,
        });
        toast.success('Recipe updated successfully');
      } else {
        await addRecipe({
          name: recipeName,
          ingredients: validIngredients,
        });
        toast.success('Recipe added successfully');
      }
      handleCloseDialog();
    } catch (error) {
      toast.error('Failed to save recipe. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (id: string, name: string) => {
    if (confirm(`Are you sure you want to delete "${name}"?`)) {
      try {
        await deleteRecipe(id);
        toast.success('Recipe deleted successfully');
      } catch (error) {
        toast.error('Failed to delete recipe. Please try again.');
      }
    }
  };

  const getIngredientName = (id: string) => {
    return ingredients.find((i) => i.id === id)?.name || 'Unknown';
  };

  const getIngredientUnit = (id: string) => {
    return ingredients.find((i) => i.id === id)?.unit || '';
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold flex items-center gap-2">
            <ChefHat className="w-6 h-6" />
            Recipe Management
          </h1>
          <p className="text-gray-600 mt-1">Manage dish recipes and their ingredients</p>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button onClick={() => handleOpenDialog()} className="gap-2">
              <Plus className="w-4 h-4" />
              Add Recipe
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>
                {editingRecipe ? 'Edit Recipe' : 'Add New Recipe'}
              </DialogTitle>
            </DialogHeader>
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="recipe-name">Recipe Name</Label>
                <Input
                  id="recipe-name"
                  value={recipeName}
                  onChange={(e) => setRecipeName(e.target.value)}
                  placeholder="Enter recipe name"
                />
              </div>

              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <Label>Ingredients</Label>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={handleAddIngredient}
                    className="gap-2"
                  >
                    <Plus className="w-3 h-3" />
                    Add Ingredient
                  </Button>
                </div>

                {recipeIngredients.map((ri, index) => (
                  <div key={index} className="flex items-end gap-2">
                    <div className="flex-1">
                      <Label className="text-xs">Ingredient</Label>
                      <Select
                        value={ri.ingredientId}
                        onValueChange={(value) =>
                          handleIngredientChange(index, 'ingredientId', value)
                        }
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select ingredient" />
                        </SelectTrigger>
                        <SelectContent>
                          {ingredients.map((ingredient) => (
                            <SelectItem key={ingredient.id} value={ingredient.id}>
                              {ingredient.name} ({ingredient.unit})
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="w-32">
                      <Label className="text-xs">Quantity</Label>
                      <Input
                        type="number"
                        step="0.01"
                        min="0"
                        value={ri.quantity || ''}
                        onChange={(e) =>
                          handleIngredientChange(index, 'quantity', e.target.value)
                        }
                        placeholder="0.00"
                      />
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => handleRemoveIngredient(index)}
                      disabled={recipeIngredients.length === 1}
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                ))}
              </div>

              <div className="flex justify-end gap-2 pt-4">
                <Button variant="outline" onClick={handleCloseDialog}>
                  Cancel
                </Button>
                <Button onClick={handleSubmit}>
                  {editingRecipe ? 'Update' : 'Add'} Recipe
                </Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
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
                <TableRow>
                  <TableHead>Recipe Name</TableHead>
                  <TableHead>Ingredients</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {recipes.map((recipe) => (
                  <TableRow key={recipe.id}>
                    <TableCell className="font-medium">{recipe.name}</TableCell>
                    <TableCell>
                      <div className="space-y-1">
                        {recipe.ingredients.map((ri, idx) => (
                          <div key={idx} className="text-sm text-gray-600">
                            {getIngredientName(ri.ingredientId)}: {ri.quantity}{' '}
                            {getIngredientUnit(ri.ingredientId)}
                          </div>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleOpenDialog(recipe)}
                        >
                          <Edit className="w-4 h-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDelete(recipe.id, recipe.name)}
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
