/** @type {import('tailwindcss').Config} */
// The palette below is the single source of truth for colours in the app.
// It mirrors the requested @theme tokens exactly. Elevation / muted surfaces
// are expressed with opacity variants of these tokens (e.g. bg-ink/5,
// border-ink/10, text-ink/60) so no colour outside this set is ever used.
module.exports = {
  content: [
    "./app/**/*.{js,jsx,ts,tsx}",
    "./components/**/*.{js,jsx,ts,tsx}",
  ],
  presets: [require("nativewind/preset")],
  theme: {
    extend: {
      colors: {
        canvas: "#121011",
        ink: "#e2e4e1",
        accent: "#00d985",
        success: "#3dba8b",
        warning: "#e4a55d",
        error: "#de586b",
        info: "#6690d2",
      },
    },
  },
  plugins: [],
};
