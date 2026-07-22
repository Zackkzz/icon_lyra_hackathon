import { Tabs, Redirect } from "expo-router";
import { ActivityIndicator, View, Platform } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { useAuth } from "../../lib/auth";
import { colors, inkAlpha } from "../../lib/theme";

export default function TabsLayout() {
  const { user, loading } = useAuth();
  const tabBarHeight = Platform.select({ ios: 84, web: 76, default: 68 });
  const tabBarBottomPadding = Platform.select({ ios: 28, web: 12, default: 10 });

  if (loading) {
    return (
      <View className="flex-1 items-center justify-center bg-canvas">
        <ActivityIndicator color={colors.accent} size="large" />
      </View>
    );
  }
  if (!user) return <Redirect href="/login" />;

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.accent,
        tabBarInactiveTintColor: inkAlpha(0.45),
        tabBarAllowFontScaling: false,
        tabBarLabelPosition: "below-icon",
        tabBarStyle: {
          backgroundColor: colors.surface,
          borderTopColor: inkAlpha(0.1),
          borderTopWidth: 1,
          height: tabBarHeight,
          paddingTop: 8,
          paddingBottom: tabBarBottomPadding,
        },
        tabBarItemStyle: { minHeight: 52 },
        tabBarLabelStyle: {
          fontSize: 11,
          lineHeight: 14,
          fontWeight: "700",
          marginBottom: 0,
        },
        tabBarIconStyle: { marginTop: 1 },
      }}
    >
      <Tabs.Screen
        name="plan"
        options={{
          title: "Plan",
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="calendar" size={size ?? 22} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="recipes"
        options={{
          title: "Recipes",
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="restaurant" size={size ?? 22} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="pantry"
        options={{
          title: "Pantry",
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="basket" size={size ?? 22} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="spending"
        options={{
          title: "Budget",
          tabBarIcon: ({ color, size }) => (
            <Ionicons name="wallet" size={size ?? 22} color={color} />
          ),
        }}
      />
    </Tabs>
  );
}
