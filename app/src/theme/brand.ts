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
    warning: "#8a5a14"
  },
  spacing: dreamLensBrand.spacing,
  radius: dreamLensBrand.radius
} as const satisfies BrandConfig;

export const activeBrand = process.env.DREAMLENS_APP_VARIANT === "astra" ? astraBrand : dreamLensBrand;

export type DreamLensBrand = typeof activeBrand;
