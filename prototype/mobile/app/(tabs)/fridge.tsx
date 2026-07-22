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
import {
  api,
  FridgeItem,
  ConfirmReceiptItem,
} from "../../services/api";
import { useAuth } from "../../lib/auth";
import { colors } from "../../lib/theme";
import { toISODate, addDays } from "../../lib/date";
import ExpiryBadge from "../../components/ExpiryBadge";
import ReceiptReview from "../../components/ReceiptReview";
import { Button, CenterState } from "../../components/ui";

export default function FridgeScreen() {
  const { signOut } = useAuth();
  const [items, setItems] = useState<FridgeItem[]>([]);
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
      setItems(await api.getFridge());
    } catch (e: any) {
      Alert.alert("Couldn't load fridge", e?.message ?? "Try again.");
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
          bestBeforeDate: p.bestBeforeDate,
        }))
      );
      setReviewTitle("Review receipt");
      setReviewSubtitle(`${parsed.length} item${parsed.length === 1 ? "" : "s"} found — edit before adding`);
      setReviewVisible(true);
    } catch (e: any) {
      Alert.alert("Scan failed", e?.message ?? "Try again.");
    } finally {
      setScanning(false);
    }
  }

  function addManually() {
    setReviewItems([
      {
        ingredientId: null,
        name: "",
        category: "Other",
        quantity: 1,
        unit: "Pieces",
        bestBeforeDate: toISODate(addDays(new Date(), 7)),
      },
    ]);
    setReviewTitle("Add items");
    setReviewSubtitle("Add groceries to your fridge");
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

  async function deleteItem(item: FridgeItem) {
    setItems((prev) => prev.filter((i) => i.id !== item.id));
    try {
      await api.deleteFridgeItem(item.id);
    } catch {
      load();
    }
  }

  return (
    <SafeAreaView className="flex-1 bg-canvas" edges={["top"]}>
      {/* header */}
      <View className="px-5 pt-2 pb-3 flex-row items-center justify-between">
        <View>
          <Text className="text-ink text-2xl font-extrabold">Fridge</Text>
          <Text className="text-ink/50 text-xs">{items.length} item{items.length === 1 ? "" : "s"}</Text>
        </View>
        <Pressable onPress={signOut} hitSlop={8} className="w-10 h-10 rounded-full bg-ink/10 items-center justify-center">
          <Ionicons name="log-out-outline" size={18} color={colors.ink} />
        </Pressable>
      </View>

      {/* actions */}
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
              <Ionicons name="snow-outline" size={44} color={colors.ink + "40"} />
              <Text className="text-ink/60 font-semibold mt-3">Your fridge is empty</Text>
              <Text className="text-ink/40 text-center mt-1">Scan a receipt or add items to get started.</Text>
            </CenterState>
          }
          renderItem={({ item }) => (
            <View className="bg-ink/5 border border-ink/10 rounded-2xl p-4 flex-row items-center">
              <View className="flex-1 pr-3">
                <View className="flex-row items-center gap-2">
                  <Text className="text-ink font-bold text-base" numberOfLines={1}>
                    {item.ingredientName}
                  </Text>
                  {item.source === "Receipt" ? (
                    <Ionicons name="receipt-outline" size={13} color={colors.ink + "80"} />
                  ) : null}
                </View>
                <Text className="text-ink/50 text-xs mt-0.5">
                  {item.category} · {formatQty(item.quantity)} {item.unit}
                </Text>
              </View>
              <View className="items-end gap-2">
                <ExpiryBadge bestBeforeDate={item.bestBeforeDate} />
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
