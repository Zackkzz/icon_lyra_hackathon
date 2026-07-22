// Raw palette values, for props that need a colour string rather than a
// className (tab bar, vector icons, StatusBar, gradients). Keep in sync with
// tailwind.config.js — these are the ONLY colours used in the app.
export const colors = {
  canvas: "#121011",
  ink: "#e2e4e1",
  accent: "#00d985",
  success: "#3dba8b",
  warning: "#e4a55d",
  error: "#de586b",
  info: "#6690d2",
} as const;

// Common opacity-derived shades expressed as rgba of the base tokens, so the
// palette stays closed. ink at low alpha gives muted text / hairline borders.
export const inkAlpha = (a: number) => `rgba(226, 228, 225, ${a})`;
