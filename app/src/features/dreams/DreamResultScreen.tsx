import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocalSearchParams } from "expo-router";
import { Image, Pressable, ScrollView, StyleSheet, Text, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/client";
import { DreamImageResponse } from "@/api/dto";
import { ResultSectionRenderer } from "@/features/dreams/ResultSectionRenderer";
import { SafetyCard } from "@/features/dreams/SafetyCard";
import { useDreamResultStore } from "@/state/dreamResultStore";
import { useTheme } from "@/theme/ThemeProvider";

export function DreamResultScreen() {
  const theme = useTheme();
  const api = useApiClient();
  const queryClient = useQueryClient();
  const params = useLocalSearchParams<{ id?: string }>();
  const id = Array.isArray(params.id) ? params.id[0] : params.id;
  const cachedDream = useDreamResultStore((state) => (id ? state.getDream(id) : null));
  const dream = useQuery({
    queryKey: ["dream", id],
    queryFn: () => api.getDream(id!),
    enabled: Boolean(id) && !cachedDream,
    initialData: cachedDream ?? undefined
  });
  const result = dream.data?.result;
  const elevatedSafety = result?.safety?.selfHarmRisk === "elevated";
  const entitlement = useQuery({
    queryKey: ["entitlements"],
    queryFn: () => api.getEntitlements(),
    enabled: Boolean(result) && !elevatedSafety
  });
  const canGenerateImage = entitlement.data?.deepAnalysisEnabled === true;
  const image = useQuery({
    queryKey: ["dream-image", id],
    queryFn: () => api.getDreamImage(id!),
    enabled: Boolean(id) && canGenerateImage,
    retry: false,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === "pending" || status === "generating" ? 3000 : false;
    }
  });
  const requestImage = useMutation({
    mutationFn: () => api.requestDreamImage(id!),
    onSuccess: (created) => {
      queryClient.setQueryData<DreamImageResponse>(["dream-image", id], created);
    }
  });

  return (
    <ScrollView contentContainerStyle={[styles.screen, { backgroundColor: theme.colors.background }]}>
      <Text style={[styles.title, { color: theme.colors.text }]}>Dream result</Text>
      <Text style={[styles.disclaimer, { color: theme.colors.warning }]} testID="result-disclaimer">
        DreamLens is for reflection and entertainment. It is not medical, mental health, or safety advice.
      </Text>

      {dream.isLoading ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Loading result</Text> : null}
      {dream.isError ? <Text style={[styles.body, { color: theme.colors.warning }]}>Result could not be loaded.</Text> : null}
      {dream.data?.status === "failed" ? (
        <Text style={[styles.body, { color: theme.colors.warning }]}>
          {dream.data.errorMessage ?? "The interpretation service could not produce a result."}
        </Text>
      ) : null}

      {result ? (
        <View style={styles.content}>
          <Text style={[styles.summary, { color: theme.colors.text }]}>{result.summary}</Text>
          <SafetyCard safety={result.safety} />
          {elevatedSafety
            ? null
            : result.sections.map((section, index) => (
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
          {elevatedSafety ? null : (
            <DreamImagePanel
              canGenerateImage={canGenerateImage}
              image={image.data}
              isRequesting={requestImage.isPending}
              onRequest={() => requestImage.mutate()}
              requestError={requestImage.error}
            />
          )}
        </View>
      ) : null}
    </ScrollView>
  );
}

function DreamImagePanel({
  canGenerateImage,
  image,
  isRequesting,
  onRequest,
  requestError
}: {
  canGenerateImage: boolean;
  image: DreamImageResponse | undefined;
  isRequesting: boolean;
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
          {isWorking ? <Text style={[styles.body, { color: theme.colors.mutedText }]}>Creating your visual</Text> : null}
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
        <Text style={[styles.body, { color: theme.colors.mutedText }]}>Dream visuals are available with Premium.</Text>
      )}
    </View>
  );
}

function mapImageError(error: Error) {
  if (error instanceof ApiError && error.status === 503) {
    return "Dream visuals are not available yet. Please try again later.";
  }

  return "Dream visual could not be created. Please try again.";
}

const styles = StyleSheet.create({
  screen: {
    gap: 18,
    padding: 20,
    paddingBottom: 48
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
    gap: 14
  },
  summary: {
    fontSize: 18,
    fontWeight: "700",
    lineHeight: 25
  },
  questions: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 8,
    padding: 14
  },
  imagePanel: {
    borderRadius: 8,
    borderWidth: 1,
    gap: 10,
    padding: 14
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
  error: {
    fontSize: 13,
    lineHeight: 18
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
