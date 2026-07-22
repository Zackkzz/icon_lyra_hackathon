import React from "react";
import { ScrollView, Pressable, Text } from "react-native";
import { Unit, UNITS } from "../services/api";

// Horizontal chip selector for measurement units (all supported types).
export default function UnitPicker({
  value,
  onChange,
}: {
  value: Unit;
  onChange: (u: Unit) => void;
}) {
  return (
    <ScrollView
      horizontal
      showsHorizontalScrollIndicator={false}
      contentContainerClassName="gap-2 py-1"
    >
      {UNITS.map((u) => {
        const active = u === value;
        return (
          <Pressable
            key={u}
            onPress={() => onChange(u)}
            className={`px-3 py-1.5 rounded-full border ${
              active ? "bg-accent border-accent" : "border-ink/20"
            }`}
          >
            <Text className={`text-sm font-semibold ${active ? "text-canvas" : "text-ink/70"}`}>
              {u}
            </Text>
          </Pressable>
        );
      })}
    </ScrollView>
  );
}
