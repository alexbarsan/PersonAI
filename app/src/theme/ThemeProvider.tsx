import { PropsWithChildren, createContext, useContext } from "react";
import { View } from "react-native";

import { DreamLensBrand, activeBrand } from "@/theme/brand";

const ThemeContext = createContext<DreamLensBrand>(activeBrand);

export function ThemeProvider({ children }: PropsWithChildren) {
  return (
    <ThemeContext.Provider value={activeBrand}>
      <View style={{ flex: 1, backgroundColor: activeBrand.colors.background }}>{children}</View>
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  return useContext(ThemeContext);
}
