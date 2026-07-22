import React from "react";
import { Text, View } from "react-native";
import { SpendingTrendPoint } from "../services/api";

const BAR_HEIGHT = 96;

export default function SpendingBarChart({ points }: { points: SpendingTrendPoint[] }) {
  const max = Math.max(1, ...points.map((point) => point.spent));
  const summary = points
    .map((point) => `${point.label}: $${point.spent.toFixed(2)}`)
    .join(", ");

  return (
    <View
      accessible
      accessibilityRole="image"
      accessibilityLabel={`Grocery spending chart. ${summary}`}
      className="mt-4"
    >
      <View className="relative">
        <View className="absolute left-0 right-0 top-5 border-t border-ink/5" />
        <View className="absolute left-0 right-0 top-[68px] border-t border-ink/5" />
        <View className="absolute left-0 right-0 top-[116px] border-t border-ink/5" />

        <View className="h-40 flex-row items-end gap-1.5">
          {points.map((point) => {
            const height = point.spent > 0
              ? Math.max(6, Math.round((point.spent / max) * BAR_HEIGHT))
              : 3;

            return (
              <View key={point.startDate} className="flex-1 h-full items-center justify-end min-w-0">
                <Text
                  className={`text-[9px] font-bold mb-1 ${point.isCurrent ? "text-accent" : "text-ink/50"}`}
                  numberOfLines={1}
                  adjustsFontSizeToFit
                >
                  {point.spent > 0 ? `$${formatAmount(point.spent)}` : ""}
                </Text>
                <View className="h-24 w-full items-center justify-end px-1">
                  <View
                    style={{ height }}
                    className={`w-full max-w-8 rounded-t-lg ${
                      point.isCurrent ? "bg-accent" : point.spent > 0 ? "bg-accent/30" : "bg-ink/10"
                    }`}
                  />
                </View>
                <Text
                  className={`text-[10px] mt-2 ${point.isCurrent ? "text-ink font-bold" : "text-ink/45"}`}
                  numberOfLines={1}
                  adjustsFontSizeToFit
                >
                  {point.label}
                </Text>
              </View>
            );
          })}
        </View>
      </View>
    </View>
  );
}

function formatAmount(value: number): string {
  if (value >= 1000) return `${(value / 1000).toFixed(1)}k`;
  if (value >= 100) return value.toFixed(0);
  return value.toFixed(value < 10 ? 1 : 0);
}
