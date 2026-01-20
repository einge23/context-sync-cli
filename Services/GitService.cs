using System.Security.Cryptography;
using System.Text;
using LibGit2Sharp;

namespace ContextSync.Services;

public class GitService
{
    public string? GetRemoteUrl(string path)
    {
        try
        {
            var repoPath = Repository.Discover(path);
            if (repoPath == null) return null;

            using var repo = new Repository(repoPath);
            var remote = repo.Network.Remotes["origin"];
            return remote?.Url;
        }
        catch
        {
            return null;
        }
    }

    public string HashUrl(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
