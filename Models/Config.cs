namespace ContextSync.Models;

public class Config
{
    public string? GitHubToken { get; set; }
    public string? ManifestGistId { get; set; }
}

public class Manifest
{
    public Dictionary<string, string> ProjectMappings { get; set; } = new();
}
