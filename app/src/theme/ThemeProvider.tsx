import { PropsWithChildren, createContext, useContext } from "react";
import { View } from "react-native";

import { DreamLensBrand, dreamLensBrand } from "@/theme/brand";

const ThemeContext = createContext<DreamLensBrand>(dreamLensBrand);

export function ThemeProvider({ children }: PropsWithChildren) {
  return (
    <ThemeContext.Provider value={dreamLensBrand}>
      <View style={{ flex: 1, backgroundColor: dreamLensBrand.colors.background }}>{children}</View>
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  return useContext(ThemeContext);
}
