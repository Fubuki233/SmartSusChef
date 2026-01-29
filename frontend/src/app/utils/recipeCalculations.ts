/**
 * Recipe Calculation Utilities
 * Functions for calculating recipe weights, carbon footprints, and costs
 */

import { Recipe, Ingredient } from '@/app/types';
import { getStandardizedQuantity } from './unitConversion';

/**
 * Recursively calculates the total weight of a recipe in kg
 * Handles nested sub-recipes
 * @param recipeId - The recipe ID to calculate
 * @param recipeMap - Map of recipe ID to recipe object
 * @param ingredientMap - Map of ingredient ID to ingredient object
 * @returns Total weight in kg
 */
export function calculateRecipeWeight(
  recipeId: string,
  recipeMap: Map<string, Recipe>,
  ingredientMap: Map<string, Ingredient>
): number {
  const recipe = recipeMap.get(recipeId);
  if (!recipe) return 0;

  // Sum up ingredient weights
  return recipe.ingredients.reduce((total, component) => {
    let componentWeight = 0;
    
    // Case 1: Direct ingredient
    if (component.ingredientId) {
      const ing = ingredientMap.get(component.ingredientId);
      if (ing) {
        componentWeight = getStandardizedQuantity(component.quantity, ing.unit);
      }
    } 
    // Case 2: Sub-recipe (recursive)
    else if (component.childRecipeId) {
      const subRecipeWeight = calculateRecipeWeight(
        component.childRecipeId,
        recipeMap,
        ingredientMap
      );
      componentWeight = subRecipeWeight * component.quantity;
    }
    
    return total + componentWeight;
  }, 0);
}

/**
 * Recursively calculates the carbon footprint of a recipe in kg CO₂
 * Handles nested sub-recipes
 * @param recipeId - The recipe ID to calculate
 * @param recipeMap - Map of recipe ID to recipe object
 * @param ingredientMap - Map of ingredient ID to ingredient object
 * @returns Total carbon footprint in kg CO₂
 */
export function calculateRecipeCarbon(
  recipeId: string,
  recipeMap: Map<string, Recipe>,
  ingredientMap: Map<string, Ingredient>
): number {
  const recipe = recipeMap.get(recipeId);
  if (!recipe) return 0;

  return recipe.ingredients.reduce((total, component) => {
    let componentCarbon = 0;
    
    // Case 1: Direct ingredient
    if (component.ingredientId) {
      const ing = ingredientMap.get(component.ingredientId);
      if (ing) {
        const weightKg = getStandardizedQuantity(component.quantity, ing.unit);
        componentCarbon = weightKg * ing.carbonFootprint;
      }
    } 
    // Case 2: Sub-recipe (recursive)
    else if (component.childRecipeId) {
      const subRecipeCarbon = calculateRecipeCarbon(
        component.childRecipeId,
        recipeMap,
        ingredientMap
      );
      componentCarbon = subRecipeCarbon * component.quantity;
    }
    
    return total + componentCarbon;
  }, 0);
}

/**
 * Calculates the cost per serving/plate for a recipe
 * Handles nested sub-recipes
 * @param recipeId - The recipe ID to calculate
 * @param recipeMap - Map of recipe ID to recipe object
 * @param ingredientMap - Map of ingredient ID to ingredient object with cost data
 * @returns Total cost per serving
 */
export function calculateRecipeCost(
  recipeId: string,
  recipeMap: Map<string, Recipe>,
  ingredientMap: Map<string, Ingredient>
): number {
  const recipe = recipeMap.get(recipeId);
  if (!recipe) return 0;

  return recipe.ingredients.reduce((total, component) => {
    let componentCost = 0;
    
    // Case 1: Direct ingredient
    if (component.ingredientId) {
      const ing = ingredientMap.get(component.ingredientId);
      if (ing && ing.supplierUnitCost && ing.totalUnitsInPackage) {
        // Calculate cost per unit (e.g., per gram or ml)
        const costPerUnit = ing.supplierUnitCost / ing.totalUnitsInPackage;
        componentCost = component.quantity * costPerUnit;
      }
    } 
    // Case 2: Sub-recipe (recursive)
    else if (component.childRecipeId) {
      const subRecipeCost = calculateRecipeCost(
        component.childRecipeId,
        recipeMap,
        ingredientMap
      );
      componentCost = subRecipeCost * component.quantity;
    }
    
    return total + componentCost;
  }, 0);
}

/**
 * Gets the display unit for a recipe
 * @param recipe - The recipe object
 * @returns The appropriate unit string
 */
export function getRecipeUnit(recipe: Recipe): string {
  // If recipe has a specified unit, use it
  if (recipe.unit) {
    return recipe.unit;
  }
  
  // Otherwise, use defaults based on type
  return recipe.isSubRecipe ? 'L' : 'plate';
}

/**
 * Breaks down a recipe into its raw ingredients with quantities
 * Useful for ingredient procurement and inventory
 * @param recipeId - The recipe ID to break down
 * @param quantity - Number of servings/plates to calculate for
 * @param recipeMap - Map of recipe ID to recipe object
 * @param ingredientMap - Map of ingredient ID to ingredient object
 * @returns Object mapping ingredient IDs to quantities needed
 */
export function getRecipeIngredientBreakdown(
  recipeId: string,
  quantity: number,
  recipeMap: Map<string, Recipe>,
  ingredientMap: Map<string, Ingredient>
): { [ingredientId: string]: number } {
  const recipe = recipeMap.get(recipeId);
  if (!recipe) return {};

  const breakdown: { [ingredientId: string]: number } = {};

  recipe.ingredients.forEach((component) => {
    // Case 1: Direct ingredient
    if (component.ingredientId) {
      const currentQty = breakdown[component.ingredientId] || 0;
      breakdown[component.ingredientId] = currentQty + (component.quantity * quantity);
    }
    // Case 2: Sub-recipe (recursive)
    else if (component.childRecipeId) {
      const subRecipe = recipeMap.get(component.childRecipeId);
      if (subRecipe) {
        // Calculate the ratio if the sub-recipe has a total weight
        const totalSubWeight = subRecipe.ingredients.reduce(
          (sum, i) => sum + i.quantity, 
          0
        );
        
        if (totalSubWeight > 0) {
          const amountUsed = component.quantity * quantity;
          
          // Break down sub-recipe ingredients proportionally
          subRecipe.ingredients.forEach((subComp) => {
            if (subComp.ingredientId) {
              const ratio = subComp.quantity / totalSubWeight;
              const finalQty = ratio * amountUsed;
              
              const currentQty = breakdown[subComp.ingredientId] || 0;
              breakdown[subComp.ingredientId] = currentQty + finalQty;
            }
          });
        }
      }
    }
  });

  return breakdown;
}