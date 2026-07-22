import React, { useEffect, useState } from "react";
import {
  Modal,
  View,
  Text,
  TextInput,
  Pressable,
  ScrollView,
  Platform,
} from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ConfirmReceiptItem, Unit } from "../services/api";
import { Button } from "./ui";
import UnitPicker from "./UnitPicker";
import { colors } from "../lib/theme";
import { addDays, toISODate, toDateOnly, addDaysIso, daysUntil, formatFriendly } from "../lib/date";

type Draft = ConfirmReceiptItem & { key: string };

let keySeq = 0;
const newKey = () => `d${keySeq++}`;

function toDraft(items: ConfirmReceiptItem[]): Draft[] {
  // Normalise any full ISO timestamp to a plain calendar date up front.
  return items.map((i) => ({ ...i, bestBeforeDate: toDateOnly(i.bestBeforeDate), key: newKey() }));
}

// Sensible +/- increment per unit so decimal metrics (kg, L) are easy to tune.
function stepFor(unit: Unit): number {
  if (unit === "Kilograms" || unit === "Litres" || unit === "Pounds") return 0.1;
  if (unit === "Grams" || unit === "Ml") return 10;
  return 1;
}

const roundQty = (n: number) => Math.round(n * 100) / 100;
const fmtQty = (n: number) => (Number.isInteger(n) ? String(n) : String(roundQty(n)));

export default function ReceiptReview({
  visible,
  title,
  subtitle,
  initialItems,
  loading,
  onCancel,
  onConfirm,
}: {
  visible: boolean;
  title: string;
  subtitle?: string;
  initialItems: ConfirmReceiptItem[];
  loading?: boolean;
  onCancel: () => void;
  onConfirm: (items: ConfirmReceiptItem[]) => void;
}) {
  const [drafts, setDrafts] = useState<Draft[]>([]);

  useEffect(() => {
    if (visible) setDrafts(toDraft(initialItems));
  }, [visible, initialItems]);

  function update(key: string, patch: Partial<Draft>) {
    setDrafts((d) => d.map((it) => (it.key === key ? { ...it, ...patch } : it)));
  }
  function remove(key: string) {
    setDrafts((d) => d.filter((it) => it.key !== key));
  }
  function addBlank() {
    setDrafts((d) => [
      ...d,
      {
        key: newKey(),
        ingredientId: null,
        name: "",
        category: "Other",
        quantity: 1,
        unit: "Pieces" as Unit,
        bestBeforeDate: toISODate(addDays(new Date(), 7)),
      },
    ]);
  }

  function confirm() {
    const cleaned = drafts
      .filter((d) => d.name.trim().length > 0)
      .map(({ key, ...rest }) => ({ ...rest, name: rest.name.trim() }));
    onConfirm(cleaned);
  }

  return (
    <Modal visible={visible} animationType="slide" transparent onRequestClose={onCancel}>
      <View className="flex-1 bg-canvas/95 justify-end">
        <View className="bg-canvas border-t border-ink/10 rounded-t-3xl max-h-[88%]" style={{ paddingBottom: Platform.OS === "ios" ? 24 : 12 }}>
          {/* header */}
          <View className="px-5 pt-4 pb-2 flex-row items-center justify-between">
            <View className="flex-1 pr-3">
              <Text className="text-ink text-lg font-extrabold">{title}</Text>
              {subtitle ? <Text className="text-ink/50 text-xs mt-0.5">{subtitle}</Text> : null}
            </View>
            <Pressable onPress={onCancel} hitSlop={10} className="w-9 h-9 rounded-full bg-ink/10 items-center justify-center">
              <Ionicons name="close" size={18} color={colors.ink} />
            </Pressable>
          </View>

          <ScrollView className="px-5" contentContainerClassName="pb-4 gap-3" keyboardShouldPersistTaps="handled">
            {drafts.length === 0 ? (
              <Text className="text-ink/50 text-center py-8">No items. Add one below.</Text>
            ) : (
              drafts.map((d) => {
                const days = daysUntil(d.bestBeforeDate);
                return (
                  <View key={d.key} className="bg-ink/5 border border-ink/10 rounded-2xl p-3 gap-2.5">
                    <View className="flex-row items-center gap-2">
                      <TextInput
                        value={d.name}
                        onChangeText={(t) => update(d.key, { name: t, ingredientId: null })}
                        placeholder="Item name"
                        placeholderTextColor={colors.ink + "66"}
                        className="flex-1 text-ink font-semibold text-base border-b border-ink/10 pb-1"
                      />
                      <Pressable onPress={() => remove(d.key)} hitSlop={8} className="w-8 h-8 rounded-full bg-error/15 items-center justify-center">
                        <Ionicons name="trash-outline" size={16} color={colors.error} />
                      </Pressable>
                    </View>

                    {/* quantity + unit */}
                    <View className="flex-row items-center gap-3">
                      <View className="flex-row items-center bg-ink/5 rounded-xl border border-ink/10">
                        <Pressable
                          onPress={() => update(d.key, { quantity: Math.max(0, roundQty(d.quantity - stepFor(d.unit))) })}
                          className="px-3 py-2"
                        >
                          <Ionicons name="remove" size={16} color={colors.ink} />
                        </Pressable>
                        <TextInput
                          value={fmtQty(d.quantity)}
                          onChangeText={(t) => update(d.key, { quantity: parseFloat(t) || 0 })}
                          keyboardType="decimal-pad"
                          className="text-ink font-bold w-14 text-center py-2"
                        />
                        <Pressable
                          onPress={() => update(d.key, { quantity: roundQty(d.quantity + stepFor(d.unit)) })}
                          className="px-3 py-2"
                        >
                          <Ionicons name="add" size={16} color={colors.ink} />
                        </Pressable>
                      </View>
                      <View className="flex-1">
                        <UnitPicker value={d.unit} onChange={(u) => update(d.key, { unit: u })} />
                      </View>
                    </View>

                    {/* best before stepper */}
                    <View className="flex-row items-center justify-between">
                      <Text className="text-ink/50 text-xs">Best before</Text>
                      <View className="flex-row items-center gap-2">
                        <Pressable
                          onPress={() => update(d.key, { bestBeforeDate: addDaysIso(d.bestBeforeDate, -1) })}
                          className="w-7 h-7 rounded-full bg-ink/10 items-center justify-center"
                        >
                          <Ionicons name="remove" size={14} color={colors.ink} />
                        </Pressable>
                        <Text className="text-ink font-semibold text-sm w-32 text-center">
                          {formatFriendly(d.bestBeforeDate)} · {days}d
                        </Text>
                        <Pressable
                          onPress={() => update(d.key, { bestBeforeDate: addDaysIso(d.bestBeforeDate, 1) })}
                          className="w-7 h-7 rounded-full bg-ink/10 items-center justify-center"
                        >
                          <Ionicons name="add" size={14} color={colors.ink} />
                        </Pressable>
                      </View>
                    </View>
                  </View>
                );
              })
            )}

            <Button variant="ghost" label="Add another item" icon={<Ionicons name="add" size={16} color={colors.ink} />} onPress={addBlank} />
          </ScrollView>

          <View className="px-5 pt-2 gap-2">
            <Button
              label={`Add ${drafts.filter((d) => d.name.trim()).length} to fridge`}
              onPress={confirm}
              loading={loading}
              disabled={drafts.filter((d) => d.name.trim()).length === 0}
            />
          </View>
        </View>
      </View>
    </Modal>
  );
}
