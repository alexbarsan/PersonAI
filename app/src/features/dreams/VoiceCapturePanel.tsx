import { useState } from "react";
import { Pressable, Switch, Text, View } from "react-native";
import {
  RecordingPresets,
  requestRecordingPermissionsAsync,
  setAudioModeAsync,
  useAudioRecorder,
  useAudioRecorderState
} from "expo-audio";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/client";
import { useTheme } from "@/theme/ThemeProvider";

type VoiceCapturePanelProps = {
  onTranscript: (transcript: string) => void;
  prominent?: boolean;
};

const maxDurationSeconds = 180;

export function VoiceCapturePanel({ onTranscript, prominent = false }: VoiceCapturePanelProps) {
  const api = useApiClient();
  const theme = useTheme();
  const recorder = useAudioRecorder(RecordingPresets.HIGH_QUALITY);
  const recorderState = useAudioRecorderState(recorder);
  const [recordingUri, setRecordingUri] = useState<string | null>(null);
  const [recordingDurationSeconds, setRecordingDurationSeconds] = useState(0);
  const [retainRecording, setRetainRecording] = useState(false);
  const [isTranscribing, setIsTranscribing] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const startRecording = async () => {
    const permission = await requestRecordingPermissionsAsync();
    if (!permission.granted) {
      setMessage("Microphone access is required to record a dream.");
      return;
    }

    setMessage(null);
    setRecordingUri(null);
    await setAudioModeAsync({ allowsRecording: true, playsInSilentMode: true });
    await recorder.prepareToRecordAsync();
    recorder.record({ forDuration: maxDurationSeconds });
  };

  const stopRecording = async () => {
    await recorder.stop();
    const uri = recorder.uri;
    if (!uri) {
      setMessage("The recording was unavailable. Please try again.");
      return;
    }

    setRecordingUri(uri);
    setRecordingDurationSeconds(Math.max(1, Math.ceil(recorderState.durationMillis / 1000)));
    setMessage("Recording ready for transcription.");
  };

  const transcribe = async () => {
    if (!recordingUri) {
      return;
    }

    setIsTranscribing(true);
    setMessage("Transcribing recording.");
    try {
      let capture = await api.uploadVoiceCapture({
        uri: recordingUri,
        contentType: contentTypeFor(recordingUri),
        durationSeconds: recordingDurationSeconds,
        retainRecording
      });
      for (let attempt = 0; attempt < 30 && (capture.status === "pending" || capture.status === "transcribing"); attempt += 1) {
        await delay(2000);
        capture = await api.getVoiceCapture(capture.id);
      }

      if (capture.status !== "completed" || !capture.transcript) {
        throw new Error(capture.errorMessage ?? "Transcription did not complete. Please try again.");
      }

      onTranscript(capture.transcript);
      setMessage("Transcript added to the dream text. Review it before interpreting.");
      setRecordingUri(null);
    } catch (error) {
      setMessage(errorMessage(error));
    } finally {
      setIsTranscribing(false);
    }
  };

  return (
    <View style={{ gap: 10 }} testID="voice-capture-panel">
      <Text style={{ color: prominent ? theme.colors.primaryText : theme.colors.text, fontSize: 15, fontWeight: "700" }}>Capture by voice</Text>
      <Text style={{ color: prominent ? "#dce3f2" : theme.colors.mutedText, fontSize: 13, lineHeight: 18 }}>
        Up to 3 minutes. Recordings are deleted after transcription unless you choose to keep one.
      </Text>
      <Pressable
        accessibilityRole="button"
        onPress={recorderState.isRecording ? stopRecording : startRecording}
        style={{ alignItems: "center", backgroundColor: recorderState.isRecording ? theme.colors.warning : theme.colors.surface, borderColor: theme.colors.border, borderRadius: 8, borderWidth: 1, minHeight: 44, justifyContent: "center", paddingHorizontal: 12 }}
        testID="voice-record-toggle"
      >
        <Text style={{ color: recorderState.isRecording ? theme.colors.primaryText : theme.colors.text, fontWeight: "700" }}>
          {recorderState.isRecording ? "Stop recording" : "Record voice"}
        </Text>
      </Pressable>
      {recordingUri ? (
        <>
          <View style={{ alignItems: "center", flexDirection: "row", gap: 8, justifyContent: "space-between" }}>
            <Text style={{ color: prominent ? theme.colors.primaryText : theme.colors.text, flex: 1, fontSize: 14 }}>Keep recording after transcription</Text>
            <Switch value={retainRecording} onValueChange={setRetainRecording} testID="voice-retention-toggle" />
          </View>
          <Pressable
            accessibilityRole="button"
            disabled={isTranscribing}
            onPress={transcribe}
            style={{ alignItems: "center", backgroundColor: prominent ? theme.colors.sage : theme.colors.primary, borderRadius: 8, minHeight: 44, justifyContent: "center", opacity: isTranscribing ? 0.6 : 1, paddingHorizontal: 12 }}
            testID="voice-transcribe"
          >
            <Text style={{ color: prominent ? theme.colors.text : theme.colors.primaryText, fontWeight: "700" }}>{isTranscribing ? "Transcribing" : "Transcribe recording"}</Text>
          </Pressable>
        </>
      ) : null}
      {message ? <Text style={{ color: prominent ? "#dce3f2" : theme.colors.mutedText, fontSize: 13, lineHeight: 18 }}>{message}</Text> : null}
    </View>
  );
}

function contentTypeFor(uri: string) {
  const normalized = uri.toLowerCase();
  if (normalized.endsWith(".webm")) return "audio/webm";
  if (normalized.endsWith(".m4a")) return "audio/m4a";
  if (normalized.endsWith(".mp3")) return "audio/mpeg";
  if (normalized.endsWith(".wav")) return "audio/wav";
  if (normalized.endsWith(".ogg")) return "audio/ogg";
  return "audio/mp4";
}

function delay(milliseconds: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, milliseconds));
}

function errorMessage(error: unknown) {
  if (error instanceof ApiError && error.status === 403) {
    return "Voice transcription requires Premium access.";
  }
  if (error instanceof ApiError && error.status === 429) {
    return "You have reached today's voice transcription limit.";
  }
  return error instanceof Error ? error.message : "Voice transcription failed. Please try again.";
}
