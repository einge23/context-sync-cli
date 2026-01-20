using System.Text.Json;
using ContextSync.Models;
using Octokit;

namespace ContextSync.Services;

public class GistService
{
    private readonly GitHubClient _client;
    private readonly ConfigService _configService;
    private const string ManifestFileName = "contextsync-manifest.json";

    public GistService(string token, ConfigService configService)
    {
        _client = new GitHubClient(new ProductHeaderValue("ContextSync"))
        {
            Credentials = new Credentials(token)
        };
        _configService = configService;
    }

    public async Task<Manifest> GetOrCreateManifest()
    {
        var config = _configService.Load();

        if (!string.IsNullOrEmpty(config.ManifestGistId))
        {
            try
            {
                var gist = await _client.Gist.Get(config.ManifestGistId);
                if (gist.Files.TryGetValue(ManifestFileName, out var file))
                    return JsonSerializer.Deserialize<Manifest>(file.Content) ?? new Manifest();
            }
            catch { /* Gist deleted, recreate */ }
        }

        // Create new manifest gist
        var newGist = new NewGist { Description = "ContextSync Manifest", Public = false };
        newGist.Files.Add(ManifestFileName, JsonSerializer.Serialize(new Manifest()));
        var created = await _client.Gist.Create(newGist);

        config.ManifestGistId = created.Id;
        _configService.Save(config);

        return new Manifest();
    }

    public async Task SaveManifest(Manifest manifest)
    {
        var config = _configService.Load();
        if (string.IsNullOrEmpty(config.ManifestGistId)) return;

        var update = new GistUpdate();
        update.Files.Add(ManifestFileName, new GistFileUpdate
        {
            Content = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true })
        });
        await _client.Gist.Edit(config.ManifestGistId, update);
    }

    public async Task<string> CreateProjectGist(Dictionary<string, string> files)
    {
        var gist = new NewGist { Description = "ContextSync Project Context", Public = false };
        foreach (var (name, content) in files)
            gist.Files.Add(name, content);

        var created = await _client.Gist.Create(gist);
        return created.Id;
    }

    public async Task UpdateProjectGist(string gistId, Dictionary<string, string> files)
    {
        var update = new GistUpdate();

        // Get existing files to handle deletions
        var existing = await _client.Gist.Get(gistId);
        foreach (var file in existing.Files.Keys)
        {
            if (!files.ContainsKey(file))
                update.Files.Add(file, new GistFileUpdate { Content = null }); // Delete
        }

        foreach (var (name, content) in files)
            update.Files.Add(name, new GistFileUpdate { Content = content });

        await _client.Gist.Edit(gistId, update);
    }

    public async Task<Dictionary<string, string>> GetProjectFiles(string gistId)
    {
        var gist = await _client.Gist.Get(gistId);
        return gist.Files.ToDictionary(f => f.Key, f => f.Value.Content);
    }
}
