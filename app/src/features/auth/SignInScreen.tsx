import { useState } from "react";
import { Link, router } from "expo-router";
import {
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from "react-native";
import Apple from "lucide-react-native/icons/apple";
import ArrowLeft from "lucide-react-native/icons/arrow-left";
import Eye from "lucide-react-native/icons/eye";
import EyeOff from "lucide-react-native/icons/eye-off";
import Mail from "lucide-react-native/icons/mail";
import { useAuthStore } from "@/auth/authStore";
import {
  confirmCognitoSignIn,
  confirmCognitoSignUp,
  finishCognitoPasswordReset,
  readCognitoSession,
  signInWithCognitoPassword,
  signUpWithCognitoPassword,
  startCognitoPasswordReset,
  startCognitoSocialSignIn,
  type SocialProvider,
} from "@/auth/cognitoAmplify";
import { createUserFromToken } from "@/auth/cognitoAuth";
import { Text } from "@/components/Text";
import { OwlMark } from "@/components/OwlMark";
import { appConfig } from "@/core/config";
import { useTheme } from "@/theme/ThemeProvider";

type FormMode = "sign-in" | "sign-up" | "confirm-sign-up" | "forgot" | "confirm-reset" | "challenge";

export function SignInScreen({ oauthError = false }: { oauthError?: boolean }) {
  const theme = useTheme();
  const setSession = useAuthStore((state) => state.setSession);
  const [mode, setMode] = useState<FormMode>("sign-in");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmationCode, setConfirmationCode] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(
    oauthError ? "Sign-in was not completed. You can try again." : null,
  );
  const [error, setError] = useState<string | null>(null);

  const completeSession = async () => {
    const session = await readCognitoSession();
    if (!session) throw new Error("No signed-in session was returned.");
    setSession(session.idToken, createUserFromToken(session.idToken));
    router.replace("/");
  };

  const run = async (action: () => Promise<void>) => {
    setPending(true);
    setError(null);
    setMessage(null);
    try {
      await action();
    } catch (cause) {
      setError(authErrorMessage(cause));
    } finally {
      setPending(false);
    }
  };

  const submit = () => run(async () => {
    if (mode === "sign-in") {
      const result = await signInWithCognitoPassword(email, password);
      if (result.isSignedIn) {
        await completeSession();
      } else if (result.nextStep.signInStep === "CONFIRM_SIGN_UP") {
        setMode("confirm-sign-up");
        setMessage("Enter the verification code sent to your email.");
      } else {
        setMode("challenge");
        setMessage(challengeMessage(result.nextStep.signInStep));
      }
      return;
    }

    if (mode === "sign-up") {
      const result = await signUpWithCognitoPassword(email, password);
      if (result.nextStep.signUpStep === "CONFIRM_SIGN_UP") {
        setMode("confirm-sign-up");
        setMessage("Enter the verification code sent to your email.");
      } else {
        setMode("sign-in");
        setMessage("Your account is ready. Sign in to continue.");
      }
      return;
    }

    if (mode === "confirm-sign-up") {
      await confirmCognitoSignUp(email, confirmationCode);
      setMode("sign-in");
      setPassword("");
      setMessage("Email verified. Sign in to continue.");
      return;
    }

    if (mode === "forgot") {
      const result = await startCognitoPasswordReset(email);
      if (result.nextStep.resetPasswordStep === "CONFIRM_RESET_PASSWORD_WITH_CODE") {
        setMode("confirm-reset");
        setMessage("Enter the reset code sent to your email and choose a new password.");
      } else {
        setMessage("Follow the instructions sent to your email.");
      }
      return;
    }

    if (mode === "confirm-reset") {
      await finishCognitoPasswordReset(email, confirmationCode, password);
      setMode("sign-in");
      setPassword("");
      setConfirmationCode("");
      setMessage("Password updated. Sign in with your new password.");
      return;
    }

    const result = await confirmCognitoSignIn(confirmationCode);
    if (result.isSignedIn) {
      await completeSession();
    } else {
      setMessage(challengeMessage(result.nextStep.signInStep));
      setConfirmationCode("");
    }
  });

  const changeMode = (next: FormMode) => {
    setMode(next);
    setError(null);
    setMessage(null);
    setConfirmationCode("");
  };

  const social = (provider: SocialProvider) => run(async () => {
    await startCognitoSocialSignIn(provider);
  });

  const needsCode = mode === "confirm-sign-up" || mode === "confirm-reset" || mode === "challenge";
  const needsPassword = mode === "sign-in" || mode === "sign-up" || mode === "confirm-reset";
  const title = mode === "sign-in" ? "Welcome back" : mode === "sign-up" ? "Create your journal" : mode === "forgot" ? "Reset your password" : mode === "confirm-sign-up" ? "Check your email" : mode === "confirm-reset" ? "Choose a new password" : "Verify your sign-in";

  return (
    <ScrollView contentContainerStyle={s.page} keyboardShouldPersistTaps="handled">
      <KeyboardAvoidingView behavior={Platform.OS === "ios" ? "padding" : undefined} style={s.frame}>
        <Link href="/" asChild>
          <Pressable accessibilityRole="link" style={s.back}>
            <ArrowLeft size={17} color={theme.colors.primary} />
            <Text style={[s.backLabel, { color: theme.colors.primary }]}>Dream DNA</Text>
          </Pressable>
        </Link>

        <View style={s.form}>
          <OwlMark size={54} />
          <Text accessibilityRole="header" style={s.title}>{title}</Text>
          <Text style={s.subtitle}>
            {mode === "sign-up" ? "Start with an email you can access." : "Your dream journal is waiting for you."}
          </Text>

          {mode === "sign-in" || mode === "sign-up" ? (
            <>
              <View style={[s.providerStack, { opacity: pending ? 0.6 : 1 }]}>
                {appConfig.cognitoGoogleEnabled ? (
                  <Pressable accessibilityRole="button" disabled={pending} onPress={() => social("Google")} style={[s.providerButton, { borderColor: theme.colors.border }]}>
                    <Text style={s.googleMark}>G</Text>
                    <Text style={s.providerText}>Continue with Google</Text>
                  </Pressable>
                ) : null}
                <Pressable
                  accessibilityRole="button"
                  accessibilityState={{ disabled: !appConfig.cognitoAppleEnabled || pending }}
                  disabled={!appConfig.cognitoAppleEnabled || pending}
                  onPress={() => social("Apple")}
                  style={[s.providerButton, s.appleButton, !appConfig.cognitoAppleEnabled && s.disabledProvider]}
                >
                  <Apple size={19} color={appConfig.cognitoAppleEnabled ? "white" : "#626b66"} />
                  <Text style={[s.providerText, appConfig.cognitoAppleEnabled && s.appleText]}>
                    {appConfig.cognitoAppleEnabled ? "Continue with Apple" : "Apple sign-in coming soon"}
                  </Text>
                </Pressable>
              </View>
              <View style={s.divider}><View style={s.rule} /><Text style={s.or}>or use email</Text><View style={s.rule} /></View>
            </>
          ) : null}

          <View style={s.field}>
            <Text style={s.label}>Email</Text>
            <View style={[s.inputRow, { borderColor: theme.colors.border }]}>
              <Mail size={17} color={theme.colors.mutedText} />
              <TextInput
                accessibilityLabel="Email"
                autoCapitalize="none"
                autoComplete="email"
                keyboardType="email-address"
                onChangeText={setEmail}
                placeholder="you@example.com"
                placeholderTextColor={theme.colors.mutedText}
                value={email}
                style={s.input}
              />
            </View>
          </View>

          {needsPassword ? (
            <View style={s.field}>
              <Text style={s.label}>{mode === "confirm-reset" ? "New password" : "Password"}</Text>
              <View style={[s.inputRow, { borderColor: theme.colors.border }]}>
                <TextInput
                  accessibilityLabel={mode === "confirm-reset" ? "New password" : "Password"}
                  autoCapitalize="none"
                  autoComplete={mode === "sign-up" ? "new-password" : "current-password"}
                  onChangeText={setPassword}
                  placeholder={mode === "sign-in" ? "Your password" : "At least 6 characters, with upper/lowercase, number and symbol"}
                  placeholderTextColor={theme.colors.mutedText}
                  secureTextEntry={!showPassword}
                  value={password}
                  style={s.input}
                />
                <Pressable accessibilityRole="button" accessibilityLabel={showPassword ? "Hide password" : "Show password"} onPress={() => setShowPassword(!showPassword)} hitSlop={8}>
                  {showPassword ? <EyeOff size={18} color={theme.colors.mutedText} /> : <Eye size={18} color={theme.colors.mutedText} />}
                </Pressable>
              </View>
            </View>
          ) : null}

          {needsCode ? (
            <View style={s.field}>
              <Text style={s.label}>{mode === "challenge" ? "Verification code" : "Email verification code"}</Text>
              <TextInput
                accessibilityLabel="Verification code"
                autoComplete="one-time-code"
                keyboardType="number-pad"
                onChangeText={setConfirmationCode}
                placeholder="Enter code"
                placeholderTextColor={theme.colors.mutedText}
                value={confirmationCode}
                style={[s.codeInput, { borderColor: theme.colors.border }]}
              />
            </View>
          ) : null}

          {mode === "sign-in" ? (
            <Pressable accessibilityRole="button" onPress={() => changeMode("forgot")} style={s.forgotAction}>
              <Text style={[s.linkText, { color: theme.colors.primary }]}>Forgot password?</Text>
            </Pressable>
          ) : null}

          {message ? <Text accessibilityLiveRegion="polite" style={s.message}>{message}</Text> : null}
          {error ? <Text accessibilityRole="alert" style={s.error}>{error}</Text> : null}

          <Pressable
            accessibilityRole="button"
            disabled={pending || !email.trim() || (needsPassword && !password) || (needsCode && !confirmationCode.trim())}
            onPress={submit}
            style={[s.submit, { backgroundColor: theme.colors.primary }, (pending || !email.trim() || (needsPassword && !password) || (needsCode && !confirmationCode.trim())) && s.submitDisabled]}
          >
            <Text style={s.submitText}>{pending ? "Please wait..." : submitLabel(mode)}</Text>
          </Pressable>

          {mode === "sign-in" || mode === "sign-up" ? (
            <View style={s.modeSwitch}>
              <Text style={s.modeCopy}>{mode === "sign-in" ? "New to Dream DNA?" : "Already have an account?"}</Text>
              <Pressable accessibilityRole="button" onPress={() => changeMode(mode === "sign-in" ? "sign-up" : "sign-in")}>
                <Text style={[s.linkText, { color: theme.colors.primary }]}>{mode === "sign-in" ? "Create account" : "Sign in"}</Text>
              </Pressable>
            </View>
          ) : (
            <Pressable accessibilityRole="button" onPress={() => changeMode("sign-in")} style={s.modeSwitch}>
              <Text style={[s.linkText, { color: theme.colors.primary }]}>Back to sign in</Text>
            </Pressable>
          )}
        </View>
      </KeyboardAvoidingView>
    </ScrollView>
  );
}

function submitLabel(mode: FormMode) {
  if (mode === "sign-in") return "Sign in";
  if (mode === "sign-up") return "Create account";
  if (mode === "confirm-sign-up") return "Verify email";
  if (mode === "forgot") return "Send reset code";
  if (mode === "confirm-reset") return "Update password";
  return "Continue";
}

function challengeMessage(step: string) {
  if (step === "CONFIRM_SIGN_IN_WITH_TOTP_CODE") return "Enter the code from your authenticator app.";
  if (step === "CONFIRM_SIGN_IN_WITH_SMS_CODE") return "Enter the code sent to your phone.";
  if (step === "CONFIRM_SIGN_IN_WITH_EMAIL_CODE") return "Enter the code sent to your email.";
  return "Complete the verification code challenge to continue.";
}

export function authErrorMessage(cause: unknown) {
  const name = cause instanceof Error ? cause.name : "";
  if (name === "NotAuthorizedException" || name === "UserNotFoundException") return "Email or password is incorrect.";
  if (name === "UsernameExistsException") return "An account already exists for this email. Try signing in instead.";
  if (name === "CodeMismatchException" || name === "ExpiredCodeException") return "That code is invalid or expired. Request a new one and try again.";
  if (name === "LimitExceededException" || name === "TooManyRequestsException") return "Too many attempts. Wait a little and try again.";
  if (name === "NetworkError" || name === "TypeError") return "Could not reach the sign-in service. Check your connection and try again.";
  return "We could not complete sign-in. Check your details and try again.";
}

const s = StyleSheet.create({
  page: { flexGrow: 1, minHeight: "100%", justifyContent: "center", padding: 24, backgroundColor: "#f7faf7" },
  frame: { width: "100%", maxWidth: 520, alignSelf: "center" },
  back: { minHeight: 44, flexDirection: "row", alignItems: "center", gap: 8, alignSelf: "flex-start", marginBottom: 24 },
  backLabel: { fontSize: 14, fontWeight: "700" },
  form: { padding: 28, gap: 18, backgroundColor: "white", borderWidth: 1, borderColor: "#e1e9e3", borderRadius: 8 },
  title: { color: "#203e36", fontSize: 28, lineHeight: 36, fontWeight: "700", marginTop: 2 },
  subtitle: { color: "#596e67", fontSize: 14, lineHeight: 21, marginTop: -10 },
  providerStack: { gap: 10 },
  providerButton: { minHeight: 48, borderWidth: 1, borderRadius: 6, flexDirection: "row", alignItems: "center", justifyContent: "center", gap: 10, paddingHorizontal: 16 },
  googleMark: { fontSize: 18, lineHeight: 22, color: "#4285f4", fontWeight: "700" },
  providerText: { color: "#25342d", fontSize: 14, fontWeight: "700" },
  appleButton: { backgroundColor: "#121512", borderColor: "#121512" },
  appleText: { color: "white" },
  disabledProvider: { backgroundColor: "#f2f5f2", borderColor: "#e1e9e3" },
  divider: { flexDirection: "row", alignItems: "center", gap: 12 },
  rule: { flex: 1, height: 1, backgroundColor: "#e5ebe6" },
  or: { color: "#75847b", fontSize: 12 },
  field: { gap: 7 },
  label: { color: "#30443a", fontSize: 13, fontWeight: "700" },
  inputRow: { minHeight: 48, borderWidth: 1, borderRadius: 6, paddingHorizontal: 13, flexDirection: "row", alignItems: "center", gap: 10 },
  input: { flex: 1, minWidth: 0, color: "#203e36", fontSize: 15, paddingVertical: 12 },
  codeInput: { minHeight: 48, borderWidth: 1, borderRadius: 6, paddingHorizontal: 13, color: "#203e36", fontSize: 16 },
  forgotAction: { minHeight: 32, alignSelf: "flex-end", justifyContent: "center", marginTop: -12 },
  linkText: { fontSize: 13, fontWeight: "700" },
  message: { color: "#426453", fontSize: 13, lineHeight: 19 },
  error: { color: "#a33e32", fontSize: 13, lineHeight: 19 },
  submit: { minHeight: 49, borderRadius: 6, alignItems: "center", justifyContent: "center", paddingHorizontal: 16, marginTop: 2 },
  submitDisabled: { opacity: 0.55 },
  submitText: { color: "white", fontSize: 14, fontWeight: "700" },
  modeSwitch: { minHeight: 40, flexDirection: "row", justifyContent: "center", alignItems: "center", gap: 6 },
  modeCopy: { color: "#596e67", fontSize: 13 },
});
