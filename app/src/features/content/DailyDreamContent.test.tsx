import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react-native";
import { PropsWithChildren } from "react";
import { ApiClientProvider } from "@/api/apiContext";
import { DailyDreamQuote, DreamingFacts } from "@/features/content/DailyDreamContent";
import { mockApiClient } from "@/mocks/mockApi";
import { mockDailyDreamContent } from "@/mocks/mockData";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("DailyDreamContent", () => {
  it("shows the database-backed daily quote and category-specific waiting facts", async () => {
    render(<><DailyDreamQuote /><DreamingFacts /><DreamingFacts category="cognitive" label="Cognitive context" /></>, { wrapper: Wrapper });

    expect(await screen.findByTestId("daily-dream-quote")).toBeTruthy();
    expect(screen.getByText(mockDailyDreamContent.quote)).toBeTruthy();
    for (const fact of mockDailyDreamContent.facts) {
      expect(screen.getByText(fact)).toBeTruthy();
    }
    for (const fact of mockDailyDreamContent.cognitiveFacts) {
      expect(screen.getByText(fact)).toBeTruthy();
    }
  });
});

function Wrapper({ children }: PropsWithChildren) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <ThemeProvider><ApiClientProvider client={mockApiClient}><QueryClientProvider client={queryClient}>{children}</QueryClientProvider></ApiClientProvider></ThemeProvider>;
}
