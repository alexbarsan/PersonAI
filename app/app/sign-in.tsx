import { useLocalSearchParams } from "expo-router";
import { SignInScreen } from "@/features/auth/SignInScreen";

export default function SignInRoute() {
  const { error } = useLocalSearchParams<{ error?: string }>();
  return <SignInScreen oauthError={error === "oauth"} />;
}
