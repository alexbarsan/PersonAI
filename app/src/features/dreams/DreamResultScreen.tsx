import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router, useLocalSearchParams } from "expo-router";
import { Image, Pressable, ScrollView, StyleSheet, useWindowDimensions, View } from "react-native";
import { Text } from "@/components/Text";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/client";
import { DreamImageResponse } from "@/api/dto";
import { AppShell, BrandMark } from "@/components/AppShell";
import { isSectionVisible, ResultSectionRenderer } from "@/features/dreams/ResultSectionRenderer";
import { InterpretationFeedbackPanel } from "@/features/dreams/InterpretationFeedbackPanel";
import { DeepInterpretationPanel } from "@/features/dreams/DeepInterpretationPanel";
import { DreamingFacts } from "@/features/content/DailyDreamContent";
import { SafetyCard } from "@/features/dreams/SafetyCard";
import { useDreamResultStore } from "@/state/dreamResultStore";
import { useAuthStore } from "@/auth/authStore";
import { useTheme } from "@/theme/ThemeProvider";

export function DreamResultScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const queryClient = useQueryClient();
  const params = useLocalSearchParams<{ id?: string }>();
  const id = Array.isArray(params.id) ? params.id[0] : params.id;
  const cachedDream = useDreamResultStore((state) => (id ? state.getDream(id) : null));
  const isAdmin = useAuthStore((state) => state.user?.groups?.some((group) => group === "dreamlens-admin" || group === "dreamlens-metrics-admin") === true);
  const dream = useQuery({
    queryKey: ["dream", id],
    queryFn: () => api.getDream(id!),
    enabled: Boolean(id),
    initialData: cachedDream ?? undefined,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === "pending" || status === "processing" ? 1500 : false;
    }
  });
  const result = dream.data?.result;
  const elevatedSafety = result?.safety?.selfHarmRisk === "elevated";
  const entitlement = useQuery({
    queryKey: ["entitlements"],
    queryFn: () => api.getEntitlements(),
    enabled: Boolean(result) && !elevatedSafety
  });
  const hasPremiumDreamFeatures = entitlement.data?.deepAnalysisEnabled === true;
  const canGenerateImage = entitlement.data !== undefined;
  const image = useQuery({
    queryKey: ["dream-image", id],
    queryFn: ({ queryKey }) => {
      const cached = queryClient.getQueryData<DreamImageResponse>(queryKey);
      return cached && (cached.status === "pending" || cached.status === "generating")
        ? api.waitForDreamImage(id!, cached.updatedAt).catch(() => api.getDreamImage(id!))
        : api.getDreamImage(id!);
    },
    enabled: Boolean(id) && canGenerateImage,
    retry: false,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === "pending" || status === "generating" ? 250 : false;
    }
  });
  const requestImage = useMutation({
    mutationFn: () => api.requestDreamImage(id!),
    onSuccess: (created) => {
      queryClient.setQueryData<DreamImageResponse>(["dream-image", id], created);
    }
  });
  const retryInterpretation = useMutation({
    mutationFn: () => api.retryDreamInterpretation(id!),
    onSuccess: (updated) => queryClient.setQueryData(["dream", id], updated)
  });
  const cancelInterpretation = useMutation({
    mutationFn: () => api.cancelDreamInterpretation(id!),
    onSuccess: (updated) => queryClient.setQueryData(["dream", id], updated)
  });
  const isInterpreting = dream.data?.status === "pending" || dream.data?.status === "processing";
  const { width } = useWindowDimensions();
  const detailSections = result?.sections.filter(isDetailSection) ?? [];
  const narrativeSections = result?.sections.filter((section) => isSectionVisible(section) && !isDetailSection(section) && !isInterpretationSection(section)) ?? [];
  const compactReadingLayout = width < 760;

  return (
    <AppShell>
      <ScrollView contentContainerStyle={styles.screen}>
        <BrandMark detail="A reflection, not a prediction." />
        <View style={styles.hero}>
          <Text style={[styles.title, { color: theme.colors.text }]}>{dream.data?.title ?? "Dream result"}</Text>
          <Text style={[styles.disclaimer, { color: theme.colors.mutedText }]} testID="result-disclaimer">
            Dream DNA is for reflection and entertainment. It is not medical, mental health, or safety advice.
          </Text>
        </View>

        {dream.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading result</Text> : null}
        {dream.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Result could not be loaded.</Text> : null}
        {dream.data?.text ? <View style={[styles.originalDream, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
          <Text style={[styles.originalLabel, { color: theme.colors.mutedText }]}>Original dream</Text>
          <Text selectable style={[styles.body, { color: theme.colors.text }]}>{dream.data.text}</Text>
        </View> : null}
        {isInterpreting ? <DreamInterpretationProgress
          attemptCount={dream.data?.processing?.attemptCount ?? 0}
          canCancel={dream.data?.processing?.canCancel === true}
          isCanceling={cancelInterpretation.isPending}
          onCancel={() => cancelInterpretation.mutate()}
        /> : null}
        {dream.data?.status === "failed" ? <DreamInterpretationFailure
          message={dream.data.errorMessage}
          canRetry={dream.data.processing?.canRetry === true}
          isRetrying={retryInterpretation.isPending}
          onRetry={() => retryInterpretation.mutate()}
        /> : null}
        {dream.data?.status === "canceled" ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Interpretation canceled. Your original dream remains in your journal.</Text> : null}

        {result ? (
          <View style={styles.content}>
          <SafetyCard safety={result.safety} />
          <View style={[styles.readingLayout, compactReadingLayout && styles.readingLayoutCompact]}>
            {elevatedSafety || detailSections.length === 0 ? null : (
              <View style={[styles.detailRail, compactReadingLayout && styles.detailRailCompact]}>
                {detailSections.map((section, index) => (
                  <ResultSectionRenderer key={`${section.title}-${index}`} section={section} />
                ))}
              </View>
            )}
            <View style={styles.interpretationColumn}>
              <View style={styles.interpretation}>
                <Text style={[styles.interpretationTitle, { color: theme.colors.text }]}>Interpretation</Text>
                <Text testID="dream-summary" style={[styles.summary, { color: theme.colors.text }]}>{result.summary}</Text>
              </View>
          {elevatedSafety
            ? null
            : narrativeSections.map((section, index) => (
                <ResultSectionRenderer key={`${section.title}-${index}`} section={section} />
              ))}
          {elevatedSafety || result.followUpQuestions.length === 0 ? null : (
            <View style={[styles.questions, { borderColor: theme.colors.border }]}>
              <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Follow-up questions</Text>
              {result.followUpQuestions.map((question) => (
                <Text key={question} style={[styles.body, { color: theme.colors.mutedText }]}>
                  {question}
                </Text>
              ))}
            </View>
          )}
            </View>
          </View>
          <InterpretationFeedbackPanel dreamId={dream.data!.id} />
          {elevatedSafety ? null : <DeepInterpretationPanel dreamId={dream.data!.id} enabled={hasPremiumDreamFeatures} />}
          {elevatedSafety ? null : (
            <DreamImagePanel
              canGenerateImage={canGenerateImage}
              image={image.data}
              isRequesting={requestImage.isPending}
              showOperationalTiming={isAdmin}
              onRequest={() => requestImage.mutate()}
              requestError={requestImage.error}
            />
          )}
          </View>
        ) : null}
      </ScrollView>
    </AppShell>
  );
}

function isDetailSection(section: { title: string }) {
  return ["symbols", "emotions", "people", "locations", "objects"].includes(section.title.trim().toLowerCase())
    && isSectionVisible(section as Parameters<typeof isSectionVisible>[0]);
}

function isInterpretationSection(section: { title: string }) {
  return section.title.trim().toLowerCase() === "interpretation";
}

function DreamImagePanel({
  canGenerateImage,
  image,
  isRequesting,
  showOperationalTiming,
  onRequest,
  requestError
}: {
  canGenerateImage: boolean;
  image: DreamImageResponse | undefined;
  isRequesting: boolean;
  showOperationalTiming: boolean;
  onRequest: () => void;
  requestError: Error | null;
}) {
  const theme = useTheme();
  const isWorking = image?.status === "pending" || image?.status === "generating";
  const error = requestError ?? (image?.status === "failed" ? new Error(image.errorMessage ?? "Image generation failed.") : null);

  return (
    <View style={[styles.imagePanel, { backgroundColor: theme.colors.surface, borderColor: theme.colors.border }]}>
      <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Dream visual</Text>
      {canGenerateImage ? (
        <>
          {image?.status === "completed" && image.downloadUrl ? (
            <Image accessibilityLabel="Generated dream visual" source={{ uri: image.downloadUrl }} style={styles.image} />
          ) : null}
          {image?.status === "pending" ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Visual queued</Text> : null}
          {image?.status === "generating" ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Generating your visual</Text> : null}
          {showOperationalTiming && image?.status === "completed" && image.providerLatencyMilliseconds !== null ? (
            <Text style={[styles.imageTiming, { color: theme.colors.mutedText }]}>Created in {formatImageDuration(image.providerLatencyMilliseconds)}</Text>
          ) : null}
          {error ? <Text style={[styles.error, { color: theme.colors.warning }]}>{mapImageError(error)}</Text> : null}
          {image?.status !== "completed" ? (
            <Pressable
              accessibilityRole="button"
              disabled={isRequesting || isWorking}
              onPress={onRequest}
              style={[styles.imageButton, { backgroundColor: theme.colors.primary }]}
              testID="request-dream-image"
            >
              <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>
                {isRequesting || isWorking ? "Creating visual" : "Visualize dream"}
              </Text>
            </Pressable>
          ) : null}
        </>
      ) : (
        <>
          <Text style={[styles.body, { color: theme.colors.mutedText }]}>Dream visuals are available with Premium.</Text>
          <Pressable accessibilityRole="button" onPress={() => router.push("/paywall")} style={[styles.secondaryImageButton, { borderColor: theme.colors.primary }]} testID="view-premium-dream-image">
            <Text style={[styles.buttonText, { color: theme.colors.primary }]}>View Premium</Text>
          </Pressable>
        </>
      )}
    </View>
  );
}

function DreamInterpretationProgress({
  attemptCount,
  canCancel,
  isCanceling,
  onCancel
}: {
  attemptCount: number;
  canCancel: boolean;
  isCanceling: boolean;
  onCancel: () => void;
}) {
  const theme = useTheme();
  return <View style={[styles.progressPanel, { backgroundColor: theme.colors.lavender, borderColor: theme.colors.border }]} testID="dream-interpretation-progress">
    <Text style={[styles.sectionTitle, { color: theme.colors.text }]}>Your interpretation is taking shape</Text>
    <Text style={[styles.body, { color: theme.colors.mutedText }]}>You can leave this screen. Dream DNA will keep working and your journal will update when it is ready.</Text>
    <DreamingFacts label="While it takes shape" />
    {attemptCount > 1 ? <Text style={[styles.progressMeta, { color: theme.colors.mutedText }]}>Retry {attemptCount}</Text> : null}
    {canCancel ? <Pressable accessibilityRole="button" disabled={isCanceling} onPress={onCancel} style={[styles.cancelButton, { borderColor: theme.colors.text }]}>
      <Text style={[styles.cancelButtonText, { color: theme.colors.text }]}>{isCanceling ? "Canceling" : "Cancel interpretation"}</Text>
    </Pressable> : null}
  </View>;
}

function DreamInterpretationFailure({
  message,
  canRetry,
  isRetrying,
  onRetry
}: {
  message: string | null | undefined;
  canRetry: boolean;
  isRetrying: boolean;
  onRetry: () => void;
}) {
  const theme = useTheme();
  return <View style={[styles.failurePanel, { borderColor: theme.colors.warning }]}>
    <Text style={[styles.body, { color: theme.colors.warning }]}>{message ?? "The interpretation service could not produce a result."}</Text>
    {canRetry ? <Pressable accessibilityRole="button" disabled={isRetrying} onPress={onRetry} style={[styles.retryButton, { backgroundColor: theme.colors.primary }]}>
      <Text style={[styles.buttonText, { color: theme.colors.primaryText }]}>{isRetrying ? "Retrying" : "Try again"}</Text>
    </Pressable> : null}
  </View>;
}

function mapImageError(error: Error) {
  if (error instanceof ApiError && error.status === 503) {
    return "Dream visuals are not available yet. Please try again later.";
  }

  if (error.message === "This dream cannot be visualized by the selected image provider." || error.message.includes("moderation_blocked")) {
    return "This dream cannot be visualized by the selected image provider.";
  }

  return "Dream visual could not be created. Please try again.";
}

function formatImageDuration(milliseconds: number) {
  return `${Math.max(0.1, milliseconds / 1000).toFixed(1)} seconds`;
}

const styles = StyleSheet.create({
  screen: {
    width: "100%",
    maxWidth: 920,
    alignSelf: "center",
    gap: 16,
    padding: 20,
    paddingBottom: 28
  },
  hero: {
    gap: 12,
    paddingVertical: 20
  },
  title: {
    fontSize: 30,
    fontWeight: "700"
  },
  disclaimer: {
    fontSize: 14,
    lineHeight: 20
  },
  body: {
    fontSize: 15,
    lineHeight: 22
  },
  content: {
    gap: 24
  },
  readingLayout: {
    alignItems: "flex-start",
    flexDirection: "row",
    gap: 36
  },
  readingLayoutCompact: {
    flexDirection: "column-reverse",
    gap: 24
  },
  detailRail: {
    flexBasis: 250,
    flexGrow: 0,
    flexShrink: 0,
    maxWidth: 280,
    width: 250
  },
  detailRailCompact: {
    alignSelf: "stretch",
    flexBasis: "auto",
    maxWidth: "100%",
    width: "100%"
  },
  interpretationColumn: {
    flex: 1,
    minWidth: 0
  },
  interpretation: {
    gap: 10,
    paddingBottom: 24
  },
  interpretationTitle: {
    fontSize: 22,
    fontWeight: "700",
    lineHeight: 30
  },
  originalDream: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 8,
    padding: 16
  },
  originalLabel: {
    fontSize: 12,
    fontWeight: "700",
    textTransform: "uppercase"
  },
  progressPanel: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 12,
    padding: 16
  },
  progressMeta: {
    fontSize: 12,
    lineHeight: 17
  },
  cancelButton: {
    alignItems: "center",
    alignSelf: "flex-start",
    borderRadius: 6,
    borderWidth: 1,
    justifyContent: "center",
    minHeight: 40,
    paddingHorizontal: 14
  },
  cancelButtonText: {
    fontSize: 14,
    fontWeight: "700"
  },
  failurePanel: {
    borderLeftWidth: 3,
    gap: 12,
    paddingLeft: 16,
    paddingVertical: 8
  },
  retryButton: {
    alignItems: "center",
    alignSelf: "flex-start",
    borderRadius: 6,
    justifyContent: "center",
    minHeight: 42,
    paddingHorizontal: 16
  },
  summary: {
    fontSize: 20,
    fontWeight: "700",
    lineHeight: 30,
    maxWidth: 720
  },
  questions: {
    borderTopWidth: 1,
    gap: 12,
    paddingVertical: 24
  },
  imagePanel: {
    borderTopWidth: 1,
    gap: 10,
    paddingVertical: 24
  },
  image: {
    aspectRatio: 1,
    borderRadius: 8,
    width: "100%"
  },
  imageButton: {
    alignItems: "center",
    borderRadius: 8,
    justifyContent: "center",
    minHeight: 44,
    paddingHorizontal: 16
  },
  secondaryImageButton: {
    alignItems: "center",
    borderRadius: 8,
    borderWidth: 1,
    justifyContent: "center",
    minHeight: 44,
    paddingHorizontal: 16
  },
  error: {
    fontSize: 13,
    lineHeight: 18
  },
  imageTiming: {
    fontSize: 12,
    lineHeight: 17
  },
  buttonText: {
    fontSize: 16,
    fontWeight: "700"
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: "700"
  }
});
