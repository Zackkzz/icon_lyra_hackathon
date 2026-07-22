import React, { useMemo, useState } from "react";
import { Pressable, View, Text, Platform } from "react-native";
import { Gesture, GestureDetector } from "react-native-gesture-handler";
import { Ionicons } from "@expo/vector-icons";
import { CookedMeal } from "../services/api";
import { colors } from "../lib/theme";
import { Badge } from "./ui";

// How long you hold before the meal "lifts" and becomes draggable. Until then,
// touches fall through to the horizontal ScrollView so the tray scrolls freely.
const HOLD_MS = 220;

// A cooked meal chip. Tap to select, or hold to pick it up and drag it onto a
// day in the calendar. Because the drag only arms after a hold, a normal
// horizontal swipe scrolls the tray instead of fighting the drag.
export default function DraggableMeal({
  meal,
  selected,
  onTap,
  onStart,
  onMove,
  onEnd,
  onCancel,
}: {
  meal: CookedMeal;
  selected?: boolean;
  onTap: (meal: CookedMeal) => void;
  onStart: (meal: CookedMeal, x: number, y: number) => void;
  onMove: (x: number, y: number) => void;
  onEnd: (x: number, y: number) => void;
  onCancel: () => void;
}) {
  const [active, setActive] = useState(false);

  // Recreated when the callbacks change so the gesture always calls the latest
  // handlers (avoids capturing a stale `dragging` value from an old render).
  const pan = useMemo(
    () =>
      Gesture.Pan()
        // Run handlers on the JS thread so we can call setState / Animated
        // directly without reanimated worklets.
        .runOnJS(true)
        // Don't steal the touch until the finger has been held still: this is
        // what separates "scroll the tray" from "pick up a meal".
        .activateAfterLongPress(HOLD_MS)
        .onStart((e) => {
          setActive(true);
          onStart(meal, e.absoluteX, e.absoluteY);
        })
        .onUpdate((e) => onMove(e.absoluteX, e.absoluteY))
        .onEnd((e) => onEnd(e.absoluteX, e.absoluteY))
        // Always runs (normal end or cancel) — guarantees cleanup so the drag
        // overlay never gets stuck.
        .onFinalize(() => {
          setActive(false);
          onCancel();
        })
        // Web keeps the old tap-to-place behaviour; drag is touch-only.
        .enabled(Platform.OS !== "web"),
    [meal, onStart, onMove, onEnd, onCancel]
  );

  return (
    <GestureDetector gesture={pan}>
      <View
        // @ts-ignore web-only styles to prevent text selection / show grab cursor
        style={{ userSelect: "none", cursor: "pointer", opacity: active ? 0.35 : 1 }}
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
    </GestureDetector>
  );
}
