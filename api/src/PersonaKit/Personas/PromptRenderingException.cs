namespace PersonaKit.Personas;

public sealed class PromptRenderingException(string message, Exception? innerException = null)
    : Exception(message, innerException);
