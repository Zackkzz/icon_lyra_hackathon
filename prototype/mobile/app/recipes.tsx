import React, { useEffect, useState } from 'react';
import {
  View, Text, FlatList, StyleSheet, TouchableOpacity,
  Modal, ScrollView, Alert, ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { api, RecipeSummary, Recipe } from '../services/api';
import RecipeCard from '../components/RecipeCard';

export default function RecipesScreen() {
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<Recipe | null>(null);
  const [detailVisible, setDetailVisible] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const data = await api.getRecipes();
        setRecipes(data);
      } catch {
        Alert.alert('Error', 'Could not load recipes');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const handlePress = async (id: number) => {
    try {
      const recipe = await api.getRecipe(id);
      setSelected(recipe);
      setDetailVisible(true);
    } catch {
      Alert.alert('Error', 'Could not load recipe details');
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <Text style={styles.heading}>📖 Recipes</Text>

      {loading ? (
        <ActivityIndicator size="large" color="#E67E22" style={{ marginTop: 40 }} />
      ) : (
        <FlatList
          data={recipes}
          keyExtractor={(r) => r.id.toString()}
          renderItem={({ item }) => (
            <RecipeCard recipe={item} onPress={() => handlePress(item.id)} />
          )}
          contentContainerStyle={styles.list}
        />
      )}

      <Modal visible={detailVisible} animationType="slide" transparent>
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            {selected && (
              <ScrollView>
                <Text style={styles.recipeName}>{selected.name}</Text>
                <Text style={styles.recipeMeta}>
                  🕐 {selected.prepTimeMinutes} min  ·  🍽 {selected.servings} servings
                </Text>
                <Text style={styles.recipeDesc}>{selected.description}</Text>

                <Text style={styles.sectionTitle}>Ingredients</Text>
                {selected.ingredients.map((ri, i) => (
                  <Text key={i} style={styles.ingredient}>
                    · {ri.quantity} {ri.unit} {ri.ingredientName}
                  </Text>
                ))}

                <Text style={styles.sectionTitle}>Instructions</Text>
                <Text style={styles.instructions}>{selected.instructions}</Text>
              </ScrollView>
            )}
            <TouchableOpacity
              style={styles.closeBtn}
              onPress={() => setDetailVisible(false)}
            >
              <Text style={styles.closeText}>Close</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF8F0' },
  heading: { fontSize: 26, fontWeight: '700', color: '#E67E22', padding: 20, paddingBottom: 10 },
  list: { paddingHorizontal: 16, paddingBottom: 20 },
  modalOverlay: { flex: 1, backgroundColor: 'rgba(0,0,0,0.4)', justifyContent: 'flex-end' },
  modalContent: {
    backgroundColor: '#FFF', borderTopLeftRadius: 20, borderTopRightRadius: 20,
    padding: 20, paddingTop: 28, maxHeight: '80%',
  },
  recipeName: { fontSize: 22, fontWeight: '700', color: '#2C3E50', marginBottom: 6 },
  recipeMeta: { fontSize: 13, color: '#95A5A6', marginBottom: 10 },
  recipeDesc: { fontSize: 14, color: '#34495E', lineHeight: 20, marginBottom: 14 },
  sectionTitle: { fontSize: 16, fontWeight: '700', color: '#E67E22', marginBottom: 6, marginTop: 6 },
  ingredient: { fontSize: 14, color: '#2C3E50', lineHeight: 22 },
  instructions: { fontSize: 14, color: '#34495E', lineHeight: 22, marginBottom: 12 },
  closeBtn: {
    padding: 14, borderRadius: 10, backgroundColor: '#F0E0D0',
    alignItems: 'center', marginTop: 10,
  },
  closeText: { color: '#7F8C8D', fontWeight: '600' },
});
