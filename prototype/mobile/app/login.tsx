import React, { useState } from "react";
import { View, Text, KeyboardAvoidingView, Platform, ScrollView } from "react-native";
import { Redirect, useRouter } from "expo-router";
import { Ionicons } from "@expo/vector-icons";
import { useAuth } from "../lib/auth";
import { Button, Input } from "../components/ui";
import { colors } from "../lib/theme";

export default function LoginScreen() {
  const { user, signIn, signUp } = useAuth();
  const router = useRouter();

  const [mode, setMode] = useState<"login" | "register">("login");
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Redirect href="/(tabs)/recipes" />;

  async function submit() {
    setError(null);
    if (!email.trim() || !password) {
      setError("Enter your email and password.");
      return;
    }
    setBusy(true);
    try {
      if (mode === "login") {
        await signIn(email, password);
      } else {
        await signUp(email, password, displayName || undefined);
      }
      router.replace("/(tabs)/recipes");
    } catch (e: any) {
      setError(e?.message ?? "Something went wrong.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <KeyboardAvoidingView
      behavior={Platform.OS === "ios" ? "padding" : undefined}
      className="flex-1 bg-canvas"
    >
      <ScrollView contentContainerClassName="flex-grow justify-center px-6 py-12" keyboardShouldPersistTaps="handled">
        <View className="items-center mb-8">
          <View className="w-16 h-16 rounded-3xl bg-accent items-center justify-center mb-4">
            <Ionicons name="snow" size={32} color={colors.canvas} />
          </View>
          <Text className="text-ink text-2xl font-extrabold">Fridge Planner</Text>
          <Text className="text-ink/50 mt-1">Cook smart. Waste less.</Text>
        </View>

        <View className="gap-4">
          {mode === "register" && (
            <Input
              label="Name"
              placeholder="Your name"
              autoCapitalize="words"
              value={displayName}
              onChangeText={setDisplayName}
            />
          )}
          <Input
            label="Email"
            placeholder="you@example.com"
            autoCapitalize="none"
            keyboardType="email-address"
            value={email}
            onChangeText={setEmail}
          />
          <Input
            label="Password"
            placeholder="••••••••"
            secureTextEntry
            value={password}
            onChangeText={setPassword}
          />

          {error ? (
            <View className="bg-error/15 border border-error/40 rounded-xl px-3 py-2">
              <Text className="text-error text-sm">{error}</Text>
            </View>
          ) : null}

          <Button
            label={mode === "login" ? "Sign in" : "Create account"}
            onPress={submit}
            loading={busy}
            className="mt-1"
          />

          <Button
            variant="ghost"
            label={mode === "login" ? "New here? Create an account" : "Have an account? Sign in"}
            onPress={() => {
              setError(null);
              setMode(mode === "login" ? "register" : "login");
            }}
          />
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
