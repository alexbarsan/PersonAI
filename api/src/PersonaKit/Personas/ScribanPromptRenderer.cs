using Scriban;
using Scriban.Runtime;

namespace PersonaKit.Personas;

public sealed class ScribanPromptRenderer : IPromptRenderer
{
    public async Task<string> RenderAsync(
        PersonaDefinition persona,
        IReadOnlyDictionary<string, object?> variables,
        CancellationToken cancellationToken = default)
    {
        var templateText = await File.ReadAllTextAsync(persona.PromptTemplatePath, cancellationToken);
        var template = Template.Parse(templateText, persona.PromptTemplatePath);
        if (template.HasErrors)
        {
            throw new PromptRenderingException(string.Join(Environment.NewLine, template.Messages.Select(message => message.ToString())));
        }

        var scriptObject = new ScriptObject();
        foreach (var variable in variables)
        {
            scriptObject.SetValue(variable.Key, variable.Value, readOnly: true);
        }

        var context = new TemplateContext
        {
            StrictVariables = true,
            EnableRelaxedMemberAccess = false,
            EnableRelaxedTargetAccess = false,
            EnableRelaxedFunctionAccess = false
        };
        context.PushGlobal(scriptObject);

        try
        {
            return await template.RenderAsync(context);
        }
        catch (Exception exception)
        {
            throw new PromptRenderingException("Prompt rendering failed.", exception);
        }
    }
}
