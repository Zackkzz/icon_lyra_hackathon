import React, { useCallback, useEffect, useState } from "react";
import { View, Text, ScrollView, RefreshControl, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useFocusEffect } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { api, RecipeGroups, RecipeSummary } from "../../services/api";
import { colors } from "../../lib/theme";
import RecipeCard from "../../components/RecipeCard";
import RecipeDetail from "../../components/RecipeDetail";
import { Badge, Button, SectionHeader, CenterState } from "../../components/ui";

const EMPTY: RecipeGroups = { bookmarked: [], canCook: [], more: [] };

export default function RecipesScreen() {
  const [data, setData] = useState<RecipeGroups>(EMPTY);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [detailId, setDetailId] = useState<number | null>(null);

  const load = useCallback(async () => {
    try {
      setData(await api.getRecipeGroups());
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
      Alert.alert("Recipes ready", `Generated ${created.length} new recipe${created.length === 1 ? "" : "s"}.`);
    } catch (e: any) {
      Alert.alert("Generation failed", e?.message ?? "Try again.");
    } finally {
      setGenerating(false);
    }
  }

  async function toggleBookmark(r: RecipeSummary) {
    // optimistic move between sections happens on reload
    try {
      await api.toggleBookmark(r.id);
      load();
    } catch (e: any) {
      Alert.alert("Couldn't update bookmark", e?.message ?? "Try again.");
    }
  }

  const isEmpty = data.bookmarked.length === 0 && data.canCook.length === 0 && data.more.length === 0;

  return (
    <SafeAreaView className="flex-1 bg-canvas" edges={["top"]}>
      <View className="px-5 pt-2 pb-3">
        <Text className="text-ink text-2xl font-extrabold">Recipes</Text>
        <Text className="text-ink/50 text-xs">See what each meal costs per serving</Text>
      </View>

      <View className="px-5 mb-1">
        <Button
          label={generating ? "Generating…" : "Generate new recipes"}
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
              <Text className="text-ink/60 font-semibold mt-3">No recipes yet</Text>
              <Text className="text-ink/40 text-center mt-1">Generate recipes to see their cost per serving.</Text>
            </CenterState>
          ) : (
            <>
              {data.bookmarked.length > 0 && (
                <View className="gap-2.5">
                  <SectionHeader
                    title="Saved"
                    subtitle="Recipes you bookmarked"
                    accentColor={colors.accent}
                    right={<Badge label={`${data.bookmarked.length}`} icon="bookmark-outline" />}
                  />
                  {data.bookmarked.map((r) => (
                    <RecipeCard key={`bm-${r.id}`} recipe={r} onPress={() => setDetailId(r.id)} onToggleBookmark={toggleBookmark} />
                  ))}
                </View>
              )}

              {data.canCook.length > 0 && (
                <View className="gap-2.5">
                  <SectionHeader
                    title="Ready to cook"
                    subtitle="You have every ingredient · cheapest first"
                    accentColor={colors.success}
                    right={<Badge label={`${data.canCook.length}`} tone="success" icon="checkmark-circle" />}
                  />
                  {data.canCook.map((r) => (
                    <RecipeCard key={`cc-${r.id}`} recipe={r} onPress={() => setDetailId(r.id)} onToggleBookmark={toggleBookmark} />
                  ))}
                </View>
              )}

              {data.more.length > 0 && (
                <View className="gap-2.5">
                  <SectionHeader
                    title="More recipes"
                    subtitle="Sorted by what you have on hand"
                    accentColor={colors.info}
                    right={<Badge label={`${data.more.length}`} tone="info" icon="restaurant-outline" />}
                  />
                  {data.more.map((r) => (
                    <RecipeCard key={`mo-${r.id}`} recipe={r} onPress={() => setDetailId(r.id)} onToggleBookmark={toggleBookmark} />
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
        onChanged={load}
      />
    </SafeAreaView>
  );
}
