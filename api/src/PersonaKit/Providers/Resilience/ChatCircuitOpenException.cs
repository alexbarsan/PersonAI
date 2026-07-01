namespace PersonaKit.Providers.Resilience;

public sealed class ChatCircuitOpenException(Exception innerException)
    : Exception("Chat provider circuit is open.", innerException);
