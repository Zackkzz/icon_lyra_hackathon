// Palette derived from the approved Mealer visual direction. Keep this in sync
// with tailwind.config.js so native props and utility classes render identically.
export const colors = {
  canvas: "#F6F5F0",
  surface: "#FFFFFF",
  ink: "#1C2321",
  accent: "#2FA96B",
  success: "#1E7A4B",
  warning: "#F0A020",
  error: "#C63A2F",
  info: "#F97316",
} as const;

// Common opacity-derived shades expressed as rgba of the base tokens, so the
// palette stays closed. ink at low alpha gives muted text / hairline borders.
export const inkAlpha = (a: number) => `rgba(28, 35, 33, ${a})`;
