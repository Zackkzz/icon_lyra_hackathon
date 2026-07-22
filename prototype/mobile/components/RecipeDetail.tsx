import React, { useEffect, useState } from "react";
import { Modal, View, Text, ScrollView, Pressable, ActivityIndicator, Platform, Alert } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { api, Recipe, RecipeIngredient } from "../services/api";
import { colors } from "../lib/theme";
import { Button } from "./ui";

function IngredientRow({ ing, scale }: { ing: RecipeIngredient; scale: number }) {
  return (
    <View className="flex-row items-center justify-between py-2 border-b border-ink/5">
      <View className="flex-row items-center gap-2 flex-1">
        <Ionicons
          name={ing.inFridge ? "checkmark-circle" : "cart-outline"}
          size={16}
          color={ing.inFridge ? colors.success : colors.warning}
        />
        <Text className="text-ink text-sm flex-1" numberOfLines={1}>
          {ing.ingredientName}
        </Text>
      </View>
      <Text className="text-ink/50 text-xs">
        {formatQty(ing.quantity * scale)} {ing.unit}
      </Text>
    </View>
  );
}

export default function RecipeDetail({
  recipeId,
  visible,
  onClose,
  onCooked,
}: {
  recipeId: number | null;
  visible: boolean;
  onClose: () => void;
  onCooked?: () => void;
}) {
  const [recipe, setRecipe] = useState<Recipe | null>(null);
  const [loading, setLoading] = useState(false);
  const [portions, setPortions] = useState(2);
  const [cooking, setCooking] = useState(false);

  useEffect(() => {
    if (!visible || recipeId == null) return;
    setRecipe(null);
    setLoading(true);
    api
      .getRecipe(recipeId)
      .then((r) => {
        setRecipe(r);
        setPortions(r.servings > 0 ? r.servings : 2);
      })
      .catch(() => setRecipe(null))
      .finally(() => setLoading(false));
  }, [visible, recipeId]);

  async function cook() {
    if (!recipe) return;
    try {
      setCooking(true);
      await api.cookRecipe(recipe.id, portions);
      onCooked?.();
      onClose();
      Alert.alert(
        "Cooked!",
        `${portions} portion${portions === 1 ? "" : "s"} of ${recipe.name} added. Plan them in the Plan tab.`
      );
    } catch (e: any) {
      Alert.alert("Couldn't cook", e?.message ?? "Try again.");
    } finally {
      setCooking(false);
    }
  }

  const have = recipe?.ingredients.filter((i) => i.inFridge) ?? [];
  const need = recipe?.ingredients.filter((i) => !i.inFridge) ?? [];
  // Recipe quantities are for its base yield; scale them to the portions being cooked.
  const scale = recipe ? portions / Math.max(1, recipe.servings) : 1;

  return (
    <Modal visible={visible} animationType="slide" transparent onRequestClose={onClose}>
      <View className="flex-1 bg-canvas/95 justify-end">
        <View className="bg-canvas border-t border-ink/10 rounded-t-3xl max-h-[90%]" style={{ paddingBottom: Platform.OS === "ios" ? 24 : 12 }}>
          <View className="px-5 pt-4 pb-2 flex-row items-start justify-between">
            <View className="flex-1 pr-3">
              <Text className="text-ink text-xl font-extrabold">{recipe?.name ?? "Recipe"}</Text>
              {recipe ? (
                <View className="flex-row items-center gap-3 mt-1">
                  <Text className="text-ink/50 text-xs">{recipe.prepTimeMinutes} min</Text>
                  <Text className="text-ink/50 text-xs">·</Text>
                  <Text className="text-ink/50 text-xs">recipe serves {recipe.servings}</Text>
                  {recipe.isAiGenerated ? (
                    <View className="flex-row items-center gap-1 bg-accent/15 px-1.5 py-0.5 rounded-full">
                      <Ionicons name="sparkles" size={10} color={colors.accent} />
                      <Text className="text-accent text-[10px] font-bold">AI</Text>
                    </View>
                  ) : null}
                </View>
              ) : null}
            </View>
            <Pressable onPress={onClose} hitSlop={10} className="w-9 h-9 rounded-full bg-ink/10 items-center justify-center">
              <Ionicons name="close" size={18} color={colors.ink} />
            </Pressable>
          </View>

          {loading || !recipe ? (
            <View className="py-16 items-center">
              <ActivityIndicator color={colors.accent} />
            </View>
          ) : (
            <ScrollView className="px-5" contentContainerClassName="pb-6 gap-5">
              {recipe.description ? (
                <Text className="text-ink/70 text-sm leading-5">{recipe.description}</Text>
              ) : null}

              <View className="bg-info/10 border border-info/25 rounded-xl px-3 py-2">
                <Text className="text-info text-xs">
                  Ingredient amounts below are for {portions} portion{portions === 1 ? "" : "s"} (this recipe serves {recipe.servings}). Cooking removes them from your fridge.
                </Text>
              </View>

              {/* In fridge */}
              <View>
                <View className="flex-row items-center gap-2 mb-1">
                  <Ionicons name="checkmark-circle" size={15} color={colors.success} />
                  <Text className="text-success font-bold text-sm">In your fridge ({have.length})</Text>
                </View>
                {have.length ? (
                  have.map((i) => <IngredientRow key={i.ingredientId} ing={i} scale={scale} />)
                ) : (
                  <Text className="text-ink/40 text-xs py-1">None yet.</Text>
                )}
              </View>

              {/* Need to buy */}
              <View>
                <View className="flex-row items-center gap-2 mb-1">
                  <Ionicons name="cart" size={15} color={colors.warning} />
                  <Text className="text-warning font-bold text-sm">Need to buy ({need.length})</Text>
                </View>
                {need.length ? (
                  need.map((i) => <IngredientRow key={i.ingredientId} ing={i} scale={scale} />)
                ) : (
                  <Text className="text-success/80 text-xs py-1">You have everything! 🎉</Text>
                )}
              </View>

              {/* Instructions */}
              {recipe.instructions ? (
                <View>
                  <Text className="text-ink font-bold text-sm mb-1">Instructions</Text>
                  <Text className="text-ink/70 text-sm leading-6">{recipe.instructions}</Text>
                </View>
              ) : null}
            </ScrollView>
          )}

          {/* cook footer */}
          {recipe ? (
            <View className="px-5 pt-3 border-t border-ink/10 gap-2.5">
              <View className="flex-row items-center justify-between">
                <Text className="text-ink/70 text-sm font-semibold">Portions to cook</Text>
                <View className="flex-row items-center bg-ink/5 rounded-xl border border-ink/10">
                  <Pressable onPress={() => setPortions((p) => Math.max(1, p - 1))} className="px-3 py-2">
                    <Ionicons name="remove" size={16} color={colors.ink} />
                  </Pressable>
                  <Text className="text-ink font-bold w-8 text-center">{portions}</Text>
                  <Pressable onPress={() => setPortions((p) => Math.min(50, p + 1))} className="px-3 py-2">
                    <Ionicons name="add" size={16} color={colors.ink} />
                  </Pressable>
                </View>
              </View>
              <Button
                label={`Cook ${portions} portion${portions === 1 ? "" : "s"}`}
                onPress={cook}
                loading={cooking}
                icon={<Ionicons name="flame" size={16} color={colors.canvas} />}
              />
            </View>
          ) : null}
        </View>
      </View>
    </Modal>
  );
}

function formatQty(q: number): string {
  return Number.isInteger(q) ? String(q) : q.toFixed(2).replace(/\.?0+$/, "");
}
