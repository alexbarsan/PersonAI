import { StyleSheet, Text as NativeText, TextProps } from "react-native";

export function Text({ style, ...props }: TextProps) {
  const weight = StyleSheet.flatten(style)?.fontWeight;
  const bold = weight === "bold" || Number(weight) >= 600;
  return (
    <NativeText
      {...props}
      style={[
        {
          fontFamily: bold ? "Nunito_700Bold" : "Nunito_400Regular",
          letterSpacing: 0,
        },
        style,
        { fontWeight: "normal" },
      ]}
    />
  );
}
