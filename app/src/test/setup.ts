process.env.EXPO_PUBLIC_MOCK_API = "true";

jest.mock("react-native-safe-area-context", () => require("react-native-safe-area-context/jest/mock").default);
jest.mock("expo-font", () => ({ ...jest.requireActual("expo-font"), useFonts: () => [true, null] }));
jest.mock("@expo-google-fonts/nunito/400Regular", () => ({ Nunito_400Regular: 1 }));
jest.mock("@expo-google-fonts/nunito/700Bold", () => ({ Nunito_700Bold: 2 }));

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
