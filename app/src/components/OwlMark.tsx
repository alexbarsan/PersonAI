import { Image } from "react-native";

export const owlSource = require("../../assets/brand/dream-dna-owl.png");
export const gardenSource = require("../../assets/brand/dream-garden.png");

export function OwlMark({ size = 48 }: { size?: number }) {
  return (
    <Image
      source={owlSource}
      accessibilityLabel="Dream DNA owl"
      style={{ width: size, height: size }}
      resizeMode="contain"
    />
  );
}
