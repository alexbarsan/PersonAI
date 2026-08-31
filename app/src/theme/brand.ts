type BrandConfig = {
  appName: string;
  personaId: string;
  colors: {
    background: string;
    surface: string;
    text: string;
    mutedText: string;
    primary: string;
    primaryText: string;
    border: string;
    warning: string;
    lavender: string;
    sage: string;
    softInk: string;
  };
  spacing: {
    xs: number;
    sm: number;
    md: number;
    lg: number;
    xl: number;
  };
  radius: {
    sm: number;
    md: number;
  };
};

export const dreamLensBrand = {
  appName: "Dream DNA",
  personaId: "dream-interpreter",
  colors: {
    background: "#f1f1ee",
    surface: "#fffdf9",
    text: "#17213d",
    mutedText: "#657083",
    primary: "#17213d",
    primaryText: "#ffffff",
    border: "#d9dce3",
    warning: "#a6493d",
    lavender: "#eeeaf7",
    sage: "#dce9d8",
    softInk: "#e7edf6"
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
} as const satisfies BrandConfig;

export const astraBrand = {
  appName: "Astra",
  personaId: "astrologer",
  colors: {
    background: "#f7f5fb",
    surface: "#ffffff",
    text: "#1f1c2b",
    mutedText: "#625d72",
    primary: "#6f4bb2",
    primaryText: "#ffffff",
    border: "#ded8ea",
    warning: "#8a5a14",
    lavender: "#eeeaf7",
    sage: "#dce9d8",
    softInk: "#e7edf6"
  },
  spacing: dreamLensBrand.spacing,
  radius: dreamLensBrand.radius
} as const satisfies BrandConfig;

export const activeBrand = process.env.DREAMLENS_APP_VARIANT === "astra" ? astraBrand : dreamLensBrand;

export type DreamLensBrand = typeof activeBrand;
