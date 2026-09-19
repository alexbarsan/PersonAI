import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen } from "@testing-library/react-native";
import { PropsWithChildren } from "react";

import { ApiClientProvider } from "@/api/apiContext";
import { InsightsResponse } from "@/api/dto";
import { InsightsScreen } from "@/features/insights/InsightsScreen";
import { mockApiClient } from "@/mocks/mockApi";
import { mockInsights } from "@/mocks/mockData";
import { ThemeProvider } from "@/theme/ThemeProvider";

describe("InsightsScreen", () => {
  it("renders themes and streaks", async () => {
    renderWithProviders(<InsightsScreen />);

    expect(await screen.findByText("1 day")).toBeTruthy();
    expect(screen.getAllByText("Recurring symbols").length).toBeGreaterThan(0);
    expect(screen.getByText(mockInsights.factGroups[0].facts[0].value)).toBeTruthy();
    expect(screen.getByLabelText("Recurring symbols distribution")).toBeTruthy();
  });

  it("renders an empty state", async () => {
    renderWithProviders(<InsightsScreen />, {
      getInsights: async (): Promise<InsightsResponse> => ({
        totalDreams: 0,
        currentStreakDays: 0,
        recurringThemes: [],
        dateRange: null,
        factGroups: [],
        timingPatterns: [],
        relationships: [],
        relationshipReadiness: {
          minimumCompletedDreams: 6,
          completedDreams: 0,
          qualifiedFactPatterns: 0,
          supportedRelationships: 0
        },
        monthlyDreamCounts: [],
        journalSynthesis: {
          status: "not_ready",
          minimumCompletedDreams: 6,
          completedDreams: 0,
          sourceDreamCount: null,
          generatedAt: null,
          summary: null,
          observations: [],
          reflectionQuestions: []
        }
      })
    });

    expect(await screen.findByText("No insights yet")).toBeTruthy();
    expect(screen.getByText("Interpret dreams to reveal recurring patterns.")).toBeTruthy();
  });

  it("renders the evidence-linked whole-journal reflection", async () => {
    renderWithProviders(<InsightsScreen />);

    expect(await screen.findByText("Journal reflection")).toBeTruthy();
    expect(screen.getByText("Curiosity near changing water")).toBeTruthy();
    expect(screen.getByLabelText("Open evidence dream The Quiet Shoreline")).toBeTruthy();
    expect(screen.getByText("For reflection, not diagnosis.")).toBeTruthy();
  });

  it("reveals the owner-scoped evidence behind a map observation", async () => {
    renderWithProviders(<InsightsScreen />);

    const waterTargets = await screen.findAllByLabelText("Explore water, appearing in 1 dream");
    fireEvent.press(waterTargets[waterTargets.length - 1]);

    expect(await screen.findByLabelText("water pattern details")).toBeTruthy();
    expect(await screen.findByText("Your DreamDNA interpretation")).toBeTruthy();
    expect(screen.getByText(/Water appears with curiosity in this journal/)).toBeTruthy();
    expect(screen.getByText("Research lenses")).toBeTruthy();
    expect(screen.getByLabelText(/Open source Continuity between waking activities/)).toBeTruthy();
    fireEvent.press(screen.getByText("Dreams"));
    expect(screen.getAllByText("The Quiet Shoreline").length).toBeGreaterThan(0);
    expect(screen.queryByText(/Sources:/i)).toBeNull();
    expect(screen.queryByText(/schema\s+\d/i)).toBeNull();
  });

  it("shows monthly history for a selected pattern", async () => {
    renderWithProviders(<InsightsScreen />);

    const waterTargets = await screen.findAllByLabelText("Explore water, appearing in 1 dream");
    fireEvent.press(waterTargets[waterTargets.length - 1]);
    fireEvent.press(await screen.findByText("Trends"));

    expect(screen.getByLabelText("Monthly pattern occurrences")).toBeTruthy();
    expect(screen.getByText("More monthly history is needed before a direction can be estimated.")).toBeTruthy();
  });

  it("opens the same pattern detail from a donut slice", async () => {
    renderWithProviders(<InsightsScreen />);

    const waterTargets = await screen.findAllByLabelText("Explore water, appearing in 1 dream");
    fireEvent.press(waterTargets[0]);

    expect(await screen.findByLabelText("water pattern details")).toBeTruthy();
    expect(screen.getByText("Overview")).toBeTruthy();
    expect(screen.getByText("Related")).toBeTruthy();
    expect(screen.getByText("Trends")).toBeTruthy();
  });

  it("renders relationship observations with linked owner evidence", async () => {
    renderWithProviders(<InsightsScreen />);

    expect(await screen.findByText("Patterns that appear together")).toBeTruthy();
    expect(screen.getByText("water + curiosity")).toBeTruthy();
    expect(screen.getByLabelText("Open The Quiet Shoreline")).toBeTruthy();
  });

  it("explains when the journal needs more evidence for connection patterns", async () => {
    renderWithProviders(<InsightsScreen />, {
      getInsights: async (): Promise<InsightsResponse> => ({
        ...mockInsights,
        totalDreams: 2,
        relationships: [],
        relationshipReadiness: {
          minimumCompletedDreams: 6,
          completedDreams: 2,
          qualifiedFactPatterns: 0,
          supportedRelationships: 0
        }
      })
    });

    expect(await screen.findByText("Connection patterns can be checked after 6 completed dreams. You have 2.")).toBeTruthy();
  });

  it("explains when no relationship passes the evidence threshold", async () => {
    renderWithProviders(<InsightsScreen />, {
      getInsights: async (): Promise<InsightsResponse> => ({
        ...mockInsights,
        relationships: [],
        relationshipReadiness: {
          minimumCompletedDreams: 6,
          completedDreams: 7,
          qualifiedFactPatterns: 3,
          supportedRelationships: 0
        }
      })
    });

    expect(await screen.findByText("No connection patterns meet the current evidence threshold yet.")).toBeTruthy();
  });
});

function renderWithProviders(
  ui: React.ReactElement,
  apiOverrides: Partial<typeof mockApiClient> = {}
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false }
    }
  });
  const client = {
    ...mockApiClient,
    ...apiOverrides
  };

  function Wrapper({ children }: PropsWithChildren) {
    return (
      <ThemeProvider>
        <ApiClientProvider client={client}>
          <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
        </ApiClientProvider>
      </ThemeProvider>
    );
  }

  return render(ui, { wrapper: Wrapper });
}
