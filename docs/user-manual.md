# DreamLens User Manual

DreamLens is a wellness and entertainment app for reflective dream interpretation. It is not medical, psychological, diagnostic, legal, financial, or crisis advice.

## Main Flow

1. Open the app.
2. Sign in. In local mock mode, use the mock sign-in button.
3. Complete onboarding and profile details.
4. Review the consent flags. AI processing consent is required before submitting dreams.
5. Open dream capture.
6. Enter your dream description, mood, sleep quality, tags, and date.
7. Submit the dream.
8. Read the result sections: summary, symbols, emotions, themes, interpretation, guidance, and follow-up questions.

## Journal

The journal lists previous dream submissions for the signed-in user. You can open a dream result detail or delete your own dream entry.

## Insights

Insights summarize recurring themes and current dream streaks from your journal.

## Privacy Notes

DreamLens sends a pseudonymized context snapshot to the AI provider. It does not send your name, email, device id, IP address, or Cognito subject. Sensitive traits are included only when consent allows it.

## Local Mock Mode

The Expo app defaults to mock mode for local development. Mock mode lets you use the app without AWS, Cognito, PostgreSQL, or DeepSeek.
