import { render, screen } from "@testing-library/react-native";

import { ResultSectionRenderer } from "@/features/dreams/ResultSectionRenderer";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("ResultSectionRenderer", () => {
  it("handles text, symbols, emotions, and list sections", () => {
    render(
      <ThemeProvider>
        <>
          <ResultSectionRenderer section={{ kind: "text", title: "Guidance", content: "Write one detail." }} />
          <ResultSectionRenderer
            section={{
              kind: "symbols",
              title: "Symbols",
              content: [{ symbol: "water", meaning: "Emotional depth", personalRelevance: "Recent stress" }]
            }}
          />
          <ResultSectionRenderer
            section={{
              kind: "emotions",
              title: "Emotions",
              content: [{ name: "anxiety", intensity: 0.7, evidence: "Fast movement" }]
            }}
          />
          <ResultSectionRenderer section={{ kind: "list", title: "Themes", content: ["transition"] }} />
          <ResultSectionRenderer
            section={{ kind: "entities", title: "People", content: [{ title: "Alex", body: ["friend"] }] }}
          />
          <ResultSectionRenderer
            section={{ kind: "symbols", title: "Mapped symbols", content: [{ title: "stairs", body: ["Change", "Progress"] }] }}
          />
        </>
      </ThemeProvider>
    );

    expect(screen.getByText("Write one detail.")).toBeTruthy();
    expect(screen.getByText("water")).toBeTruthy();
    expect(screen.getByText("Emotional depth")).toBeTruthy();
    expect(screen.getByText("anxiety")).toBeTruthy();
    expect(screen.getByText("transition")).toBeTruthy();
    expect(screen.getByText("Alex")).toBeTruthy();
    expect(screen.getByText("stairs")).toBeTruthy();
    expect(screen.getByText("Change\nProgress")).toBeTruthy();
  });
});
