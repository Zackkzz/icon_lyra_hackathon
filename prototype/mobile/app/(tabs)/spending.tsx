import React, { useCallback, useState } from "react";
import { View, Text, ScrollView, RefreshControl, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useFocusEffect } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { api, SpendingResponse } from "../../services/api";
import { colors } from "../../lib/theme";
import { formatFriendly } from "../../lib/date";
import { Card, CenterState } from "../../components/ui";

const EMPTY: SpendingResponse = { totalSpent: 0, weeks: [] };

export default function SpendingScreen() {
  const [data, setData] = useState<SpendingResponse>(EMPTY);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      setData(await api.getSpending());
    } catch (e: any) {
      Alert.alert("Couldn't load spending", e?.message ?? "Try again.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load])
  );

  return (
    <SafeAreaView className="flex-1 bg-canvas" edges={["top"]}>
      <View className="px-5 pt-2 pb-3">
        <Text className="text-ink text-2xl font-extrabold">Spending</Text>
        <Text className="text-ink/50 text-xs">Grocery spend and meals prepped, by week</Text>
      </View>

      {loading ? (
        <CenterState>
          <Text className="text-ink/50">Loading…</Text>
        </CenterState>
      ) : (
        <ScrollView
          contentContainerClassName="px-5 pb-10 gap-4"
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
          {/* total */}
          <View className="bg-accent/15 border border-accent/30 rounded-2xl p-5 items-center">
            <Text className="text-ink/60 text-xs font-semibold uppercase tracking-wide">Total spent</Text>
            <Text className="text-accent text-4xl font-extrabold mt-1">${data.totalSpent.toFixed(2)}</Text>
          </View>

          {data.weeks.length === 0 ? (
            <CenterState>
              <Ionicons name="wallet-outline" size={44} color={colors.ink + "40"} />
              <Text className="text-ink/60 font-semibold mt-3">No spending yet</Text>
              <Text className="text-ink/40 text-center mt-1">Scan a grocery receipt in the Pantry tab to start tracking.</Text>
            </CenterState>
          ) : (
            data.weeks.map((w) => (
              <Card key={w.weekStart}>
                <View className="flex-row items-center justify-between mb-2">
                  <View>
                    <Text className="text-ink font-bold text-base">Week of {formatFriendly(w.weekStart)}</Text>
                    <Text className="text-ink/40 text-xs">
                      {w.purchaseCount} shop{w.purchaseCount === 1 ? "" : "s"}
                      {w.mealsPrepped.length > 0 ? ` · ${w.mealsPrepped.length} meal${w.mealsPrepped.length === 1 ? "" : "s"} prepped` : ""}
                    </Text>
                  </View>
                  <View className="items-end">
                    <Text className="text-accent font-extrabold text-lg">${w.spent.toFixed(2)}</Text>
                    <Text className="text-ink/40 text-[11px]">spent</Text>
                  </View>
                </View>

                {w.mealsPrepped.length > 0 ? (
                  <View className="border-t border-ink/10 pt-2 gap-1.5">
                    <View className="flex-row items-center gap-1.5 mb-0.5">
                      <Ionicons name="restaurant-outline" size={13} color={colors.ink + "90"} />
                      <Text className="text-ink/60 text-xs font-semibold">Meals prepped</Text>
                    </View>
                    {w.mealsPrepped.map((m, idx) => (
                      <View key={idx} className="flex-row items-center justify-between">
                        <Text className="text-ink/80 text-sm flex-1" numberOfLines={1}>
                          {m.recipeName}
                          <Text className="text-ink/40"> · {m.portions} portion{m.portions === 1 ? "" : "s"}</Text>
                        </Text>
                        <Text className="text-ink/50 text-xs">
                          {m.cost > 0 ? `$${m.cost.toFixed(2)}` : "—"}
                        </Text>
                      </View>
                    ))}
                  </View>
                ) : null}
              </Card>
            ))
          )}
        </ScrollView>
      )}
    </SafeAreaView>
  );
}
