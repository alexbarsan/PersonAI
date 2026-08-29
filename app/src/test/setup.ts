process.env.EXPO_PUBLIC_MOCK_API = "true";

jest.mock("expo-audio", () => ({
  RecordingPresets: { HIGH_QUALITY: {} },
  requestRecordingPermissionsAsync: jest.fn(async () => ({ granted: true })),
  setAudioModeAsync: jest.fn(async () => undefined),
  useAudioRecorder: jest.fn(() => ({
    prepareToRecordAsync: jest.fn(async () => undefined),
    record: jest.fn(),
    stop: jest.fn(async () => undefined),
    uri: null
  })),
  useAudioRecorderState: jest.fn(() => ({
    canRecord: true,
    durationMillis: 0,
    isRecording: false,
    mediaServicesDidReset: false,
    url: null
  }))
}));

afterEach(() => {
  jest.clearAllMocks();
});
