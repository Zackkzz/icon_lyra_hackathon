import React from "react";
import {
  View,
  Text,
  Pressable,
  TextInput,
  ActivityIndicator,
  TextInputProps,
  ViewProps,
} from "react-native";
import { colors } from "../lib/theme";

// ---- Button ----

type ButtonVariant = "primary" | "ghost" | "danger" | "subtle";

export function Button({
  label,
  onPress,
  variant = "primary",
  loading,
  disabled,
  className = "",
  icon,
}: {
  label: string;
  onPress?: () => void;
  variant?: ButtonVariant;
  loading?: boolean;
  disabled?: boolean;
  className?: string;
  icon?: React.ReactNode;
}) {
  const base = "flex-row items-center justify-center rounded-2xl px-4 py-3 gap-2";
  const styles: Record<ButtonVariant, string> = {
    primary: "bg-accent",
    danger: "bg-error/15 border border-error/40",
    ghost: "border border-ink/15",
    subtle: "bg-ink/10",
  };
  const textStyles: Record<ButtonVariant, string> = {
    primary: "text-canvas",
    danger: "text-error",
    ghost: "text-ink",
    subtle: "text-ink",
  };
  const isDisabled = disabled || loading;

  return (
    <Pressable
      onPress={onPress}
      disabled={isDisabled}
      className={`${base} ${styles[variant]} ${isDisabled ? "opacity-50" : ""} ${className}`}
    >
      {loading ? (
        <ActivityIndicator color={variant === "primary" ? colors.canvas : colors.accent} />
      ) : (
        <>
          {icon}
          <Text className={`font-bold ${textStyles[variant]}`}>{label}</Text>
        </>
      )}
    </Pressable>
  );
}

// ---- Card ----

export function Card({
  children,
  className = "",
  ...rest
}: ViewProps & { className?: string }) {
  return (
    <View className={`bg-ink/5 border border-ink/10 rounded-2xl p-4 ${className}`} {...rest}>
      {children}
    </View>
  );
}

// ---- Input ----

export function Input({
  label,
  className = "",
  ...rest
}: TextInputProps & { label?: string; className?: string }) {
  return (
    <View className="gap-1.5">
      {label ? <Text className="text-ink/60 text-xs font-semibold uppercase tracking-wide">{label}</Text> : null}
      <TextInput
        placeholderTextColor={colors.ink + "66"}
        className={`bg-ink/5 border border-ink/15 rounded-xl px-4 py-3 text-ink ${className}`}
        {...rest}
      />
    </View>
  );
}

// ---- Section header ----

export function SectionHeader({
  title,
  subtitle,
  accentColor = colors.ink,
  right,
}: {
  title: string;
  subtitle?: string;
  accentColor?: string;
  right?: React.ReactNode;
}) {
  return (
    <View className="flex-row items-center justify-between mb-2 mt-1">
      <View className="flex-row items-center gap-2 flex-1">
        <View style={{ width: 4, height: 18, borderRadius: 2, backgroundColor: accentColor }} />
        <View className="flex-1">
          <Text className="text-ink font-bold text-base">{title}</Text>
          {subtitle ? <Text className="text-ink/50 text-xs">{subtitle}</Text> : null}
        </View>
      </View>
      {right}
    </View>
  );
}

// ---- Match bar (recipe availability) ----

export function MatchBar({ pct }: { pct: number }) {
  const clamped = Math.max(0, Math.min(100, pct));
  const tone = clamped >= 100 ? colors.success : clamped >= 70 ? colors.info : colors.warning;
  return (
    <View className="h-1.5 bg-ink/10 rounded-full overflow-hidden">
      <View style={{ width: `${clamped}%`, backgroundColor: tone }} className="h-full rounded-full" />
    </View>
  );
}

// ---- Empty / loading states ----

export function CenterState({ children }: { children: React.ReactNode }) {
  return <View className="flex-1 items-center justify-center p-8">{children}</View>;
}
