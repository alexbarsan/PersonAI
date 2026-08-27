# Catch Dreamer --- Recommended Features

## Product Direction

Catch Dreamer should evolve beyond "dream in → AI interpretation out"
into a **personal map of the user's dreams and recurring patterns over
time**.

## MVP

-   Dream journal: create, edit, delete, search and browse dreams.
-   Optional voice capture/transcription.
-   AI interpretation with structured output:
    -   summary
    -   main and alternative interpretations
    -   emotions
    -   symbols
    -   people, places and objects
    -   themes
    -   lucidity score
    -   nightmare/intensity score
-   "Visualize my dream" image generation.
-   Persist interpretations and images so reopening a dream never
    triggers unnecessary AI calls.

## Dream DNA

Personal analytics across dream history: - Most frequent symbols,
people, places and emotions. - Recurring themes and locations. -
Nightmare/lucidity trends. - Weekly/monthly changes. - Day-of-week
correlations where meaningful. - Automatically discovered clusters such
as flying, childhood, work anxiety, water and recurring locations.

Insights should be presented as patterns/correlations, not causal facts.

## AI Memory

Use previous dreams to personalize new interpretations without sending
the user's entire journal.

Recommended flow: 1. Embed each dream. 2. Store embeddings in PostgreSQL
with pgvector. 3. Retrieve the most relevant previous dreams. 4. Add a
compact Dream DNA/user context summary. 5. Send only this selected
context to the interpretation model.

## Similar Dreams

-   "Find dreams similar to this one."
-   Automatic semantic clustering.
-   Links between recurring people, places, symbols and themes.

## Ask Catch Dreamer

Allow questions over the user's own dream history: - "Why do I keep
dreaming about water?" - "When did I last dream about this place?" -
"What themes became more common recently?" - "Compare this month's
nightmares with last month."

Use retrieval + pgvector rather than placing the entire history in the
prompt.

## Deep Interpretation

Premium analysis using a stronger model and richer context: - current
dream - relevant previous dreams - recurring symbols - emotional
patterns - Dream DNA - voluntarily supplied user context

Limit Deep Interpretations by subscription tier.

## Suggested Plans

### Free

-   3 interpretations/month
-   Dream journal/history
-   Basic search
-   Voice capture
-   One image trial or occasional promotional image
-   Limited analytics

### Premium --- target \~€3.99/month or €29.99/year

-   \~30--50 interpretations/month
-   \~5 images/month
-   Dream DNA
-   Recurring-symbol analysis
-   Similar-dream search
-   AI memory
-   Ask Catch Dreamer
-   Limited Deep Interpretations
-   Journal export

### Pro --- target \~€7.99/month or €59.99/year

-   High interpretation allowance
-   \~20 images/month
-   More Deep Interpretations
-   Full analytics/history features

Tune limits after observing real production usage and conversion.

## Safety & Privacy

-   Treat dream content as potentially sensitive.
-   Encrypt data in transit and at rest.
-   Provide clear export/delete controls.
-   Avoid diagnostic or predictive claims.
-   Interpretations should be framed as reflective possibilities.
-   Add safety handling for self-harm, violence, abuse and other
    sensitive content.
-   Review GDPR implications and AI processing regions for EU users.

## Cost Controls

-   Persist all completed interpretations.
-   Store generated images in S3.
-   Generate images on demand, not automatically.
-   Combine tagging/extraction with the main structured AI response
    where practical.
-   Use cheaper models for background classification.
-   Reserve stronger models for premium analysis.
-   Use embeddings/retrieval instead of full-history prompts.
-   Record provider, model, prompt version, tokens, latency and
    estimated cost for every AI operation.

## Metrics From Day One

-   Cost per MAU / Free MAU / Premium MAU
-   AI cost per interpretation and image
-   Interpretations/images per user
-   Token usage
-   Free-to-paid conversion
-   Retention
-   CAC and LTV
-   Subscription revenue per MAU
-   Gross margin
