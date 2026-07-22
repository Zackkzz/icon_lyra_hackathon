import React, { useCallback, useEffect, useState } from "react";
import {
  View,
  Text,
  FlatList,
  Pressable,
  RefreshControl,
  Alert,
  Platform,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Ionicons } from "@expo/vector-icons";
import * as ImagePicker from "expo-image-picker";
import { api, PantryItem, ConfirmReceiptItem } from "../../services/api";
import { useAuth } from "../../lib/auth";
import { colors } from "../../lib/theme";
import ReceiptReview from "../../components/ReceiptReview";
import { Badge, Button, CenterState } from "../../components/ui";

export default function PantryScreen() {
  const { signOut } = useAuth();
  const [items, setItems] = useState<PantryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [scanning, setScanning] = useState(false);

  const [reviewVisible, setReviewVisible] = useState(false);
  const [reviewItems, setReviewItems] = useState<ConfirmReceiptItem[]>([]);
  const [reviewTitle, setReviewTitle] = useState("Review items");
  const [reviewSubtitle, setReviewSubtitle] = useState<string | undefined>();
  const [confirming, setConfirming] = useState(false);

  const load = useCallback(async () => {
    try {
      setItems(await api.getPantry());
    } catch (e: any) {
      Alert.alert("Couldn't load pantry", e?.message ?? "Try again.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function scanReceipt() {
    try {
      if (Platform.OS !== "web") {
        const perm = await ImagePicker.requestMediaLibraryPermissionsAsync();
        if (!perm.granted) {
          Alert.alert("Permission needed", "Allow photo access to scan a receipt.");
          return;
        }
      }
      const res = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ["images"],
        base64: true,
        quality: 0.6,
      });
      if (res.canceled || !res.assets?.[0]?.base64) return;

      setScanning(true);
      const { items: parsed } = await api.scanReceipt(res.assets[0].base64);
      if (parsed.length === 0) {
        Alert.alert("No items found", "Couldn't read any groceries from that photo. Try a clearer picture.");
        return;
      }
      setReviewItems(
        parsed.map((p) => ({
          ingredientId: p.ingredientId,
          name: p.suggestedName,
          category: p.category,
          quantity: p.quantity,
          unit: p.unit,
          price: p.price,
        }))
      );
      setReviewTitle("Review receipt");
      setReviewSubtitle(`${parsed.length} item${parsed.length === 1 ? "" : "s"} — check prices before adding`);
      setReviewVisible(true);
    } catch (e: any) {
      Alert.alert("Scan failed", e?.message ?? "Try again.");
    } finally {
      setScanning(false);
    }
  }

  function addManually() {
    setReviewItems([
      { ingredientId: null, name: "", category: "Other", quantity: 1, unit: "Pieces", price: 0 },
    ]);
    setReviewTitle("Add items");
    setReviewSubtitle("Add groceries and their prices");
    setReviewVisible(true);
  }

  async function confirmItems(confirmed: ConfirmReceiptItem[]) {
    try {
      setConfirming(true);
      const updated = await api.confirmReceipt(confirmed);
      setItems(updated);
      setReviewVisible(false);
    } catch (e: any) {
      Alert.alert("Couldn't add items", e?.message ?? "Try again.");
    } finally {
      setConfirming(false);
    }
  }

  async function deleteItem(item: PantryItem) {
    setItems((prev) => prev.filter((i) => i.id !== item.id));
    try {
      await api.deletePantryItem(item.id);
    } catch {
      load();
    }
  }

  const totalValue = items.reduce((s, i) => s + (i.lineValue ?? 0), 0);

  return (
    <SafeAreaView className="flex-1 bg-canvas" edges={["top"]}>
      <View className="px-5 pt-2 pb-3 flex-row items-center justify-between">
        <View>
          <Text className="text-ink text-2xl font-extrabold">Pantry</Text>
          <Text className="text-ink/50 text-xs">
            {items.length} item{items.length === 1 ? "" : "s"}
            {totalValue > 0 ? ` · ~$${totalValue.toFixed(2)} on hand` : ""}
          </Text>
        </View>
        <Pressable onPress={signOut} hitSlop={8} className="w-10 h-10 rounded-xl bg-surface border border-ink/5 items-center justify-center">
          <Ionicons name="log-out-outline" size={18} color={colors.ink} />
        </Pressable>
      </View>

      <View className="px-5 flex-row gap-3 mb-2">
        <View className="flex-1">
          <Button
            label={scanning ? "Scanning…" : "Scan receipt"}
            onPress={scanReceipt}
            loading={scanning}
            icon={<Ionicons name="scan" size={18} color={colors.canvas} />}
          />
        </View>
        <Button
          variant="subtle"
          label="Add"
          onPress={addManually}
          icon={<Ionicons name="add" size={18} color={colors.ink} />}
        />
      </View>

      {loading ? (
        <CenterState>
          <Text className="text-ink/50">Loading…</Text>
        </CenterState>
      ) : (
        <FlatList
          data={items}
          keyExtractor={(i) => String(i.id)}
          contentContainerClassName="px-5 pb-8 gap-2.5"
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
          ListEmptyComponent={
            <CenterState>
              <Ionicons name="basket-outline" size={44} color={colors.ink + "40"} />
              <Text className="text-ink/60 font-semibold mt-3">Your pantry is empty</Text>
              <Text className="text-ink/40 text-center mt-1">Scan a receipt to add items and capture their prices.</Text>
            </CenterState>
          }
          renderItem={({ item }) => (
            <View className="bg-surface border border-ink/5 rounded-2xl p-4 flex-row items-center shadow-sm">
              <View className="flex-1 pr-3">
                <Text className="text-ink font-bold text-base" numberOfLines={1}>
                  {item.ingredientName}
                </Text>
                <View className="flex-row items-center gap-1 mt-1">
                  <Ionicons name="cube-outline" size={12} color={colors.ink + "80"} />
                  <Text className="text-ink/50 text-xs">
                    {item.category} · {formatQty(item.quantity)} {item.unit}
                  {item.unitPrice != null ? ` · $${item.unitPrice.toFixed(item.unitPrice < 1 ? 3 : 2)}/${item.priceUnit}` : ""}
                  </Text>
                </View>
                <View className="mt-2">
                  <Badge
                    label={item.source === "Receipt" ? "Receipt" : "Manual"}
                    icon={item.source === "Receipt" ? "receipt-outline" : "create-outline"}
                  />
                </View>
              </View>
              <View className="items-end gap-2">
                {item.lineValue != null ? (
                  <Text className="text-accent font-bold text-sm">${item.lineValue.toFixed(2)}</Text>
                ) : (
                  <Text className="text-ink/30 text-xs">no price</Text>
                )}
                <Pressable onPress={() => deleteItem(item)} hitSlop={8}>
                  <Ionicons name="trash-outline" size={16} color={colors.error} />
                </Pressable>
              </View>
            </View>
          )}
        />
      )}

      <ReceiptReview
        visible={reviewVisible}
        title={reviewTitle}
        subtitle={reviewSubtitle}
        initialItems={reviewItems}
        loading={confirming}
        onCancel={() => setReviewVisible(false)}
        onConfirm={confirmItems}
      />
    </SafeAreaView>
  );
}

function formatQty(q: number): string {
  return Number.isInteger(q) ? String(q) : q.toFixed(2).replace(/\.?0+$/, "");
}
