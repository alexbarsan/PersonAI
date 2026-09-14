# Dream DNA User Manual

Dream DNA is a wellness and entertainment app for reflective dream interpretation. It is not medical, psychological, diagnostic, legal, financial, or crisis advice.

## Main Flow

1. Open the app.
2. Sign in. The deployed web app uses Cognito hosted login. In local mock mode, use the mock sign-in button.
3. Complete onboarding and profile details.
4. Review the consent flags. AI processing consent is required before submitting dreams.
5. Open dream capture.
6. Enter your dream description, mood, sleep quality, tags, and date.
7. Optionally choose Voice capture, record a dream note, stop the recording, and choose whether to keep the recording. By default, it is deleted after transcription.
8. Select Transcribe recording and review the transcript inserted into Dream text.
9. Submit the dream.
10. Read the summary and available interpretation details. Empty categories are omitted, and Themes, Scenarios, and Alternative interpretations are not shown on the result page.
11. Select **Helpful** or **Not for me** below the interpretation. A dislike asks what did not work and accepts an optional written detail. You can change previously saved feedback.

## Journal

The journal lists previous dream submissions for the signed-in user. You can open a dream result detail or delete your own dream entry.

## Insights

Insights currently summarize recurring themes and current dream streaks from your journal.

As your journal grows, Dream DNA builds a personal map of recurring symbols, emotions, people, places, scenarios, activity, and guarded timing observations. Results include sample sizes and are reflective patterns, not causes, predictions, or diagnoses.

## Ask Dream DNA

Open **Ask**, enter a question about patterns in your dream history, and select **Ask Dream DNA**. The app retrieves a small set of relevant dreams and shows the answer, observations, and the journal entries used as evidence. AI processing and dream-history consent must be enabled. If your semantic memory has not been indexed yet, the app will ask you to try again later. Free and Premium daily limits apply.

## Premium

The Premium screen shows planned paid-tier benefits. In the current local build, purchases are not connected yet. Free users have a lower daily dream limit, and Premium entitlement support is available for testing through mock/configured state.

Voice transcription is a Premium feature. A recording can be up to three minutes and is subject to a daily cap. The transcript is retained as journal data. The audio recording is deleted after transcription unless you explicitly turn on Keep recording after transcription; retained audio remains private and is available only through a short-lived link.

Free users receive an interpretation generated with the standard model. Premium users receive their main interpretation directly from the stronger Premium model, without first generating the Free interpretation.

On a completed dream, **Cognitive Analysis** offers an optional cognitive and psychological reading focused on possible thought patterns, attention, agency, uncertainty, and emotional responses. It shows the focused interpretation and a short list of evidence-linked cognitive symbols from the dream. It uses relevant journal context only when dream-history consent is enabled, is saved after the first request, and has a daily Premium limit. Free users see **Cognitive Analysis** as a link to the Premium screen.

The landing page includes a daily dream thought. While an interpretation or Cognitive Analysis is being prepared, Dream DNA shows a few short facts about dreaming.

## Privacy Notes

Dream DNA sends a pseudonymized context snapshot to the AI provider. It does not send your name, email, device id, IP address, or Cognito subject. Sensitive traits are included only when consent allows it.

## Administration

Authorized administrators can open **Profile**, then **Open operations**. The Health view shows queue state, delayed or failed work, provider latency, and estimated AI cost. Requeue and acknowledgement actions require a written reason and are audited.

The Dreams view is available only to privacy administrators. Search results show bounded metadata. Selecting a dream and choosing **View original dream** reveals the original text, interpretations, and generated images, and creates an immutable access audit automatically.

## Local Mock Mode

The Expo app defaults to mock mode for local development. Mock mode lets you use the app without AWS, Cognito, PostgreSQL, or DeepSeek.
