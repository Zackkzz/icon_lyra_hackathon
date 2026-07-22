import React, { useRef } from "react";
import { PanResponder, Pressable, View, Text, Platform } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { CookedMeal } from "../services/api";
import { colors } from "../lib/theme";
import { Badge } from "./ui";

// A cooked meal chip that can be dragged up into the calendar, OR tapped to select
// and then tapped onto a day. Horizontal swipes are left to the ScrollView.
export default function DraggableMeal({
  meal,
  selected,
  onTap,
  onStart,
  onMove,
  onEnd,
}: {
  meal: CookedMeal;
  selected?: boolean;
  onTap: (meal: CookedMeal) => void;
  onStart: (meal: CookedMeal, x: number, y: number) => void;
  onMove: (x: number, y: number) => void;
  onEnd: (x: number, y: number) => void;
}) {
  const responder = useRef(
    PanResponder.create({
      onStartShouldSetPanResponder: () => false,
      onMoveShouldSetPanResponder: (_, g) =>
        Math.abs(g.dy) > Math.abs(g.dx) && Math.abs(g.dy) > 6,
      onPanResponderGrant: (_, g) => onStart(meal, g.x0, g.y0),
      onPanResponderMove: (_, g) => onMove(g.moveX, g.moveY),
      onPanResponderRelease: (_, g) => onEnd(g.moveX, g.moveY),
      onPanResponderTerminate: (_, g) => onEnd(g.moveX, g.moveY),
    })
  ).current;

  // On web, the responder handlers swallow the inner Pressable's click, so drag
  // is enabled only on touch platforms; web relies on tap-to-place.
  const panProps = Platform.OS === "web" ? {} : responder.panHandlers;

  return (
    <View
      {...panProps}
      // @ts-ignore web-only styles to prevent text selection / show grab cursor
      style={{ userSelect: "none", cursor: "pointer" }}
      className="mr-2.5 select-none"
    >
      <Pressable
        onPress={() => onTap(meal)}
        className={`rounded-2xl px-3 py-2.5 w-44 border ${
          selected ? "bg-accent/15 border-accent" : "bg-surface border-ink/10"
        }`}
      >
        <View className="flex-row items-center justify-between mb-0.5">
          <Ionicons name={selected ? "checkmark-circle" : "reorder-three"} size={14} color={selected ? colors.accent : colors.ink + "80"} />
          <Badge label={`${meal.portionsAvailable} left`} tone="accent" icon="restaurant-outline" />
        </View>
        <Text className={`font-bold text-sm ${selected ? "text-accent" : "text-ink"}`} numberOfLines={2}>
          {meal.recipeName}
        </Text>
        {meal.portions > 0 && meal.cost > 0 ? (
          <View className="flex-row items-center gap-1 mt-1">
            <Ionicons name="cash-outline" size={11} color={colors.ink + "66"} />
            <Text className="text-ink/40 text-[11px]">
              ${(meal.cost / meal.portions).toFixed(2)}/portion
            </Text>
          </View>
        ) : null}
      </Pressable>
    </View>
  );
}
