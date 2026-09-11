using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Dreams;

public sealed record DreamImagePrompt(string Version, string Text);

public interface IDreamImagePromptComposer
{
    DreamImagePrompt Compose(DreamRecord dream, string style, string version);
}

public sealed class DreamImagePromptComposer : IDreamImagePromptComposer
{
    private const int MaxPromptLength = 512;

    public DreamImagePrompt Compose(DreamRecord dream, string style, string version)
    {
        var summary = DreamMapper.ReadSummary(dream) ?? "A reflective dream scene";
        var prompt = $"A symbolic, reflective dream scene in {DescribeStyle(style)}. {summary}. Dreamlike composition, no text or letters, no identifiable real people, and no explicit sexual or graphic violence.";
        return new DreamImagePrompt(version, prompt.Length <= MaxPromptLength ? prompt : prompt[..MaxPromptLength]);
    }

    private static string DescribeStyle(string style) => style switch
    {
        "3D_ANIMATED_FAMILY_FILM" => "a warm, gentle 3D animated family-film style",
        "DESIGN_SKETCH" => "an expressive hand-drawn design sketch style",
        "FLAT_VECTOR_ILLUSTRATION" => "a clear flat vector illustration style",
        "GRAPHIC_NOVEL_ILLUSTRATION" => "an atmospheric graphic novel illustration style",
        "MAXIMALISM" => "a rich, layered maximalist illustration style",
        "MIDCENTURY_RETRO" => "a restrained midcentury retro illustration style",
        "PHOTOREALISM" => "a cinematic photorealistic style",
        _ => "a soft digital painting style"
    };
}
