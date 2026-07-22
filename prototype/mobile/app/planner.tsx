import React, { useEffect, useState } from 'react';
import {
  View, Text, ScrollView, StyleSheet, TouchableOpacity,
  Modal, FlatList, Alert, ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { api, MealPlan, MealType, RecipeSummary } from '../services/api';
import MealSlot from '../components/MealSlot';
import RecipeCard from '../components/RecipeCard';

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const MEALS: MealType[] = ['Breakfast', 'Lunch', 'Dinner', 'Snack'];

function getMonday(d: Date): string {
  const date = new Date(d);
  const day = date.getDay();
  const diff = date.getDate() - day + (day === 0 ? -6 : 1);
  date.setDate(diff);
  return date.toISOString().split('T')[0];
}

export default function PlannerScreen() {
  const [weekStart, setWeekStart] = useState(getMonday(new Date()));
  const [plans, setPlans] = useState<MealPlan[]>([]);
  const [recipes, setRecipes] = useState<RecipeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [pickerVisible, setPickerVisible] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState<{ date: string; mealType: MealType } | null>(null);

  const load = async () => {
    try {
      setLoading(true);
      const [planData, recipeData] = await Promise.all([
        api.getMealPlan(weekStart),
        api.getRecipes(),
      ]);
      setPlans(planData);
      setRecipes(recipeData);
    } catch {
      Alert.alert('Error', 'Could not load meal plan');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [weekStart]);

  const handleSlotPress = (date: string, mealType: MealType) => {
    setSelectedSlot({ date, mealType });
    setPickerVisible(true);
  };

  const handlePickRecipe = async (recipeId: number) => {
    if (!selectedSlot) return;
    try {
      await api.createMealPlan({
        userId: 'default',
        date: selectedSlot.date,
        mealType: selectedSlot.mealType,
        recipeId,
      });
      setPickerVisible(false);
      load();
    } catch {
      Alert.alert('Error', 'Could not assign meal');
    }
  };

  const handleClearSlot = async (planId: number) => {
    try {
      await api.deleteMealPlan(planId);
      load();
    } catch {
      Alert.alert('Error', 'Could not clear slot');
    }
  };

  const getDateForDay = (dayOffset: number): string => {
    const d = new Date(weekStart + 'T00:00:00');
    d.setDate(d.getDate() + dayOffset);
    return d.toISOString().split('T')[0];
  };

  const getPlanFor = (date: string, mealType: MealType): MealPlan | undefined => {
    return plans.find(p => p.date === date && p.mealType === mealType);
  };

  return (
    <SafeAreaView style={styles.container}>
      <Text style={styles.heading}>📅 Meal Planner</Text>
      <Text style={styles.weekLabel}>Week of {weekStart}</Text>

      {loading ? (
        <ActivityIndicator size="large" color="#E67E22" style={{ marginTop: 40 }} />
      ) : (
        <ScrollView horizontal contentContainerStyle={styles.weekScroll}>
          {DAYS.map((day, i) => {
            const date = getDateForDay(i);
            const isToday = date === new Date().toISOString().split('T')[0];
            return (
              <View key={day} style={[styles.dayCol, isToday && styles.todayCol]}>
                <Text style={styles.dayName}>
                  {day} {date.slice(5)}
                </Text>
                {MEALS.map((mt) => {
                  const plan = getPlanFor(date, mt);
                  return (
                    <MealSlot
                      key={mt}
                      mealType={mt}
                      recipeName={plan?.recipeName || undefined}
                      onPress={() => handleSlotPress(date, mt)}
                      onClear={plan ? () => handleClearSlot(plan.id) : undefined}
                    />
                  );
                })}
              </View>
            );
          })}
        </ScrollView>
      )}

      <Modal visible={pickerVisible} animationType="slide" transparent>
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Pick a Recipe</Text>
            <FlatList
              data={recipes}
              keyExtractor={(r) => r.id.toString()}
              renderItem={({ item }) => (
                <RecipeCard recipe={item} onPress={() => handlePickRecipe(item.id)} />
              )}
              style={{ maxHeight: 400 }}
            />
            <TouchableOpacity style={styles.cancelBtn} onPress={() => setPickerVisible(false)}>
              <Text style={styles.cancelText}>Cancel</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#FFF8F0' },
  heading: { fontSize: 26, fontWeight: '700', color: '#E67E22', padding: 20, paddingBottom: 4 },
  weekLabel: { fontSize: 13, color: '#BDC3C7', paddingHorizontal: 20, marginBottom: 12 },
  weekScroll: { paddingHorizontal: 12 },
  dayCol: {
    width: 140, padding: 8, marginRight: 8,
    backgroundColor: '#FFF', borderRadius: 12,
    shadowColor: '#000', shadowOpacity: 0.04, shadowRadius: 6, elevation: 1,
  },
  todayCol: { borderWidth: 2, borderColor: '#E67E22' },
  dayName: { fontSize: 13, fontWeight: '700', color: '#2C3E50', marginBottom: 8, textAlign: 'center' },
  modalOverlay: { flex: 1, backgroundColor: 'rgba(0,0,0,0.4)', justifyContent: 'flex-end' },
  modalContent: {
    backgroundColor: '#FFF', borderTopLeftRadius: 20, borderTopRightRadius: 20,
    padding: 20, paddingTop: 28,
  },
  modalTitle: { fontSize: 20, fontWeight: '700', color: '#2C3E50', marginBottom: 12 },
  cancelBtn: {
    padding: 14, borderRadius: 10, backgroundColor: '#F0E0D0',
    alignItems: 'center', marginTop: 10,
  },
  cancelText: { color: '#7F8C8D', fontWeight: '600' },
});
