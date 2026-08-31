import { PropsWithChildren } from "react";
import { router, usePathname } from "expo-router";
import { Pressable, StyleSheet, Text, View } from "react-native";

import { useTheme } from "@/theme/ThemeProvider";

type AppShellProps = PropsWithChildren<{
  showNavigation?: boolean;
}>;

const destinations = [
  { href: "/", label: "Today" },
  { href: "/journal", label: "Journal" },
  { href: "/insights", label: "Map" },
  { href: "/profile", label: "Profile" }
] as const;

export function AppShell({ children, showNavigation = true }: AppShellProps) {
  const theme = useTheme();
  const pathname = usePathname?.() ?? "/";

  return (
    <View style={{ ...styles.safeArea, backgroundColor: theme.colors.background }}>
      <View style={styles.canvas}>
        <View style={styles.content}>{children}</View>
        {showNavigation ? (
          <View style={{ ...styles.navigation, backgroundColor: theme.colors.surface, borderColor: theme.colors.border }}>
            {destinations.map((destination) => {
              const active = destination.href === "/" ? pathname === "/" : pathname.startsWith(destination.href);
              return <Pressable key={destination.href} accessibilityLabel={destination.label} accessibilityRole="button" onPress={() => router.push(destination.href)} style={active ? { ...styles.navItem, backgroundColor: theme.colors.softInk } : styles.navItem}><Text style={{ ...styles.navLabel, color: active ? theme.colors.text : theme.colors.mutedText }}>{destination.label}</Text></Pressable>;
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
    <View style={styles.brandMark}>
      <Text style={{ ...styles.brand, color: theme.colors.text }}>Dream DNA</Text>
      {detail ? <Text style={{ ...styles.brandDetail, color: theme.colors.mutedText }}>{detail}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  safeArea: { flex: 1 },
  canvas: { alignSelf: "center", flex: 1, maxWidth: 680, width: "100%" },
  content: { flex: 1 },
  navigation: { borderTopWidth: 1, flexDirection: "row", gap: 4, paddingHorizontal: 12, paddingVertical: 8 },
  navItem: { alignItems: "center", borderRadius: 6, flex: 1, justifyContent: "center", minHeight: 40, paddingHorizontal: 4 },
  navLabel: { fontSize: 12, fontWeight: "700" },
  brandMark: { gap: 2 },
  brand: { fontSize: 18, fontWeight: "800", letterSpacing: 0 },
  brandDetail: { fontSize: 12, lineHeight: 17 }
});
