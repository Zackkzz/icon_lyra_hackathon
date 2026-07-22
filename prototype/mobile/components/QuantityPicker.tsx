import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { Unit } from '../services/api';

interface Props {
  quantity: number;
  unit: Unit;
  onIncrement: () => void;
  onDecrement: () => void;
}

export default function QuantityPicker({ quantity, unit, onIncrement, onDecrement }: Props) {
  return (
    <View style={styles.row}>
      <TouchableOpacity style={styles.btn} onPress={onDecrement}>
        <Text style={styles.btnText}>−</Text>
      </TouchableOpacity>
      <Text style={styles.value}>{quantity} {unit}</Text>
      <TouchableOpacity style={styles.btn} onPress={onIncrement}>
        <Text style={styles.btnText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  btn: {
    backgroundColor: '#F0E0D0',
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  btnText: {
    fontSize: 18,
    color: '#E67E22',
    fontWeight: '700',
  },
  value: {
    fontSize: 15,
    fontWeight: '600',
    color: '#2C3E50',
    minWidth: 80,
    textAlign: 'center',
  },
});
