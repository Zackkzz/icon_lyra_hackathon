import React from 'react';
import { View, Text, StyleSheet } from 'react-native';

interface Props {
  bestBeforeDate: string;
}

export default function ExpiryBadge({ bestBeforeDate }: Props) {
  const daysUntilExpiry = Math.ceil(
    (new Date(bestBeforeDate).getTime() - Date.now()) / (1000 * 60 * 60 * 24)
  );

  const color = daysUntilExpiry < 2 ? '#E74C3C' : daysUntilExpiry <= 5 ? '#F39C12' : '#27AE60';
  const label = daysUntilExpiry < 0
    ? 'Expired'
    : daysUntilExpiry === 0
    ? 'Today'
    : `${daysUntilExpiry}d`;

  return (
    <View style={[styles.badge, { backgroundColor: color }]}>
      <Text style={styles.text}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 12,
    alignSelf: 'flex-start',
  },
  text: {
    color: '#fff',
    fontSize: 11,
    fontWeight: '700',
  },
});
