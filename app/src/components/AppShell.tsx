import { PropsWithChildren } from "react";
import { Link, usePathname } from "expo-router";
import {
  Platform,
  Pressable,
  StyleSheet,
  useWindowDimensions,
  View,
} from "react-native";
import BookOpen from "lucide-react-native/icons/book-open";
import House from "lucide-react-native/icons/house";
import Map from "lucide-react-native/icons/map";
import MessageCircle from "lucide-react-native/icons/message-circle";
import Plus from "lucide-react-native/icons/plus";
import UserRound from "lucide-react-native/icons/user-round";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useTheme } from "@/theme/ThemeProvider";
import { OwlMark } from "@/components/OwlMark";
import { Text } from "@/components/Text";

const destinations = [
  { href: "/", label: "Today", icon: House },
  { href: "/journal", label: "Journal", icon: BookOpen },
  { href: "/insights", label: "Map", icon: Map },
  { href: "/ask", label: "Ask", icon: MessageCircle },
  { href: "/profile", label: "Profile", icon: UserRound },
] as const;

export function AppShell({
  children,
  showNavigation = true,
}: PropsWithChildren<{ showNavigation?: boolean }>) {
  const theme = useTheme();
  const pathname = usePathname?.() ?? "/";
  const { width } = useWindowDimensions();
  const insets = useSafeAreaInsets();
  const desktop = Platform.OS === "web" && width >= 1000;
  return (
    <View
      style={[
        styles.shell,
        { backgroundColor: theme.colors.background, paddingTop: insets.top },
        desktop && styles.desktop,
      ]}
    >
      {showNavigation && desktop ? (
        <View
          style={[
            styles.sidebar,
            {
              backgroundColor: theme.colors.surface,
              borderColor: theme.colors.border,
            },
          ]}
        >
          <BrandMark />
          <Text
            style={[styles.sidebarCaption, { color: theme.colors.mutedText }]}
          >
            Your inner world, remembered.
          </Text>
          <Link href="/dreams/capture" asChild>
            <Pressable
              accessibilityLabel="Capture a dream"
              accessibilityRole="link"
              style={StyleSheet.flatten([styles.capture, { backgroundColor: theme.colors.primary }])}
            >
              <Plus size={18} color="white" />
              <Text style={styles.captureText}>Capture a dream</Text>
            </Pressable>
          </Link>
          <View accessibilityLabel="Primary navigation" style={styles.sideLinks}>
            {destinations.map(({ href, label, icon: Icon }) => {
              const active =
                href === "/" ? pathname === "/" : pathname.startsWith(href);
              return <Link key={href} href={href} asChild><Pressable
                accessibilityLabel={label}
                accessibilityRole="link"
                accessibilityState={{ selected: active }}
                style={StyleSheet.flatten([
                  styles.sideItem,
                  active && { backgroundColor: theme.colors.sage },
                ])}
              >
                <Icon
                  size={21}
                  color={
                    active ? theme.colors.primary : theme.colors.mutedText
                  }
                />
                <Text
                  style={[
                    styles.sideLabel,
                    {
                      color: active
                        ? theme.colors.primary
                        : theme.colors.mutedText,
                    },
                  ]}
                >
                  {label}
                </Text>
              </Pressable></Link>;
            })}
          </View>
          <View style={styles.sidebarFoot}>
            <Link href="/" asChild>
              <Pressable
                accessibilityRole="link"
                accessibilityLabel="Dream DNA home"
              >
                <OwlMark size={76} />
              </Pressable>
            </Link>
            <Text
              style={[styles.sidebarCaption, { color: theme.colors.mutedText }]}
            >
              A little reflection, every day.
            </Text>
          </View>
        </View>
      ) : null}
      <View style={[styles.canvas, desktop && styles.desktopCanvas]}>
        <View style={styles.content}>{children}</View>
        {showNavigation && !desktop ? (
          <View
            accessibilityLabel="Primary navigation"
            style={[
              styles.navigation,
              {
                backgroundColor: theme.colors.surface,
                borderColor: theme.colors.border,
                paddingBottom: Math.max(8, insets.bottom),
              },
            ]}
          >
            {destinations.map(({ href, label, icon: Icon }) => {
              const active =
                href === "/" ? pathname === "/" : pathname.startsWith(href);
              return <Link key={href} href={href} asChild><Pressable
                accessibilityLabel={label}
                accessibilityRole="link"
                accessibilityState={{ selected: active }}
                style={styles.navItem}
              >
                <View
                  style={[
                    styles.navIcon,
                    active && { backgroundColor: theme.colors.sage },
                  ]}
                >
                  <Icon
                    size={21}
                    strokeWidth={active ? 2.3 : 1.7}
                    color={
                      active ? theme.colors.primary : theme.colors.mutedText
                    }
                  />
                </View>
                <Text
                  style={[
                    styles.navLabel,
                    {
                      color: active
                        ? theme.colors.primary
                        : theme.colors.mutedText,
                    },
                  ]}
                >
                  {label}
                </Text>
              </Pressable></Link>;
            })}
          </View>
        ) : null}
      </View>
    </View>
  );
}

export function BrandMark({ detail }: { detail?: string }) {
  const theme = useTheme();
  return (
    <Link href="/" asChild>
      <Pressable
        accessibilityRole="link"
        accessibilityLabel="Dream DNA home"
        style={styles.brandMark}
      >
        <OwlMark />
        <View style={styles.brandWords}>
          <Text style={[styles.brand, { color: theme.colors.text }]}>
            {theme.appName}
          </Text>
          {detail ? (
            <Text
              style={[styles.brandDetail, { color: theme.colors.mutedText }]}
            >
              {detail}
            </Text>
          ) : null}
        </View>
      </Pressable>
    </Link>
  );
}

const styles = StyleSheet.create({
  shell: { flex: 1 },
  desktop: { flexDirection: "row" },
  canvas: {
    alignSelf: "center",
    flex: 1,
    maxWidth: 820,
    width: "100%",
    minHeight: 0,
  },
  desktopCanvas: {
    alignSelf: "stretch",
    maxWidth: 1160,
    minWidth: 0,
    paddingHorizontal: 28,
    paddingTop: 16,
  },
  content: { flex: 1, minHeight: 0 },
  sidebar: {
    width: 248,
    borderRightWidth: 1,
    paddingHorizontal: 24,
    paddingVertical: 30,
  },
  sidebarCaption: { fontSize: 13, lineHeight: 20, marginTop: 8 },
  capture: {
    flexDirection: "row",
    gap: 9,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 8,
    minHeight: 48,
    marginVertical: 28,
  },
  captureText: { color: "white", fontWeight: "700", fontSize: 14 },
  sideLinks: { gap: 8 },
  sideItem: {
    flexDirection: "row",
    alignItems: "center",
    minHeight: 50,
    borderRadius: 8,
    gap: 14,
    paddingHorizontal: 16,
  },
  sideLabel: { fontSize: 15, fontWeight: "700" },
  sidebarFoot: { marginTop: "auto", paddingTop: 30, alignItems: "center" },
  navigation: {
    borderTopWidth: 1,
    flexDirection: "row",
    paddingHorizontal: 8,
    paddingTop: 7,
  },
  navItem: {
    alignItems: "center",
    flex: 1,
    minWidth: 0,
    minHeight: 52,
    gap: 2,
  },
  navIcon: {
    width: 48,
    height: 30,
    borderRadius: 8,
    alignItems: "center",
    justifyContent: "center",
  },
  navLabel: { fontSize: 11, fontWeight: "700" },
  brandMark: { flexDirection: "row", alignItems: "center", gap: 9 },
  brandWords: { flexShrink: 1, gap: 2 },
  brand: { fontSize: 21, fontWeight: "700" },
  brandDetail: { fontSize: 12, lineHeight: 18 },
});
