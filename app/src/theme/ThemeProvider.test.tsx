import { render, screen } from "@testing-library/react-native";
import { Text } from "react-native";

import { ThemeProvider } from "@/theme/ThemeProvider";

describe("ThemeProvider", () => {
  it("renders children without platform-specific failures", () => {
    render(
      <ThemeProvider>
        <Text>Theme ready</Text>
      </ThemeProvider>
    );

    expect(screen.getByText("Theme ready")).toBeTruthy();
  });
});
