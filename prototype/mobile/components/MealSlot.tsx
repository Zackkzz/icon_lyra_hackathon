import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';

interface Props {
  mealType: string;
  recipeName?: string;
  onPress?: () => void;
  onClear?: () => void;
}

export default function MealSlot({ mealType, recipeName, onPress, onClear }: Props) {
  return (
    <TouchableOpacity style={styles.slot} onPress={onPress} activeOpacity={0.7}>
      <Text style={styles.mealType}>{mealType}</Text>
      {recipeName ? (
        <View style={styles.filled}>
          <Text style={styles.recipeName} numberOfLines={1}>{recipeName}</Text>
          {onClear && (
            <TouchableOpacity onPress={onClear} style={styles.clearBtn}>
              <Text style={styles.clearText}>✕</Text>
            </TouchableOpacity>
          )}
        </View>
      ) : (
        <Text style={styles.empty}>Tap to add</Text>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  slot: {
    backgroundColor: '#FEF9F3',
    borderRadius: 10,
    padding: 10,
    marginBottom: 6,
    borderWidth: 1,
    borderColor: '#F0E0D0',
    minHeight: 52,
    justifyContent: 'center',
  },
  mealType: {
    fontSize: 11,
    fontWeight: '700',
    color: '#E67E22',
    marginBottom: 2,
  },
  filled: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  recipeName: {
    fontSize: 14,
    color: '#2C3E50',
    fontWeight: '500',
    flex: 1,
  },
  empty: {
    fontSize: 12,
    color: '#BDC3C7',
    fontStyle: 'italic',
  },
  clearBtn: {
    padding: 4,
  },
  clearText: {
    color: '#E74C3C',
    fontWeight: '600',
    fontSize: 14,
  },
});
