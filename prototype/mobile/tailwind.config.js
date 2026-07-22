/** @type {import('tailwindcss').Config} */
// Approved Mealer visual direction: warm canvas, white surfaces, near-black
// type, green primary actions, and orange/red attention states.
module.exports = {
  content: [
    "./app/**/*.{js,jsx,ts,tsx}",
    "./components/**/*.{js,jsx,ts,tsx}",
  ],
  presets: [require("nativewind/preset")],
  theme: {
    extend: {
      colors: {
        canvas: "#F6F5F0",
        surface: "#FFFFFF",
        ink: "#1C2321",
        accent: "#2FA96B",
        success: "#1E7A4B",
        warning: "#F0A020",
        error: "#C63A2F",
        info: "#F97316",
      },
    },
  },
  plugins: [],
};
