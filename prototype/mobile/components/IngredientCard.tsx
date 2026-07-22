import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { FridgeItem } from '../services/api';
import ExpiryBadge from './ExpiryBadge';

interface Props {
  item: FridgeItem;
  onUse?: (id: number) => void;
}

export default function IngredientCard({ item, onUse }: Props) {
  return (
    <View style={styles.card}>
      <View style={styles.header}>
        <Text style={styles.name} numberOfLines={1}>{item.ingredientName}</Text>
        <ExpiryBadge bestBeforeDate={item.bestBeforeDate} />
      </View>
      <Text style={styles.category}>{item.category}</Text>
      <View style={styles.footer}>
        <Text style={styles.quantity}>
          {item.quantity} {item.unit}
        </Text>
        {onUse && (
          <TouchableOpacity style={styles.useBtn} onPress={() => onUse(item.id)}>
            <Text style={styles.useBtnText}>Use</Text>
          </TouchableOpacity>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 14,
    padding: 14,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOpacity: 0.06,
    shadowRadius: 8,
    elevation: 2,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  name: {
    fontSize: 16,
    fontWeight: '600',
    color: '#2C3E50',
    flex: 1,
    marginRight: 8,
  },
  category: {
    fontSize: 12,
    color: '#95A5A6',
    marginBottom: 8,
  },
  footer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  quantity: {
    fontSize: 14,
    fontWeight: '500',
    color: '#34495E',
  },
  useBtn: {
    backgroundColor: '#27AE60',
    paddingHorizontal: 16,
    paddingVertical: 6,
    borderRadius: 8,
  },
  useBtnText: {
    color: '#FFF',
    fontWeight: '600',
    fontSize: 12,
  },
});
