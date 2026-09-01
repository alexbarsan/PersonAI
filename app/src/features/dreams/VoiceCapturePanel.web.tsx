import { useEffect, useRef, useState } from "react";
import { Pressable, StyleSheet, Switch, Text, View } from "react-native";

import { useApiClient } from "@/api/apiContext";
import { ApiError } from "@/api/client";
import { useTheme } from "@/theme/ThemeProvider";

type VoiceCapturePanelProps = {
  onTranscript: (transcript: string) => void;
  prominent?: boolean;
};

type CompletedRecording = {
  contentType: string;
  durationSeconds: number;
  uri: string;
};

const maxDurationSeconds = 180;

export function VoiceCapturePanel({ onTranscript, prominent = false }: VoiceCapturePanelProps) {
  const api = useApiClient();
  const theme = useTheme();
  const recorderRef = useRef<MediaRecorder | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const recordingUrlRef = useRef<string | null>(null);
  const startedAtRef = useRef(0);
  const [isRecording, setIsRecording] = useState(false);
  const [recording, setRecording] = useState<CompletedRecording | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const [retainRecording, setRetainRecording] = useState(false);
  const [isTranscribing, setIsTranscribing] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => () => {
    clearTimers();
    stopStream();
    revokeRecordingUrl();
  }, []);

  const clearTimers = () => {
    if (timerRef.current) clearInterval(timerRef.current);
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    timerRef.current = null;
    timeoutRef.current = null;
  };

  const stopStream = () => {
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
  };

  const revokeRecordingUrl = () => {
    if (recordingUrlRef.current) URL.revokeObjectURL(recordingUrlRef.current);
    recordingUrlRef.current = null;
  };

  const finishRecording = () => {
    clearTimers();
    stopStream();
    setIsRecording(false);

    const mimeType = recorderRef.current?.mimeType || "audio/webm";
    const audio = new Blob(chunksRef.current, { type: mimeType });
    if (audio.size === 0) {
      setMessage("No audio was captured. Check microphone access and try again.");
      return;
    }

    revokeRecordingUrl();
    const uri = URL.createObjectURL(audio);
    recordingUrlRef.current = uri;
    const durationSeconds = Math.max(1, Math.ceil((Date.now() - startedAtRef.current) / 1000));
    setRecording({ uri, contentType: audio.type || "audio/webm", durationSeconds });
    setElapsedSeconds(durationSeconds);
    setMessage("Recording ready for transcription.");
  };

  const startRecording = async () => {
    if (!canRecordInBrowser()) {
      setMessage("Voice recording is not supported by this browser. Try a current version of Chrome, Safari, or Edge.");
      return;
    }

    try {
      setMessage(null);
      setRecording(null);
      revokeRecordingUrl();
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const recorder = createRecorder(stream);
      streamRef.current = stream;
      recorderRef.current = recorder;
      chunksRef.current = [];
      startedAtRef.current = Date.now();
      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) chunksRef.current.push(event.data);
      };
      recorder.onstop = finishRecording;
      recorder.start();
      setIsRecording(true);
      setElapsedSeconds(0);
      timerRef.current = setInterval(() => setElapsedSeconds(Math.floor((Date.now() - startedAtRef.current) / 1000)), 500);
      timeoutRef.current = setTimeout(stopRecording, maxDurationSeconds * 1000);
    } catch {
      stopStream();
      setMessage("Microphone access is required to record a dream.");
    }
  };

  const stopRecording = () => {
    if (recorderRef.current?.state === "recording") recorderRef.current.stop();
  };

  const transcribe = async () => {
    if (!recording) return;

    setIsTranscribing(true);
    setMessage("Transcribing recording.");
    try {
      let capture = await api.uploadVoiceCapture({ ...recording, retainRecording });
      for (let attempt = 0; attempt < 30 && (capture.status === "pending" || capture.status === "transcribing"); attempt += 1) {
        await delay(2000);
        capture = await api.getVoiceCapture(capture.id);
      }

      if (capture.status !== "completed" || !capture.transcript) {
        throw new Error(capture.errorMessage ?? "Transcription did not complete. Please try again.");
      }

      onTranscript(capture.transcript);
      revokeRecordingUrl();
      setRecording(null);
      setMessage("Transcript added. Review it before interpreting.");
    } catch (error) {
      setMessage(errorMessage(error));
    } finally {
      setIsTranscribing(false);
    }
  };

  return (
    <View style={[styles.panel, prominent ? styles.prominent : null]} testID="voice-capture-panel">
      <View style={styles.heading}>
        <Text style={[styles.title, { color: prominent ? theme.colors.primaryText : theme.colors.text }]}>Capture by voice</Text>
        <Text style={[styles.detail, { color: prominent ? "#c9d1e2" : theme.colors.mutedText }]}>Up to 3 minutes</Text>
      </View>
      <Text style={[styles.body, { color: prominent ? "#dce3f2" : theme.colors.mutedText }]}>
        Say what you remember. Audio is deleted after transcription unless you choose to keep it.
      </Text>
      <Pressable
        accessibilityLabel={isRecording ? "Stop voice recording" : "Record voice"}
        accessibilityRole="button"
        onPress={isRecording ? stopRecording : startRecording}
        style={[styles.recordButton, { backgroundColor: isRecording ? theme.colors.warning : theme.colors.surface, borderColor: isRecording ? theme.colors.warning : theme.colors.border }]}
        testID="voice-record-toggle"
      >
        <Text style={[styles.recordButtonText, { color: isRecording ? theme.colors.primaryText : theme.colors.text }]}>
          {isRecording ? `Stop recording ${formatDuration(elapsedSeconds)}` : "Record voice"}
        </Text>
      </Pressable>
      {recording ? (
        <>
          <View style={styles.retentionRow}>
            <View style={styles.retentionCopy}>
              <Text style={[styles.retentionTitle, { color: prominent ? theme.colors.primaryText : theme.colors.text }]}>Keep recording</Text>
              <Text style={[styles.detail, { color: prominent ? "#dce3f2" : theme.colors.mutedText }]}>Off by default for privacy</Text>
            </View>
            <Switch value={retainRecording} onValueChange={setRetainRecording} testID="voice-retention-toggle" />
          </View>
          <Pressable
            accessibilityRole="button"
            disabled={isTranscribing}
            onPress={transcribe}
            style={[styles.transcribeButton, { backgroundColor: prominent ? theme.colors.sage : theme.colors.primary, opacity: isTranscribing ? 0.6 : 1 }]}
            testID="voice-transcribe"
          >
            <Text style={[styles.transcribeButtonText, { color: prominent ? theme.colors.text : theme.colors.primaryText }]}>{isTranscribing ? "Transcribing" : "Add transcript"}</Text>
          </Pressable>
        </>
      ) : null}
      {message ? <Text accessibilityLiveRegion="polite" style={[styles.message, { color: prominent ? "#dce3f2" : theme.colors.mutedText }]}>{message}</Text> : null}
    </View>
  );
}

function canRecordInBrowser() {
  return typeof navigator !== "undefined" && Boolean(navigator.mediaDevices?.getUserMedia) && typeof MediaRecorder !== "undefined";
}

function createRecorder(stream: MediaStream) {
  const mimeType = ["audio/webm;codecs=opus", "audio/webm", "audio/mp4"].find((candidate) => MediaRecorder.isTypeSupported(candidate));
  return mimeType ? new MediaRecorder(stream, { mimeType }) : new MediaRecorder(stream);
}

function formatDuration(seconds: number) {
  return `${Math.floor(seconds / 60).toString().padStart(2, "0")}:${(seconds % 60).toString().padStart(2, "0")}`;
}

function delay(milliseconds: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, milliseconds));
}

function errorMessage(error: unknown) {
  if (error instanceof ApiError && error.status === 403) return "Voice transcription requires Premium access.";
  if (error instanceof ApiError && error.status === 429) return "You have reached today's voice transcription limit.";
  return error instanceof Error ? error.message : "Voice transcription failed. Please try again.";
}

const styles = StyleSheet.create({
  panel: { gap: 10 },
  prominent: { marginTop: 2 },
  heading: { alignItems: "baseline", flexDirection: "row", justifyContent: "space-between" },
  title: { fontSize: 16, fontWeight: "800" },
  body: { fontSize: 13, lineHeight: 19 },
  detail: { fontSize: 12, lineHeight: 17 },
  recordButton: { alignItems: "center", borderRadius: 6, borderWidth: 1, justifyContent: "center", minHeight: 48, paddingHorizontal: 14 },
  recordButtonText: { fontSize: 15, fontWeight: "800" },
  retentionRow: { alignItems: "center", flexDirection: "row", justifyContent: "space-between", minHeight: 48 },
  retentionCopy: { flex: 1, gap: 1, paddingRight: 12 },
  retentionTitle: { fontSize: 14, fontWeight: "700" },
  transcribeButton: { alignItems: "center", borderRadius: 6, justifyContent: "center", minHeight: 46, paddingHorizontal: 14 },
  transcribeButtonText: { fontSize: 15, fontWeight: "800" },
  message: { fontSize: 13, lineHeight: 19 }
});
