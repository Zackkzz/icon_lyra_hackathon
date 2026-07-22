import React, { useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  TextInput, Alert, ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { api, ShoppingList } from '../services/api';

export default function ShoppingListScreen() {
  const [week, setWeek] = useState('2026-07-22');
  const [list, setList] = useState<ShoppingList | null>(null);
  const [loading, setLoading] = useState(false);
  const [purchasedIds, setPurchasedIds] = useState<Set<number>>(new Set());

  const generate = async () => {
    setLoading(true);
    try {
      const data = await api.generateShoppingList(week);
      setList(data);
      setPurchasedIds(new Set(data.items.filter(i => i.purchased).map(i => i.id)));
    } catch {
      Alert.alert('Error', 'Could not generate shopping list');
    } finally {
      setLoading(false);
    }
  };

  const togglePurchased = (id: number) => {
    setPurchasedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  return (
    <SafeAreaView style={styles.container}>
      <Text style={styles.heading}>🛒 Shopping List</Text>

      <View style={styles.inputRow}>
        <TextInput
          style={styles.input}
          placeholder="Week start (YYYY-MM-DD)"
          value={week}
          onChangeText={setWeek}
        />
        <TouchableOpacity style={styles.generateBtn} onPress={generate} disabled={loading}>
          {loading ? (
            <ActivityIndicator color="#FFF" size="small" />
          ) : (
            <Text style={styles.generateText}>Generate</Text>
          )}
        </TouchableOpacity>
      </View>

      {list && (
        <View style={styles.summary}>
          <Text style={styles.summaryText}>
            Week of {list.weekStartDate} · {list.items.length} items
          </Text>
        </View>
      )}

      {list && (
        <FlatList
          data={list.items}
          keyExtractor={(i) => i.id.toString()}
          contentContainerStyle={styles.list}
          renderItem={({ item }) => {
            const isChecked = purchasedIds.has(item.id);
            return (
              <TouchableOpacity
                style={[styles.item, isChecked && styles.itemDone]}
                onPress={() => togglePurchased(item.id)}
              >
                <View style={[styles.checkbox, isChecked && styles.checkboxDone]}>
                  {isChecked && <Text style={styles.checkmark}>✓</Text>}
                </View>
                <View style={styles.itemInfo}>
                  <Text style={[styles.itemName, isChecked && styles.itemNameDone]}>
                    {item.ingredientName}
                  </Text>
                  <Text style={styles.itemQty}>{item.quantity} {item.unit}</Text>
                </View>
              </TouchableOpacity>
            );
          }}
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF8F0' },
  heading: { fontSize: 26, fontWeight: '700', color: '#E67E22', padding: 20, paddingBottom: 10 },
  inputRow: { flexDirection: 'row', paddingHorizontal: 16, gap: 10, marginBottom: 10 },
  input: {
    flex: 1, backgroundColor: '#FFF', borderRadius: 10, padding: 12,
    borderWidth: 1, borderColor: '#F0E0D0', fontSize: 14,
  },
  generateBtn: {
    backgroundColor: '#27AE60', paddingHorizontal: 20, borderRadius: 10,
    alignItems: 'center', justifyContent: 'center',
  },
  generateText: { color: '#FFF', fontWeight: '600', fontSize: 14 },
  summary: { paddingHorizontal: 20, marginBottom: 8 },
  summaryText: { fontSize: 12, color: '#BDC3C7' },
  list: { paddingHorizontal: 16, paddingBottom: 20 },
  item: {
    flexDirection: 'row', alignItems: 'center',
    backgroundColor: '#FFF', borderRadius: 12, padding: 14, marginBottom: 8,
    shadowColor: '#000', shadowOpacity: 0.04, shadowRadius: 4, elevation: 1,
  },
  itemDone: { opacity: 0.5 },
  checkbox: {
    width: 24, height: 24, borderRadius: 12, borderWidth: 2,
    borderColor: '#BDC3C7', marginRight: 12,
    alignItems: 'center', justifyContent: 'center',
  },
  checkboxDone: { backgroundColor: '#27AE60', borderColor: '#27AE60' },
  checkmark: { color: '#FFF', fontWeight: '700', fontSize: 14 },
  itemInfo: { flex: 1 },
  itemName: { fontSize: 15, fontWeight: '500', color: '#2C3E50' },
  itemNameDone: { textDecorationLine: 'line-through', color: '#BDC3C7' },
  itemQty: { fontSize: 13, color: '#95A5A6', marginTop: 2 },
});
