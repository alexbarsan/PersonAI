export type OAuthSession = { idToken: string };

export function hasOAuthCallbackError(error: string | string[] | undefined) {
  return typeof error === "string" ? error.length > 0 : Array.isArray(error) && error.some((value) => value.length > 0);
}

export async function waitForOAuthSession(
  readSession: () => Promise<OAuthSession | null>,
  maxAttempts = 30,
  delayMs = 500,
  pause: (milliseconds: number) => Promise<void> = delay,
) {
  for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
    try {
      const session = await readSession();
      if (session) return session;
    } catch {
      // Amplify can still be exchanging the callback code on the first read.
    }
    if (attempt + 1 < maxAttempts) await pause(delayMs);
  }
  return null;
}

function delay(milliseconds: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, milliseconds));
}
