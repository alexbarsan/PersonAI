export const dreamLensBrand = {
  appName: "DreamLens",
  personaId: "dream-interpreter",
  colors: {
    background: "#f8faf9",
    surface: "#ffffff",
    text: "#17211d",
    mutedText: "#5c6b63",
    primary: "#2f6f5e",
    primaryText: "#ffffff",
    border: "#d8e2dc",
    warning: "#8a5a14"
  },
  spacing: {
    xs: 4,
    sm: 8,
    md: 16,
    lg: 24,
    xl: 32
  },
  radius: {
    sm: 6,
    md: 8
  }
} as const;

export type DreamLensBrand = typeof dreamLensBrand;
