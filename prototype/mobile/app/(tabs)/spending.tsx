import React, { useCallback, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  Pressable,
  RefreshControl,
  ScrollView,
  Text,
  View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useFocusEffect } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import {
  api,
  SpendingPeriod,
  SpendingResponse,
  SpendingTrendResponse,
} from "../../services/api";
import { colors } from "../../lib/theme";
import { formatFriendly } from "../../lib/date";
import { Badge, BadgeTone, Card, CenterState } from "../../components/ui";
import SpendingBarChart from "../../components/SpendingBarChart";

const EMPTY: SpendingResponse = { totalSpent: 0, weeks: [] };
const PERIODS: SpendingPeriod[] = ["day", "week", "month"];

type BadgeIcon = React.ComponentProps<typeof Ionicons>["name"];

export default function SpendingScreen() {
  const [data, setData] = useState<SpendingResponse>(EMPTY);
  const [period, setPeriod] = useState<SpendingPeriod>("week");
  const [trend, setTrend] = useState<SpendingTrendResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [trendLoading, setTrendLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      setTrendLoading(true);
      const utcOffsetMinutes = -new Date().getTimezoneOffset();
      const [overview, nextTrend] = await Promise.all([
        api.getSpending(),
        api.getSpendingTrend(period, utcOffsetMinutes),
      ]);
      setData(overview);
      setTrend(nextTrend);
    } catch (e: any) {
      Alert.alert("Couldn't load budget", e?.message ?? "Try again.");
    } finally {
      setLoading(false);
      setTrendLoading(false);
      setRefreshing(false);
    }
  }, [period]);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load])
  );

  if (loading && !trend) {
    return (
      <SafeAreaView className="flex-1 bg-canvas" edges={["top"]}>
        <CenterState>
          <ActivityIndicator color={colors.accent} />
        </CenterState>
      </SafeAreaView>
    );
  }

  const comparison = trend ? comparisonBadge(trend) : null;
  const periodCount = period === "day" ? 7 : period === "week" ? 8 : 6;

  return (
    <SafeAreaView className="flex-1 bg-canvas" edges={["top"]}>
      <View className="px-5 pt-2 pb-3">
        <View className="flex-row items-center gap-2">
          <View className="w-8 h-8 rounded-xl bg-accent items-center justify-center">
            <Ionicons name="wallet-outline" size={17} color={colors.surface} />
          </View>
          <Text className="text-ink text-2xl font-extrabold">Budget</Text>
        </View>
        <Text className="text-ink/50 text-xs mt-1">See how your grocery spending changes over time</Text>
      </View>

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
        <View className="flex-row bg-surface border border-ink/5 rounded-2xl p-1">
          {PERIODS.map((option) => {
            const selected = option === period;
            return (
              <Pressable
                key={option}
                onPress={() => setPeriod(option)}
                accessibilityRole="tab"
                accessibilityState={{ selected }}
                className={`flex-1 items-center rounded-xl py-2.5 ${selected ? "bg-accent" : "bg-transparent"}`}
              >
                <Text className={`text-sm font-bold capitalize ${selected ? "text-surface" : "text-ink/45"}`}>
                  {option}
                </Text>
              </Pressable>
            );
          })}
        </View>

        {trend ? (
          <>
            <Card className="p-5">
              <View className="flex-row items-start justify-between gap-3">
                <View className="flex-1">
                  <Text className="text-ink/50 text-xs font-semibold uppercase tracking-wide">
                    {trend.currentPeriodLabel}
                  </Text>
                  <Text className="text-ink text-4xl font-extrabold mt-1">
                    ${trend.currentSpent.toFixed(2)}
                  </Text>
                  <Text className="text-ink/40 text-xs mt-1">
                    {formatRange(trend.currentPeriodStart, trend.currentPeriodEnd)}
                  </Text>
                </View>
                <View className="w-11 h-11 rounded-2xl bg-accent/10 items-center justify-center">
                  <Ionicons name="trending-up-outline" size={21} color={colors.accent} />
                </View>
              </View>
              <View className="flex-row flex-wrap gap-2 mt-4">
                {comparison ? (
                  <Badge label={comparison.label} tone={comparison.tone} icon={comparison.icon} />
                ) : null}
                <Badge
                  label={`${trend.currentPurchaseCount} shop${trend.currentPurchaseCount === 1 ? "" : "s"}`}
                  icon="receipt-outline"
                />
              </View>
            </Card>

            <Card className="p-5">
              <View className="flex-row items-start justify-between gap-3">
                <View>
                  <Text className="text-ink font-bold text-base">Spending trend</Text>
                  <Text className="text-ink/45 text-xs mt-0.5">
                    ${trend.visibleTotal.toFixed(2)} across the last {periodCount} {periodLabel(period)}
                  </Text>
                </View>
                {trendLoading ? <ActivityIndicator size="small" color={colors.accent} /> : null}
              </View>
              <SpendingBarChart points={trend.points} />
            </Card>
          </>
        ) : null}

        <View className="flex-row items-center justify-between mt-1">
          <View>
            <Text className="text-ink font-bold text-base">Recent weeks</Text>
            <Text className="text-ink/45 text-xs">Receipts and meals prepared</Text>
          </View>
          <Badge label={`$${data.totalSpent.toFixed(2)} tracked`} tone="accent" icon="wallet-outline" />
        </View>

        {data.weeks.length === 0 ? (
          <Card className="items-center py-8">
            <View className="w-12 h-12 rounded-2xl bg-info/10 items-center justify-center">
              <Ionicons name="receipt-outline" size={23} color={colors.info} />
            </View>
            <Text className="text-ink/70 font-semibold mt-3">No spending yet</Text>
            <Text className="text-ink/40 text-center mt-1 px-4">
              Scan a grocery receipt in Pantry to start building your budget history.
            </Text>
          </Card>
        ) : (
          data.weeks.map((week) => (
            <Card key={week.weekStart}>
              <View className="flex-row items-start justify-between gap-3 mb-3">
                <View className="flex-1">
                  <Text className="text-ink font-bold text-base">Week of {formatFriendly(week.weekStart)}</Text>
                  <View className="flex-row flex-wrap gap-1.5 mt-2">
                    <Badge
                      label={`${week.purchaseCount} shop${week.purchaseCount === 1 ? "" : "s"}`}
                      icon="receipt-outline"
                    />
                    {week.mealsPrepped.length > 0 ? (
                      <Badge
                        label={`${week.mealsPrepped.length} prepped`}
                        tone="success"
                        icon="restaurant-outline"
                      />
                    ) : null}
                  </View>
                </View>
                <View className="items-end">
                  <Text className="text-accent font-extrabold text-lg">${week.spent.toFixed(2)}</Text>
                  <Text className="text-ink/40 text-[11px]">spent</Text>
                </View>
              </View>

              {week.mealsPrepped.length > 0 ? (
                <View className="border-t border-ink/5 pt-3 gap-2">
                  {week.mealsPrepped.map((meal, index) => (
                    <View key={`${meal.recipeName}-${index}`} className="flex-row items-center gap-2">
                      <View className="w-7 h-7 rounded-lg bg-success/10 items-center justify-center">
                        <Ionicons name="restaurant-outline" size={13} color={colors.success} />
                      </View>
                      <Text className="text-ink/75 text-sm flex-1" numberOfLines={1}>
                        {meal.recipeName}
                        <Text className="text-ink/40"> · {meal.portions} portion{meal.portions === 1 ? "" : "s"}</Text>
                      </Text>
                      <Text className="text-ink/50 text-xs">
                        {meal.cost > 0 ? `$${meal.cost.toFixed(2)}` : "—"}
                      </Text>
                    </View>
                  ))}
                </View>
              ) : null}
            </Card>
          ))
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

function formatRange(start: string, end: string): string {
  if (start === end) return formatFriendly(start);
  return `${formatFriendly(start)} – ${formatFriendly(end)}`;
}

function periodLabel(period: SpendingPeriod): string {
  if (period === "day") return "days";
  if (period === "week") return "weeks";
  return "months";
}

function comparisonBadge(trend: SpendingTrendResponse): {
  label: string;
  tone: BadgeTone;
  icon: BadgeIcon;
} {
  if (trend.changePercentage == null) {
    return trend.currentSpent > 0
      ? { label: "First recorded spend", tone: "info", icon: "sparkles-outline" }
      : { label: "No previous spending", tone: "neutral", icon: "remove-outline" };
  }

  if (trend.changePercentage < 0) {
    return {
      label: `${Math.abs(trend.changePercentage).toFixed(0)}% less than previous`,
      tone: "success",
      icon: "trending-down-outline",
    };
  }

  if (trend.changePercentage > 0) {
    return {
      label: `${trend.changePercentage.toFixed(0)}% more than previous`,
      tone: "warning",
      icon: "trending-up-outline",
    };
  }

  return { label: "Same as previous", tone: "neutral", icon: "remove-outline" };
}
