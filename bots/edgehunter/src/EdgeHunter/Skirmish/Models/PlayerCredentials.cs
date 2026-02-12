using System.Text.Json;
using System.Text.Json.Serialization;

namespace EdgeHunter.Skirmish.Models;

public class PlayerCredentials
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    [JsonPropertyName("authSecret")]
    public string? AuthSecret { get; set; }

    [JsonPropertyName("botName")]
    public string BotName { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    private const string CredentialsFileName = ".player-credentials.json";

    public static string GetCredentialsPath()
    {
        var dataDir = "/data";
        if (Directory.Exists(dataDir))
        {
            return Path.Combine(dataDir, CredentialsFileName);
        }
        return CredentialsFileName;
    }

    public static async Task<PlayerCredentials?> LoadAsync(string? expectedBotName = null)
    {
        var path = GetCredentialsPath();

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            var credentials = JsonSerializer.Deserialize<PlayerCredentials>(json);

            if (credentials != null && expectedBotName != null && credentials.BotName != expectedBotName)
            {
                return null;
            }

            return credentials;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync()
    {
        var path = GetCredentialsPath();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
    }
}
