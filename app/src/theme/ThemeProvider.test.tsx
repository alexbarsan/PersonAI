import { render, screen } from "@testing-library/react-native";
import { Text } from "react-native";

import { ThemeProvider } from "@/theme/ThemeProvider";
import { activeBrand, astraBrand, dreamLensBrand } from "@/theme/brand";

describe("ThemeProvider", () => {
  it("renders children without platform-specific failures", () => {
    render(
      <ThemeProvider>
        <Text>Theme ready</Text>
      </ThemeProvider>
    );

    expect(screen.getByText("Theme ready")).toBeTruthy();
  });

  it("keeps DreamLens as the default brand", () => {
    expect(activeBrand).toBe(dreamLensBrand);
    expect(activeBrand.personaId).toBe("dream-interpreter");
  });

  it("defines an Astra brand variant without changing app code paths", () => {
    expect(astraBrand.appName).toBe("Astra");
    expect(astraBrand.personaId).toBe("astrologer");
  });
});
