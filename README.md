# ContextSync CLI

Sync AI context documents across repositories using GitHub Gists as storage.

## Installation

### Prerequisites
- .NET 10 SDK
- GitHub Personal Access Token with `gist` scope

### Install as Global Tool

```bash
git clone https://github.com/yourusername/context-sync-cli.git
cd context-sync-cli
dotnet pack
dotnet tool install --global --add-source ./bin/Release ContextSync
```

### Uninstall

```bash
dotnet tool uninstall --global ContextSync
```

## Setup

Generate a GitHub PAT at https://github.com/settings/tokens with the `gist` scope, then:

```bash
ctx init <your-github-token>
```

This creates `~/.contextsync/config.json` and a private manifest Gist.

## Usage

### Push Context

In any git repository with a `.ai-context/` folder:

```bash
ctx push
```

Uploads all `.md` and `.txt` files to a private Gist linked to this repo.

### Pull Context

```bash
ctx pull
```

Downloads the context files from the linked Gist to `.ai-context/`.

### Check Status

```bash
ctx status
```

Shows the repository URL, project key, and linked Gist URL.

## How It Works

1. **Project Identity**: Uses SHA256 hash of git remote URL as unique project key
2. **Manifest**: A private Gist maps project keys to their context Gists
3. **Sync**: Each project gets its own private Gist containing `.ai-context/` files

```
~/.contextsync/config.json     # Stores GitHub token + manifest Gist ID
Manifest Gist                   # Maps: project-hash -> gist-id
Project Gists                   # Contains .ai-context/ files per repo
```

## Multi-Repo Example

```bash
# Frontend repo
cd ~/projects/my-app-frontend
mkdir .ai-context
echo "# Frontend Context" > .ai-context/overview.md
ctx push

# Backend repo
cd ~/projects/my-app-backend
mkdir .ai-context
echo "# Backend Context" > .ai-context/overview.md
ctx push

# On another machine
cd ~/projects/my-app-frontend
ctx pull  # Gets frontend context
```

## Recommended

Add `.ai-context/` to your global gitignore:

```bash
echo ".ai-context/" >> ~/.gitignore_global
git config --global core.excludesfile ~/.gitignore_global
```
