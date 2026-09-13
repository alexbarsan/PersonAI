import { useState } from "react";
import {
  Image,
  Linking,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  useWindowDimensions,
  View,
} from "react-native";
import ArrowRight from "lucide-react-native/icons/arrow-right";
import BookOpen from "lucide-react-native/icons/book-open";
import Check from "lucide-react-native/icons/check";
import ChevronDown from "lucide-react-native/icons/chevron-down";
import Mic from "lucide-react-native/icons/mic";
import Monitor from "lucide-react-native/icons/monitor";
import Smartphone from "lucide-react-native/icons/smartphone";
import Sparkles from "lucide-react-native/icons/sparkles";
import Waypoints from "lucide-react-native/icons/waypoints";
import { BrandMark } from "@/components/AppShell";
import { gardenSource, OwlMark } from "@/components/OwlMark";
import { Text } from "@/components/Text";

type Props = {
  onStart: () => void;
  pending?: boolean;
  error?: string | null;
  mock?: boolean;
};
const stores = [
  {
    name: "App Store",
    platform: "iOS",
    url: process.env.EXPO_PUBLIC_IOS_STORE_URL,
  },
  {
    name: "Google Play",
    platform: "Android",
    url: process.env.EXPO_PUBLIC_ANDROID_STORE_URL,
  },
];
const questions = [
  [
    "Can I use Dream DNA without downloading anything?",
    "Yes. Sign in and capture dreams, read interpretations, explore your journal, and revisit your dream map right here in your browser.",
  ],
  [
    "What if I only remember a fragment?",
    "A feeling, a person, or a single image is enough to start. You can type a dream or capture it by voice, and return to your journal later.",
  ],
  [
    "Are dream interpretations a diagnosis?",
    "No. Interpretations are possibilities for reflection, not facts about your mind, predictions, or medical advice. Your own meaning matters most.",
  ],
  [
    "What control do I have over my data?",
    "Your profile includes consent settings for AI processing and dream history use. AI features send relevant content to our service providers. Authorized administrators can access original dreams for support and review; this is not an end-to-end encrypted diary.",
  ],
];

export function LandingScreen({ onStart, pending, error, mock }: Props) {
  const { width, height } = useWindowDimensions();
  const mobile = width < 720;
  const [preview, setPreview] = useState<"Journal" | "Dream map" | "Visuals">(
    "Journal",
  );
  const [openQuestion, setOpenQuestion] = useState<number | null>(null);
  const start = (testID?: string) => (
    <Pressable
      accessibilityRole="button"
      disabled={pending}
      onPress={onStart}
      testID={testID}
      style={({ pressed }) => [s.button, pressed && { opacity: 0.8 }]}
    >
      <Text style={s.buttonText}>
        {pending
          ? "Opening your journal..."
          : mock && testID
            ? "Use mock account"
            : "Start your dream journal"}
      </Text>
      <ArrowRight color="white" size={18} />
    </Pressable>
  );

  if (Platform.OS !== "web")
    return (
      <ScrollView contentContainerStyle={s.nativeWelcome}>
        <OwlMark size={170} />
        <Text accessibilityRole="header" style={s.nativeTitle}>
          Dream DNA
        </Text>
        <Text style={s.nativeBody}>
          A few seconds is enough to keep a dream from disappearing.
        </Text>
        <Text style={s.body}>
          A personal map of your subconscious, over time.
        </Text>
        {start(mock ? "mock-sign-in" : "cognito-sign-in")}
        <Text testID="auth-state" style={s.small}>
          Signed out
        </Text>
        {error ? (
          <Text testID="auth-error" accessibilityRole="alert" style={s.error}>
            {error}
          </Text>
        ) : null}
      </ScrollView>
    );

  return (
    <ScrollView style={s.page} contentContainerStyle={s.pageContent}>
      <View style={s.header}>
        <BrandMark />
        <Pressable
          accessibilityRole="button"
          onPress={onStart}
          disabled={pending}
          style={s.headerAction}
        >
          <Text style={s.headerActionText}>Open web app</Text>
          <ArrowRight size={17} color="#245c49" />
        </Pressable>
      </View>
      <View
        style={[
          s.hero,
          {
            minHeight: Math.max(
              mobile ? 500 : 530,
              Math.min(height * 0.76, 720),
            ),
          },
        ]}
      >
        <Image source={gardenSource} resizeMode="cover" style={s.heroImage} />
        <View
          style={[
            s.heroInner,
            mobile && { paddingHorizontal: 24, paddingTop: 38 },
          ]}
        >
          <View style={s.eyebrow}>
            <View style={s.smallDot} />
            <Text style={s.eyebrowText}>For the part of you that dreams</Text>
          </View>
          <Text
            accessibilityRole="header"
            style={[s.heroTitle, mobile && s.heroTitleMobile]}
          >
            Dream DNA
          </Text>
          <Text style={[s.heroStatement, mobile && s.heroStatementMobile]}>
            Your inner world deserves a place to stay.
          </Text>
          <Text style={s.heroBody}>
            A few seconds is enough to keep a dream from disappearing. Remember
            it. Explore its meaning. Discover what connects your nights.
          </Text>
          <View style={s.heroActions}>
            {start(mock ? "mock-sign-in" : "cognito-sign-in")}
            <Text style={s.heroFootnote}>
              Start on web. Make room for a little wonder.
            </Text>
          </View>
          {error ? (
            <Text testID="auth-error" accessibilityRole="alert" style={s.error}>
              {error}
            </Text>
          ) : null}
        </View>
      </View>
      <View style={s.trustBand}>
        <Text style={s.trustItem}>Your words, written or spoken</Text>
        <Text style={s.trustItem}>Meaning, without judgment</Text>
        <Text style={s.trustItem}>Patterns that grow with you</Text>
      </View>

      <View style={s.section}>
        <Text style={s.kicker}>
          From a fleeting moment to a familiar pattern
        </Text>
        <Text accessibilityRole="header" style={s.sectionTitle}>
          There is more to your nights.
        </Text>
        <View style={[s.steps, mobile && s.stacked]}>
          {[
            {
              icon: Mic,
              title: "Keep the little things",
              body: "The blue door. An old friend. That feeling when you woke up. Type or speak before the details fade.",
              color: "#e0f0e7",
              number: "01",
            },
            {
              icon: Sparkles,
              title: "Find your own meaning",
              body: "Read a thoughtful interpretation, ask a deeper question, or turn a dream into a symbolic visual.",
              color: "#eeebf8",
              number: "02",
            },
            {
              icon: Waypoints,
              title: "See the threads appear",
              body: "Revisit recurring people, places, feelings, and themes. A personal dream map, shaped by your own history.",
              color: "#fae8df",
              number: "03",
            },
          ].map(({ icon: Icon, title, body, color, number }) => (
            <View key={number} style={s.step}>
              <View style={s.stepTop}>
                <View style={[s.featureIcon, { backgroundColor: color }]}>
                  <Icon size={25} color="#245c49" />
                </View>
                <Text style={s.stepNumber}>{number}</Text>
              </View>
              <Text style={s.stepTitle}>{title}</Text>
              <Text style={s.body}>{body}</Text>
            </View>
          ))}
        </View>
      </View>

      <View style={s.previewBand}>
        <View style={[s.previewInner, mobile && s.stacked]}>
          <View style={s.previewCopy}>
            <OwlMark size={105} />
            <Text style={s.kicker}>A little curiosity goes a long way</Text>
            <Text accessibilityRole="header" style={s.sectionTitle}>
              Not just a dream diary. A way to know your patterns.
            </Text>
            <Text style={s.body}>
              Some dreams stand alone. Others quietly echo. Keep them together,
              and notice what returns without forcing a meaning.
            </Text>
            <View style={s.checkRow}>
              <Check color="#245c49" size={18} />
              <Text style={s.body}>
                Search your memories, not a symbol dictionary.
              </Text>
            </View>
          </View>
          <View style={s.previewTool}>
            <View style={s.previewTabs}>
              {(["Journal", "Dream map", "Visuals"] as const).map((tab) => (
                <Pressable
                  key={tab}
                  accessibilityRole="tab"
                  accessibilityState={{ selected: preview === tab }}
                  onPress={() => setPreview(tab)}
                  style={[s.previewTab, preview === tab && s.previewTabActive]}
                >
                  <Text style={s.tabText}>{tab}</Text>
                </Pressable>
              ))}
            </View>
            <View style={s.previewBody}>
              <Text style={s.small}>Illustrative example</Text>
              {preview === "Journal" ? (
                <>
                  <Text style={s.previewTitle}>
                    A river, a bridge, a familiar face.
                  </Text>
                  <Text style={s.body}>
                    "I followed the river until I found the little bridge again.
                    This time, I wasn't in a hurry."
                  </Text>
                  <View style={s.sampleTags}>
                    <Text style={s.sampleTag}>Calm</Text>
                    <Text style={[s.sampleTag, { backgroundColor: "#eeebf8" }]}>
                      Water
                    </Text>
                    <Text style={[s.sampleTag, { backgroundColor: "#fae8df" }]}>
                      A familiar place
                    </Text>
                  </View>
                  <View style={s.sampleReflection}>
                    <BookOpen size={21} color="#245c49" />
                    <Text style={[s.body, { flex: 1 }]}>
                      What felt different about returning this time?
                    </Text>
                  </View>
                </>
              ) : preview === "Dream map" ? (
                <>
                  <Text style={s.previewTitle}>The feelings that return</Text>
                  {[
                    { title: "Curiosity", percent: 55, color: "#91bca8" },
                    { title: "Calm", percent: 30, color: "#b5a1d9" },
                    { title: "Anxiety", percent: 15, color: "#e49c81" },
                  ].map((item) => (
                    <View key={item.title} style={s.sampleMetric}>
                      <View style={s.sampleMetricLabel}>
                        <Text style={s.body}>{item.title}</Text>
                        <Text style={s.body}>{item.percent}%</Text>
                      </View>
                      <View style={s.track}>
                        <View
                          style={{
                            width: `${item.percent}%`,
                            height: 8,
                            backgroundColor: item.color,
                            borderRadius: 4,
                          }}
                        />
                      </View>
                    </View>
                  ))}
                  <Text style={s.small}>
                    Example data, not your journal statistics.
                  </Text>
                </>
              ) : (
                <>
                  <Image
                    accessibilityLabel="Illustrative dream garden visual"
                    source={gardenSource}
                    style={s.sampleImage}
                  />
                  <Text style={s.previewTitle}>
                    Give a memory a little color.
                  </Text>
                  <Text style={s.body}>
                    Create a visual when you choose. Sensitive dreams may be
                    represented symbolically.
                  </Text>
                </>
              )}
            </View>
          </View>
        </View>
      </View>

      <View style={s.section}>
        <Text style={s.kicker}>A place on your everyday screen</Text>
        <Text accessibilityRole="header" style={s.sectionTitle}>
          By your bedside. Or in your browser.
        </Text>
        <Text style={[s.body, { maxWidth: 600 }]}>
          Your dream journal is ready on the web. Our Android and iOS apps are
          coming next, so a thought is never far from somewhere to keep it.
        </Text>
        <View style={s.downloads}>
          <Pressable
            accessibilityRole="button"
            onPress={onStart}
            style={s.webDownload}
          >
            <Monitor size={24} color="white" />
            <View>
              <Text style={s.downloadSmall}>Available now</Text>
              <Text style={s.downloadTitle}>Open web app</Text>
            </View>
            <ArrowRight size={18} color="white" />
          </Pressable>
          {stores.map((store) => {
            const available = Boolean(
              store.url && /^https:\/\//.test(store.url),
            );
            return (
              <Pressable
                key={store.platform}
                accessibilityRole="link"
                accessibilityLabel={`${store.platform}: ${available ? store.name : "coming soon"}`}
                accessibilityState={{ disabled: !available }}
                disabled={!available}
                testID={`store-${store.platform.toLowerCase()}`}
                onPress={() => {
                  if (available) void Linking.openURL(store.url!);
                }}
                style={s.storeDownload}
              >
                <Smartphone size={25} color="#596e67" />
                <View>
                  <Text style={s.small}>
                    {available
                      ? `Download for ${store.platform}`
                      : `${store.platform} - coming soon`}
                  </Text>
                  <Text style={s.storeTitle}>{store.name}</Text>
                </View>
              </Pressable>
            );
          })}
        </View>
      </View>

      <View style={s.faqBand}>
        <View style={s.section}>
          <Text accessibilityRole="header" style={s.sectionTitle}>
            A few things you might wonder.
          </Text>
          {questions.map(([question, answer], index) => (
            <View key={question} style={s.faqItem}>
              <Pressable
                accessibilityRole="button"
                accessibilityState={{ expanded: openQuestion === index }}
                onPress={() =>
                  setOpenQuestion(openQuestion === index ? null : index)
                }
                style={s.faqButton}
              >
                <Text style={s.faqQuestion}>{question}</Text>
                <ChevronDown
                  size={20}
                  color="#245c49"
                  style={{
                    transform: [
                      { rotate: openQuestion === index ? "180deg" : "0deg" },
                    ],
                  }}
                />
              </Pressable>
              {openQuestion === index ? (
                <Text style={[s.body, { paddingBottom: 20 }]}>{answer}</Text>
              ) : null}
            </View>
          ))}
        </View>
      </View>
      <View style={s.footer}>
        <BrandMark />
        <Text style={s.small}>
          For reflection, not prediction. Not medical or mental health advice.
        </Text>
        <Text testID="auth-state" style={s.small}>
          Signed out
        </Text>
      </View>
    </ScrollView>
  );
}

const s = StyleSheet.create({
  page: { flex: 1, backgroundColor: "#fff" },
  pageContent: { flexGrow: 1 },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    paddingHorizontal: 24,
    minHeight: 84,
    width: "100%",
    maxWidth: 1200,
    alignSelf: "center",
  },
  headerAction: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    minHeight: 44,
  },
  headerActionText: { color: "#245c49", fontWeight: "700", fontSize: 14 },
  hero: { width: "100%", backgroundColor: "#dff2ec", overflow: "hidden" },
  heroImage: {
    position: "absolute",
    top: 0,
    left: 0,
    width: "100%",
    height: "100%",
  },
  heroInner: {
    width: "100%",
    maxWidth: 1200,
    alignSelf: "center",
    paddingHorizontal: 40,
    paddingTop: 54,
    paddingBottom: 160,
  },
  eyebrow: {
    flexDirection: "row",
    gap: 8,
    alignItems: "center",
    marginBottom: 15,
  },
  smallDot: {
    width: 7,
    height: 7,
    borderRadius: 4,
    backgroundColor: "#245c49",
  },
  eyebrowText: { fontSize: 13, color: "#245c49", fontWeight: "700" },
  heroTitle: {
    fontSize: 68,
    lineHeight: 80,
    color: "#203e36",
    fontWeight: "700",
    marginBottom: 12,
  },
  heroTitleMobile: { fontSize: 44, lineHeight: 54 },
  heroStatement: {
    fontSize: 29,
    lineHeight: 37,
    maxWidth: 470,
    color: "#203e36",
    fontWeight: "700",
    marginBottom: 16,
  },
  heroStatementMobile: { fontSize: 24, lineHeight: 31, maxWidth: 320 },
  heroBody: { fontSize: 17, lineHeight: 26, maxWidth: 455, color: "#345d4e" },
  heroActions: { gap: 12, alignItems: "flex-start", marginTop: 24 },
  heroFootnote: { fontSize: 12, color: "#345d4e" },
  button: {
    backgroundColor: "#245c49",
    minHeight: 50,
    borderRadius: 8,
    paddingHorizontal: 20,
    paddingVertical: 12,
    flexDirection: "row",
    gap: 14,
    alignItems: "center",
    justifyContent: "center",
  },
  buttonText: {
    color: "white",
    fontSize: 15,
    fontWeight: "700",
    flexShrink: 1,
  },
  trustBand: {
    backgroundColor: "#eff6f2",
    flexDirection: "row",
    flexWrap: "wrap",
    justifyContent: "center",
    gap: 22,
    paddingHorizontal: 24,
    paddingVertical: 22,
  },
  trustItem: { color: "#426453", fontSize: 13, fontWeight: "700" },
  section: {
    width: "100%",
    maxWidth: 1160,
    paddingHorizontal: 24,
    paddingVertical: 64,
    alignSelf: "center",
    gap: 15,
  },
  kicker: { fontSize: 13, fontWeight: "700", color: "#627b6d" },
  sectionTitle: {
    color: "#203e36",
    fontSize: 32,
    lineHeight: 40,
    fontWeight: "700",
    maxWidth: 680,
  },
  body: { color: "#596e67", fontSize: 15, lineHeight: 24 },
  small: { color: "#596e67", fontSize: 12, lineHeight: 18 },
  steps: { flexDirection: "row", gap: 40, paddingTop: 22 },
  stacked: { flexDirection: "column" },
  step: { flex: 1, gap: 12 },
  stepTop: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
  },
  featureIcon: {
    width: 52,
    height: 52,
    borderRadius: 8,
    alignItems: "center",
    justifyContent: "center",
  },
  stepNumber: { color: "#83968c", fontSize: 13 },
  stepTitle: {
    fontSize: 20,
    lineHeight: 27,
    color: "#203e36",
    fontWeight: "700",
  },
  previewBand: { backgroundColor: "#f1eef8" },
  previewInner: {
    flexDirection: "row",
    gap: 60,
    maxWidth: 1160,
    width: "100%",
    alignSelf: "center",
    paddingHorizontal: 24,
    paddingVertical: 54,
  },
  previewCopy: { flex: 1, gap: 16 },
  checkRow: { flexDirection: "row", alignItems: "flex-start", gap: 9 },
  previewTool: {
    flex: 1,
    backgroundColor: "white",
    borderWidth: 1,
    borderColor: "#ded9ea",
    borderRadius: 8,
    alignSelf: "stretch",
    overflow: "hidden",
  },
  previewTabs: {
    flexDirection: "row",
    padding: 8,
    gap: 4,
    borderBottomWidth: 1,
    borderBottomColor: "#eeebf4",
  },
  previewTab: {
    flex: 1,
    minHeight: 44,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 6,
  },
  previewTabActive: { backgroundColor: "#eeebf8" },
  tabText: { fontSize: 13, color: "#203e36", fontWeight: "700" },
  previewBody: { padding: 24, gap: 18, minHeight: 480 },
  previewTitle: {
    fontSize: 22,
    lineHeight: 30,
    color: "#203e36",
    fontWeight: "700",
  },
  sampleTags: { flexDirection: "row", flexWrap: "wrap", gap: 7 },
  sampleTag: {
    backgroundColor: "#e0f0e7",
    color: "#365648",
    paddingHorizontal: 9,
    paddingVertical: 5,
    borderRadius: 5,
    fontSize: 12,
  },
  sampleReflection: {
    borderTopWidth: 1,
    borderTopColor: "#e6eae6",
    paddingTop: 20,
    flexDirection: "row",
    gap: 12,
  },
  sampleMetric: { gap: 7 },
  sampleMetricLabel: { flexDirection: "row", justifyContent: "space-between" },
  track: { height: 8, backgroundColor: "#f0f1ee", borderRadius: 4 },
  sampleImage: { width: "100%", height: 150, borderRadius: 6 },
  downloads: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 14,
    paddingTop: 14,
  },
  webDownload: {
    backgroundColor: "#245c49",
    flexDirection: "row",
    alignItems: "center",
    gap: 14,
    padding: 18,
    borderRadius: 8,
    minHeight: 80,
  },
  downloadSmall: { color: "#d8e8df", fontSize: 12 },
  downloadTitle: { color: "white", fontSize: 17, fontWeight: "700" },
  storeDownload: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    padding: 18,
    borderWidth: 1,
    borderColor: "#d9e5df",
    borderRadius: 8,
    minHeight: 80,
  },
  storeTitle: { color: "#596e67", fontSize: 17, fontWeight: "700" },
  faqBand: { backgroundColor: "#faf5f2" },
  faqItem: { borderBottomWidth: 1, borderBottomColor: "#e7ddd7" },
  faqButton: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    gap: 20,
    paddingVertical: 20,
    minHeight: 60,
  },
  faqQuestion: {
    flex: 1,
    color: "#203e36",
    fontSize: 16,
    fontWeight: "700",
    lineHeight: 23,
  },
  footer: {
    maxWidth: 1160,
    width: "100%",
    alignSelf: "center",
    paddingHorizontal: 24,
    paddingVertical: 30,
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 20,
    alignItems: "center",
    justifyContent: "space-between",
  },
  nativeWelcome: {
    flexGrow: 1,
    backgroundColor: "#e7f4ed",
    alignItems: "center",
    justifyContent: "center",
    padding: 28,
    gap: 24,
  },
  nativeTitle: { fontSize: 38, color: "#203e36", fontWeight: "700" },
  nativeBody: {
    fontSize: 25,
    lineHeight: 34,
    color: "#203e36",
    fontWeight: "700",
    textAlign: "center",
  },
  error: { color: "#a33e32", fontSize: 14, lineHeight: 21, marginTop: 12 },
});
