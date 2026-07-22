import React from "react";
import { View, Text } from "react-native";
import { daysUntil } from "../lib/date";

// Colour-codes how close an item is to expiry, using only palette tokens.
export default function ExpiryBadge({ bestBeforeDate }: { bestBeforeDate: string }) {
  const days = daysUntil(bestBeforeDate);

  let label: string;
  let tone: "error" | "warning" | "success";

  if (days < 0) {
    label = "Expired";
    tone = "error";
  } else if (days === 0) {
    label = "Today";
    tone = "error";
  } else if (days <= 3) {
    label = `${days}d left`;
    tone = "warning";
  } else {
    label = `${days}d left`;
    tone = "success";
  }

  const toneBg = {
    error: "bg-error/15",
    warning: "bg-warning/15",
    success: "bg-success/15",
  }[tone];
  const toneText = {
    error: "text-error",
    warning: "text-warning",
    success: "text-success",
  }[tone];

  return (
    <View className={`px-2 py-0.5 rounded-full ${toneBg}`}>
      <Text className={`text-xs font-semibold ${toneText}`}>{label}</Text>
    </View>
  );
}
