namespace PersonaKit.Providers.DeepSeek;

public sealed class DeepSeekOptions
{
    public Uri BaseUrl { get; set; } = new("https://api.deepseek.com");

    public string Model { get; set; } = "deepseek-chat";

    public string ApiKey { get; set; } = "";
}
