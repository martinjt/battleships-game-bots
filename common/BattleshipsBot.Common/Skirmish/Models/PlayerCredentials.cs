using System.Text.Json;
using System.Text.Json.Serialization;

namespace BattleshipsBot.Common.Skirmish.Models;

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
        // Use /data if available (Kubernetes persistent volume), otherwise current directory
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

            // Validate that the bot name matches if specified
            if (credentials != null && expectedBotName != null && credentials.BotName != expectedBotName)
            {
                // Bot name changed, don't reuse credentials
                return null;
            }

            return credentials;
        }
        catch
        {
            // If we can't read or parse the file, treat it as if it doesn't exist
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

        // Ensure directory exists
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
    }
}
