import React from "react";
import { View, Text, Pressable } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { RecipeSummary } from "../services/api";
import { MatchBar } from "./ui";
import { colors } from "../lib/theme";

export default function RecipeCard({
  recipe,
  onPress,
  showExpiring,
}: {
  recipe: RecipeSummary;
  onPress: () => void;
  showExpiring?: boolean;
}) {
  return (
    <Pressable onPress={onPress} className="bg-ink/5 border border-ink/10 rounded-2xl p-4 gap-3 active:opacity-70">
      <View className="flex-row items-start justify-between gap-3">
        <View className="flex-1">
          <View className="flex-row items-center gap-2">
            <Text className="text-ink font-bold text-base flex-shrink" numberOfLines={1}>
              {recipe.name}
            </Text>
            {recipe.isAiGenerated ? (
              <View className="flex-row items-center gap-1 bg-accent/15 px-1.5 py-0.5 rounded-full">
                <Ionicons name="sparkles" size={10} color={colors.accent} />
                <Text className="text-accent text-[10px] font-bold">AI</Text>
              </View>
            ) : null}
          </View>
          <Text className="text-ink/50 text-xs mt-1" numberOfLines={2}>
            {recipe.description}
          </Text>
        </View>
        <View className="items-center">
          <Text className="text-ink font-extrabold text-lg leading-5">{Math.round(recipe.matchPercentage)}%</Text>
          <Text className="text-ink/40 text-[10px]">have</Text>
        </View>
      </View>

      <MatchBar pct={recipe.matchPercentage} />

      <View className="flex-row items-center gap-3">
        <View className="flex-row items-center gap-1">
          <Ionicons name="time-outline" size={13} color={colors.ink + "80"} />
          <Text className="text-ink/60 text-xs">{recipe.prepTimeMinutes} min</Text>
        </View>
        <View className="flex-row items-center gap-1">
          <Ionicons name="people-outline" size={13} color={colors.ink + "80"} />
          <Text className="text-ink/60 text-xs">{recipe.servings}</Text>
        </View>
        <Text className="text-ink/60 text-xs">
          {recipe.matchCount}/{recipe.totalIngredients} ingredients
        </Text>
      </View>

      {showExpiring && recipe.expiringIngredients.length > 0 ? (
        <View className="flex-row items-center gap-1.5 flex-wrap">
          <Ionicons name="alert-circle" size={13} color={colors.warning} />
          <Text className="text-warning text-xs font-semibold">
            Uses {recipe.expiringIngredients.join(", ")}
          </Text>
        </View>
      ) : null}
    </Pressable>
  );
}
