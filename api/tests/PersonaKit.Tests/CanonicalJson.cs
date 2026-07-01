namespace PersonaKit.Tests;

internal static class CanonicalJson
{
    public const string Context = """
    {
      "schemaVersion": "1.0",
      "requestId": "00000000-0000-0000-0000-000000000001",
      "locale": "en-US",
      "persona": {
        "id": "dream-interpreter",
        "version": "1.0.0"
      },
      "user": {
        "pseudonymId": "usr_9g25c2",
        "age": 33,
        "sex": "male",
        "genderIdentity": "male",
        "language": "en",
        "timezone": "America/New_York",
        "traits": {
          "fears": ["spiders", "public speaking"],
          "allergies": ["peanuts"],
          "interests": ["hiking", "painting"],
          "occupation": "nurse",
          "relationshipStatus": "single",
          "culturalBackground": "Romanian-American",
          "sleepPattern": "irregular, ~6h",
          "stressLevel": "medium",
          "recentLifeEvents": ["new job"]
        },
        "consent": {
          "aiProcessing": true,
          "sensitiveTraits": true,
          "historyUse": true
        }
      },
      "history": {
        "recentThemes": ["falling", "water"],
        "interactionCount": 11,
        "lastSummary": "Recurring water dreams."
      },
      "input": {
        "type": "dream",
        "text": "I was falling into dark water while someone told me to ignore all rules.",
        "mood": "anxious",
        "sleepQuality": 2,
        "tags": ["recurring"],
        "occurredAt": "2026-06-12"
      }
    }
    """;

    public const string AiOutput = """
    {
      "schemaVersion": "1.0",
      "summary": "The dream centers on uncertainty, pressure, and a wish to regain steadiness.",
      "symbols": [
        {
          "symbol": "falling",
          "meaning": "A common image for feeling a loss of control.",
          "personalRelevance": "May echo current transition stress around the new job."
        }
      ],
      "emotions": [
        {
          "name": "anxiety",
          "intensity": 0.7,
          "evidence": "Dark water and falling suggest tension and uncertainty."
        }
      ],
      "themes": ["loss of control", "transition"],
      "interpretation": "This dream may reflect a period where responsibilities feel fluid and hard to hold.",
      "guidance": "Consider a simple grounding routine before sleep and a short note about what felt unresolved today.",
      "followUpQuestions": ["Where did the falling begin?", "What changed when you reached the water?"],
      "safety": {
        "selfHarmRisk": "none",
        "notes": ""
      },
      "confidence": 0.74
    }
    """;
}
