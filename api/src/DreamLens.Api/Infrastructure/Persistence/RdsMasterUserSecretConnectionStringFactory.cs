using System.Text.Json;
using Npgsql;

namespace DreamLens.Api.Infrastructure.Persistence;

public static class RdsMasterUserSecretConnectionStringFactory
{
    private const string SecretConfigKey = "Database:MasterUserJson";

    public static string? Create(IConfiguration configuration)
    {
        var secretJson = configuration[SecretConfigKey];
        if (string.IsNullOrWhiteSpace(secretJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(secretJson);
        var root = document.RootElement;

        var username = ReadString(root, "username")
            ?? throw new InvalidOperationException($"{SecretConfigKey} must include username.");
        var password = ReadString(root, "password")
            ?? throw new InvalidOperationException($"{SecretConfigKey} must include password.");

        var fallbackEndpoint = ParseEndpoint(configuration["ConnectionStrings:Host"]);
        var host = ReadString(root, "host") ?? fallbackEndpoint.Host
            ?? throw new InvalidOperationException($"{SecretConfigKey} must include host or ConnectionStrings:Host must be configured.");
        var port = ReadInt(root, "port") ?? fallbackEndpoint.Port ?? 5432;
        var database = ReadString(root, "dbname")
            ?? ReadString(root, "database")
            ?? configuration["ConnectionStrings:Database"]
            ?? "dreamlens";

        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            SslMode = SslMode.Prefer
        }.ConnectionString;
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(property.GetString(), out var value) => value,
            _ => null
        };
    }

    private static (string? Host, int? Port) ParseEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return (null, null);
        }

        var separatorIndex = endpoint.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == endpoint.Length - 1)
        {
            return (endpoint, null);
        }

        var host = endpoint[..separatorIndex];
        return int.TryParse(endpoint[(separatorIndex + 1)..], out var port)
            ? (host, port)
            : (endpoint, null);
    }
}
