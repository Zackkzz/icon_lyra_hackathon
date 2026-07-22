import React, { useCallback, useEffect, useRef, useState } from "react";
import {
  View,
  Text,
  ScrollView,
  Pressable,
  Animated,
  Alert,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useFocusEffect } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import {
  api,
  MealPlan,
  CookedMeal,
  MealType,
  MEAL_TYPES,
} from "../../services/api";
import { colors } from "../../lib/theme";
import {
  getMonday,
  weekDates,
  addDays,
  toISODate,
  shortDay,
  dayNum,
} from "../../lib/date";
import DraggableMeal from "../../components/DraggableMeal";
import { Badge, CenterState } from "../../components/ui";

type Rect = { x: number; y: number; width: number; height: number };

const CHIP_W = 150;
const MEAL_ICONS: Record<MealType, React.ComponentProps<typeof Ionicons>["name"]> = {
  Breakfast: "sunny-outline",
  Lunch: "cafe-outline",
  Dinner: "restaurant-outline",
  Snack: "nutrition-outline",
};

export default function PlanScreen() {
  const [weekStart, setWeekStart] = useState<Date>(() => getMonday());
  const [cookedMeals, setCookedMeals] = useState<CookedMeal[]>([]);
  const [plans, setPlans] = useState<MealPlan[]>([]);
  const [loading, setLoading] = useState(true);

  const [dragging, setDragging] = useState<CookedMeal | null>(null);
  const [hovered, setHovered] = useState<number | null>(null);
  const [selected, setSelected] = useState<CookedMeal | null>(null);
  const dragPos = useRef(new Animated.ValueXY({ x: 0, y: 0 })).current;

  const dayRefs = useRef<Array<View | null>>([]);
  const dayRects = useRef<Array<Rect | null>>([]);

  const days = weekDates(weekStart);
  const weekIso = toISODate(weekStart);
  const availableMeals = cookedMeals.filter((m) => m.portionsAvailable > 0);
  // What the meals planned this week cost in ingredients.
  const weekCost = plans.reduce((s, p) => s + (p.costPerPortion ?? 0), 0);

  const load = useCallback(async () => {
    try {
      const [p, m] = await Promise.all([api.getMealPlan(weekIso), api.getCookedMeals()]);
      setPlans(p);
      setCookedMeals(m);
    } catch (e: any) {
      Alert.alert("Couldn't load plan", e?.message ?? "Try again.");
    } finally {
      setLoading(false);
    }
  }, [weekIso]);

  useEffect(() => {
    setLoading(true);
    load();
  }, [load]);

  useFocusEffect(
    useCallback(() => {
      load();
    }, [load])
  );

  function plansForDay(date: Date): MealPlan[] {
    const iso = toISODate(date);
    return plans
      .filter((p) => p.date === iso)
      .sort((a, b) => MEAL_TYPES.indexOf(a.mealType) - MEAL_TYPES.indexOf(b.mealType));
  }

  function measureAll() {
    dayRects.current = [];
    dayRefs.current.forEach((ref, i) => {
      if (!ref) {
        dayRects.current[i] = null;
        return;
      }
      // @ts-ignore measureInWindow exists on host component refs
      ref.measureInWindow((x: number, y: number, width: number, height: number) => {
        dayRects.current[i] = { x, y, width, height };
      });
    });
  }

  function hitTest(x: number, y: number): number | null {
    for (let i = 0; i < dayRects.current.length; i++) {
      const r = dayRects.current[i];
      if (r && x >= r.x && x <= r.x + r.width && y >= r.y && y <= r.y + r.height) return i;
    }
    return null;
  }

  const onDragStart = (meal: CookedMeal, x: number, y: number) => {
    measureAll();
    dragPos.setValue({ x, y });
    setDragging(meal);
    setHovered(null);
  };
  const onDragMove = (x: number, y: number) => {
    dragPos.setValue({ x, y });
    setHovered(hitTest(x, y));
  };
  const onDragEnd = (x: number, y: number) => {
    const idx = hitTest(x, y);
    const meal = dragging;
    setDragging(null);
    setHovered(null);
    if (idx != null && meal) assign(days[idx], meal);
  };

  async function assign(date: Date, meal: CookedMeal) {
    if (meal.portionsAvailable <= 0) return;
    const iso = toISODate(date);
    const used = plans.filter((p) => p.date === iso).length;
    const mealType: MealType = MEAL_TYPES[Math.min(used, MEAL_TYPES.length - 1)];
    // optimistic: place the meal and consume a portion
    const tempId = -Date.now();
    setPlans((prev) => [
      ...prev,
      {
        id: tempId,
        date: iso,
        mealType,
        cookedMealId: meal.id,
        recipeId: meal.recipeId,
        recipeName: meal.recipeName,
        costPerPortion: meal.portions > 0 ? meal.cost / meal.portions : 0,
      },
    ]);
    setCookedMeals((prev) =>
      prev.map((m) => (m.id === meal.id ? { ...m, portionsAvailable: m.portionsAvailable - 1 } : m))
    );
    try {
      const created = await api.createMealPlan({ date: iso, mealType, cookedMealId: meal.id });
      setPlans((prev) => prev.map((p) => (p.id === tempId ? created : p)));
    } catch (e: any) {
      setPlans((prev) => prev.filter((p) => p.id !== tempId));
      setCookedMeals((prev) =>
        prev.map((m) => (m.id === meal.id ? { ...m, portionsAvailable: m.portionsAvailable + 1 } : m))
      );
      Alert.alert("Couldn't add to plan", e?.message ?? "Try again.");
    }
  }

  async function removePlan(plan: MealPlan) {
    setPlans((prev) => prev.filter((p) => p.id !== plan.id));
    // return the portion to its cooked meal
    if (plan.cookedMealId != null)
      setCookedMeals((prev) =>
        prev.map((m) => (m.id === plan.cookedMealId ? { ...m, portionsAvailable: m.portionsAvailable + 1 } : m))
      );
    try {
      if (plan.id > 0) await api.deleteMealPlan(plan.id);
    } catch {
      load();
    }
  }

  return (
    <SafeAreaView className="flex-1 bg-canvas" edges={["top"]}>
      {/* header + week nav */}
      <View className="px-5 pt-2 pb-2">
        <View className="flex-row items-end justify-between">
          <View>
            <Text className="text-ink text-2xl font-extrabold">Plan</Text>
            <Text className="text-ink/50 text-xs">
              {plans.length} meal{plans.length === 1 ? "" : "s"} planned this week
            </Text>
          </View>
          {weekCost > 0 ? (
            <Badge label={`$${weekCost.toFixed(2)} planned`} tone="accent" icon="wallet-outline" />
          ) : null}
        </View>
        <View className="flex-row items-center justify-between mt-2">
          <Pressable
            onPress={() => setWeekStart((w) => addDays(w, -7))}
            className="w-9 h-9 rounded-xl bg-surface border border-ink/5 items-center justify-center"
          >
            <Ionicons name="chevron-back" size={18} color={colors.ink} />
          </Pressable>
          <Pressable onPress={() => setWeekStart(getMonday())} className="items-center">
            <Text className="text-ink font-bold text-sm">
              {days[0].toLocaleDateString(undefined, { month: "short", day: "numeric" })} –{" "}
              {days[6].toLocaleDateString(undefined, { month: "short", day: "numeric" })}
            </Text>
            <Text className="text-ink/40 text-[11px]">Tap for this week</Text>
          </Pressable>
          <Pressable
            onPress={() => setWeekStart((w) => addDays(w, 7))}
            className="w-9 h-9 rounded-xl bg-surface border border-ink/5 items-center justify-center"
          >
            <Ionicons name="chevron-forward" size={18} color={colors.ink} />
          </Pressable>
        </View>
      </View>

      {loading ? (
        <CenterState>
          <Text className="text-ink/50">Loading…</Text>
        </CenterState>
      ) : (
        <>
          {/* calendar — 7 day drop zones filling available space */}
          <View className="flex-1 px-5 pt-1 pb-2">
            {days.map((date, i) => {
              const dayPlans = plansForDay(date);
              const highlight = (hovered === i && dragging) || !!selected;
              const isToday = toISODate(date) === toISODate(new Date());
              return (
                <Pressable
                  key={i}
                  ref={(r) => {
                    dayRefs.current[i] = r as unknown as View;
                  }}
                  onPress={() => {
                    if (selected) {
                      assign(date, selected);
                      setSelected(null);
                    }
                  }}
                  className={`flex-1 mb-1.5 rounded-2xl border px-3 py-2 flex-row items-center ${
                    highlight ? "border-accent bg-accent/10" : "border-ink/5 bg-surface"
                  }`}
                >
                  <View className="w-12 items-center">
                    <Text className={`text-xs font-bold ${isToday ? "text-accent" : "text-ink/60"}`}>
                      {shortDay(date)}
                    </Text>
                    <Text className={`text-lg font-extrabold ${isToday ? "text-accent" : "text-ink"}`}>
                      {dayNum(date)}
                    </Text>
                  </View>
                  <View className="flex-1 pl-1">
                    {dayPlans.length === 0 ? (
                      <Text className="text-ink/30 text-xs italic">
                        {selected ? `Tap to add ${selected.recipeName}` : "—"}
                      </Text>
                    ) : (
                      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerClassName="gap-1.5">
                        {dayPlans.map((p) => (
                          <Pressable
                            key={p.id}
                            onPress={() => removePlan(p)}
                            className="flex-row items-center gap-1 bg-accent/15 border border-accent/30 rounded-full pl-2.5 pr-1.5 py-1"
                          >
                            <Ionicons name={MEAL_ICONS[p.mealType]} size={11} color={colors.accent} />
                            <Text className="text-accent text-xs font-semibold max-w-[110px]" numberOfLines={1}>
                              {p.recipeName ?? "Meal"}
                            </Text>
                            <Ionicons name="close" size={12} color={colors.accent} />
                          </Pressable>
                        ))}
                      </ScrollView>
                    )}
                  </View>
                </Pressable>
              );
            })}
          </View>

          {/* meal tray */}
          <View className="border-t border-ink/10 pt-3 pb-2">
            <View className="px-5 flex-row items-center gap-1.5 mb-2">
              <Ionicons name="hand-left-outline" size={14} color={selected ? colors.accent : colors.ink + "80"} />
              <Text className={`text-xs font-semibold ${selected ? "text-accent" : "text-ink/60"}`}>
                {selected
                  ? `Tap a day to add "${selected.recipeName}"`
                  : "Cooked meals — tap one then a day, or drag it up · tap a planned meal to remove"}
              </Text>
            </View>
            {availableMeals.length === 0 ? (
              <Text className="px-5 text-ink/40 text-sm pb-2">
                No cooked meals yet — cook a recipe in the Recipes tab.
              </Text>
            ) : (
              <ScrollView
                horizontal
                showsHorizontalScrollIndicator={false}
                contentContainerClassName="px-5"
                scrollEnabled={!dragging}
              >
                {availableMeals.map((m) => (
                  <DraggableMeal
                    key={m.id}
                    meal={m}
                    selected={selected?.id === m.id}
                    onTap={(meal) => setSelected((prev) => (prev?.id === meal.id ? null : meal))}
                    onStart={onDragStart}
                    onMove={onDragMove}
                    onEnd={onDragEnd}
                  />
                ))}
              </ScrollView>
            )}
          </View>
        </>
      )}

      {/* floating drag overlay */}
      {dragging ? (
        <Animated.View
          pointerEvents="none"
          style={{
            position: "absolute",
            left: 0,
            top: 0,
            width: CHIP_W,
            transform: [
              { translateX: Animated.subtract(dragPos.x, CHIP_W / 2) },
              { translateY: Animated.subtract(dragPos.y, 24) },
            ],
            zIndex: 1000,
          }}
        >
          <View className="bg-accent rounded-2xl px-3 py-2.5 shadow-lg">
            <Text className="text-canvas font-bold text-sm" numberOfLines={1}>
              {dragging.recipeName}
            </Text>
          </View>
        </Animated.View>
      ) : null}
    </SafeAreaView>
  );
}
