import { Tabs } from 'expo-router';
import { SafeAreaProvider } from 'react-native-safe-area-context';

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <Tabs screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: '#E67E22',
        tabBarInactiveTintColor: '#BDC3C7',
        tabBarStyle: {
          backgroundColor: '#FFF',
          borderTopColor: '#F0E0D0',
          paddingTop: 4,
          paddingBottom: 4,
          height: 56,
        },
        tabBarLabelStyle: {
          fontSize: 11,
          fontWeight: '600',
        },
      }}>
        <Tabs.Screen
          name="fridge"
          options={{ title: 'Fridge', tabBarIcon: ({ color }: any) => null }}
        />
        <Tabs.Screen
          name="planner"
          options={{ title: 'Planner', tabBarIcon: ({ color }: any) => null }}
        />
        <Tabs.Screen
          name="recipes"
          options={{ title: 'Recipes', tabBarIcon: ({ color }: any) => null }}
        />
        <Tabs.Screen
          name="shopping"
          options={{ title: 'Shopping', tabBarIcon: ({ color }: any) => null }}
        />
        <Tabs.Screen
          name="chat"
          options={{ title: 'Chat', tabBarIcon: ({ color }: any) => null }}
        />
      </Tabs>
    </SafeAreaProvider>
  );
}
