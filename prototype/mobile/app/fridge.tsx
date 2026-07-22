import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
  Modal,
  TextInput,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { api, FridgeItem, AddFridgeRequest, Unit } from '../services/api';
import IngredientCard from '../components/IngredientCard';

export default function FridgeScreen() {
  const [items, setItems] = useState<FridgeItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalVisible, setModalVisible] = useState(false);
  const [name, setName] = useState('');
  const [qty, setQty] = useState('1');
  const [unit, setUnit] = useState<Unit>('Pieces');
  const [expiry, setExpiry] = useState('');

  const loadFridge = async () => {
    try {
      setLoading(true);
      const data = await api.getFridge();
      setItems(data);
    } catch (e) {
      Alert.alert('Error', 'Could not load fridge');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadFridge(); }, []);

  const handleUse = async (id: number) => {
    try {
      await api.useFridgeItem(id, 1);
      loadFridge();
    } catch {
      Alert.alert('Error', 'Could not use item');
    }
  };

  const handleAdd = async () => {
    if (!name || !expiry) return Alert.alert('Error', 'Name and expiry required');
    try {
      const ingredientId = 1;
      await api.addToFridge({
        ingredientId,
        quantity: parseFloat(qty) || 1,
        unit,
        bestBeforeDate: expiry,
      });
      setModalVisible(false);
      setName('');
      setQty('1');
      setUnit('Pieces');
      setExpiry('');
      loadFridge();
    } catch {
      Alert.alert('Error', 'Could not add item');
    }
  };

  const sorted = [...items].sort(
    (a, b) => new Date(a.bestBeforeDate).getTime() - new Date(b.bestBeforeDate).getTime()
  );

  return (
    <SafeAreaView style={styles.container}>
      <Text style={styles.heading}>🥕 My Fridge</Text>
      {loading ? (
        <ActivityIndicator size="large" color="#E67E22" style={{ marginTop: 40 }} />
      ) : (
        <FlatList
          data={sorted}
          keyExtractor={(i) => i.id.toString()}
          renderItem={({ item }) => (
            <IngredientCard item={item} onUse={handleUse} />
          )}
          contentContainerStyle={styles.list}
        />
      )}

      <TouchableOpacity style={styles.fab} onPress={() => setModalVisible(true)}>
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>

      <Modal visible={modalVisible} animationType="slide" transparent>
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Add to Fridge</Text>
            <TextInput
              style={styles.input}
              placeholder="Ingredient name"
              value={name}
              onChangeText={setName}
            />
            <TextInput
              style={styles.input}
              placeholder="Quantity"
              value={qty}
              onChangeText={setQty}
              keyboardType="decimal-pad"
            />
            <View style={styles.unitRow}>
              {(['Grams', 'Ml', 'Pieces', 'Cups', 'Tbsp', 'Tsp'] as Unit[]).map((u) => (
                <TouchableOpacity
                  key={u}
                  style={[styles.unitBtn, unit === u && styles.unitBtnActive]}
                  onPress={() => setUnit(u)}
                >
                  <Text style={[styles.unitText, unit === u && styles.unitTextActive]}>{u}</Text>
                </TouchableOpacity>
              ))}
            </View>
            <TextInput
              style={styles.input}
              placeholder="Best before (YYYY-MM-DD)"
              value={expiry}
              onChangeText={setExpiry}
            />
            <View style={styles.modalActions}>
              <TouchableOpacity style={styles.cancelBtn} onPress={() => setModalVisible(false)}>
                <Text style={styles.cancelText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity style={styles.addBtn} onPress={handleAdd}>
                <Text style={styles.addBtnText}>Add</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF8F0' },
  heading: { fontSize: 26, fontWeight: '700', color: '#E67E22', padding: 20, paddingBottom: 10 },
  list: { paddingHorizontal: 16, paddingBottom: 80 },
  fab: {
    position: 'absolute', bottom: 24, right: 24,
    backgroundColor: '#E67E22', width: 56, height: 56,
    borderRadius: 28, alignItems: 'center', justifyContent: 'center',
    shadowColor: '#000', shadowOpacity: 0.2, shadowRadius: 8, elevation: 4,
  },
  fabText: { color: '#FFF', fontSize: 28, fontWeight: '300' },
  modalOverlay: {
    flex: 1, backgroundColor: 'rgba(0,0,0,0.4)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#FFF', borderTopLeftRadius: 20, borderTopRightRadius: 20,
    padding: 20, paddingTop: 28,
  },
  modalTitle: { fontSize: 20, fontWeight: '700', color: '#2C3E50', marginBottom: 16 },
  input: {
    backgroundColor: '#F8F9FA', borderRadius: 10, padding: 12,
    borderWidth: 1, borderColor: '#E9ECEF', fontSize: 14,
    marginBottom: 10,
  },
  unitRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 6, marginBottom: 10 },
  unitBtn: { paddingHorizontal: 12, paddingVertical: 6, borderRadius: 8, backgroundColor: '#F0E0D0' },
  unitBtnActive: { backgroundColor: '#E67E22' },
  unitText: { fontSize: 12, color: '#7F8C8D' },
  unitTextActive: { color: '#FFF', fontWeight: '600' },
  modalActions: { flexDirection: 'row', gap: 10, marginTop: 8 },
  cancelBtn: { flex: 1, padding: 14, borderRadius: 10, backgroundColor: '#F0E0D0', alignItems: 'center' },
  cancelText: { color: '#7F8C8D', fontWeight: '600' },
  addBtn: { flex: 1, padding: 14, borderRadius: 10, backgroundColor: '#E67E22', alignItems: 'center' },
  addBtnText: { color: '#FFF', fontWeight: '600' },
});
