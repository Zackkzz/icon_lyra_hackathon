import React from "react";
import { View, Text, Pressable } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { RecipeSummary } from "../services/api";
import { Badge, MatchBar } from "./ui";
import { colors } from "../lib/theme";

export default function RecipeCard({
  recipe,
  onPress,
  onToggleBookmark,
}: {
  recipe: RecipeSummary;
  onPress: () => void;
  onToggleBookmark: (r: RecipeSummary) => void;
}) {
  return (
    <Pressable onPress={onPress} className="bg-surface border border-ink/5 rounded-2xl p-4 gap-3 active:opacity-70 shadow-sm">
      <View className="flex-row items-start justify-between gap-3">
        <View className="flex-1">
          <View className="flex-row items-center gap-2">
            <Text className="text-ink font-bold text-base flex-shrink" numberOfLines={1}>
              {recipe.name}
            </Text>
            {recipe.isAiGenerated ? (
              <Badge label="AI" tone="accent" icon="sparkles" />
            ) : null}
          </View>
          <Text className="text-ink/50 text-xs mt-1" numberOfLines={2}>
            {recipe.description}
          </Text>
        </View>
        <View className="items-end gap-1">
          {recipe.costPerServing != null ? (
            <>
              <Text className="text-accent font-extrabold text-lg leading-5">${recipe.costPerServing.toFixed(2)}</Text>
              <Text className="text-ink/40 text-[10px]">per serving</Text>
            </>
          ) : (
            <Text className="text-ink/30 text-xs">no price</Text>
          )}
        </View>
      </View>

      <MatchBar pct={recipe.matchPercentage} />

      <View className="flex-row items-center justify-between">
        <View className="flex-row items-center gap-2 flex-wrap flex-1">
          <Badge
            label={recipe.matchPercentage >= 100 ? "Ready" : `${Math.round(recipe.matchPercentage)}% match`}
            tone={recipe.matchPercentage >= 100 ? "success" : "neutral"}
            icon={recipe.matchPercentage >= 100 ? "checkmark-circle" : "layers-outline"}
          />
          <View className="flex-row items-center gap-1">
            <Ionicons name="time-outline" size={13} color={colors.ink + "80"} />
            <Text className="text-ink/60 text-xs">{recipe.prepTimeMinutes} min</Text>
          </View>
          <View className="flex-row items-center gap-1">
            <Ionicons name="basket-outline" size={13} color={colors.ink + "80"} />
            <Text className="text-ink/60 text-xs">
              {recipe.matchCount}/{recipe.totalIngredients}
            </Text>
          </View>
        </View>
        <Pressable onPress={() => onToggleBookmark(recipe)} hitSlop={10} className="p-1">
          <Ionicons
            name={recipe.isBookmarked ? "bookmark" : "bookmark-outline"}
            size={18}
            color={recipe.isBookmarked ? colors.accent : colors.ink + "80"}
          />
        </Pressable>
      </View>
    </Pressable>
  );
}
