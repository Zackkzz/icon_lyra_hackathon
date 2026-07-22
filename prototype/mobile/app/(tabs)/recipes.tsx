import React, { useCallback, useEffect, useState } from "react";
import { View, Text, ScrollView, RefreshControl, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useFocusEffect } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { api, FridgeSuggestions, RecipeSummary } from "../../services/api";
import { colors } from "../../lib/theme";
import RecipeCard from "../../components/RecipeCard";
import RecipeDetail from "../../components/RecipeDetail";
import { Button, SectionHeader, CenterState } from "../../components/ui";

const EMPTY: FridgeSuggestions = { cookFirst: [], canCook: [], almostCanCook: [] };

export default function RecipesScreen() {
  const [data, setData] = useState<FridgeSuggestions>(EMPTY);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [detailId, setDetailId] = useState<number | null>(null);

  const load = useCallback(async () => {
    try {
      setData(await api.getFridgeSuggestions());
    } catch (e: any) {
      Alert.alert("Couldn't load recipes", e?.message ?? "Try again.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  // Refresh when returning to the tab (fridge may have changed).
  useFocusEffect(
    useCallback(() => {
      load();
    }, [load])
  );

  async function generate() {
    try {
      setGenerating(true);
      const created = await api.generateRecipes(3);
      await load();
      Alert.alert("Recipes ready", `Generated ${created.length} recipe${created.length === 1 ? "" : "s"} from your soon-to-expire ingredients.`);
    } catch (e: any) {
      Alert.alert("Generation failed", e?.message ?? "Try again.");
    } finally {
      setGenerating(false);
    }
  }

  const openDetail = (r: RecipeSummary) => setDetailId(r.id);

  const isEmpty =
    data.cookFirst.length === 0 && data.canCook.length === 0 && data.almostCanCook.length === 0;

  return (
    <SafeAreaView className="flex-1 bg-canvas" edges={["top"]}>
      <View className="px-5 pt-2 pb-3">
        <Text className="text-ink text-2xl font-extrabold">Recipes</Text>
        <Text className="text-ink/50 text-xs">Built around what's in your fridge</Text>
      </View>

      <View className="px-5 mb-1">
        <Button
          label={generating ? "Generating…" : "Generate recipes to use expiring food"}
          onPress={generate}
          loading={generating}
          icon={<Ionicons name="sparkles" size={16} color={colors.canvas} />}
        />
      </View>

      {loading ? (
        <CenterState>
          <Text className="text-ink/50">Loading…</Text>
        </CenterState>
      ) : (
        <ScrollView
          contentContainerClassName="px-5 pb-10 pt-2 gap-5"
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => {
                setRefreshing(true);
                load();
              }}
              tintColor={colors.accent}
            />
          }
        >
          {isEmpty ? (
            <CenterState>
              <Ionicons name="restaurant-outline" size={44} color={colors.ink + "40"} />
              <Text className="text-ink/60 font-semibold mt-3">No matches yet</Text>
              <Text className="text-ink/40 text-center mt-1">
                Add items to your fridge, then generate recipes to make the most of them.
              </Text>
            </CenterState>
          ) : (
            <>
              {data.cookFirst.length > 0 && (
                <View className="gap-2.5">
                  <SectionHeader
                    title="Cook these first"
                    subtitle="Uses ingredients about to expire"
                    accentColor={colors.warning}
                  />
                  {data.cookFirst.map((r) => (
                    <RecipeCard key={`cf-${r.id}`} recipe={r} onPress={() => openDetail(r)} showExpiring />
                  ))}
                </View>
              )}

              {data.canCook.length > 0 && (
                <View className="gap-2.5">
                  <SectionHeader
                    title="Ready to cook"
                    subtitle="You have every ingredient"
                    accentColor={colors.success}
                  />
                  {data.canCook.map((r) => (
                    <RecipeCard key={`cc-${r.id}`} recipe={r} onPress={() => openDetail(r)} />
                  ))}
                </View>
              )}

              {data.almostCanCook.length > 0 && (
                <View className="gap-2.5">
                  <SectionHeader
                    title="Almost there"
                    subtitle="70%+ of ingredients on hand"
                    accentColor={colors.info}
                  />
                  {data.almostCanCook.map((r) => (
                    <RecipeCard key={`ac-${r.id}`} recipe={r} onPress={() => openDetail(r)} />
                  ))}
                </View>
              )}
            </>
          )}
        </ScrollView>
      )}

      <RecipeDetail
        recipeId={detailId}
        visible={detailId != null}
        onClose={() => setDetailId(null)}
        onCooked={load}
      />
    </SafeAreaView>
  );
}
