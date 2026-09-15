import { useMutation } from "@tanstack/react-query";
import { router } from "expo-router";

import { useApiClient } from "@/api/apiContext";
import { SubmitDreamRequest } from "@/api/dto";
import { useDreamDraftStore } from "@/state/dreamDraftStore";
import { useDreamResultStore } from "@/state/dreamResultStore";

export function useDreamSubmission(onSubmitted?: (dreamId: string) => void) {
  const api = useApiClient();
  const clearDraft = useDreamDraftStore((state) => state.clearDraft);
  const rememberDream = useDreamResultStore((state) => state.rememberDream);
  const mutation = useMutation({ mutationFn: (request: SubmitDreamRequest) => api.submitDream(request) });

  async function submit(request: SubmitDreamRequest) {
    const dream = await mutation.mutateAsync(request);
    rememberDream(dream);
    clearDraft();
    if (onSubmitted) {
      onSubmitted(dream.id);
      return;
    }

    router.push(`/dreams/${dream.id}`);
  }

  return { ...mutation, submit };
}
