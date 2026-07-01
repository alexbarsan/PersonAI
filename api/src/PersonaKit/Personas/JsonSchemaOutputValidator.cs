using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace PersonaKit.Personas;

public sealed class JsonSchemaOutputValidator : IOutputValidator
{
    public async Task<OutputValidationResult> ValidateAsync(
        PersonaDefinition persona,
        string outputJson,
        CancellationToken cancellationToken = default)
    {
        var schemaText = await File.ReadAllTextAsync(persona.OutputSchemaPath, cancellationToken);
        var schema = JsonSchema.FromText(schemaText);

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(outputJson);
        }
        catch (JsonException exception)
        {
            return OutputValidationResult.Invalid([$"Invalid JSON: {exception.Message}"]);
        }

        if (node is null)
        {
            return OutputValidationResult.Invalid(["Output JSON is empty."]);
        }

        var result = schema.Evaluate(node, new EvaluationOptions
        {
            OutputFormat = OutputFormat.Hierarchical,
            RequireFormatValidation = true
        });

        if (result.IsValid)
        {
            return OutputValidationResult.Valid;
        }

        return OutputValidationResult.Invalid(FlattenErrors(result).Distinct(StringComparer.Ordinal).ToArray());
    }

    private static IEnumerable<string> FlattenErrors(EvaluationResults result)
    {
        if (result.HasErrors)
        {
            foreach (var error in result.Errors!)
            {
                yield return $"{result.InstanceLocation}: {error.Key} {error.Value}";
            }
        }

        if (!result.HasDetails)
        {
            yield break;
        }

        foreach (var detail in result.Details)
        {
            foreach (var error in FlattenErrors(detail))
            {
                yield return error;
            }
        }
    }
}
