using System.CommandLine;
using ContextSync.Services;

const string ContextFolder = ".ai-context";
const string PathSeparatorEncoding = "__";

// Gists don't support subdirectories, so we encode paths
static string EncodePath(string path) => path.Replace("/", PathSeparatorEncoding);
static string DecodePath(string encoded) => encoded.Replace(PathSeparatorEncoding, "/");

var configService = new ConfigService();
var gitService = new GitService();

// Root command
var rootCommand = new RootCommand("ContextSync - Sync AI context documents across repositories");

// Init command
var initCommand = new Command("init", "Initialize ContextSync with your GitHub token");
var tokenArg = new Argument<string>("token", "GitHub Personal Access Token (with gist scope)");
initCommand.AddArgument(tokenArg);

initCommand.SetHandler(async (token) =>
{
    var config = configService.Load();
    config.GitHubToken = token;
    configService.Save(config);

    // Initialize manifest
    var gistService = new GistService(token, configService);
    await gistService.GetOrCreateManifest();

    Console.WriteLine("Initialized. Config saved to ~/.contextsync/config.json");
}, tokenArg);

// Push command
var pushCommand = new Command("push", "Push local .ai-context/ to GitHub Gist");
pushCommand.SetHandler(async () =>
{
    if (!configService.IsInitialized())
    {
        Console.WriteLine("Error: Run 'ctx init <token>' first");
        return;
    }

    var remoteUrl = gitService.GetRemoteUrl(Directory.GetCurrentDirectory());
    if (remoteUrl == null)
    {
        Console.WriteLine("Error: Not a git repository or no origin remote");
        return;
    }

    var contextPath = Path.Combine(Directory.GetCurrentDirectory(), ContextFolder);
    if (!Directory.Exists(contextPath))
    {
        Console.WriteLine($"Error: No {ContextFolder}/ folder found");
        return;
    }

    var files = Directory.GetFiles(contextPath, "*.*", SearchOption.AllDirectories)
        .Where(f => f.EndsWith(".md") || f.EndsWith(".txt"))
        .ToDictionary(
            f => EncodePath(Path.GetRelativePath(contextPath, f).Replace(Path.DirectorySeparatorChar, '/')),
            f => File.ReadAllText(f));

    if (files.Count == 0)
    {
        Console.WriteLine("No .md or .txt files found in .ai-context/");
        return;
    }

    var config = configService.Load();
    var gistService = new GistService(config.GitHubToken!, configService);
    var projectKey = gitService.HashUrl(remoteUrl);

    var manifest = await gistService.GetOrCreateManifest();

    if (manifest.ProjectMappings.TryGetValue(projectKey, out var gistId))
    {
        await gistService.UpdateProjectGist(gistId, files);
        Console.WriteLine($"Updated gist {gistId} with {files.Count} file(s)");
    }
    else
    {
        var newGistId = await gistService.CreateProjectGist(files);
        manifest.ProjectMappings[projectKey] = newGistId;
        await gistService.SaveManifest(manifest);
        Console.WriteLine($"Created gist {newGistId} with {files.Count} file(s)");
    }
});

// Pull command
var pullCommand = new Command("pull", "Pull .ai-context/ from GitHub Gist");
pullCommand.SetHandler(async () =>
{
    if (!configService.IsInitialized())
    {
        Console.WriteLine("Error: Run 'ctx init <token>' first");
        return;
    }

    var remoteUrl = gitService.GetRemoteUrl(Directory.GetCurrentDirectory());
    if (remoteUrl == null)
    {
        Console.WriteLine("Error: Not a git repository or no origin remote");
        return;
    }

    var config = configService.Load();
    var gistService = new GistService(config.GitHubToken!, configService);
    var projectKey = gitService.HashUrl(remoteUrl);

    var manifest = await gistService.GetOrCreateManifest();

    if (!manifest.ProjectMappings.TryGetValue(projectKey, out var gistId))
    {
        Console.WriteLine("No context found for this repository. Run 'ctx push' first.");
        return;
    }

    var files = await gistService.GetProjectFiles(gistId);
    var contextPath = Path.Combine(Directory.GetCurrentDirectory(), ContextFolder);

    // Clean existing folder
    if (Directory.Exists(contextPath))
        Directory.Delete(contextPath, true);

    Directory.CreateDirectory(contextPath);

    foreach (var (name, content) in files)
    {
        var filePath = Path.Combine(contextPath, DecodePath(name));
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, content);
    }

    Console.WriteLine($"Pulled {files.Count} file(s) to {ContextFolder}/");
});

// Status command
var statusCommand = new Command("status", "Show current repository sync status");
statusCommand.SetHandler(async () =>
{
    if (!configService.IsInitialized())
    {
        Console.WriteLine("Not initialized. Run 'ctx init <token>'");
        return;
    }

    var remoteUrl = gitService.GetRemoteUrl(Directory.GetCurrentDirectory());
    if (remoteUrl == null)
    {
        Console.WriteLine("Not a git repository");
        return;
    }

    var config = configService.Load();
    var projectKey = gitService.HashUrl(remoteUrl);
    var gistService = new GistService(config.GitHubToken!, configService);
    var manifest = await gistService.GetOrCreateManifest();

    Console.WriteLine($"Repository: {remoteUrl}");
    Console.WriteLine($"Project Key: {projectKey}");

    if (manifest.ProjectMappings.TryGetValue(projectKey, out var gistId))
        Console.WriteLine($"Linked Gist: https://gist.github.com/{gistId}");
    else
        Console.WriteLine("Status: Not synced");
});

rootCommand.AddCommand(initCommand);
rootCommand.AddCommand(pushCommand);
rootCommand.AddCommand(pullCommand);
rootCommand.AddCommand(statusCommand);

return await rootCommand.InvokeAsync(args);
