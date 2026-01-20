using System.Text.Json;
using ContextSync.Models;

namespace ContextSync.Services;

public class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".contextsync");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public Config Load()
    {
        if (!File.Exists(ConfigPath))
            return new Config();

        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<Config>(json) ?? new Config();
    }

    public void Save(Config config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public bool IsInitialized() => !string.IsNullOrEmpty(Load().GitHubToken);
}
