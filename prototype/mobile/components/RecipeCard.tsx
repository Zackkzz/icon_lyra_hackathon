import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { RecipeSummary } from '../services/api';

interface Props {
  recipe: RecipeSummary;
  onPress: () => void;
}

export default function RecipeCard({ recipe, onPress }: Props) {
  return (
    <TouchableOpacity style={styles.card} onPress={onPress} activeOpacity={0.7}>
      <View style={styles.content}>
        <Text style={styles.name} numberOfLines={1}>{recipe.name}</Text>
        <Text style={styles.desc} numberOfLines={2}>{recipe.description}</Text>
        <View style={styles.meta}>
          <Text style={styles.metaText}>{recipe.prepTimeMinutes} min</Text>
          <Text style={styles.metaText}>·</Text>
          <Text style={styles.metaText}>{recipe.servings} servings</Text>
        </View>
      </View>
      {recipe.matchPercentage !== undefined && recipe.matchPercentage > 0 && (
        <View style={styles.matchBadge}>
          <Text style={styles.matchText}>{Math.round(recipe.matchPercentage)}%</Text>
          <Text style={styles.matchLabel}>match</Text>
        </View>
      )}
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 14,
    padding: 14,
    marginBottom: 10,
    flexDirection: 'row',
    shadowColor: '#000',
    shadowOpacity: 0.06,
    shadowRadius: 8,
    elevation: 2,
  },
  content: {
    flex: 1,
    marginRight: 10,
  },
  name: {
    fontSize: 16,
    fontWeight: '600',
    color: '#2C3E50',
    marginBottom: 4,
  },
  desc: {
    fontSize: 12,
    color: '#95A5A6',
    marginBottom: 6,
    lineHeight: 16,
  },
  meta: {
    flexDirection: 'row',
    gap: 6,
  },
  metaText: {
    fontSize: 11,
    color: '#BDC3C7',
  },
  matchBadge: {
    backgroundColor: '#27AE60',
    borderRadius: 12,
    paddingHorizontal: 10,
    paddingVertical: 8,
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'center',
  },
  matchText: {
    color: '#FFF',
    fontWeight: '800',
    fontSize: 18,
  },
  matchLabel: {
    color: '#E8F8F0',
    fontSize: 10,
  },
});
