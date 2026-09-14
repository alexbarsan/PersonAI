import { fireEvent, render, screen } from "@testing-library/react-native";

import { ResultSectionRenderer } from "@/features/dreams/ResultSectionRenderer";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("ResultSectionRenderer", () => {
  it("switches between people without losing their explanations", () => {
    render(<ThemeProvider><ResultSectionRenderer section={{ kind: "entities", title: "People", content: [
      { title: "Alex", body: "A familiar companion" }, { title: "Maria", body: "Someone offering direction" }
    ] }} /></ThemeProvider>);
    expect(screen.getByText("A familiar companion")).toBeTruthy();
    fireEvent.press(screen.getByRole("tab", { name: "Maria" }));
    expect(screen.getByText("Someone offering direction")).toBeTruthy();
    expect(screen.queryByText("A familiar companion")).toBeNull();
  });
  it("handles text, symbols, emotions, and list sections while hiding excluded and empty categories", () => {
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
          <ResultSectionRenderer section={{ kind: "list", title: "Scenarios", content: ["falling"] }} />
          <ResultSectionRenderer section={{ kind: "list", title: "Alternative interpretation", content: ["A memory"] }} />
          <ResultSectionRenderer section={{ kind: "entities", title: "Locations", content: [] }} />
          <ResultSectionRenderer section={{ kind: "list", title: "Objects", content: [] }} />
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
    expect(screen.queryByText("Themes")).toBeNull();
    expect(screen.queryByText("transition")).toBeNull();
    expect(screen.queryByText("Scenarios")).toBeNull();
    expect(screen.queryByText("falling")).toBeNull();
    expect(screen.queryByText("Alternative interpretation")).toBeNull();
    expect(screen.queryByText("A memory")).toBeNull();
    expect(screen.queryByText("Locations")).toBeNull();
    expect(screen.queryByText("Objects")).toBeNull();
    expect(screen.getByText("Alex")).toBeTruthy();
    expect(screen.getByText("stairs")).toBeTruthy();
    expect(screen.getByText("Change\nProgress")).toBeTruthy();
  });
});
